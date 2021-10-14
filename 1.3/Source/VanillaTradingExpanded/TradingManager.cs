using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VanillaTradingExpanded
{

    public class TradingManager : GameComponent
    {
        public static TradingManager Instance;

        // goods
        public Dictionary<ThingDef, float> priceModifiers;
        public Dictionary<ThingDef, float> rawUnprocessedPriceModifiers;
        public Dictionary<ThingDef, float> previousPriceModifiers;
        public HashSet<ThingDef> cachedTradeables;

        // banks
        public Dictionary<Faction, Bank> banksByFaction = new Dictionary<Faction, Bank>();
        public TradingManager()
        {
            Instance = this;
        }

        public TradingManager(Game game)
        {
            Instance = this;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            cachedTradeables = new HashSet<ThingDef>();
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.tradeability != Tradeability.None && thingDef.GetStatValueAbstract(StatDefOf.MarketValue) > 0)
                {
                    Log.Message($"Adding: {thingDef}, {thingDef.tradeability}, {thingDef.GetStatValueAbstract(StatDefOf.MarketValue)}");
                    cachedTradeables.Add(thingDef);
                }
            }
        }

        public void PreInit()
        {
            Instance = this;
            priceModifiers ??= new Dictionary<ThingDef, float>();
            rawUnprocessedPriceModifiers ??= new Dictionary<ThingDef, float>();
            previousPriceModifiers ??= new Dictionary<ThingDef, float>();
            banksByFaction ??= new Dictionary<Faction, Bank>();
            RecheckBanks();
        }

        private void RecheckBanks()
        {
            foreach (var faction in Find.FactionManager.AllFactions)
            {
                var bankExtension = faction.def.GetModExtension<BankExtension>();
                if (bankExtension != null)
                {
                    if (!banksByFaction.ContainsKey(faction))
                    {
                        banksByFaction[faction] = new Bank(faction);
                    }
                }
                else if (banksByFaction.ContainsKey(faction))
                {
                    banksByFaction.Remove(faction);
                }
            }
        }

        private static HashSet<ThingDef> itemsToIgnore = new HashSet<ThingDef>
        {
            ThingDefOf.Silver,
        };
        public void RegisterSoldThing(Thing soldThing, int countToSell)
        {
            if (!itemsToIgnore.Contains(soldThing.def))
            {
                Log.Message(soldThing + " is sold by " + countToSell);
                if (rawUnprocessedPriceModifiers.ContainsKey(soldThing.def))
                {
                    rawUnprocessedPriceModifiers[soldThing.def] += soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    rawUnprocessedPriceModifiers[soldThing.def] = soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
            }
        }
        public void RegisterPurchasedThing(Thing soldThing, int countToSell)
        {
            if (!itemsToIgnore.Contains(soldThing.def))
            {
                Log.Message(soldThing + " is purchased by " + countToSell);
                if (rawUnprocessedPriceModifiers.ContainsKey(soldThing.def))
                {
                    rawUnprocessedPriceModifiers[soldThing.def] -= soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    rawUnprocessedPriceModifiers[soldThing.def] = -soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
            }
        }


        public override void StartedNewGame()
        {
            PreInit();
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            PreInit();
            base.LoadedGame();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 180000 == 0) // every 3 day
            {
                previousPriceModifiers.Clear();
                foreach (var key in priceModifiers.Keys)
                {
                    previousPriceModifiers.Add(key, priceModifiers[key]);
                }
                foreach (var thingDefData in rawUnprocessedPriceModifiers)
                {
                    var chance = Math.Abs(thingDefData.Value / 10f) / 100f;
                    Log.Message($"Chance for {thingDefData.Key} is {chance}. amount of spent silver is {thingDefData.Value}");
                    if (Rand.Chance(chance))
                    {
                        Log.Message($"Success: Chance for {thingDefData.Key} is {chance}. amount of spent silver is {thingDefData.Value}");
                        var change = Rand.Range(0.01f, 0.1f);
                        if (priceModifiers.ContainsKey(thingDefData.Key))
                        {
                            if (thingDefData.Value > 0)
                            {
                                priceModifiers[thingDefData.Key] *= 1 + change;
                            }
                            else
                            {
                                priceModifiers[thingDefData.Key] /= 1 + change;
                            }
                        }
                        else
                        {
                            if (thingDefData.Value > 0)
                            {
                                var baseMarketValue = thingDefData.Key.GetStatValueAbstract(StatDefOf.MarketValue);
                                priceModifiers[thingDefData.Key] = baseMarketValue * (1 + change);
                            }
                            else
                            {
                                var baseMarketValue = thingDefData.Key.GetStatValueAbstract(StatDefOf.MarketValue);
                                priceModifiers[thingDefData.Key] = baseMarketValue / (1 + change);
                            }
                        }
                    }
                }
                rawUnprocessedPriceModifiers.Clear();
                StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = true;
                foreach (var data in priceModifiers)
                {
                    Log.Message("Price modifier: " + data.Key + " - " + data.Value + " - base market value: " + data.Key.GetStatValueAbstract(StatDefOf.MarketValue));
                }
                StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = false;
                var window = Find.WindowStack.WindowOfType<Window_MarketPrices>();
                if (window != null)
                {
                    window.SetDirty();
                }
            }
        }
        public void RemoveDestroyedPawn(Pawn key)
        {

        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref priceModifiers, "priceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys1, ref floatValues1);
            Scribe_Collections.Look(ref rawUnprocessedPriceModifiers, "rawUnprocessedPriceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys2, ref floatValues2);
            Scribe_Collections.Look(ref previousPriceModifiers, "previousPriceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys3, ref floatValues3);
            Scribe_Collections.Look(ref banksByFaction, "banksByFaction", LookMode.Reference, LookMode.Deep, ref factionKeys, ref bankValues);
        }

        private List<ThingDef> thingDefsKeys1;
        private List<float> floatValues1;
        private List<ThingDef> thingDefsKeys2;
        private List<float> floatValues2;
        private List<ThingDef> thingDefsKeys3;
        private List<float> floatValues3;

        private List<Faction> factionKeys;
        private List<Bank> bankValues;
    }
}