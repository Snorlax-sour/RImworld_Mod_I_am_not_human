using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace PseudoHumanMod
{
    // ==========================================
    // 防線 1：「心靈衝擊槍」 
    // ==========================================
    [HarmonyPatch(typeof(CompTargetEffect_PsychicShock), "DoEffectOn")]
    public static class Patch_PsychicShock
    {
        public static bool Prefix(Pawn user, Thing target)
        {
            if (target is Pawn pawn)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

                if (mark != null)
                {
                    // 🔥 核心邏輯：如果他還沒撕破臉，且開啟了「完美演員」，他就放行(回傳 true)，讓自己被電暈！
                    if (!mark.isRevealed && PseudoHumanCore.settings.perfectActor)
                    {
                        return true; 
                    }

                    // 否則 (已經現形，或玩家關閉演員模式)，則免疫並跳出警告
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "心靈干擾無效", Color.gray);
                    Messages.Message("強大的心靈衝擊如同泥牛入海，對這個「怪物」毫無作用...", pawn, MessageTypeDefOf.RejectInput, false);
                    return false; 
                }
            }
            return true;
        }
    }

    // ==========================================
    // 防線 2：「心靈瘋狂槍」 
    // ==========================================
    [HarmonyPatch(typeof(CompTargetEffect_Berserk), "DoEffectOn")]
    public static class Patch_PsychicBerserk
    {
        public static bool Prefix(Pawn user, Thing target)
        {
            if (target is Pawn pawn)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

                if (mark != null)
                {
                    if (!mark.isRevealed && PseudoHumanCore.settings.perfectActor)
                    {
                        return true; // 假裝發瘋
                    }

                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "心靈干擾無效", Color.gray);
                    Messages.Message("心靈瘋狂的波動被某種空洞的東西吞噬了...", pawn, MessageTypeDefOf.RejectInput, false);
                    return false; 
                }
            }
            return true;
        }
    }

    
}