using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace PseudoHumanMod
{
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new System.Type[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class Patch_CorpseLoverTrait
    {
        private static readonly string[] kinDiedThoughts = new string[]
        {
            "MySonDied", "MyDaughterDied", "MyFatherDied", "MyMotherDied",
            "MyBrotherDied", "MySisterDied", "MyHalfSiblingDied", "MyGrandparentDied",
            "MyGrandchildDied", "MyNephewDied", "MyNieceDied", "MyUncleDied",
            "MyAuntDied", "MyCousinDied", "MyKinDied"
        };

        public static bool Prefix(MemoryThoughtHandler __instance, ref Thought_Memory newThought, Pawn otherPawn)
        {
            if (__instance.pawn?.story?.traits == null || newThought == null || newThought.def == null) return true;

            TraitDef loverTrait = DefDatabase<TraitDef>.GetNamedSilentFail("CorpseLover");
            if (loverTrait != null && __instance.pawn.story.traits.HasTrait(loverTrait))
            {
                string defName = newThought.def.defName;

                // 1. 看到屍體
                if (defName == "ObservedLayingCorpse" || defName == "ObservedCorpse")
                {
                    ThoughtDef goodDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("ObservedCorpse_Lover");
                    // 🔥 修正：前面加上 (Thought_Memory) 進行強制轉型
                    if (goodDef != null) newThought = (Thought_Memory)ThoughtMaker.MakeThought(goodDef);
                }
                // 2. 看到腐爛屍體
                else if (defName == "ObservedLayingRottingCorpse")
                {
                    ThoughtDef betterDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("ObservedRottingCorpse_Lover");
                    if (betterDef != null) newThought = (Thought_Memory)ThoughtMaker.MakeThought(betterDef);
                }
                // 3. 伴侶死亡
                else if (defName == "MyLoverDied" || defName == "MyFianceDied" || defName == "MySpouseDied")
                {
                    ThoughtDef twistedDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("MyLoverDied_CorpseLover");
                    if (twistedDef != null) newThought = (Thought_Memory)ThoughtMaker.MakeThought(twistedDef);
                }
                // 4. 親戚死亡
                else if (kinDiedThoughts.Contains(defName))
                {
                    ThoughtDef kinDef = DefDatabase<ThoughtDef>.GetNamedSilentFail("MyKinDied_CorpseLover");
                    if (kinDef != null) newThought = (Thought_Memory)ThoughtMaker.MakeThought(kinDef);
                }
            }
            return true; 
        }
    }
}