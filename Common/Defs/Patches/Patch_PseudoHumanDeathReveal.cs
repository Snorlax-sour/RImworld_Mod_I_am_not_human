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

       public static void Postfix(Pawn __instance, ref bool __state)
        {
            if (__state && __instance.Corpse != null && __instance.Corpse.Map != null)
            {
                Map map = __instance.Corpse.Map;
                IntVec3 imposterPos = __instance.Corpse.Position; 

                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = __instance.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                
                // 完美演到死：如果還沒現形，就當個普通屍體！
                if (mark != null && !mark.isRevealed)
                {
                    return; 
                }

                // ==========================================
                // 💥 已經現形被擊殺的結算魔術
                // ==========================================
                bool hasHost = mark != null && mark.hasHostInVent;
                
                // 🔥 新增判定：死掉的這個人是不是我們殖民地的？
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
                        __instance.DeSpawn(); 
                        GenSpawn.Spawn(__instance, dropPos, map); 
                    }

                    if (mark != null) __instance.health.RemoveHediff(mark);
                    FleckMaker.ThrowDustPuff(dropPos, map, 2f);
                    
                    __instance.health.AddHediff(HediffDefOf.Malnutrition);
                    Hediff malnutrition = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                    if (malnutrition != null) malnutrition.Severity = 0.6f; 
                    __instance.health.AddHediff(HediffDefOf.Anesthetic); 

                    ThoughtDef traumaThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ReplacedByPseudoHuman");
                    if (traumaThought != null) __instance.needs?.mood?.thoughts.memories.TryGainMemory(traumaThought);

                    // 因為有真人掉下來 (通常都是我們的人)，所以這個一定要發信件！
                    Find.LetterStack.ReceiveLetter(
                        "隱藏的真相！",
                        $"隨著偽裝被擊破，這具假皮囊在原地迅速溶解成了一灘綠色黏液。\n\n與此同時，遠處天花板的通風管傳來一陣巨響，【{__instance.NameShortColored}】滿身是傷地從通風口掉了下來！\n\n原來真正的 {__instance.NameShortColored} 一直被藏在通風管裡，而剛剛被我們殺死的竟然是個替身！",
                        LetterDefOf.PositiveEvent,
                        __instance 
                    );
                }
                else
                {
                    // 結局 B：原生偽人死亡 (讀取玩家的設定)
                     // 🔥 1. 先呼叫實體魔術，並記錄「有沒有成功生出怪物？」
                    bool spawnedEntity = AnomalyEntitySpawner.TrySpawnEntity(imposterPos, map);
                    if (PseudoHumanCore.settings.meltIntoSlime)
                    {
                        FilthMaker.TryMakeFilth(imposterPos, map, ThingDefOf.Filth_Slime, 6);
                        __instance.Corpse.Destroy(DestroyMode.Vanish);
                        
                        // 🔥 根據派系決定要不要發信件
                        // // 🔥 如果「沒有」生出怪物，才發送普通的爛泥信件！
                        if (!spawnedEntity){
                            if (isOurColonist)
                            {
                                Find.LetterStack.ReceiveLetter("化為爛泥", $"這具皮囊在死後迅速溶解，化為了一灘發出惡臭的綠色黏液。\n\n牠並不是任何人的替身，牠打從一開始就是一頭徹頭徹尾的怪物...", LetterDefOf.NeutralEvent, new TargetInfo(imposterPos, map));
                            }
                            else
                            {
                                // 敵人死掉只在左上角跳一行字
                                Messages.Message("一頭敵方偽人在死後化為了一灘爛泥。", new TargetInfo(imposterPos, map), MessageTypeDefOf.NeutralEvent, false);
                            }
                        }
                    }
                    else
                    {
                        // 🔥 同理，如果「沒有」生出怪物，才發送普通的留屍體信件！
                        if (!spawnedEntity)
                        {
                            // 玩家關閉了化為爛泥 (保留屍體)
                            if (isOurColonist)
                            {
                                Find.LetterStack.ReceiveLetter("偽人死亡", $"隨著致命一擊，這頭怪物終於倒下。\n\n這具屍體的內部充滿了詭異的黑色組織。牠並不是任何人的替身，打從一開始就是個怪物...\n\n現在，這具駭人的屍體任由你們處置了。", LetterDefOf.NeutralEvent, new TargetInfo(imposterPos, map));
                            }
                            else
                            {
                                // 敵人死掉只在左上角跳一行字
                                Messages.Message("一頭敵方偽人被擊殺，留下了一具充滿異星組織的屍體。", new TargetInfo(imposterPos, map), MessageTypeDefOf.NeutralEvent, false);
                            }
                        }
                    }

                     // 🔥 呼叫我們的實體變異魔術！
                    AnomalyEntitySpawner.TrySpawnEntity(imposterPos, map);
                }
            }
        }


    }
}