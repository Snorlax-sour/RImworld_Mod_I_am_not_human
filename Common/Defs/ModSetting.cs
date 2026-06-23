using Verse;
using UnityEngine;

namespace PseudoHumanMod
{
    // ==========================================
    // 宣告一個列舉 (Enum) 來記錄 3 種模式
    // ==========================================
    public enum RobotInspectMode
    {
        Perfect,           // 絕對精準
        Fooled,            // 完美偽裝(被騙)
        MechanitorHacked   // 機械師內鬼
    }
    // ==========================================
    // 儲存設定資料的類別
    // ==========================================
    public class PseudoHumanSettings : ModSettings
    {
        public float nonsenseChance = 0.10f; // 預設 10% 胡話
        public bool showNonsenseText = true; // 預設顯示氣泡
        public float generateChance = 0.10f; // 預設 10% 生成機率
        // 在 PseudoHumanSettings.cs 裡面新增：
        public bool allowBasicEntities = true;      // 🔥 新增變數：基礎怪物
        public bool meltIntoSlime = false; // 預設：原生偽人死後會化為綠水
        public float pseudoHumanOccupy = 0.40f; // 預設 40% 佔有率
         // 🔥 新增：午夜暗殺的基礎機率 (預設 10%)
        public float midnightAssassinationBaseChance = 0.10f; 
        // 在 PseudoHumanSettings.cs 中新增：
    public bool perfectActor = true; // 預設開啟：潛伏期會假裝被電暈
// 記得在 ExposeData 裡面加上這行存檔：
         // 🔥 新增：機器人掃描模式 (預設為完美偽裝)
        public RobotInspectMode robotMode = RobotInspectMode.Fooled; 
        // 🔥 新增：被攻擊時現形的機率 (預設 100%)
        public float revealOnAttackedChance = 1.0f; 
        
        // 🔥 新增：其他偽人連鎖暴動的機率 (預設 50%)
        public float chainReactionChance = 0.50f; 
        // 在 PseudoHumanSettings 類別裡加入：
        public float corpseLoverCommonality = 0.3f; // 世界自然生成屍體癖好的權重
        public float pseudoHumanCorpseLoverChance = 0.20f; // 偽人「強制自帶」該特性的機率
         // ==========================================
        // 🔥 新增：異象實體變身開關
        // ==========================================
        public bool spawnEntityOnDeath = true;      // 總開關
        public bool allowAdvancedEntities = false;  // 允許進階怪物 (奇美拉、吞噬者、夜魔等)
        public bool allowRevenant = false;          // 允許亡魂 (隱形怪)
        public bool allowMetalhorror = true;        // 允許金屬異形
        public bool allowNociosphere = false;       // 允許痛苦之球
        public bool allowGoldenCube = true;        // 允許黃金立方體
        public bool allowFleshmassHeart = false; // 🔥 允許血肉心臟 (預設關閉)
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nonsenseChance, "nonsenseChance", 0.10f);
            Scribe_Values.Look(ref showNonsenseText, "showNonsenseText", true);
            Scribe_Values.Look(ref generateChance, "generateChance", 0.10f);
            Scribe_Values.Look(ref pseudoHumanOccupy, "pseudoHumanOccupy", 0.40f);
            // 🔥 新增：告訴系統如何存檔/讀檔這兩個新變數
               Scribe_Values.Look(ref allowBasicEntities, "allowBasicEntities", true); // 🔥 存檔
            Scribe_Values.Look(ref revealOnAttackedChance, "revealOnAttackedChance", 1.0f);
            Scribe_Values.Look(ref chainReactionChance, "chainReactionChance", 0.50f);
            Scribe_Values.Look(ref perfectActor, "perfectActor", true);      
             // 存檔列舉
            Scribe_Values.Look(ref robotMode, "robotMode", RobotInspectMode.Fooled); 
            // 🔥 記得存檔
            Scribe_Values.Look(ref midnightAssassinationBaseChance, "midnightAssassinationBaseChance", 0.10f);
            // 在 ExposeData 裡加上存檔：
            Scribe_Values.Look(ref meltIntoSlime, "meltIntoSlime", false);
            // 存檔實體開關
            Scribe_Values.Look(ref spawnEntityOnDeath, "spawnEntityOnDeath", true);
            Scribe_Values.Look(ref allowAdvancedEntities, "allowAdvancedEntities", false);
            Scribe_Values.Look(ref allowRevenant, "allowRevenant", false);
            Scribe_Values.Look(ref allowMetalhorror, "allowMetalhorror", true);
            Scribe_Values.Look(ref allowNociosphere, "allowNociosphere", false);
            Scribe_Values.Look(ref allowGoldenCube, "allowGoldenCube", true);
            Scribe_Values.Look(ref allowFleshmassHeart, "allowFleshmassHeart", false); // 🔥 記得存檔
            // 在 ExposeData 裡加上：
            Scribe_Values.Look(ref corpseLoverCommonality, "corpseLoverCommonality", 0.3f);
            Scribe_Values.Look(ref pseudoHumanCorpseLoverChance, "pseudoHumanCorpseLoverChance", 0.20f);
        }
    }

    
}