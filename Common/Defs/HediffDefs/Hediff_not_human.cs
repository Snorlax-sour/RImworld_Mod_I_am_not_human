using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using Verse.AI; // 🔥 必須加這行！食屍鬼才能發動攻擊 (StartJob)
namespace PseudoHumanMod
{
    // ==========================================
    // 🌟 創造我們專屬的 XML 標籤容器！
    // ==========================================
    public class HediffStage_PseudoHuman : HediffStage
    {
        // 宣告一個變數，這個名稱就是你以後可以在 XML 裡直接打的標籤！
        // 預設為 0f (代表沒設定的話就不回血)
        public float regenPerSecond = 0f; 
    }

    // ==========================================
    // 偽人標記主程式
    // ==========================================
    public class Hediff_PseudoHuman : HediffWithComps
    {
        public bool isRevealed = false; 
        public override bool Visible => Prefs.DevMode || isRevealed;
          // 🔥 新增：記錄這個偽人是不是「半夜替換事件」產生的替身！
        public bool hasHostInVent = false; 
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isRevealed, "isRevealed", false);
               // 🔥 記得存檔
            Scribe_Values.Look(ref hasHostInVent, "hasHostInVent", false);
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            // ==========================================
            // 🔥 終極防線：血統審查
            // ==========================================
            // 如果被寄生的宿主不是「純人類/異種人」
            if (this.pawn.def != ThingDefOf.Human)
            {
                // 這個標記會感到非常嫌棄，然後「自動毀滅 (刪除自己)」！
                this.pawn.health.RemoveHediff(this);
                return; // 結束執行，不要往下跑了
            }

            // 原本的防呆：強制設定為全身
            if (this.Part != null) 
            {
                this.Part = null; 
            }
        }

        public override void Tick()
        {
            base.Tick();
            
            if (pawn.IsHashIntervalTick(60))
            {
                // ==========================================
                // 🔥 核心修正：實力隱藏機制
                // ==========================================
                if (isRevealed)
                {
                    // 【撕破臉狀態】：解放全部力量！
                    // 根據失去的血量計算嚴重度，可以進入階段 2、3、4
                    float lostHealth = 1.0f - pawn.health.summaryHealth.SummaryHealthPercent;
                    this.Severity = Mathf.Max(0.01f, lostHealth);

                    // 執行恐怖的肉體再生邏輯 (讀取 XML 設定)
                    HediffStage_PseudoHuman currentStage = this.CurStage as HediffStage_PseudoHuman;
                    if (currentStage != null && currentStage.regenPerSecond > 0f)
                    {
                        // 1. 治癒普通物理傷口 (槍傷、撕裂傷)
                        Hediff_Injury woundToHeal = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_Injury>()
                            .Where(i => i.CanHealNaturally())
                            .FirstOrDefault();

                        if (woundToHeal != null)
                        {
                            woundToHeal.Heal(currentStage.regenPerSecond);
                            
                            // (溫馨小提醒：你這裡用了 pseudoHumanOccupy 當作特效機率，雖然不影響運作，但特效機率通常寫死 0.3f 就好囉！)
                            if (Rand.Chance(PseudoHumanCore.settings.pseudoHumanOccupy))
                            {
                                FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
                            }
                        }

                        // ==========================================
                        // 🔥 2. 全新：斷肢重生系統 (Body Part Regeneration)
                        // ==========================================
                        // 為了平衡與視覺節奏，設定 5% 的機率 (大約每 20 秒長出一個器官)
                        if (Rand.Chance(0.05f))
                        {
                            // GetMissingPartsCommonAncestors() 是 RimWorld 超神的內建方法
                            // 它會自動找「最根部」的斷肢。例如整條手臂斷了，它會先長手臂，不會直接長手指。
                            Hediff_MissingPart missingPart = pawn.health.hediffSet.GetMissingPartsCommonAncestors().FirstOrDefault();
                            
                            if (missingPart != null)
                            {
                                // 強制移除「部位遺失」的狀態，斷肢瞬間長回來了！
                                pawn.health.RemoveHediff(missingPart);
                                
                                // 丟出綠色黏液與恐怖的提示字
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "血肉增生...", Color.green);
                                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_Slime, 1);
                            }
                        }
                    }
                }
                else
                {
                    // 【潛伏狀態】：裝得像個正常人！
                    // 將嚴重度死死鎖定在 0.01 (XML 的階段 1)
                    // 這樣他不管被打多慘，都不會觸發無痛、護甲和回血！
                    this.Severity = 0.01f;
                    // ==========================================
                    // 🔥 全新機制：食屍鬼的第六感 (新鮮度雷達)
                    // ==========================================
                    // 改成每 2500 Ticks (遊戲內半小時) 掃描一次，避免洗頻
                    if (pawn.Map != null && ModsConfig.AnomalyActive)
                    {
                        if (pawn.IsHashIntervalTick(2500))
                        {
                            // 判斷「新鮮度」：是不是這 24 小時內才被替換的？ (1天 = 60000 Ticks)
                            bool isFreshlyInfected = this.ageTicks < 60000;

                            // 動態機率：剛替換的超危險，潛伏久的相對安全
                            float warnChance = isFreshlyInfected ? 0.50f : 0.10f;
                            float attackChance = isFreshlyInfected ? 0.15f : 0.01f;

                            // 掃描地圖上所有生物
                            foreach (Pawn nearby in pawn.Map.mapPawns.AllPawnsSpawned)
                            {
                                // 尋找距離 4 格以內的「食屍鬼」
                                if (nearby.IsGhoul && nearby.Position.DistanceTo(pawn.Position) <= 4f)
                                {
                                    if (Rand.Chance(warnChance))
                                    {
                                        string moteText = isFreshlyInfected ? "聞到濃烈的血肉味！" : "對著皮囊發出低吼...";
                                        Color moteColor = isFreshlyInfected ? Color.red : Color.yellow;
                                        
                                        MoteMaker.ThrowText(nearby.DrawPos, nearby.Map, moteText, moteColor);
                                        
                                        // 食屍鬼失控攻擊！
                                        if (Rand.Chance(attackChance))
                                        {
                                            string attackMsg = isFreshlyInfected 
                                                ? $"{nearby.NameShortColored} 聞到了 {pawn.NameShortColored} 身上尚未褪去的濃烈異星血肉味，發狂撲了上去！"
                                                : $"{nearby.NameShortColored} 的本能壓過了理智，對 {pawn.NameShortColored} 隱藏的異星皮囊發起了攻擊！";

                                            Messages.Message(attackMsg, new TargetInfo(nearby.Position, nearby.Map), MessageTypeDefOf.ThreatSmall, false);
                                            
                                            // 強制食屍鬼向偽人發起近戰攻擊 (這會瞬間觸發受傷現形邏輯)
                                            Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, pawn);
                                            nearby.jobs.StartJob(attackJob, JobCondition.InterruptForced);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }


    }
}