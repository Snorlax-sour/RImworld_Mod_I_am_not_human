using UnityEngine;
using Verse;
using RimWorld; // 🔥 必須加這行，才能抓到 TraitDef
namespace PseudoHumanMod
{
    // ==========================================
    // 畫出設定介面 (UI) 的類別
    // ==========================================
    public class PseudoHumanCore : Mod 
    {
        public static PseudoHumanSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        public PseudoHumanCore(ModContentPack content) : base(content)
        {
            settings = GetSettings<PseudoHumanSettings>();
            
            // 🔥 在這裡寫入機率是最安全的！因為設定檔保證已經讀取完畢了！
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
                if (loverTrait != null && loverTrait.degreeDatas != null && loverTrait.degreeDatas.Count > 0)
                {
                    loverTrait.degreeDatas[0].commonality = settings.corpseLoverCommonality;
                }
            });
        }

       

        public override string SettingsCategory()
        {
            return "Pseudo Human (偽人設定)"; 
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 1000f); // 畫布再拉長一點
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            // 🔥 核心修復：使用 try...finally 保證視窗一定會關閉
            try
            {
                Listing_Standard listingStandard = new Listing_Standard();
                listingStandard.Begin(viewRect);

                // 1. 胡話機率
                listingStandard.Label($"偽人說胡話機率: {settings.nonsenseChance.ToString("P0")}");
                settings.nonsenseChance = listingStandard.Slider(settings.nonsenseChance, 0f, 1f);
                listingStandard.Gap(); 

                // 🔥 2. 新增：被打現形的機率滑桿
                listingStandard.Label($"偽人被攻擊時暴露現形的機率: {settings.revealOnAttackedChance.ToString("P0")}");
                settings.revealOnAttackedChance = listingStandard.Slider(settings.revealOnAttackedChance, 0f, 1f);
                listingStandard.Gap(); 

                // 🔥 3. 新增：聯合暴動的機率滑桿
                listingStandard.Label($"其他偽人連鎖現形(聯合暴動)的機率: {settings.chainReactionChance.ToString("P0")}");
                settings.chainReactionChance = listingStandard.Slider(settings.chainReactionChance, 0f, 1f);
                listingStandard.Gap(); 

                // 4. 文字氣泡開關
                listingStandard.CheckboxLabeled("顯示「詭異的囈語...」文字氣泡", ref settings.showNonsenseText);
                // 5. 偽人自然生成機率
                listingStandard.Label($"世界生成小人時，帶有偽人標記的機率: {settings.generateChance.ToString("P0")}");
                settings.generateChance = listingStandard.Slider(settings.generateChance, 0f, 1f);
                listingStandard.Gap();
                

                
                // 6. 偽人佔比多少被攻擊狂暴的機率
                // 🔥 修正：文字顯示換成 pseudoHumanOccupy
                listingStandard.Label($"偽人佔比多少被攻擊狂暴的機率: {settings.pseudoHumanOccupy.ToString("P0")}");
                
                // 🔥 修正：存入的變數換成 pseudoHumanOccupy
                settings.pseudoHumanOccupy = listingStandard.Slider(settings.pseudoHumanOccupy, 0f, 1f);
                
                listingStandard.Gap(); // 建議把 Gap 放在滑桿下面，排版會比較漂亮

                listingStandard.CheckboxLabeled("偽人屍體是否要自行消失", ref settings.meltIntoSlime);
                // ==========================================
                // 🔥 新增的特性機率滑桿
                // ==========================================
                listingStandard.Label($"「屍體癖好」特性在世界自然生成的權重: {settings.corpseLoverCommonality.ToString("F2")} (原版嗜血大約為0.4)");
                settings.corpseLoverCommonality = listingStandard.Slider(settings.corpseLoverCommonality, 0f, 5f); // 最高可以調到 5，保證滿街都是變態
                listingStandard.Gap(); 

                listingStandard.Label($"偽人生成時，強制自帶「屍體癖好」的機率: {settings.pseudoHumanCorpseLoverChance.ToString("P0")}");
                settings.pseudoHumanCorpseLoverChance = listingStandard.Slider(settings.pseudoHumanCorpseLoverChance, 0f, 1f);
                listingStandard.GapLine(); 
                if (ModsConfig.BiotechActive)
                {
                    listingStandard.Label("醫療機器人(生技DLC) 屍檢掃描能力設定：");
                    if (listingStandard.RadioButton("機器人絕對精準", settings.robotMode == RobotInspectMode.Perfect))
                        settings.robotMode = RobotInspectMode.Perfect;
                        
                    if (listingStandard.RadioButton("偽人完美偽裝：機器人的感測器會被偽裝細胞欺騙 ", settings.robotMode == RobotInspectMode.Fooled))
                        settings.robotMode = RobotInspectMode.Fooled;
                        
                    if (listingStandard.RadioButton("內鬼", settings.robotMode == RobotInspectMode.MechanitorHacked))
                        settings.robotMode = RobotInspectMode.MechanitorHacked;
                }
                // ==========================================
                // 🔥 新增：異象 DLC 實體變身 UI
                // ==========================================
                if (ModsConfig.AnomalyActive)
                {
                    listingStandard.Label("【異象 DLC 連動】原生偽人死後的第二階段變異：");
                    listingStandard.CheckboxLabeled("開啟「死後現出真面目」 (根據玩家圖鑑解鎖進度，隨機變異出實體)", ref settings.spawnEntityOnDeath);
                    
                    if (settings.spawnEntityOnDeath)
                    {    // 🔥 新增：基礎怪物開關
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：基礎怪物 (血肉獸、凝血獸、竊視者)", ref settings.allowBasicEntities);
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：進階怪物 (奇美拉、吞噬者、夜魔、恐懼融合體)", ref settings.allowAdvancedEntities);
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：金屬異形 (Metalhorror)", ref settings.allowMetalhorror);
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：亡魂 (Revenant) [極度危險]", ref settings.allowRevenant);
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：痛苦之球 (Nociosphere) [毀滅級危險]", ref settings.allowNociosphere);
                        listingStandard.CheckboxLabeled(" └─ 允許掉落物：黃金立方體 (神的祝福)", ref settings.allowGoldenCube);
                        listingStandard.CheckboxLabeled(" └─ 允許變異為：血肉心臟 (Fleshmass Heart) [基地毀滅級！]", ref settings.allowFleshmassHeart);
                    }
                    listingStandard.GapLine();
                    // 🔥 新增：血肉心臟的開關

                }
                listingStandard.End();
            }
             finally
            {
                // 🔥 終極保證：不管 try 裡面發生什麼事，這裡一定會執行，UI 絕對不會崩潰！
                Widgets.EndScrollView();
            }
                base.DoSettingsWindowContents(inRect);
        }
        public override void WriteSettings()
        {
            // 先讓原版的存檔功能執行
            base.WriteSettings();
            
            // 把玩家拉好的「屍體癖好」機率，強行蓋過 XML 的設定！
            TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
            if (loverTrait != null && loverTrait.degreeDatas != null && loverTrait.degreeDatas.Count > 0)
            {
                // 注意：這裡改成了去 degreeDatas[0] 裡面修改！
                loverTrait.degreeDatas[0].commonality = settings.corpseLoverCommonality;
            }
        }


    }

    
    

}