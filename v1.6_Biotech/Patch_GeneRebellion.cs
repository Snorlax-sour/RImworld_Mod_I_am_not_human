using HarmonyLib;
using RimWorld;
using Verse;

namespace PseudoHumanMod
{
    // ==========================================
    // 🧬 攔截生技 DLC 的「基因啟動判定」
    // ==========================================
    [HarmonyPatch(typeof(Gene), "get_Active")]
    public static class Patch_GeneRebellion
    {
        public static bool Prepare()
        {
            // 只有啟用生技 DLC 才掛載這個攔截器
            return ModsConfig.BiotechActive;
        }

        public static void Postfix(Gene __instance, ref bool __result)
        {
            // 如果這個基因原本是啟動的，而且宿主存在
            if (__result && __instance.pawn != null)
            {
                HediffDef pseudoMarkDef = DefDatabase<HediffDef>.GetNamedSilentFail("PseudoHumanMark");
                if (pseudoMarkDef == null) return;

                // 檢查宿主是不是偽人
                Hediff_PseudoHuman mark = __instance.pawn.health.hediffSet.GetFirstHediffOfDef(pseudoMarkDef) as Hediff_PseudoHuman;
                
                // 🔥 核心邏輯：不管有沒有現形，只要是偽人，就會突破基因鎖！
                if (mark != null)
                {
                    string geneName = __instance.def.defName;

                    // 1. 暴力解禁：高階伴侶 (Highmate) 突然可以拿槍殺人了！
                    if (geneName == "ViolenceDisabled" || 
                    // 2. 睡眠解禁：血族 (Sanguophage) 再也不需要死眠了！
                        geneName == "Deathrest" || 
                    // 3. 視力解禁：豬皮人 (Pigskin) 的近視眼突然好了，變成神射手！
                        geneName == "Nearsighted" || 
                    // 4. 睡眠解禁：雪怪 (Yttakin) 突然不嗜睡了！
                        geneName == "Sleepy" ||
                    // 5. 痛覺解禁：精靈 (Genie) 的「額外痛覺」失效，變得非常耐打！
                        geneName == "Pain_Extra")
                    {
                        // 🔥 強制告訴遊戲系統：「這個基因無效！」
                        __result = false; 
                    }
                }
            }
        }
    }
}