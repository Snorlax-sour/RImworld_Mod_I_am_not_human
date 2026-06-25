using LudeonTK; // 🔥 必須引入，這是開發者模式(Dev Mode)的工具箱
using RimWorld;
using Verse;
using System.Linq;

namespace PseudoHumanMod
{
    public static class PseudoHumanTests
    {
        // 加上這個標籤，這個方法就會出現在遊戲上方的開發者選單裡！
        // 分類叫 "PseudoHuman Tests"，按鈕叫 "Test: Force Midnight Event"
        [DebugAction("PseudoHuman Tests", "Test: Force Midnight Event (測試午夜暗殺)", actionType = DebugActionType.Action)]
        public static void TestMidnightEvent()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;

            IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail("PseudoHuman_MidnightReplacement");
            if (def != null)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
                
                // 強制執行！
                bool success = def.Worker.TryExecute(parms);
                
                // 印出測試結果
                if (success)
                    Log.Message("[測試通過] 午夜暗殺事件成功觸發！");
                else
                    Log.Error("[測試失敗] 午夜暗殺觸發失敗 (可能沒人在睡覺，或沒偽人)");
            }
        }

        // 可以再做一個測試按鈕：統計地圖上的偽人數量
        [DebugAction("PseudoHuman Tests", "Test: Count PseudoHumans (統計偽人)", actionType = DebugActionType.Action)]
        public static void TestCountPseudoHumans()
        {
            Map map = Find.CurrentMap;
            HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");

            int count = map.mapPawns.AllPawnsSpawned.Count(p => p.health.hediffSet.HasHediff(pseudoMarkDef));
            int total = map.mapPawns.FreeColonists.Count;

            Log.Message($"[測試報告] 目前地圖上的偽人總數：{count}。 殖民者滲透率：{(float)count/total * 100}%");
        }
    }
}