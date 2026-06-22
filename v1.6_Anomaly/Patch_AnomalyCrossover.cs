using HarmonyLib;
using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(MutantUtility), "SetPawnAsMutantInstantly")]
    public static class Patch_GhoulInfusionRejection
    {
        public static bool Prepare()
        {
            return ModsConfig.AnomalyActive;
        }

        public static bool Prefix(Pawn pawn, MutantDef mutant)
        {
            if (pawn != null && mutant != null)
            {
                if (mutant.defName == "Ghoul")
                {
                    HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                    if (pseudoMarkDef == null) return true;

                    Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

                    // 🔥 修正：只要是偽人，不管有沒有現形，都進來這裡！
                    if (mark != null)
                    {
                        if (!mark.isRevealed)
                        {
                            // ==========================================
                            // 情況 A：潛伏中的偽人被抓包
                            // ==========================================
                            Find.LetterStack.ReceiveLetter(
                                "食屍鬼轉化排斥！",
                                $"當食屍鬼血清注入 {pawn.NameShortColored} 體內的瞬間，發生了極度劇烈的排斥反應！\n\n這些異星細胞拒絕被同化，牠們為了自保，直接撕裂了皮囊！這傢伙根本不是人類！",
                                LetterDefOf.ThreatBig,
                                pawn
                            );

                            Hediff anesthetic = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic);
                            if (anesthetic != null) pawn.health.RemoveHediff(anesthetic);

                            Patch_PseudoHumanAttacked.RevealPseudoHuman(pawn, mark);
                        }
                        else
                        {
                            // ==========================================
                            // 情況 B：已經現形的怪物被強行打血清
                            // ==========================================
                            // 只在左上角跳提示，告訴玩家這是在浪費資源
                            Messages.Message($"食屍鬼血清剛注入 {pawn.NameShortColored} 的體內，就被牠強悍的異星細胞瞬間吞噬分解了...轉化無效！", pawn, MessageTypeDefOf.RejectInput, false);
                        }

                        // 🔥 終極封殺：無論是 A 還是 B，絕對不准變成食屍鬼！
                        return false; 
                    }
                }
            }
            return true;
        }
    }
}