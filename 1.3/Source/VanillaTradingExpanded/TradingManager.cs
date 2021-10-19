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
        public Dictionary<ThingDef, float> priceModifiers;
        public Dictionary<ThingDef, float> playerTransactionsBySoldPurchasedMarketValue;


        public Dictionary<ThingDef, float> previousPriceModifiers;
        public HashSet<ThingDef> cachedTradeables;

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
            cachedTradeables = new HashSet<ThingDef>();
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.tradeability != Tradeability.None && thingDef.GetStatValueAbstract(StatDefOf.MarketValue) > 0)
                {
                    //Log.Message($"Adding: {thingDef}, {thingDef.tradeability}, {thingDef.GetStatValueAbstract(StatDefOf.MarketValue)}");
                    cachedTradeables.Add(thingDef);
                }
            }
        }

        public void InitVars()
        {
            Instance = this;
            priceModifiers ??= new Dictionary<ThingDef, float>();
            playerTransactionsBySoldPurchasedMarketValue ??= new Dictionary<ThingDef, float>();
            previousPriceModifiers ??= new Dictionary<ThingDef, float>();
            banksByFaction ??= new Dictionary<Faction, Bank>();
            allNews ??= new List<News>();
            unProcessedNews ??= new List<News>();
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

        private static HashSet<ThingDef> itemsToIgnore = new HashSet<ThingDef>
        {
            ThingDefOf.Silver,
        };
        public void RegisterSoldThing(Thing soldThing, int countToSell)
        {
            if (!itemsToIgnore.Contains(soldThing.def))
            {
                Log.Message(soldThing + " is sold by " + countToSell);
                if (playerTransactionsBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    playerTransactionsBySoldPurchasedMarketValue[soldThing.def] += soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    playerTransactionsBySoldPurchasedMarketValue[soldThing.def] = soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
            }
        }
        public void RegisterPurchasedThing(Thing soldThing, int countToSell)
        {
            if (!itemsToIgnore.Contains(soldThing.def))
            {
                Log.Message(soldThing + " is purchased by " + countToSell);
                if (playerTransactionsBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    playerTransactionsBySoldPurchasedMarketValue[soldThing.def] -= soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    playerTransactionsBySoldPurchasedMarketValue[soldThing.def] = -soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
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
            // create news every 7 days in average
            if (Rand.MTBEventOccurs(7f, 60000f, 1f) )
            {
                var newsDefs = DefDatabase<NewsDef>.AllDefs.RandomElement();
                var news = CreateNews(newsDefs);
                RegisterNews(news);
            }

            // process news and do price impacts based on them
            DoPriceImpactsFromNews();

            // every 3 day process player transactions and do price impacts based on them
            if (Find.TickManager.TicksGame % (GenDate.TicksPerDay * 3) == 0)
            {
                ProcessPlayerTransactions();
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
                        AffectPrice(thingDef, news.priceImpact, priceImpactChange);
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
            foreach (var priceModifierKvp in playerTransactionsBySoldPurchasedMarketValue)
            {
                var chance = Math.Abs(priceModifierKvp.Value / 10f) / 100f;
                Log.Message($"Chance for {priceModifierKvp.Key} is {chance}. amount of spent silver is {priceModifierKvp.Value}");
                if (Rand.Chance(chance))
                {
                    Log.Message($"Success: Chance for {priceModifierKvp.Key} is {chance}. amount of spent silver is {priceModifierKvp.Value}");
                    var priceImpactChange = Rand.Range(0.01f, 0.1f);
                    AffectPrice(priceModifierKvp.Key, priceModifierKvp.Value, priceImpactChange);
                }
            }
            playerTransactionsBySoldPurchasedMarketValue.Clear();

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
                if (diff > 0)
                {
                    var change = Rand.Range(-0.1f, -0.01f);
                    AffectPrice(key, change, Mathf.Abs(change));
                }
                else if (diff < 0)
                {
                    var change = Rand.Range(0.01f, 0.1f);
                    AffectPrice(key, change, change);
                }
            }
            StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = false;
        }

        private int prevDay;
        private void AffectPrice(ThingDef thingDef, float priceImpactBase, float priceImpactChange)
        {
            Log.Message("Affecing price of " + thingDef + ", priceImpactBase: " + priceImpactBase + ", priceImpactChange: " + priceImpactChange);
            if (priceModifiers.ContainsKey(thingDef))
            {
                if (priceImpactBase > 0)
                {
                    Log.Message("1 Before: " + priceModifiers[thingDef]);
                    priceModifiers[thingDef] *= 1 + priceImpactChange;
                    Log.Message("1 After: " + priceModifiers[thingDef]);
                }
                else
                {
                    Log.Message("1 Before: " + priceModifiers[thingDef]);
                    priceModifiers[thingDef] /= 1 + priceImpactChange;
                    Log.Message("1 After: " + priceModifiers[thingDef]);
                }
            }
            else
            {
                if (priceImpactBase > 0)
                {
                    var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
                    Log.Message("2 Before: " + baseMarketValue);
                    priceModifiers[thingDef] = baseMarketValue * (1 + priceImpactChange);
                    Log.Message("2 After: " + priceModifiers[thingDef]);
                }
                else
                {
                    var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
                    Log.Message("2 Before: " + baseMarketValue);
                    priceModifiers[thingDef] = baseMarketValue / (1 + priceImpactChange);
                    Log.Message("2 After: " + priceModifiers[thingDef]);
                }
            }

            priceModifiers[thingDef] = Mathf.Min(0.11f, priceModifiers[thingDef]);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref priceModifiers, "priceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys1, ref floatValues1);
            Scribe_Collections.Look(ref playerTransactionsBySoldPurchasedMarketValue, "playerTransactionsBySoldPurchasedMarketValue", LookMode.Def, LookMode.Value, ref thingDefsKeys2, ref floatValues2);
            Scribe_Collections.Look(ref previousPriceModifiers, "previousPriceModifiers", LookMode.Def, LookMode.Value, ref thingDefsKeys3, ref floatValues3);
            Scribe_Collections.Look(ref banksByFaction, "banksByFaction", LookMode.Reference, LookMode.Deep, ref factionKeys, ref bankValues);
            Scribe_Collections.Look(ref allNews, "allNews");
            Scribe_Collections.Look(ref unProcessedNews, "unProcessedNews");
            Scribe_Values.Look(ref prevDay, "prevDay");
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