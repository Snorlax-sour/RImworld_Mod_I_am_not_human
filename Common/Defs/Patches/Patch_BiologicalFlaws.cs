using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace PseudoHumanMod
{
    // 🔥 攔截所有健康狀態的源頭
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Patch_BiologicalFlaws
    {
        // 🔥 核心修正：加入 Pawn ___pawn (三個底線)，讓 Harmony 幫我們偷出私有變數！
        public static bool Prefix(Pawn_HealthTracker __instance, Hediff hediff, Pawn ___pawn)
        {
            // 直接使用 Harmony 挖出來的 ___pawn
            Pawn pawn = ___pawn;
            
            if (pawn == null || hediff == null || hediff.def == null) return true;

            HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
            if (pseudoMarkDef == null) return true;

            Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
            
            // 只要是偽人，不管有沒有現形，其異星細胞絕對免疫這些人類的疾病！
            if (mark != null)
            {
                string defName = hediff.def.defName;
                
                // 免疫：食物中毒、毒素累積、以及生技 DLC 的戒斷症狀(Withdrawal)與缺乏症(Deficiency)
                if (defName == "FoodPoisoning" || defName == "ToxicBuildup" || 
                    defName.Contains("Withdrawal") || defName.Contains("Deficiency"))
                {
                    // 🔥 直接回傳 false！強制阻擋這個狀態！
                    return false; 
                }
            }
            return true;
        }
    }
}