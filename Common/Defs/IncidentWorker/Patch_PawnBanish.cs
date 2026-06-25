using HarmonyLib;
using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    // 🔥 修正：攔截 UI 真正呼叫的「2個參數」版本！保證 100% 抓得到！
    [HarmonyPatch(typeof(PawnBanishUtility), "Banish", new System.Type[] { typeof(Pawn), typeof(bool) })]
    public static class Patch_PawnBanish
    {
        public static void Prefix(Pawn pawn)
        {
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                Hediff_PseudoHuman mark = pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

                if (mark != null)
                {
                    // 🔥 劇情邏輯：只有「潛伏中」被放逐才算濫殺無辜！
                    // 如果他已經暴動現形了(isRevealed)，放逐他叫清理垃圾，不增加計數器！
                    if (!mark.isRevealed)
                    {
                        GameComponent_PseudoHumanTracker.CheckFourthWallBreak(pawn, true);
                    }
                }
            }
        }
    }
}