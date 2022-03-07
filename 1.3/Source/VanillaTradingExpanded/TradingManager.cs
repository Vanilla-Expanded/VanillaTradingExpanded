using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace VanillaTradingExpanded
{
    public class TradingManager : GameComponent
    {
        public static TradingManager Instance;
        // goods
        private Dictionary<ThingDef, float> priceModifiers;
        public Dictionary<ThingDef, float> thingsAffectedBySoldPurchasedMarketValue;

        private Dictionary<ThingDef, PriceHistoryAutoRecorder> priceHistoryRecorders;

        public Dictionary<ThingDef, float> previousPriceModifiers;
        public HashSet<ThingDef> cachedTradeables;
        public float minTradePrice;
        // banks
        public Dictionary<Faction, Bank> banksByFaction;
        // news
        private List<News> allNews;
        private List<News> unProcessedNews;
        public List<News> AllNews => allNews.Concat(unProcessedNews).ToList();
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
            InitVars();
            minTradePrice = float.MaxValue;
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                var marketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
                if (thingDef.tradeability != Tradeability.None && marketValue > 0 && !itemsToIgnore.Contains(thingDef))
                {
                    if (marketValue > minTradePrice)
                    {
                        minTradePrice = marketValue;
                    }
                    //Log.Message($"Adding: {thingDef}, {thingDef.tradeability}, {thingDef.GetStatValueAbstract(StatDefOf.MarketValue)}, {string.Join(", ", thingDef.tradeTags ?? new List<string>())}");
                    cachedTradeables.Add(thingDef);

                    if (!priceHistoryRecorders.ContainsKey(thingDef))
                    {
                        var recorder = new PriceHistoryAutoRecorder { thingDef = thingDef };
                        recorder.Record();
                        priceHistoryRecorders[thingDef] = recorder;
                    }
                }
            }
        }

        public void InitVars()
        {
            Instance = this;
            priceModifiers ??= new Dictionary<ThingDef, float>();
            thingsAffectedBySoldPurchasedMarketValue ??= new Dictionary<ThingDef, float>();
            previousPriceModifiers ??= new Dictionary<ThingDef, float>();
            banksByFaction ??= new Dictionary<Faction, Bank>();
            allNews ??= new List<News>();
            unProcessedNews ??= new List<News>();
            priceHistoryRecorders ??= new Dictionary<ThingDef, PriceHistoryAutoRecorder>();
            cachedTradeables ??= new HashSet<ThingDef>();
            if (Find.World != null)
            {
                RecheckBanks();
            }
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

        public PriceHistoryAutoRecorder GetRecorder(ThingDef thingDef)
        {
            return priceHistoryRecorders[thingDef];
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
                if (thingsAffectedBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] += soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] = soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
            }
        }
        public void RegisterPurchasedThing(Thing soldThing, int countToSell)
        {
            if (!itemsToIgnore.Contains(soldThing.def))
            {
                Log.Message(soldThing + " is purchased by " + countToSell);
                if (thingsAffectedBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] -= soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] = -soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
            }
        }

        public News CreateNews(NewsDef newsDef)
        {
            var context = newsDef.Worker.GenerateContext();
            var nameRequest = newsDef.Worker.GetGrammarRequest(context);
            nameRequest.Includes.Add(newsDef.textRulePack);
            return new News
            {
                text = GrammarResolver.Resolve("root", nameRequest),
                creationTick = Find.TickManager.TicksGame,
                priceImpact = newsDef.priceImpactRandomInRange.RandomInRange,
                priceImpactStartTick = Find.TickManager.TicksGame + newsDef.priceImpactTicksDelay.RandomInRange,
                affectedThingCategories = newsDef.thingCategories,
                affectedThingDefs = newsDef.thingDefs
            };
        }
        public void RegisterNews(News news)
        {
            unProcessedNews.Add(news);
        }
        public override void StartedNewGame()
        {
            InitVars();
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            InitVars();
            base.LoadedGame();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            // we tick banks here
            foreach (var kvp in banksByFaction)
            {
                kvp.Value.Tick();
            }

            // we record prices every day
            if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
            {
                foreach (var kvp in priceHistoryRecorders)
                {
                    kvp.Value.Record();
                }
            }

            // create news every 7 days in average
            if (Rand.MTBEventOccurs(7f, 60000f, 1f) )
            {
                var newsDefs = DefDatabase<NewsDef>.AllDefs.RandomElement();
                var news = CreateNews(newsDefs);
                RegisterNews(news);
            }

            // process news and do price impacts based on them
            DoPriceImpactsFromNews();

            // every 3 day 
            if (Find.TickManager.TicksGame % (GenDate.TicksPerDay * 3) == 0)
            {
                // process player transactions and do price impacts based on them
                ProcessPlayerTransactions();

                // simulate world trading, by trading 20% of whole tradeable items
                SimulateWorldTrading();
            }
            // price rebalance every year at day 1
            var localMap = Find.AnyPlayerHomeMap;
            var day = GenDate.DayOfYear(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(localMap.Tile).x);
            if (prevDay != day && day == 1)
            {
                prevDay = day;
                DoPriceRebalances();
            }
        }

        private void SimulateWorldTrading()
        {
            var affectedItems = this.cachedTradeables.InRandomOrder().Take((int)(this.cachedTradeables.Count * 0.2f)).ToList();
            foreach (var item in affectedItems)
            {
                var actions = new List<Pair<Action, float>>();
                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, false, Rand.Range(0.01f, 0.05f));
                }, 0.15f));
                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, false, Rand.Range(0.01f, 0.03f));
                }, 0.15f));
                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, false, 0.01f);
                }, 0.20f));

                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, true, 0.01f);
                }, 0.20f));
                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, true, Rand.Range(0.01f, 0.03f));
                }, 0.15f));
                actions.Add(new Pair<Action, float>(delegate
                {
                    AffectPrice(item, true, Rand.Range(0.01f, 0.05f));
                }, 0.15f));

                if (actions.TryRandomElementByWeight(x => x.Second, out var result))
                {
                    result.First();
                }
            }
        }
        private void DoPriceImpactsFromNews()
        {
            for (int num = unProcessedNews.Count - 1; num >= 0; num--)
            {
                var news = unProcessedNews[num];
                if (Find.TickManager.TicksGame >= news.priceImpactStartTick)
                {
                    allNews.Add(news);
                    unProcessedNews.RemoveAt(num);
                    var priceImpactChange = Mathf.Abs(news.priceImpact);
                    foreach (var thingDef in news.AffectedThingDefs())
                    {
                        AffectPrice(thingDef, news.priceImpact > 0, priceImpactChange);
                    }
                    var window = Find.WindowStack.WindowOfType<Window_MarketPrices>();
                    if (window != null)
                    {
                        window.SetDirty();
                    }
                }
            }
        }
        private void ProcessPlayerTransactions()
        {
            previousPriceModifiers.Clear();
            foreach (var key in priceModifiers.Keys.ToList())
            {
                previousPriceModifiers.Add(key, priceModifiers[key]);
            }
            foreach (var priceModifierKvp in thingsAffectedBySoldPurchasedMarketValue)
            {
                var chance = Math.Abs(priceModifierKvp.Value / 10f) / 100f;
                Log.Message($"Chance for {priceModifierKvp.Key} is {chance}. amount of spent silver is {priceModifierKvp.Value}");
                if (Rand.Chance(chance))
                {
                    Log.Message($"Success: Chance for {priceModifierKvp.Key} is {chance}. amount of spent silver is {priceModifierKvp.Value}");
                    AffectPrice(priceModifierKvp.Key, priceModifierKvp.Value > 0, Rand.Range(0.01f, 0.1f));
                }
            }
            thingsAffectedBySoldPurchasedMarketValue.Clear();
            StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = true;
            foreach (var data in priceModifiers)
            {
                Log.Message("Price modifier: " + data.Key + " - " + data.Value + " - base market value: " + data.Key.GetStatValueAbstract(StatDefOf.MarketValue));
            }
            StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = false;
        }
        private void DoPriceRebalances()
        {
            StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = true;
            foreach (var key in priceModifiers.Keys.ToList())
            {
                var diff = priceModifiers[key] - key.GetStatValueAbstract(StatDefOf.MarketValue);
                var change = Rand.Range(0.01f, 0.1f);
                AffectPrice(key, diff > 0, change);
            }
            StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = false;
        }

        private int prevDay;
        private void AffectPrice(ThingDef thingDef, bool priceIncrease, float priceImpactChange)
        {
            Log.Message("Affecing price of " + thingDef + ", priceIncrease: " + priceIncrease + ", priceImpactChange: " + priceImpactChange);
            var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
            if (priceModifiers.ContainsKey(thingDef))
            {
                if (priceIncrease)
                {
                    priceModifiers[thingDef] *= 1f + priceImpactChange;
                }
                else
                {
                    priceModifiers[thingDef] /= 1f + priceImpactChange;
                }
            }
            else
            {
                if (priceIncrease)
                {
                    priceModifiers[thingDef] = baseMarketValue * (1f + priceImpactChange);
                }
                else
                {
                    priceModifiers[thingDef] = baseMarketValue / (1f + priceImpactChange);
                }
            }
            Log.ResetMessageCount();
            if (priceModifiers[thingDef] < 0.0000001f)
            {
                priceModifiers[thingDef] = 0.0000001f;
            }
            Log.Message(thingDef + " - new price: " + priceModifiers[thingDef]);
        }

        public bool TryGetModifiedPriceFor(ThingDef thingDef, out float price)
        {
            if (this.priceModifiers is null)
            {
                InitVars();
            }
            return this.priceModifiers.TryGetValue(thingDef, out price);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref priceModifiers, "priceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys1, ref floatValues1);
            Scribe_Collections.Look(ref thingsAffectedBySoldPurchasedMarketValue, "playerTransactionsBySoldPurchasedMarketValue", LookMode.Def, LookMode.Value, ref thingDefsKeys2, ref floatValues2);
            Scribe_Collections.Look(ref previousPriceModifiers, "previousPriceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys3, ref floatValues3);
            Scribe_Collections.Look(ref banksByFaction, "banksByFaction", LookMode.Reference, LookMode.Deep, ref factionKeys, ref bankValues);
            Scribe_Collections.Look(ref allNews, "allNews");
            Scribe_Collections.Look(ref unProcessedNews, "unProcessedNews");
            Scribe_Values.Look(ref prevDay, "prevDay");
            Scribe_Collections.Look(ref priceHistoryRecorders, "priceHistoryRecorders", LookMode.Def, LookMode.Deep);

            InitVars();
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