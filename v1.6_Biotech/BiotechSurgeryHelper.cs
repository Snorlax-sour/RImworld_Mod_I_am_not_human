using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    public static class BiotechSurgeryHelper
    {
        public static bool TryRobotSurgery(Pawn doctor, Pawn patient, bool isPatientPseudo)
        {
            if (ModsConfig.BiotechActive && !doctor.RaceProps.Humanlike)
            {
                string resultText = isPatientPseudo 
                    ? "【警告】偵測到微量的未知細胞活躍，但無法確認其威脅性。" 
                    : "【確認】生命徵象與人類相符，無異常。";
                
                // 因為機器人看不出來，所以就算病人是偽人，也只會給普通的藍色信件
                Find.LetterStack.ReceiveLetter(
                    "機僕外科掃描", 
                    $"{doctor.NameShortColored} 對 {patient.NameShortColored} 完成了外科掃描。\n\n系統回報：\n{resultText}\n\n(然而，機器的掃描完美地被偽人的偽裝細胞給欺騙了。你需要具有直覺的「人類醫生」來親自操刀才能看出端倪。)", 
                    LetterDefOf.NeutralEvent, 
                    patient
                );
                
                return true; 
            }
            return false; 
        }
    }
}