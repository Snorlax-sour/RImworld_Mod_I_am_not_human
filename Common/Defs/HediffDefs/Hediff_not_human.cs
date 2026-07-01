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
                    float lostHealth = 1.0f - pawn.health.summaryHealth.SummaryHealthPercent;

                    // ==========================================
                    // 🩸 全新計算：斷肢帶來的極度憤怒！
                    // ==========================================
                    // 算出他目前有幾個「真正斷掉的部位」(GetMissingPartsCommonAncestors)
                    int missingPartsCount = pawn.health.hediffSet.GetMissingPartsCommonAncestors().Count;
                    
                    // 每斷一個部位，額外增加 0.25 (25%) 的嚴重度！
                    float missingPartPenalty = missingPartsCount * 0.25f;

                    // 將流血受傷與斷肢的嚴重度加總！(最高鎖定在 0.99，避免超過 1.0 引發預期外的 Bug)
                    this.Severity = Mathf.Clamp(lostHealth + missingPartPenalty, 0.01f, 0.99f);

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
                        // ==========================================
                        // 🔥 2. 終極動態斷肢重生系統 (支援超過 100% 多重再生！)
                        // ==========================================
                        float recover_missing_part_rate = this.Severity * 100.0f * 0.05f;

                        // 數學魔術 1：抽出「整數」部分 (例如 4.5 會抽出 4)
                        int guaranteedHeals = Mathf.FloorToInt(recover_missing_part_rate);
                        
                        // 數學魔術 2：抽出「小數」部分 (例如 4.5 - 4 = 0.5，也就是 50% 機率)
                        float extraChance = recover_missing_part_rate - guaranteedHeals;
                        
                        // 計算這 1 秒內，到底要長出幾個部位？
                        // (必定生長數 + 骰中機率多送 1 個)
                        int totalHeals = guaranteedHeals + (Rand.Chance(extraChance) ? 1 : 0);

                        // 使用迴圈，讓他瞬間長出複數器官！
                        for (int i = 0; i < totalHeals; i++)
                        {
                            Hediff_MissingPart missingPart = pawn.health.hediffSet.GetMissingPartsCommonAncestors().FirstOrDefault();
                            
                            if (missingPart != null)
                            {
                                // 長出一個部位！
                                pawn.health.RemoveHediff(missingPart);
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "血肉暴增！", Color.green);
                                FilthMaker.TryMakeFilth(pawn.Position, pawn.Map, ThingDefOf.Filth_Slime, 1);
                            }
                            else
                            {
                                // 防呆：如果身體已經全部長滿了(沒有斷肢)，就提早結束迴圈！
                                break; 
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
                // 掃描他身上所有的健康狀態
                for (int i = 0; i < pawn.health.hediffSet.hediffs.Count; i++)
                {
                    Hediff h = pawn.health.hediffSet.hediffs[i];
                    string defName = h.def.defName;
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[偽人測試] who: {pawn.Name} defname: {defName} type: {h.GetType()}");
                    }
                    // ------------------------------------------
                        // 1. 生技 DLC 的「基因依賴 (如呀呦粉、果汁依賴)」
                        // 🔥 最強黑魔法：直接判斷它的底層類別是不是 Hediff_ChemicalDependency！
                        // ------------------------------------------
                        if (h is Hediff_ChemicalDependency || h.def.defName.Contains("GeneticDrugNeed") )
                        {
                            // 官方設定：5天發作，30天昏迷，60天致死。
                            // 我們把它死死卡在 29.5 天！他會痛不欲生、狂砸房間，但「絕對不會昏倒或死亡」！
                            if (h.Severity > 5f)
                            {
                                h.Severity = 5f;
                            }
                            if (Prefs.DevMode)
                            {
                                Log.Message($"[偽人測試] who: {pawn.Name} defname: {defName} type: {h.GetType()}");
                                Log.Message($"[偽人測試] who: {pawn.Name} defname: {defName} severity: {h.Severity}");
                            }
                            continue; // 處理完了，檢查下一個病
                            
                        }

                        // ------------------------------------------
                        // 2. 血族的「死眠耗竭 (DeathrestExhaustion)」
                        // ------------------------------------------
                        if (defName == "DeathrestExhaustion")
                        {
                            // 官方設定：超過 1.0 會導致強制致命昏迷
                            // 我們卡在 0.99，讓他極度疲勞但不倒地
                            if (h.Severity > 0.99f) h.Severity = 0.99f;
                            continue;
                        }

                    // ------------------------------------------
                        // 3. 原版毒品戒斷 (Withdrawal) 與 渴血症
                        // ------------------------------------------
                        if (defName.Contains("Dependency") || 
                            defName.Contains("Deficiency") || 
                            defName.Contains("Withdrawal") || 
                            defName == "HemogenCraving")
                        {
                            // 其他常規戒斷症的滿級通常是 1.0
                            if (h.Severity > 0.65f) h.Severity = 0.65f;
                        }
                        // ==========================================
                    // 🔥 4. 新增：營養不良 (Malnutrition) 鎖血！
                    // ==========================================
                    if (defName == "Malnutrition")
                    {
                        // 原版營養不良：
                        // 0.8 = 極度飢餓 (重度昏迷)
                        // 1.0 = 餓死
                        // 我們把他卡在 0.99，他會餓到倒在地上爬不起來(裝死)，但他永遠不會餓死！
                        if (h.Severity > 0.8f) h.Severity = 0.7f;
                        continue;
                    }
                }
            }
        }


    }
}