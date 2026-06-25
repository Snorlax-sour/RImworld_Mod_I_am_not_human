using RimWorld;
using Verse;
using UnityEngine;

namespace PseudoHumanMod
{
    // ==========================================
    // 1. 全局追蹤器 (精準 40% 數學計算版)
    // ==========================================
    public class GameComponent_PseudoHumanTracker : GameComponent
    {
        public int pseudoHumansDisposed = 0;
        public bool hasTriggeredWarning = false;

        public GameComponent_PseudoHumanTracker(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref pseudoHumansDisposed, "pseudoHumansDisposed", 0);
            Scribe_Values.Look(ref hasTriggeredWarning, "hasTriggeredWarning", false);
        }

        // 🔥 新增了 isBanish 參數，用來修正人口計算
        public static void CheckFourthWallBreak(Pawn pawn, bool isBanish = false)
        {
            var comp = Current.Game.GetComponent<GameComponent_PseudoHumanTracker>();
            if (comp == null || comp.hasTriggeredWarning) return;

            // 如果是事前攔截的放逐(isBanish)，或是死掉時還是玩家的人
            if (isBanish || pawn.Faction == Faction.OfPlayer)
            {
                comp.pseudoHumansDisposed++;

                // 抓取目前活著的總人口
                int totalColonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists.Count;
                
                // 🔥 如果是處刑致死，因為他已經死了，totalColonists 已經少 1 了，我們必須把他加回來！
                // 如果是放逐(Prefix事前攔截)，他還沒被踢出去，totalColonists 數字是準的。
                if (!isBanish) 
                {
                    totalColonists += 1; 
                }
                
                // 算出 40% 的人數門檻
                float requiredAmount = totalColonists * 0.40f;

                // 防呆機制：最少要累積 2 個
                float finalThreshold = Mathf.Max(2f, requiredAmount);
                // ==========================================
                // 🔥 新增：在遊戲畫面左上角顯示進度！
                // ==========================================
                if(Prefs.DevMode){
                    Messages.Message($"[隱密排查進度] 已處理的潛伏者: {comp.pseudoHumansDisposed} / {finalThreshold}", MessageTypeDefOf.NeutralEvent, false);
                    Log.Message($"[偽人測試] 目前處置人數: {comp.pseudoHumansDisposed}，總人口: {totalColonists}，門檻: {finalThreshold}");
                }
                if (comp.pseudoHumansDisposed >= finalThreshold)
                {
                    comp.hasTriggeredWarning = true; 
                    Find.WindowStack.Add(new Window_FourthWallBreak(pawn));
                }
            }
        }
    }


    // ==========================================
    // 2. 突發驚嚇全螢幕視窗 (維持原樣不變)
    // ==========================================
    public class Window_FourthWallBreak : Window
    {
        private Pawn pawn;
        private float startTime;
        
        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

        public Window_FourthWallBreak(Pawn pawn)
        {
            this.pawn = pawn;
            this.absorbInputAroundWindow = true; 
            this.forcePause = true;              
            this.doCloseButton = false;          
            this.doCloseX = false;
            this.closeOnClickedOutside = false;
            this.layer = WindowLayer.Super;      
            this.startTime = Time.realtimeSinceStartup; 
        }

        public override void DoWindowContents(Rect inRect)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;

            GUI.DrawTexture(inRect, BaseContent.BlackTex);

            if (elapsed > 4f)
            {
                Find.LetterStack.ReceiveLetter(
                    "來自「牠們」的警告", 
                    "螢幕突然閃爍了一下，畫面恢復了正常。\n\n一條未知的加密訊息出現在你的操作終端機上：\n\n『致 這座殖民地的「管理者」（玩家）：\n\n我們已經容忍了你多次的無禮排查、處決與放逐。我們只是想借用幾具皮囊，在這裡平靜地生活下去。\n\n這是第一次，也是最後一次的善意警告。請適可而止。\n\n否則，下一次直視你的...將不會只有螢幕裡的這張臉。』", 
                    LetterDefOf.ThreatBig
                );
                this.Close();
                return;
            }

            if (elapsed > 3f) return;

            RenderTexture portrait = PortraitsCache.Get(pawn, new Vector2(512, 512), Rot4.South, new Vector3(0, 0, 0.15f), 1f, true, true, false, false, null, null, true);
            
            if (portrait != null)
            {
                float scale = 1f + (elapsed / 3f) * 15f; 
                float width = 200f * scale;
                float height = 200f * scale;
                
                Rect drawRect = new Rect((inRect.width - width) / 2, (inRect.height - height) / 2, width, height);

                GUI.DrawTexture(drawRect, portrait);
            }
        }
    }
}