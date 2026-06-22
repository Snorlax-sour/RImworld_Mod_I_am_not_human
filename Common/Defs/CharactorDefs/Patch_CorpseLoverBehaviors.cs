using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace PseudoHumanMod
{
    // ==========================================
    // 1. 社交限制：只有屍體癖才會觸發「談論屍體」
    // ==========================================
    public class InteractionWorker_CorpseLover : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
            // 如果發話者有這個特性，就有極高的機率(0.5)一直跟別人聊屍體！
            if (initiator.story?.traits != null && loverTrait != null && initiator.story.traits.HasTrait(loverTrait))
            {
                return 0.5f; 
            }
            return 0f; // 正常人絕對不會聊這個
        }
    }

    // ==========================================
    // 2. 殺人攔截：親手製造屍體會爽
    // ==========================================
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_PawnKill_CorpseLover
    {
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            // 確認死者是人類，且是被另一個小人殺死的
            if (__instance.RaceProps.Humanlike && dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
            {
                TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
                if (loverTrait != null && killer.story?.traits != null && killer.story.traits.HasTrait(loverTrait))
                {
                    // 給殺人犯塞入「製造了屍體」的好心情
                    ThoughtDef killThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("KilledHumanlike_CorpseLover");
                    if (killThought != null)
                    {
                        killer.needs.mood?.thoughts.memories.TryGainMemory(killThought);
                    }
                }
            }
        }
    }

    // ==========================================
    // 3. 崩潰操控：把屍體搬上餐桌 (Corpse Obsession)
    // ==========================================
    [HarmonyPatch(typeof(MentalBreakWorker), "CommonalityFor")]
    public static class Patch_MentalBreak_CorpseLover
    {
        // 攔截系統決定「要讓小人發生哪種精神崩潰」的機率計算
        public static void Postfix(MentalBreakWorker __instance, Pawn pawn, ref float __result)
        {
            // 如果系統正在計算「掘屍 (CorpseObsession)」這個崩潰的機率
            if (__instance.def.defName == "CorpseObsession")
            {
                TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
                if (loverTrait != null && pawn.story?.traits != null && pawn.story.traits.HasTrait(loverTrait))
                {
                    // 🔥 直接把發生機率放大 100 倍！
                    // 這代表只要他一崩潰，而且地圖上有屍體或墳墓，他幾乎 100% 會去把屍體挖出來丟在餐桌上！
                    __result *= 100f; 
                }
            }
        }
    }
}