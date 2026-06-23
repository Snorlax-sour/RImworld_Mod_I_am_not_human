using RimWorld;
using Verse;
using System.Collections.Generic;

namespace PseudoHumanMod
{
    public static class AnomalyEntitySpawner
    {
        public static bool TrySpawnEntity(IntVec3 pos, Map map)
        {
            if (!ModsConfig.AnomalyActive || !PseudoHumanCore.settings.spawnEntityOnDeath) return false;

            List<string> possiblePawns = new List<string>();

            // 檢查圖鑑是否解鎖
            void AddIfDiscovered(string pawnName, string codexName, bool isAllowed)
            {
                if (!isAllowed) return;
                EntityCodexEntryDef codexDef = DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail(codexName);
                if (codexDef != null && Find.EntityCodex.Discovered(codexDef))
                {
                    possiblePawns.Add(pawnName);
                }
            }

            // ==========================================
            // 🩸 基礎與進階怪物
            // ==========================================
            bool bas = PseudoHumanCore.settings.allowBasicEntities; 
            AddIfDiscovered("Fingerspike", "Fleshbeasts", bas); 
            AddIfDiscovered("Toughspike", "Fleshbeasts", bas);  
            AddIfDiscovered("Trispike", "Fleshbeasts", bas);    
            AddIfDiscovered("Bulbfreak", "Fleshbeasts", bas);   
            AddIfDiscovered("Gorehulk", "Gorehulk", bas);       
            AddIfDiscovered("Sightstealer", "Sightstealer", bas); 

            bool adv = PseudoHumanCore.settings.allowAdvancedEntities;
            AddIfDiscovered("Chimera", "Chimera", adv);      
            AddIfDiscovered("Devourer", "Devourer", adv);    
            AddIfDiscovered("Noctol", "Noctol", adv);        
            AddIfDiscovered("Dreadmeld", "Dreadmeld", adv);  

            AddIfDiscovered("Metalhorror", "Metalhorror", PseudoHumanCore.settings.allowMetalhorror);
            AddIfDiscovered("Revenant", "Revenant", PseudoHumanCore.settings.allowRevenant);

            // ==========================================
            // 🎁 黃金立方體
            // ==========================================
            bool canDropCube = PseudoHumanCore.settings.allowGoldenCube && Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("GoldenCube"));
            if (canDropCube && Rand.Chance(0.1f))
            {
                Thing cube = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamedSilentFail("GoldenCube"));
                GenSpawn.Spawn(cube, pos, map);
                Messages.Message("一塊散發著誘人光芒的黃金立方體，從偽人的殘骸中滾了出來...", new TargetInfo(pos, map), MessageTypeDefOf.NeutralEvent, false);
                return true; 
            }

            // ==========================================
            // 🎱 痛苦之球 (測試機率 1.0f)
            // ==========================================
            bool canSpawnNociosphere = PseudoHumanCore.settings.allowNociosphere && Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("Nociosphere"));
            // 🔥 你可以先用 1.0f 測試，測試完記得改回 0.1f 喔！
            if (canSpawnNociosphere && Rand.Chance(0.1f))
            {
                 // 🔥 真正的修正：使用 PawnKindDef 和 PawnGenerator 生成！
                PawnKindDef nocioKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Nociosphere");
                if (nocioKind != null)
                {
                    PawnGenerationRequest request = new PawnGenerationRequest(
                        nocioKind, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, 1f, false, true, false
                    );
                    
                    Pawn nociosphere = PawnGenerator.GeneratePawn(request);
                    GenSpawn.Spawn(nociosphere, pos, map);
                    
                    // 痛苦之球有自己的特殊 AI 狀態機，不需要強加 Manhunter
                    Find.LetterStack.ReceiveLetter("異象降臨：痛苦之球！", "這具偽人的皮囊徹底崩解，從中浮現出的竟然是一顆充斥著無盡痛苦的暗黑球體！這將是一場毀滅性的災難！", LetterDefOf.ThreatBig, new TargetInfo(pos, map));
                    return true; 
                }
            }

            // ==========================================
            // 🥩 血肉心臟
            // ==========================================
            bool canSpawnHeart = PseudoHumanCore.settings.allowFleshmassHeart && Find.EntityCodex.Discovered(DefDatabase<EntityCodexEntryDef>.GetNamedSilentFail("FleshmassHeart"));
            if (canSpawnHeart && Rand.Chance(0.1f))
            {
                ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail("FleshmassHeart");
                if (heartDef != null)
                {
                    Thing heart = ThingMaker.MakeThing(heartDef);
                    GenSpawn.Spawn(heart, pos, map);
                    Find.LetterStack.ReceiveLetter("異象降臨：血肉心臟！", "這具偽人的皮囊崩解後，裡面的黑色組織並沒有死去...相反地，牠們開始瘋狂增生、融合！\n\n幾秒鐘之內，一顆巨大且不斷跳動的【血肉心臟】在原地誕生了！", LetterDefOf.ThreatBig, new TargetInfo(pos, map));
                    return true; 
                }
            }

            // ==========================================
            // 👾 生成普通怪物 (防崩潰安全版 + 黃信不暫停)
            // ==========================================
            if (possiblePawns.Count > 0)
            {
                string chosenPawnName = possiblePawns.RandomElement();
                PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(chosenPawnName);

                if (kindDef != null)
                {
                    // 🔥 必須使用 Request 包裝，否則生成無腦怪物時遊戲會當機！
                    PawnGenerationRequest request = new PawnGenerationRequest(
                        kindDef, Faction.OfEntities, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, 1f, false, true, false
                    );
                    
                    Pawn monster = PawnGenerator.GeneratePawn(request);
                    GenSpawn.Spawn(monster, pos, map);
                    monster.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);

                    // 🔥 使用 ThreatSmall (黃信)，這樣玩家在機槍掃射時就不會被強制暫停了！
                    Find.LetterStack.ReceiveLetter("異象降臨：第二階段！", $"這具偽人的皮囊徹底崩解，但這並不是結束...\n\n一頭【{monster.Label}】撕開了血肉殘骸，從裡面爬了出來！這才是牠真正的面目！", LetterDefOf.ThreatSmall, monster);
                    return true; 
                }
            }
            
            return false; 
        }
    }
}