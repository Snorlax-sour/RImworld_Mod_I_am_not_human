using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    public class Hediff_AutopsyRecord : HediffWithComps
    {
        public string doctorName = "未知醫生";
        public string reportResult = "未知結果";

        // 🔥 這個會讓健康面板上顯示：屍檢縫合痕跡 (Bob的報告: 正常)
        public override string LabelInBrackets => $"{doctorName}的報告: {reportResult}";

        // 必須加上這個，存檔時才不會忘記是誰解剖的
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref doctorName, "doctorName", "未知醫生");
            Scribe_Values.Look(ref reportResult, "reportResult", "未知結果");
        }
    }
}