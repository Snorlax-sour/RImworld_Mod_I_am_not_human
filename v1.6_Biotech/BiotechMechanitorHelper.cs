using RimWorld;
using Verse;
using System.Linq;

namespace PseudoHumanMod
{
    public static class BiotechMechanitorHelper
    {
        // 觸發機械族叛變的邏輯
        public static void TryHackMechs(Pawn pawn)
        {
            // 防呆：如果沒有啟動生技 DLC，或者這個人不是機械師，就直接跳過
            if (!ModsConfig.BiotechActive || pawn.mechanitor == null) return;

            // 把他控制的機械族名單複製一份出來 (避免修改時發生報錯)
            var controlledMechs = pawn.mechanitor.ControlledPawns.ToList();
            if (controlledMechs.Count == 0) return;

            bool mechsHacked = false;

            foreach (Pawn mech in controlledMechs)
            {
                // 🔥 50% 機率：機械族連線被竄改，變成敵對並發瘋
                if (Rand.Chance(0.5f))
                {
                    // ==========================================
                    // 1. 強制切斷控制 (使用你查到的官方 API！)
                    // 這會安全地釋放機械師的控制頻寬，並清除群組資料
                    // ==========================================
                    pawn.mechanitor.UnassignPawnFromAnyControlGroup(mech);

                    // 保險起見：確保 Overseer 的社交關係線也被徹底斬斷
                    if (pawn.relations != null && mech.relations != null)
                    {
                        pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, mech);
                    }
                    
                    // ==========================================
                    // 2. 叛變與發瘋邏輯
                    // ==========================================
                    // 把機械族變成敵對派系 (野生機械族)
                    if (mech.Faction != Faction.OfMechanoids)
                    {
                        mech.SetFaction(Faction.OfMechanoids);
                    }

                    // 讓機器人直接進入狂暴狀態 (見人就打)
                    mech.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.Berserk, "網路遭駭入", true, false, false, null, false, false, false);

                    // 畫面上製造電火花短路的特效
                    FleckMaker.ThrowMicroSparks(mech.DrawPos, mech.Map);
                    
                    mechsHacked = true;
                }
            }

            // 發送專屬的絕望信件
            if (mechsHacked)
            {
                Find.LetterStack.ReceiveLetter(
                    "機械網路遭駭入！",
                    $"當 {pawn.NameShortColored} 撕下偽裝的同時，牠順著機械師的神經網路，發送了惡意的異星病毒指令！\n\n牠所控制的部分機械族已經被強制切斷了與殖民地的連線，並轉為敵對狂暴狀態！",
                    LetterDefOf.ThreatBig,
                    pawn
                );
            }
        }
    }
}