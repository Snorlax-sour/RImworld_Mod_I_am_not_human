using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Grammar; // 🔥 必須加上這個，這是生成語法的核心


namespace PseudoHumanMod
{
    
        // not_human.cs 裡面的 HarmonyInit 恢復成這樣：
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("com.yourname.pseudohuman");
            harmony.PatchAll(); 
        }
    }
    

    // 攔截社交互動
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractWith")]
    public static class Patch_TryInteractWith
    {
        // 🔥【修改點】：參數加入了 Pawn ___pawn (注意是三個底線)
        public static void Postfix(Pawn_InteractionsTracker __instance, Pawn ___pawn, Pawn recipient, InteractionDef intDef, bool __result)
        {
            // 現在 ___pawn 就是直接抓出來的發話者了！
            Pawn initiator = ___pawn;

            // 🔥 嚴格限制：發話者與聽話者「都必須是人類」(包含生技的異種人，但排除動物、機械、實體與外星種族)
            if (initiator.def != ThingDefOf.Human || recipient.def != ThingDefOf.Human)
                return;

            // 檢查發話者(initiator)是不是偽人
            bool isSpeakerPseudo = initiator.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("PseudoHumanMark"));
            // 檢查聽話者(recipient)是不是正常人
            bool isRecipientNormal = !recipient.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("PseudoHumanMark"));

            if (isSpeakerPseudo && isRecipientNormal)
            {
                if (Rand.Chance(PseudoHumanCore.settings.nonsenseChance))
                {
                    // ==========================================
                    // 1. 安全獲取 XML 定義 (找不到會回傳 null，不會直接報錯)
                    // ==========================================
                    ThoughtDef moodDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("HeardPseudoHumanNonsense");
                    ThoughtDef socialDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("HeardPseudoHuman_Social");
                    InteractionDef creepyInteraction = DefDatabase<InteractionDef>.GetNamedSilentFail("PseudoHuman_CreepyChatLog");

                    // ==========================================
                    // 2. 給予心情與社交扣分 (確定 XML 存在才給！)
                    // ==========================================
                    if (moodDef != null)
                    {
                        recipient.needs.mood?.thoughts.memories.TryGainMemory(moodDef);
                    }
                    else
                    {
                        Log.Warning("PseudoHuman: 找不到 ThoughtDef 'HeardPseudoHumanNonsense'");
                    }

                    if (socialDef != null)
                    {
                        recipient.needs.mood?.thoughts.memories.TryGainMemory(socialDef, initiator);
                    }
                    else
                    {
                        Log.Warning("PseudoHuman: 找不到 ThoughtDef 'HeardPseudoHuman_Social'");
                    }

                    // ==========================================
                    // 3. 寫入社交日誌 (確定 XML 存在才寫！)
                    // ==========================================
                    if (creepyInteraction != null)
                    {
                        Find.PlayLog.Add(new PlayLogEntry_Interaction(creepyInteraction, initiator, recipient, null));
                    }
                    else
                    {
                        Log.Warning("PseudoHuman: 找不到 InteractionDef 'PseudoHuman_CreepyChatLog'");
                    }

                    // ==========================================
                    // 4. 畫面上飄出紅色的簡單提示
                    // ==========================================
                    if (PseudoHumanCore.settings.showNonsenseText && initiator.Map != null)
                    {
                        MoteMaker.ThrowText(initiator.DrawPos, initiator.Map, "詭異的囈語...", Color.red);
                    }
                }

            }
        }
    }
    // 攔截遊戲的「小人生成系統」
    // 保留「怪異加入者 (CreepJoiner)」、排除食屍鬼與殭屍
     [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new System.Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_GeneratePawn
    {
         public static void Postfix(Pawn __result)
        {
            if (__result == null || __result.Dead) return;
            
            // 基礎防線：不是人類的不要 (排除動物、機械族)
            if (__result.def != ThingDefOf.Human) return;

            // ==========================================
            // 🔥 新增：異象 DLC 突變體防線！
            // ==========================================
            // 食屍鬼 (IsGhoul)、蹣跚者殭屍 (IsShambler)、以及任何突變體 (IsMutant)
            // 牠們雖然底層是人類，但肉體已經被異象力量改變，絕對不可能是偽人！
            if (__result.IsGhoul || __result.IsShambler || __result.IsMutant)
            {
                return; 
            }

            // 判斷是否要生成為偽人
            if (Rand.Chance(PseudoHumanCore.settings.generateChance))
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                if (pseudoMarkDef != null && !__result.health.hediffSet.HasHediff(pseudoMarkDef))
                {
                    // 1. 打上偽人標記
                    __result.health.AddHediff(pseudoMarkDef);

                    // 2. 偽人專屬福利：強制獲得「屍體癖好」
                    if (Rand.Chance(PseudoHumanCore.settings.pseudoHumanCorpseLoverChance))
                    {
                        TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
                        if (loverTrait != null && __result.story != null && __result.story.traits != null)
                        {
                            if (!__result.story.traits.HasTrait(loverTrait))
                            {
                                __result.story.traits.GainTrait(new Trait(loverTrait));
                            }
                        }
                    }
                }
            }
        }


    }

}
