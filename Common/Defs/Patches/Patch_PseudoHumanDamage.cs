using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq; 

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class Patch_PseudoHumanAttacked
    {
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null || __instance.Dead || __instance.Map == null || totalDamageDealt <= 0) 
                return;

            // 必須是外部暴力傷害
            if (!dinfo.Def.ExternalViolenceFor(__instance)) return;
            // ==========================================
            // 🔥 修正 1：排除環境引發的「燒傷」與「著火」！
            // 偽人不會因為不小心踩到營火，或是身上著火就氣到現形，這太蠢了！
            // ==========================================
            if (dinfo.Def == DamageDefOf.Burn || dinfo.Def == DamageDefOf.Flame) return;
            HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
            if (pseudoMarkDef == null) return;

            Hediff_PseudoHuman mark = __instance.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;

            if (mark != null)
            {
                // ==========================================
                // 🛡️ 情況 A：如果被打的「受害者」是外人 (海盜/難民)
                // ==========================================
                if (__instance.Faction != Faction.OfPlayer)
                {
                    // 只要被打就必定現形！
                    if (!mark.isRevealed && Rand.Chance(PseudoHumanCore.settings.revealOnAttackedChance))
                    {
                        RevealPseudoHuman(__instance, mark);
                    }
                    // 外人的事外人自己解決，絕對不引發玩家基地的連鎖暴動，直接結束！
                    return; 
                }
                // 條件 2：檢查滲透率
                var colonists = __instance.Map.mapPawns.FreeColonists.ToList();
                int totalColonists = colonists.Count;
                int pseudoCount = colonists.Count(p => p.health.hediffSet.HasHediff(pseudoMarkDef));

                float infiltrationRate = totalColonists > 0 ? (float)pseudoCount / totalColonists : 0f;
                // ==========================================
                // 🏠 情況 B：如果被打的「受害者」是玩家的殖民者
                // ==========================================
                bool shouldReveal = false;

                // 條件 1：兇手是誰？如果是被「真正的外敵」攻擊
                if (dinfo.Instigator != null && dinfo.Instigator.Faction != null)
                {
                     // 條件 1：被真正的外敵攻擊
                    if (dinfo.Instigator.Faction.HostileTo(Faction.OfPlayer))
                    {
                        shouldReveal = true; // 遭遇外敵，隱藏不住了！
                    }
                    // 條件 2：被「自己人(玩家)」攻擊，且滲透率達標！
                    else if (dinfo.Instigator.Faction == Faction.OfPlayer)
                    {
                        if (infiltrationRate >= PseudoHumanCore.settings.pseudoHumanOccupy)
                        {
                            shouldReveal = true; // 玩家想殺我們，而且我們人夠多，直接反了！
                        }
                    }
                }

                

                // 🏆 [Bad Ending] 100% 絕對同化結局
                if (infiltrationRate >= 1.0f)
                {
                    bool reconciled = false;
                    foreach (Pawn col in colonists)
                    {
                        if (col.InMentalState && col.MentalStateDef == MentalStateDefOf.Berserk)
                        {
                            col.mindState.mentalStateHandler.CurState.RecoverFromState();
                            MoteMaker.ThrowText(col.DrawPos, col.Map, "血肉...同化完成...", Color.green);
                            reconciled = true;
                        }
                        Hediff_PseudoHuman pm = col.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                        if (pm != null) pm.isRevealed = true; 
                    }

                    if (reconciled)
                    {
                        Find.LetterStack.ReceiveLetter("絕對同化", "殖民地中最後一個正常人類也消失了...\n\n偽人們突然停下了狂暴的互相攻擊。他們看著彼此，空洞的眼神中達成了一種詭異的共識。他們輕聲呢喃著未知的語言，隨後像什麼事都沒發生過一樣，安靜地回到了各自的工作崗位上。\n\n這座基地，現在已經徹底屬於牠們了。", LetterDefOf.PositiveEvent);
                    }
                    return; 
                }

               

                // ==========================================
                // 綜合判定結果：隱忍 或 撕破臉
                // ==========================================
                if (!shouldReveal) return; // 繼續隱忍

                // 決定撕破臉，執行狂暴與連鎖判定
                if (!mark.isRevealed && Rand.Chance(PseudoHumanCore.settings.revealOnAttackedChance))
                {
                    // 1. 被打的這個人現形
                    RevealPseudoHuman(__instance, mark);

                    bool chainReaction = false;

                    // 2. 呼叫家裡的其他偽人一起暴動
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

                    if (chainReaction)
                    {
                        Find.LetterStack.ReceiveLetter("偽人暴動", "攻擊引發了連鎖反應！隱藏在殖民地中的其他偽人感應到了同伴的鮮血，紛紛撕下了偽裝開始無差別攻擊！", LetterDefOf.ThreatBig, __instance);
                    }
                }
            }
        }

        public static void RevealPseudoHuman(Pawn pawn, Hediff_PseudoHuman mark)
        {
            mark.isRevealed = true;
            
            if (!pawn.Dead && !pawn.Downed && !pawn.InMentalState)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "嘶嘶...皮囊好痛苦...", Color.red);
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "偽人現身", true, false, false, null, false, false, false);
            }

            BiotechMechanitorHelper.TryHackMechs(pawn);

            TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
            ThoughtDef mutationThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("ObservedMutation_Lover");

            if (loverTrait != null && mutationThought != null)
            {
                foreach (Pawn col in pawn.Map.mapPawns.FreeColonists)
                {
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