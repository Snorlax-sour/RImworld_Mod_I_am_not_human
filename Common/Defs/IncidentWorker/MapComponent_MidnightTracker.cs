using RimWorld;
using Verse;
using System.Linq; // 🔥 必須引入

namespace PseudoHumanMod
{
    public class MapComponent_MidnightTracker : MapComponent
    {
        private int lastFiredDay = -1;

        public MapComponent_MidnightTracker(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastFiredDay, "lastFiredDay", -1);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                int currentDay = GenLocalDate.DayOfYear(map);
                int currentHour = GenLocalDate.HourInteger(map);

                // 每天凌晨 2 點判定一次
                if (currentHour == 2 && lastFiredDay != currentDay)
                {
                    // 標記為已判定，避免同一個半夜重複執行
                    lastFiredDay = currentDay;

                    // ==========================================
                    // 1. 計算目前的「偽人佔比」
                    // ==========================================
                    HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                    if (pseudoMarkDef == null) return;

                    var colonists = map.mapPawns.FreeColonists.ToList();
                    if (colonists.Count == 0) return;

                    int pseudoCount = colonists.Count(p => p.health.hediffSet.HasHediff(pseudoMarkDef));
                    
                    // 偽人佔比 (例如 2個偽人/10人 = 0.20f)
                    float pseudoRatio = (float)pseudoCount / colonists.Count;

                    // ==========================================
                    // 2. 計算最終發動機率： 基礎設定 + 偽人佔比
                    // ==========================================
                    float finalChance = PseudoHumanCore.settings.midnightAssassinationBaseChance + pseudoRatio;

                    // ==========================================
                    // 3. 擲骰子！
                    // ==========================================
                    // Rand.Chance 會根據傳入的小數決定機率 (例如 finalChance 是 0.3，就是 30% 機率)
                    if (Rand.Chance(finalChance))
                    {
                        IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("PseudoHuman_MidnightReplacement");
                        if (def != null)
                        {
                            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
                            // 呼叫事件 (如果事件執行失敗，比如沒人在睡覺，它會回傳 false，這很正常)
                            def.Worker.TryExecute(parms);
                        }
                    }
                }
            }
        }
    }
}