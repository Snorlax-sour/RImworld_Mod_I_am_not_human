using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq; // 🔥 必須加入這行！用來使用 ToList() 影印名單

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_PseudoHumanAttacked
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead || __instance.Map == null || totalDamageDealt <= 0) 
                return;

            HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
            if (pseudoMarkDef == null) return;

            Hediff_PseudoHuman mark = __instance.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

            if (mark != null)
            {
                // ==========================================
                // 🔥 核心恐怖邏輯：判定是否要「撕破臉」
                // ==========================================
                bool shouldReveal = false;

                // 條件 1：是否被「敵人」攻擊？(例如海盜、蟲族、機械族)
                // dinfo.Instigator 是造成傷害的來源
                if (dinfo.Instigator != null && dinfo.Instigator.Faction != null)
                {
                    // 如果攻擊者的派系，對玩家是敵對的
                    if (dinfo.Instigator.Faction.HostileTo(Faction.OfPlayer))
                    {
                        shouldReveal = true; // 遭遇外敵，隱藏不住了，爆發！
                    }
                }

                // 條件 2：殖民地的偽人滲透率是否達到 30%？
                var colonists = __instance.Map.mapPawns.FreeColonists.ToList();
                int totalColonists = colonists.Count;
                int pseudoCount = 0;

                foreach (Pawn col in colonists)
                {
                    if (col.health.hediffSet.HasHediff(pseudoMarkDef))
                    {
                        pseudoCount++;
                    }
                }

                // 計算比例 (例如：3個偽人 / 10個殖民者 = 0.3f)
                float infiltrationRate = totalColonists > 0 ? (float)pseudoCount / totalColonists : 0f;
                // ==========================================
                // 🏆 [Bad Ending] 100% 絕對同化結局
                // ==========================================
                if (infiltrationRate >= 1.0f)
                {
                    bool reconciled = false;

                    foreach (Pawn col in colonists)
                    {
                        // 如果有偽人還在發瘋互毆，強制讓他們清醒！
                        if (col.InMentalState && col.MentalStateDef == MentalStateDefOf.Berserk)
                        {
                            // 拔除發瘋狀態
                            col.mindState.mentalStateHandler.CurState.RecoverFromState();
                            
                            // 講出詭異的胡話 (互打招呼)
                            MoteMaker.ThrowText(col.DrawPos, col.Map, "血肉...同化完成...", Color.green);
                            reconciled = true;
                        }
                        
                        // 既然都佔領基地了，也不用裝了，保留現形狀態但乖乖去工作
                        Hediff_PseudoHuman pm = col.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                        if (pm != null) pm.isRevealed = true; 
                    }

                    // 寄出一封讓人絕望的信件給玩家
                    if (reconciled)
                    {
                        Find.LetterStack.ReceiveLetter(
                            "絕對同化", 
                            "殖民地中最後一個正常人類也消失了...\n\n偽人們突然停下了狂暴的互相攻擊。他們看著彼此，空洞的眼神中達成了一種詭異的共識。他們輕聲呢喃著未知的語言，隨後像什麼事都沒發生過一樣，安靜地回到了各自的工作崗位上。\n\n這座基地，現在已經徹底屬於牠們了。", 
                            LetterDefOf.PositiveEvent
                        );
                    }
                    return; // 遊戲結束了，不用再往下跑狂暴邏輯了
                }

                if (infiltrationRate >= PseudoHumanCore.settings.pseudoHumanOccupy)
                {
                    shouldReveal = true; // 數量夠多了，不需要再隱忍了，全面開戰！
                }

                // ==========================================
                // 如果條件不滿足，偽人就會「隱忍」(當作自己是正常人受傷)
                // ==========================================
                if (!shouldReveal)
                {
                    return; // 默默流血，什麼事都不做，直接結束程式碼
                }

                // ==========================================
                // 如果決定撕破臉，就執行我們之前的狂暴判定
                // ==========================================
                if (Rand.Chance(PseudoHumanCore.settings.revealOnAttackedChance))
                {
                    // 1. 被打的這個偽人必定現形
                    RevealPseudoHuman(__instance, mark);

                    bool chainReaction = false;

                    // 2. 檢查地圖上的其他人
                    foreach (Pawn otherColonist in colonists)
                    {
                        if (otherColonist == __instance) continue;

                        Hediff_PseudoHuman otherMark = otherColonist.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                        
                        if (otherMark != null && !otherMark.isRevealed)
                        {
                            if (Rand.Chance(PseudoHumanCore.settings.chainReactionChance))
                            {
                                RevealPseudoHuman(otherColonist, otherMark);
                                chainReaction = true;
                            }
                        }
                    }

                    // 3. 如果引發了全體暴動，發送警告信
                    if (chainReaction)
                    {
                        Find.LetterStack.ReceiveLetter(
                            "偽人暴動", 
                            "攻擊引發了連鎖反應！隱藏在殖民地中的其他偽人感應到了同伴的鮮血，紛紛撕下了偽裝開始無差別攻擊！", 
                            LetterDefOf.ThreatBig, 
                            __instance
                        );
                    }
                }
            }
        }

        public static void RevealPseudoHuman(Pawn pawn, Hediff_PseudoHuman mark)
        {
            // 標記為已現形 (健康面板會顯示出來)
            mark.isRevealed = true;
            
            // 檢查他「現在」是不是已經在發瘋了
            if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "嘶嘶...皮囊好痛苦...", Color.red);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "偽人現身", true, false, false, null, false, false, false);
            }
            // ==========================================
            // 🔥 新增：呼叫生技 DLC 專屬的「機械師叛變」邏輯
            // ==========================================
            BiotechMechanitorHelper.TryHackMechs(pawn);

            // 🔥 新增：讓周圍有「屍體癖好」的人看到這一幕會高潮！
            TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
            ThoughtDef mutationThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ObservedMutation_Lover");

            if (loverTrait != null && mutationThought != null)
            {
                // 掃描地圖上所有的殖民者
                foreach (Pawn col in pawn.Map.mapPawns.FreeColonists)
                {
                    // 如果有這個特性，並且距離在 15 格以內 (看得到)
                    if (col.story?.traits != null && col.story.traits.HasTrait(loverTrait) && col.Position.DistanceTo(pawn.Position) < 15f)
                    {
                        col.needs.mood?.thoughts.memories.TryGainMemory(mutationThought);
                        MoteMaker.ThrowText(col.DrawPos, col.Map, "太美妙了！", Color.green);
                    }
                }
            }
        }
    }
}