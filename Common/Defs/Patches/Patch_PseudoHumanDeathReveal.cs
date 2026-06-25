using HarmonyLib;
using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_PseudoHumanDeathReveal
    {
        public static void Prefix(Pawn __instance, ref bool __state)
        {
            __state = false;
            if (__instance != null && __instance.RaceProps.Humanlike)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                if (pseudoMarkDef != null && __instance.health.hediffSet.HasHediff(pseudoMarkDef))
                {
                    __state = true; 
                }
            }
        }

        // 🔥 修正：加上了 DamageInfo? dinfo 參數，用來追查死因！
        public static void Postfix(Pawn __instance, DamageInfo? dinfo, ref bool __state)
        {
            if (__state && __instance.Corpse != null && __instance.Corpse.Map != null)
            {
                Map map = __instance.Corpse.Map;
                IntVec3 imposterPos = __instance.Corpse.Position; 

                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = __instance.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                
                // ==========================================
                // 🎭 完美演到死：如果還沒現形，就當個普通屍體！
                // ==========================================
                if (mark != null && !mark.isRevealed)
                {
                    // 🔥 核心修正：只攔截「真正的處決與安樂死」！
                    // 排除友軍傷害、社交打架、或被玩家開槍打死
                    if (dinfo.HasValue && dinfo.Value.Instigator != null && dinfo.Value.Instigator.Faction == Faction.OfPlayer)
                    {
                        // ExecutionCut 是監獄裡的「處決」指令
                        // SurgicalCut 是病床上的「安樂死」或是「手術致死」
                        if (dinfo.Value.Def == DamageDefOf.ExecutionCut || dinfo.Value.Def == DamageDefOf.SurgicalCut)
                        {
                            // 只有動用這種極刑，怪物才會記恨你！
                            GameComponent_PseudoHumanTracker.CheckFourthWallBreak(__instance);
                        }
                    }
                    return; 
                }

                // ==========================================
                // 💥 已經現形被擊殺的結算魔術
                // ==========================================
                bool hasHost = mark != null && mark.hasHostInVent;
                bool isOurColonist = __instance.Faction == Faction.OfPlayer;

                if (hasHost)
                {
                    // 結局 A：替身死亡，化為爛泥，真人掉落！
                    FilthMaker.TryMakeFilth(imposterPos, map, ThingDefOf.Filth_Slime, 6);
                    
                    IntVec3 dropPos = IntVec3.Invalid;
                    ThingDef ventDef = DefDatabase<ThingDef>.GetNamedSilentFail("Vent");
                    if (ventDef != null)
                    {
                        var vents = map.listerThings.ThingsOfDef(ventDef);
                        if (vents.Count > 0) CellFinder.TryRandomClosewalkCellNear(vents.RandomElement().Position, map, 2, out dropPos, null);
                    }
                    if (!dropPos.IsValid) CellFinder.TryFindRandomCell(map, c => map.areaManager.Home[c] && c.Standable(map) && c.Roofed(map), out dropPos);
                    if (!dropPos.IsValid) dropPos = imposterPos;

                    ResurrectionUtility.TryResurrect(__instance);
                    
                    if (dropPos != imposterPos)
                    {
                        __instance.Position = dropPos; 
                        __instance.Notify_Teleported(true, true); 
                    }

                    if (mark != null) __instance.health.RemoveHediff(mark);
                    FleckMaker.ThrowDustPuff(dropPos, map, 2f);
                    
                    __instance.health.AddHediff(HediffDefOf.Malnutrition);
                    Hediff malnutrition = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                    if (malnutrition != null) malnutrition.Severity = 0.6f; 
                    __instance.health.AddHediff(HediffDefOf.Anesthetic); 

                    ThoughtDef traumaThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ReplacedByPseudoHuman");
                    if (traumaThought != null) __instance.needs?.mood?.thoughts.memories.TryGainMemory(traumaThought);

                    Find.LetterStack.ReceiveLetter(
                        "隱藏的真相！",
                        $"隨著偽裝被擊破，這具假皮囊在原地迅速溶解成了一灘綠色黏液。\n\n與此同時，遠處天花板的通風管傳來一陣巨響，【{__instance.NameShortColored}】滿身是傷地從通風口掉了下來！\n\n原來真正的 {__instance.NameShortColored} 一直被藏在通風管裡，而剛剛被我們殺死的竟然是個替身！",
                        LetterDefOf.PositiveEvent,
                        __instance 
                    );
                }
                else
                {
                    // ==========================================
                    // 結局 B：原生偽人死亡 (延遲魔術秀！)
                    // ==========================================
                    Corpse theCorpse = __instance.Corpse; 
                    
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        if (theCorpse != null && !theCorpse.Destroyed && map != null)
                        {
                            bool spawnedEntity = AnomalyEntitySpawner.TrySpawnEntity(imposterPos, map);

                            if (PseudoHumanCore.settings.meltIntoSlime)
                            {
                                FilthMaker.TryMakeFilth(imposterPos, map, ThingDefOf.Filth_Slime, 6);
                                theCorpse.Destroy(DestroyMode.Vanish); 
                                
                                if (!spawnedEntity)
                                {
                                    if (isOurColonist) Find.LetterStack.ReceiveLetter("化為爛泥", $"這具皮囊在死後迅速溶解，化為了一灘發出惡臭的綠色黏液。\n\n牠並不是任何人的替身，牠打從一開始就是一頭徹頭徹尾的怪物...", LetterDefOf.NeutralEvent, new TargetInfo(imposterPos, map));
                                    else Messages.Message("一頭敵方偽人在死後化為了一灘爛泥。", new TargetInfo(imposterPos, map), MessageTypeDefOf.NeutralEvent, false);
                                }
                            }
                            else
                            {
                                if (!spawnedEntity)
                                {
                                    if (isOurColonist) Find.LetterStack.ReceiveLetter("偽人死亡", $"這具屍體的內部充滿了詭異的黑色組織。牠並不是任何人的替身，打從一開始就是個怪物...\n\n現在，這具駭人的屍體任由你們處置了。", LetterDefOf.NeutralEvent, new TargetInfo(imposterPos, map));
                                    else Messages.Message("一頭敵方偽人被擊殺，留下了一具充滿異星組織的屍體。", new TargetInfo(imposterPos, map), MessageTypeDefOf.NeutralEvent, false);
                                }
                            }
                        }
                    });
                }
            }
        }
    }
}