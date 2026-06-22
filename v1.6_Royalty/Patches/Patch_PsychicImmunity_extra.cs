using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace PseudoHumanMod
{
    

    // ==========================================
    // 防線 3：「皇權/生技 靈能與特殊能力」
    // ==========================================
    [HarmonyPatch(typeof(RimWorld.Ability), "Activate", new System.Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) })]
    public static class Patch_PsycastImmunity
    {
        public static bool Prepare()
        {
            return ModsConfig.RoyaltyActive || ModsConfig.AnomalyActive || ModsConfig.BiotechActive;
        }

        public static bool Prefix(RimWorld.Ability __instance, LocalTargetInfo target)
        {
            if (target.Thing is Pawn pawn)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

                if (mark != null && __instance.def.category != null && __instance.def.category.defName == "Psychic")
                {
                    if (__instance.pawn == pawn) return true; // 自己對自己放靈能，放行

                    // 演戲邏輯
                    if (!mark.isRevealed && PseudoHumanCore.settings.perfectActor)
                    {
                        return true; // 假裝被靈能影響
                    }

                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "靈能無效", Color.gray);
                    Messages.Message("這具軀殼內部的心靈是空洞的，無法被靈能影響...", pawn, MessageTypeDefOf.RejectInput, false);
                    return false; 
                }
            }
            return true;
        }
    }
}