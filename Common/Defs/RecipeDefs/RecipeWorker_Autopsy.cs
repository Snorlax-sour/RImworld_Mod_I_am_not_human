using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    public static class Patch_Autopsy
    {
        public static void Prefix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients)
        {
            if (recipeDef.defName == "AutopsyPseudoHuman")
            {
                Corpse corpse = ingredients.OfType<Corpse>().FirstOrDefault();

                if (corpse != null && corpse.InnerPawn != null)
                {
                    // ==========================================
                    // 1. 偷天換日：把屍體從銷毀清單中移除，並丟回地板！
                    // ==========================================
                    ingredients.Remove(corpse); 
                    if (!corpse.Spawned && worker.Map != null)
                    {
                        GenPlace.TryPlaceThing(corpse, worker.Position, worker.Map, ThingPlaceMode.Near);
                    }

                    Pawn subject = corpse.InnerPawn; 
                    Pawn doctor = worker;            
                    
                    HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                    HediffDef autopsyMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("AutopsyRecordMark");
                    if (pseudoMarkDef == null || autopsyMarkDef == null) return;

                    bool isCorpsePseudo = subject.health.hediffSet.HasHediff(pseudoMarkDef);
                    bool isDoctorPseudo = doctor.health.hediffSet.HasHediff(pseudoMarkDef);

                    // ==========================================
                    // 2. 讀取「過去的病歷紀錄」
                    // ==========================================
                    string pastRecords = "";
                    var oldRecords = subject.health.hediffSet.hediffs.OfType<Hediff_AutopsyRecord>().ToList();
                    
                    if (oldRecords.Any(r => r.doctorName == doctor.NameShortColored.Resolve()))
                    {
                        Messages.Message($"{doctor.NameShortColored} 已經檢查過這具屍體了，沒有必要再切開一次。", MessageTypeDefOf.RejectInput, false);
                        return;
                    }

                    if (oldRecords.Count > 0)
                    {
                        pastRecords = "\n\n【屍體上殘留的其他醫生縫合紀錄】：\n";
                        foreach (var r in oldRecords)
                        {
                            pastRecords += $"- {r.doctorName}：{r.reportResult}\n";
                        }
                    }

                    // 準備寫入這次的新紀錄
                    Hediff_AutopsyRecord newRecord = (Hediff_AutopsyRecord)HediffMaker.MakeHediff(autopsyMarkDef, subject);
                    newRecord.doctorName = doctor.NameShortColored.Resolve();

                    // ==========================================
                    // 🔥 3. 機器人判斷 (呼叫生技輔助程式)
                    // ==========================================
                    // ==========================================
                    // 3. 機器人判斷 (呼叫生技輔助程式)
                    // ==========================================
                    string robotResult = "";
                    // 🔥 加入 out robotResult
                    if (BiotechAutopsyHelper.TryRobotAutopsy(doctor, subject, isCorpsePseudo, pastRecords, out robotResult))
                    {
                        // 把剛剛機器人決定的結果，寫入病歷本並放回屍體身上
                        newRecord.reportResult = robotResult;
                        subject.health.AddHediff(newRecord);
                        return; // 結束執行
                    }

                    // ==========================================
                    // 4. 人類醫生猜忌邏輯
                    // ==========================================
                    string letterTitle = "";
                    string letterText = "";
                    LetterDef letterDef = LetterDefOf.NeutralEvent;

                    if (isDoctorPseudo)
                    {
                        // 醫生是偽人：包庇
                        newRecord.reportResult = "正常人類";
                        letterTitle = "屍檢報告：正常";
                        letterText = $"{doctor.NameShortColored} 完成了屍檢。平靜地說：「內臟結構沒有任何異常，這是一具純粹的人類屍體。」\n\n(但你真的能完全信任這位醫生的診斷嗎？)" + pastRecords;
                    }
                    else
                    {
                        // 醫生是正常人
                        if (isCorpsePseudo)
                        {
                            newRecord.reportResult = "發現偽人組織！";
                            letterTitle = "屍檢報告：偽人！";
                            letterDef = LetterDefOf.ThreatSmall;
                            letterText = $"{doctor.NameShortColored} 切開了胸腔，眼前的一幕讓其毛骨悚然！心臟與肺部的結構完全被黑色組織取代！這根本不是人類！" + pastRecords;
                        }
                        else
                        {
                            newRecord.reportResult = "正常人類";
                            letterTitle = "屍檢報告：正常";
                            letterDef = LetterDefOf.PositiveEvent;
                            letterText = $"{doctor.NameShortColored} 完成了深度切片檢查，確認所有的器官特徵都符合正常人類的標準。死者生前確實是個普通人。" + pastRecords;
                        }
                    }

                    // 把新紀錄寫進屍體的肚子裡，並發送信件
                    subject.health.AddHediff(newRecord);
                    Find.LetterStack.ReceiveLetter(letterTitle, letterText, letterDef, doctor);
                }
            }
        }
    }
}