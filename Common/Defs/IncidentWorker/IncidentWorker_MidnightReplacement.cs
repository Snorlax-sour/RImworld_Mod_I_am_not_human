using RimWorld;
using Verse;
using System.Linq;

namespace PseudoHumanMod
{
    public class IncidentWorker_MidnightReplacement : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;
            
            Map map = (Map)parms.target;
            int hour = GenLocalDate.HourInteger(map);
            
            // 限制在半夜 (22:00 到 04:00) 發生
            if (hour > 4 && hour < 22) return false;
            
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
            if (pseudoMarkDef == null) return false;

            // 尋找兇手(已經潛伏在基地裡的偽人)
            var perpetrators = map.mapPawns.FreeColonists.Where(p => 
                p.health.hediffSet.HasHediff(pseudoMarkDef)).ToList();

            if (perpetrators.Count == 0) return false;

            // 尋找獵物：純人類、不是偽人、而且「正在睡覺」
            var sleepingTargets = map.mapPawns.FreeColonists.Where(p => 
                p.def == ThingDefOf.Human && 
                !p.health.hediffSet.HasHediff(pseudoMarkDef) &&
                p.jobs?.curDriver?.asleep == true).ToList();

            if (sleepingTargets.Count == 0) return false;

            // 隨機挑選一個受害者
            Pawn victim = sleepingTargets.RandomElement();

            // 1. 給他打上偽人標記
            victim.health.AddHediff(pseudoMarkDef);

            // 標記這個偽人是「替身」(有真人藏在通風管)
            Hediff_PseudoHuman mark = victim.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
            if (mark != null)
            {
                mark.hasHostInVent = true;
            }

            // ==========================================
            // 🔥 2. 尋找懸疑的血跡噴灑點 (遠離臥室)
            // ==========================================
            IntVec3 bloodPos = IntVec3.Invalid;

            // 優先嘗試：尋找基地裡的「通風口」
            ThingDef ventDef = DefDatabase<ThingDef>.GetNamedSilentFail("Vent");
            if (ventDef != null)
            {
                var vents = map.listerThings.ThingsOfDef(ventDef);
                if (vents.Count > 0)
                {
                    Thing randomVent = vents.RandomElement();
                    CellFinder.TryRandomClosewalkCellNear(randomVent.Position, map, 2, out bloodPos, null);
                }
            }

            // 如果沒有通風口，或通風口旁邊沒空地：尋找「公共區域」
            if (!bloodPos.IsValid)
            {
                CellFinder.TryFindRandomCell(map, c =>
                {
                    // 必須是玩家居住區、可站立、且在屋頂下
                    if (!map.areaManager.Home[c] || !c.Standable(map) || !c.Roofed(map)) return false;
                    
                    Room room = c.GetRoom(map);
                    // 🔥 絕對不能是臥室或通鋪！以免暴露受害者身分
                    if (room != null && (room.Role == RoomRoleDefOf.Bedroom || room.Role == RoomRoleDefOf.Barracks)) return false;
                    
                    return true;
                }, out bloodPos);
            }

            // 終極備用方案 (如果基地小到連個走廊都沒有，只好隨便噴在室外居住區)
            if (!bloodPos.IsValid)
            {
                CellFinder.TryRandomClosewalkCellNear(victim.Position, map, 5, out bloodPos, null);
            }

            // 在決定的地點噴灑大量血跡
            if (bloodPos.IsValid)
            {
                FilthMaker.TryMakeFilth(bloodPos, map, ThingDefOf.Filth_Blood, Rand.Range(3, 6));
            }

            // ==========================================
            // 🔥 3. 發送符合午夜情境的信件
            // ==========================================
            Find.LetterStack.ReceiveLetter(
                "午夜的血跡",
                "在寂靜的深夜中，有一名正在值夜班（或半夜起床）的殖民者，在基地內部發現了一灘駭人的新鮮血跡。\n\n血跡的來源不明，周圍也沒有發現任何打鬥的痕跡或屍體。所有人似乎都安穩地睡在自己的床上...\n\n這可能只是小動物受傷，也可能是某種更險惡的事情，剛剛在我們熟睡時悄悄發生了。",
                LetterDefOf.NeutralEvent,
                new TargetInfo(bloodPos, map)
            );

            return true;
        }
    }
}