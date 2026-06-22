using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    public static class BiotechAutopsyHelper
    {
        // 🔥 注意：多了一個 out string recordResult，用來傳遞報告結論給主程式
        public static bool TryRobotAutopsy(Pawn doctor, Pawn subject, bool isCorpsePseudo, string pastRecords, out string recordResult)
        {
            recordResult = ""; // 預設空值

            if (ModsConfig.BiotechActive && !doctor.RaceProps.Humanlike)
            {
                bool isReportHacked = false;
                string hackingHint = "";

                // ==========================================
                // 🧠 核心：根據玩家設定決定報告是否造假
                // ==========================================
                if (PseudoHumanCore.settings.robotMode == RobotInspectMode.Fooled)
                {
                    isReportHacked = true; // 模式 2：機器人一定被騙
                    hackingHint = "\n\n(機器的死板掃描完美地被偽人的偽裝細胞給欺騙了。)";
                }
                else if (PseudoHumanCore.settings.robotMode == RobotInspectMode.MechanitorHacked)
                {
                    // 模式 3：找尋這隻機器人的「監督者(機械師)」
                    Pawn overseer = doctor.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Overseer);
                    if (overseer != null)
                    {
                        HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                        // 如果機械師自己就是偽人，他會竄改機器人的報告！
                        if (overseer.health.hediffSet.HasHediff(pseudoMarkDef))
                        {
                            isReportHacked = true;
                            hackingHint = "\n\n(系統警告：這份報告的封包似乎在傳輸到終端的過程中，被具有最高權限的網路管理者攔截並修改過了...)";
                        }
                    }
                }

                // 決定最終機器人說出的結論
                // 只有當「屍體真的是偽人」而且「報告沒有被造假/騙過」時，才會顯示發現偽人
                bool finalResultPseudo = isCorpsePseudo && !isReportHacked;

                string resultText = finalResultPseudo 
                    ? "【警告】偵測到未知的偽人異變細胞！死者並非純粹人類！" 
                    : "【確認】生理特徵 100% 符合人類標準。無異常。";
                
                LetterDef letterType = finalResultPseudo ? LetterDefOf.ThreatSmall : LetterDefOf.PositiveEvent;

                // 發送給玩家的信件
                Find.LetterStack.ReceiveLetter(
                    "機僕屍檢掃描", 
                    $"{doctor.NameShortColored} 啟動了內建的掃描儀，對屍體進行分析。\n\n掃描結果：\n{resultText}{hackingHint}{pastRecords}", 
                    letterType, 
                    doctor
                );
                
                // 準備寫入屍體紀錄本的字串
                recordResult = finalResultPseudo ? "發現偽人組織 (機器掃描)" : "正常人類 (機器掃描)";
                
                return true; 
            }
            return false; 
        }
    }
}