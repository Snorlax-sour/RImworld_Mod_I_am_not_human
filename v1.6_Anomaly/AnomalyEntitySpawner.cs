using RimWorld;
using Verse;
using System.Collections.Generic;

namespace PseudoHumanMod
{
    public static class AnomalyEntitySpawner
    {
        // 🔥 修正：把 void 改成 bool，讓它能回傳是否成功生成！
        public static bool TrySpawnEntity(IntVec3 pos, Map map)
        {
            if (!ModsConfig.AnomalyActive || !PseudoHumanCore.settings.spawnEntityOnDeath) return false;

            List<string> possiblePawns = new List<string>();

            void AddIfDiscovered(string pawnName, string codexName, bool isAllowed)
            {
                if (!isAllowed) return;
                EntityCodexEntryDef codexDef = DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail(codexName);
                if (codexDef != null && Find.EntityCodex.Discovered(codexDef))
                {
                    possiblePawns.Add(pawnName);
                }
            }

            AddIfDiscovered("Fingerspike", "Fleshbeasts", true); 
            AddIfDiscovered("Toughspike", "Fleshbeasts", true);  
            AddIfDiscovered("Trispike", "Fleshbeasts", true);    
            AddIfDiscovered("Bulbfreak", "Fleshbeasts", true);   
            AddIfDiscovered("Gorehulk", "Gorehulk", true);       
            AddIfDiscovered("Sightstealer", "Sightstealer", true); 

            bool adv = PseudoHumanCore.settings.allowAdvancedEntities;
            AddIfDiscovered("Chimera", "Chimera", adv);      
            AddIfDiscovered("Devourer", "Devourer", adv);    
            AddIfDiscovered("Noctol", "Noctol", adv);        
            AddIfDiscovered("Dreadmeld", "Dreadmeld", adv);  

            AddIfDiscovered("Metalhorror", "Metalhorror", PseudoHumanCore.settings.allowMetalhorror);
            AddIfDiscovered("Revenant", "Revenant", PseudoHumanCore.settings.allowRevenant);

            bool canDropCube = PseudoHumanCore.settings.allowGoldenCube && 
                               Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("GoldenCube"));
            
            if (canDropCube && Rand.Chance(0.1f))
            {
                Thing cube = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamedSilentFail("GoldenCube"));
                GenSpawn.Spawn(cube, pos, map);
                Messages.Message("一塊散發著誘人光芒的黃金立方體，從偽人的殘骸中滾了出來...", new TargetInfo(pos, map), MessageTypeDefOf.NeutralEvent, false);
                return true; // 🔥 回傳成功
            }

            bool canSpawnNociosphere = PseudoHumanCore.settings.allowNociosphere && 
                                       Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("Nociosphere"));

            if (canSpawnNociosphere && Rand.Chance(0.1f))
            {
                Thing nociosphere = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamedSilentFail("Nociosphere"));
                GenSpawn.Spawn(nociosphere, pos, map);
                Find.LetterStack.ReceiveLetter("異象降臨：痛苦之球！", "這具偽人的皮囊徹底崩解，從中浮現出的竟然是一顆充斥著無盡痛苦的暗黑球體！這將是一場毀滅性的災難！", LetterDefOf.ThreatBig, new TargetInfo(pos, map));
                return true; // 🔥 回傳成功
            }

            bool canSpawnHeart = PseudoHumanCore.settings.allowFleshmassHeart && 
                                 Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("FleshmassHeart"));

            if (canSpawnHeart && Rand.Chance(0.1f))
            {
                ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail("FleshmassHeart");
                if (heartDef != null)
                {
                    Thing heart = ThingMaker.MakeThing(heartDef);
                    GenSpawn.Spawn(heart, pos, map);
                    
                    Find.LetterStack.ReceiveLetter(
                        "異象降臨：血肉心臟！", 
                        "這具偽人的皮囊崩解後，裡面的黑色組織並沒有死去...相反地，牠們開始瘋狂增生、融合！\n\n幾秒鐘之內，一顆巨大且不斷跳動的【血肉心臟】在原地誕生了！牠正在向四周噴吐出令人作嘔的肉塊與神經索，這座基地即將被血肉吞噬！", 
                        LetterDefOf.ThreatBig, 
                        new TargetInfo(pos, map)
                    );
                    return true; // 🔥 回傳成功
                }
            }

            if (possiblePawns.Count > 0)
            {
                string chosenPawnName = possiblePawns.RandomElement();
                PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(chosenPawnName);

                if (kindDef != null)
                {
                    Pawn monster = PawnGenerator.GeneratePawn(kindDef, Faction.OfEntities);
                    GenSpawn.Spawn(monster, pos, map);
                    // 🔥 修正：使用官方正確的變數名稱 Manhunter (獵殺人類)
                    monster.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);

                    Find.LetterStack.ReceiveLetter(
                        "異象降臨：第二階段！", 
                        $"這具偽人的皮囊徹底崩解，但這並不是結束...\n\n一頭【{monster.Label}】撕開了血肉殘骸，從裡面爬了出來！這才是牠真正的面目！", 
                        LetterDefOf.NegativeEvent, 
                        monster
                    );
                    return true; // 🔥 回傳成功
                }
            }
            
            return false; // 🔥 什麼都沒生出來，回傳失敗
        }


    }
}