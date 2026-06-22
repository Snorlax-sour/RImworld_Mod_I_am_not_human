using RimWorld;
using Verse;
using System.Collections.Generic;

namespace PseudoHumanMod
{
    // 活體手術繼承的是 Recipe_Surgery
    public class Recipe_InspectPseudoHuman : Recipe_Surgery
    {
        // 當手術讀條結束時執行
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && pawn != null)
            {
                // 扣除醫藥
                for (int i = 0; i < ingredients.Count; i++)
                {
                    ingredients[i].Destroy(DestroyMode.Vanish);
                }

                // 判斷手術是否成功 (如果醫生醫術太爛導致手術失敗，就直接結束)
                if (!CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    // 手術成功，開始進行身分判定！
                    Pawn patient = pawn;
                    Pawn doctor = billDoer;

                    HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                    if (pseudoMarkDef == null) return;

                    bool isPatientPseudo = patient.health.hediffSet.HasHediff(pseudoMarkDef);

                    // ==========================================
                    // 1. 呼叫生技機器人輔助邏輯
                    // ==========================================
                    if (BiotechSurgeryHelper.TryRobotSurgery(doctor, patient, isPatientPseudo))
                    {
                        return; // 機器人處理完就結束
                    }

                    // ==========================================
                    // 2. 人類醫生的恐怖邏輯
                    // ==========================================
                    bool isDoctorPseudo = doctor.health.hediffSet.HasHediff(pseudoMarkDef);

                    if (isDoctorPseudo)
                    {
                        if (isPatientPseudo)
                        {
                            // 情況 A：醫生是偽人，病人也是偽人 (包庇)
                            Find.LetterStack.ReceiveLetter("外科檢查：健康", $"{doctor.NameShortColored} 縫合了傷口，並出具了報告：「{patient.NameShortColored} 非常健康，是純正的人類。」\n\n(但你真的能信任這份報告嗎？)", LetterDefOf.PositiveEvent, patient);
                        }
                        else
                        {
                            // 💀 情況 B：醫生是偽人，病人是正常人 (惡意感染！)
                            // 醫生趁病人被麻醉時，偷偷把偽人細胞植入病聯體內！
                            patient.health.AddHediff(pseudoMarkDef);
                             // ==========================================
                            // 🔥 標記這個病人「也是替身」(真人被醫生塞進通風管了)
                            // ==========================================
                            Hediff_PseudoHuman newMark = patient.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                            if (newMark != null)
                            {
                                newMark.hasHostInVent = true; 
                            }

                            Find.LetterStack.ReceiveLetter("外科檢查：健康", $"{doctor.NameShortColored} 縫合了傷口，並出具了報告：「{patient.NameShortColored} 非常健康，是純正的人類。」", LetterDefOf.NeutralEvent, patient);
                        }
                    }
                    else
                    {
                        // 醫生是正常人類
                        if (isPatientPseudo)
                        {
                            // 🩸 情況 C：正常醫生發現了偽人 (直接在手術台上發瘋！)
                            Find.LetterStack.ReceiveLetter(
                                "外科檢查：偽人！", 
                                $"{doctor.NameShortColored} 切開了 {patient.NameShortColored} 的胸腔，赫然發現裡面蠕動的黑色組織！\n\n偽人意識到自己暴露了，它不顧麻醉藥效，直接扯斷了縫線，從手術台上暴起！", 
                                LetterDefOf.ThreatBig, 
                                new LookTargets(patient, doctor)
                            );

                            // 呼叫我們之前寫的「現形邏輯」，讓他解除麻醉並發瘋！
                            Hediff_PseudoHuman mark = patient.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                            if (mark != null)
                            {
                                // 解除麻醉 (移除 Anesthetic 狀態)
                                Hediff anesthetic = patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic);
                                if (anesthetic != null) patient.health.RemoveHediff(anesthetic);

                                // 直接呼叫受傷檔案裡的現形方法！
                                Patch_PseudoHumanAttacked.RevealPseudoHuman(patient, mark);
                            }
                        }
                        else
                        {
                            // 情況 D：正常醫生確認病人是正常人
                            Find.LetterStack.ReceiveLetter("外科檢查：健康", $"{doctor.NameShortColored} 完成了深度切片檢查，確認 {patient.NameShortColored} 的組織一切正常，沒有被偽人替換的痕跡。", LetterDefOf.PositiveEvent, patient);
                        }
                    }
                }
            }
        }
    }
}