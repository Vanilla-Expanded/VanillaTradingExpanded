using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
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
        public Dictionary<ThingDef, float> thingsAffectedBySoldPurchasedMarketValue;

        public Dictionary<ThingDef, PriceHistoryAutoRecorderThing> priceHistoryRecorders;
        // banks
        public Dictionary<Faction, Bank> banksByFaction;
        public List<Bank> Banks => banksByFaction.Values.ToList();
        // news
        private List<News> allNews;
        private List<News> unProcessedNews;
        public List<News> AllNews => allNews.Concat(unProcessedNews).ToList();

        public List<Company> companies;

        public Dictionary<ThingDef, int> itemsToBeCrashedInTicks;
        public Dictionary<ThingDef, int> itemsToBeSqueezedInTicks;

        public List<Contract> playerSubmittedContracts;
        public List<Contract> npcSubmittedContracts;
        public List<Contract> npcContractsToBeCompleted;
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
            Startup();
        }

        private void Startup()
        {
            GenerateAllPriceRecorders();
            GenerateAllStartingCompanies();
            GenerateAllStartingBanks();
            GenerateAllStartingContracts();
        }

        public void GenerateAllPriceRecorders()
        {
            foreach (var thingDef in Utils.cachedTradeableItems)
            {
                if (!priceHistoryRecorders.ContainsKey(thingDef))
                {
                    var recorder = new PriceHistoryAutoRecorderThing { thingDef = thingDef };
                    recorder.RecordCurrentPrice();
                    priceHistoryRecorders[thingDef] = recorder;
                }
            }
        }
        private void GenerateAllStartingBanks()
        {
            if (Find.World != null)
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
        }

        private void GenerateAllStartingCompanies()
        {
            var orbitalTraders = DefDatabase<TraderKindDef>.AllDefs.Where(x => x.orbital).ToList();
            var companiesToGenerate = VanillaTradingExpandedMod.settings.maxCompanyCount - companies.Count;
            for (var i = 0; i < companiesToGenerate; i++)
            {
                var tradeKind = orbitalTraders.RandomElement();
                var company = new Company(GetFaction(tradeKind), tradeKind, companies.Count);
                companies.Add(company);
            }
        }

        public void GenerateAllStartingContracts()
        {
            for (var i = 0; i < VanillaTradingExpandedMod.settings.maxNPCContractCount; i++)
            {
                if (npcSubmittedContracts.Count < VanillaTradingExpandedMod.settings.maxNPCContractCount)
                {
                    npcSubmittedContracts.Add(GenerateRandomContract());
                }
            }
        }

        public Contract GenerateRandomContract()
        {
            var baseMap = Find.AnyPlayerHomeMap;
            var wealth = baseMap.wealthWatcher.WealthTotal;
            var targetMarketValue = new FloatRange(Mathf.Min(2000, wealth * 0.01f), wealth * 0.03f);
            var contract = new Contract
            {
                expiresInTicks = Find.TickManager.TicksGame + (GenDate.TicksPerDay * Rand.Range(15, 60))
            };
            contract.GenerateItem(targetMarketValue);
            contract.GenerateReward();
            //Log.Message("Target targetMarketValue: " + targetMarketValue + ", current wealth: " + wealth + 
            //    ", generated contract: " + contract.Name + ", base market value: " + contract.BaseMarketValue + ", rate: " + contract.BaseMarketValue / targetMarketValue.Average);
            return contract;
        }
        public void CompleteContract(Contract contract)
        {
            npcSubmittedContracts.Remove(contract);
            npcContractsToBeCompleted.Add(contract);
            contract.arrivesInTicks = Find.TickManager.TicksGame + (GenDate.TicksPerDay * 3);
            Find.WindowStack.Add(new Dialog_MessageBox("VTE.ContractCompletedMessage".Translate()));
        }
        private Faction GetFaction(TraderKindDef trader)
        {
            if (trader.faction == null)
            {
                return null;
            }
            if (!Find.FactionManager.AllFactions.Where((Faction f) => f.def == trader.faction).TryRandomElement(out var result))
            {
                return null;
            }
            return result;
        }
        public void InitVars()
        {
            Instance = this;
            priceModifiers ??= new Dictionary<ThingDef, float>();
            thingsAffectedBySoldPurchasedMarketValue ??= new Dictionary<ThingDef, float>();
            banksByFaction ??= new Dictionary<Faction, Bank>();
            allNews ??= new List<News>();
            unProcessedNews ??= new List<News>();
            priceHistoryRecorders ??= new Dictionary<ThingDef, PriceHistoryAutoRecorderThing>();
            itemsToBeCrashedInTicks ??= new Dictionary<ThingDef, int>();
            itemsToBeSqueezedInTicks ??= new Dictionary<ThingDef, int>();
            companies ??= new List<Company>();
            playerSubmittedContracts ??= new List<Contract>();
            npcSubmittedContracts ??= new List<Contract>();
            npcContractsToBeCompleted ??= new List<Contract>();
        }

        public PriceHistoryAutoRecorder GetRecorder(ThingDef thingDef)
        {
            return priceHistoryRecorders[thingDef];
        }
        public void RegisterSoldThing(Thing soldThing, int countToSell)
        {
            if (!Utils.tradeableItemsToIgnore.Contains(soldThing.def))
            {
                //Log.Message("before thingsAffectedBySoldPurchasedMarketValue: " + soldThing.def + " - " 
                    //+ thingsAffectedBySoldPurchasedMarketValue.TryGetValue(soldThing.def, out var test));
                var totalValue = soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                //Log.Message(soldThing + " is sold by " + countToSell + " with " + totalValue + " base market value: " + soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue));
                if (thingsAffectedBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] += totalValue;
                }
                else
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] = totalValue;
                }
                //Log.Message("thingsAffectedBySoldPurchasedMarketValue: " + soldThing.def + " - " + thingsAffectedBySoldPurchasedMarketValue[soldThing.def]);
            }
        }
        public void RegisterPurchasedThing(Thing soldThing, int countToSell)
        {
            if (!Utils.tradeableItemsToIgnore.Contains(soldThing.def))
            {
                //Log.Message(soldThing + " is purchased by " + countToSell);
                if (thingsAffectedBySoldPurchasedMarketValue.ContainsKey(soldThing.def))
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] -= soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                else
                {
                    thingsAffectedBySoldPurchasedMarketValue[soldThing.def] = -soldThing.def.GetStatValueAbstract(StatDefOf.MarketValue) * countToSell;
                }
                //Log.Message("thingsAffectedBySoldPurchasedMarketValue: " + soldThing.def + " - " + thingsAffectedBySoldPurchasedMarketValue[soldThing.def]);
            }
        }

        public News CreateNews(NewsDef newsDef)
        {
            var context = newsDef.Worker.GenerateContext();
            var nameRequest = newsDef.Worker.GetGrammarRequest(context);
            nameRequest.Includes.Add(newsDef.textRulePack);
            var news = new News
            {
                def = newsDef,
                text = GrammarResolver.Resolve("root", nameRequest),
                newsContext = context,
                creationTick = Find.TickManager.TicksAbs,
                priceImpact = RandomPriceImpact(newsDef.priceImpactRandomInRange) * VanillaTradingExpandedMod.settings.newsPriceImpactMultiplier,
                priceImpactStartTick = Find.TickManager.TicksGame + newsDef.priceImpactTicksDelay.RandomInRange,
            };
            newsDef.Worker.OnCreate(news);
            return news;
        }

        public float RandomPriceImpact(FloatRange floatRange)
        {
            if (floatRange.min > floatRange.max)
            {
                return Rand.Range(floatRange.max, floatRange.min);
            }
            return Rand.Range(floatRange.min, floatRange.max);
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
                    var recorder = kvp.Value;
                    recorder.RecordCurrentPrice();

                    // here we check prices for last 30 days and see how they changed. if price fell down by 25%, there is 50% chance of squeezing. and vice versa 
                    if (!itemsToBeCrashedInTicks.ContainsKey(kvp.Key) && !itemsToBeSqueezedInTicks.ContainsKey(kvp.Key) && (!recorder.squeezeOccured || !recorder.crashOccured))
                    {
                        var priceIn30Days = recorder.GetPriceInPreviousDays(30, false);
                        if (priceIn30Days != -1f)
                        {
                            var pct = recorder.records.Last() / priceIn30Days;
                            if (!recorder.squeezeOccured && pct >= 1.25f)
                            {
                                if (Rand.Bool)
                                {
                                    recorder.squeezeOccured = true;
                                    AffectPrice(kvp.Key, true, Rand.Range(0.01f, 1f), true);
                                    itemsToBeCrashedInTicks[kvp.Key] = Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay, GenDate.TicksPerDay * 2);
                                }
                            }
                            else if (!recorder.crashOccured && pct <= 0.75f)
                            {
                                if (Rand.Bool)
                                {
                                    recorder.crashOccured = true;
                                    AffectPrice(kvp.Key, false, Rand.Range(0.01f, 1f), true);
                                    itemsToBeSqueezedInTicks[kvp.Key] = Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay, GenDate.TicksPerDay * 2);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var key in itemsToBeCrashedInTicks.Keys.ToList())
            {
                if (Find.TickManager.TicksGame > itemsToBeCrashedInTicks[key])
                {
                    if (!priceHistoryRecorders[key].crashOccured)
                    {
                        AffectPrice(key, false, Rand.Range(0.01f, 1f), true);
                        priceHistoryRecorders[key].crashOccured = true;
                    }
                    itemsToBeCrashedInTicks.Remove(key);
                }
            }

            foreach (var key in itemsToBeSqueezedInTicks.Keys.ToList())
            {
                if (Find.TickManager.TicksGame > itemsToBeSqueezedInTicks[key])
                {
                    if (!priceHistoryRecorders[key].squeezeOccured)
                    {
                        AffectPrice(key, true, Rand.Range(0.01f, 1f), true);
                        priceHistoryRecorders[key].squeezeOccured = true;
                    }
                    itemsToBeSqueezedInTicks.Remove(key);
                }
            }

            // process every 3 day 
            if (Find.TickManager.TicksGame % (GenDate.TicksPerDay * 3) == 0)
            {
                // process player transactions and do price impacts based on them
                ProcessPlayerTransactions();
            
                // handles prices on seasonal items, such as food
                SeasonalPriceUpdates();
            
                // simulate world trading, by trading 20% of whole tradeable items
                SimulateWorldTrading();
            }

            // create news every 3 days in average (default)
            if (Rand.MTBEventOccurs(VanillaTradingExpandedMod.settings.newsSpawnRate, 60000f, 1f))
            {
                var newsDefs = DefDatabase<NewsDef>.AllDefs.Where(x => x.CanOccur).RandomElementByWeight(x => x.commonality);
                var news = CreateNews(newsDefs);
                RegisterNews(news);
            }

            // process news and do price impacts based on them
            DoPriceImpactsFromNews();

            // price rebalance every year at day 1
            var localMap = Find.AnyPlayerHomeMap;
            var day = GenDate.DayOfYear(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(localMap.Tile).x);
            if (prevDay != day && day == 1)
            {
                prevDay = day;
                DoPriceRebalances();
            }
            ProcessContracts(localMap);
        }

        private void ProcessContracts(Map localMap)
        {
            // iterate over completed npc contracts and make sure to spawn caravans to pick up things
            for (var i = npcContractsToBeCompleted.Count - 1; i >= 0; i--)
            {
                var contract = npcContractsToBeCompleted[i];
                if (Find.TickManager.TicksGame > contract.arrivesInTicks)
                {
                    var map = contract.mapToTakeItems ?? localMap;
                    var parms = StorytellerUtility.DefaultParmsNow(VTE_DefOf.VTE_CaravanArriveForItems.category, map);
                    IncidentWorker_CaravanArriveForItems.contract = contract;
                    if (VTE_DefOf.VTE_CaravanArriveForItems.Worker.TryExecute(parms))
                    {
                        npcContractsToBeCompleted.RemoveAt(i);
                    }
                    IncidentWorker_CaravanArriveForItems.contract = null;
                }
            }

            bool checkIntervalThisTick = Find.TickManager.TicksGame % 30000 == 0;
            for (var i = npcSubmittedContracts.Count - 1; i >= 0; i--)
            {
                var contract = npcSubmittedContracts[i];
                if (Find.TickManager.TicksGame > contract.expiresInTicks)
                {
                    npcSubmittedContracts.RemoveAt(i);
                }
                else if (checkIntervalThisTick)
                {
                    if (Rand.Chance(contract.ContractFulfilmentChance() * VanillaTradingExpandedMod.settings.npcContractFulfilmentMultiplier))
                    {
                        npcSubmittedContracts.RemoveAt(i); 
                        if (npcSubmittedContracts.Count < VanillaTradingExpandedMod.settings.maxNPCContractCount)
                        {
                            npcSubmittedContracts.Add(GenerateRandomContract());
                        }
                    }
                }
            }

            for (var i = playerSubmittedContracts.Count - 1; i >= 0; i--)
            {
                var contract = playerSubmittedContracts[i];
                if (Find.TickManager.TicksGame > contract.expiresInTicks)
                {
                    playerSubmittedContracts.Remove(contract);
                }
                else if (checkIntervalThisTick)
                {
                    if (Rand.Chance(contract.ContractFulfilmentChance() * VanillaTradingExpandedMod.settings.playerContractFulfilmentMultiplier))
                    {
                        Find.WindowStack.Add(new Window_PerformTransactionCosts("VTE.PayMoneyForContract".Translate(contract.Name), new TransactionProcess
                        {
                            transactionCost = contract.reward,
                            postTransactionAction = delegate
                            {
                                var things = new List<Thing>();
                                while (contract.amount > 0)
                                {
                                    var thing = contract.MakeItem();
                                    contract.amount -= thing.stackCount;
                                    things.Add(thing);
                                }
                                IntVec3 intVec = DropCellFinder.TradeDropSpot(localMap);
                                DropPodUtility.DropThingsNear(intVec, localMap, things, 110, canInstaDropDuringInit: false, leaveSlag: true);
                                var list = "";
                                foreach (var thing in things)
                                {
                                    list += "x" + thing.stackCount + " " + thing.LabelNoCount + "\n";
                                }
                                Find.LetterStack.ReceiveLetter("VTE.ContractFullfilled".Translate(), "VTE.ContractFullfilledDesc".Translate() + list.TrimEndNewlines(), LetterDefOf.PositiveEvent, things);
                            },
                            postCloseAction = delegate
                            {
                                playerSubmittedContracts.Remove(contract);
                            },
                        }));
                    }
                }
            }
        }

        private void SeasonalPriceUpdates()
        {
            var baseMap = Find.AnyPlayerHomeMap;
            if (baseMap != null)
            {
                var season = GenLocalDate.Season(baseMap);
                var actions = new List<Pair<Action, float>>();
                if (season == Season.Winter || season == Season.PermanentWinter)
                {
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        // do nothing
                    }, 0.70f));
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        AffectPrice(Utils.cachedFoodItems, true, () => 0.01f);
                    }, 0.20f));
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        AffectPrice(Utils.cachedFoodItems, true, () => Rand.Range(0.01f, 0.03f));
                    }, 0.10f));
                }
                else if (season == Season.Summer || season == Season.PermanentSummer)
                {
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        // do nothing
                    }, 0.70f));
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        AffectPrice(Utils.cachedFoodItems, false, () => 0.01f);
                    }, 0.20f));
                    actions.Add(new Pair<Action, float>(delegate
                    {
                        AffectPrice(Utils.cachedFoodItems, false, () => Rand.Range(0.01f, 0.03f));
                    }, 0.10f));
                }

                if (actions.TryRandomElementByWeight(x => x.Second, out var result))
                {
                    result.First();
                }
            }
        }
        private void SimulateWorldTrading()
        {
            var affectedItems = Utils.cachedTradeableItems.InRandomOrder()
                .Take((int)(Utils.cachedTradeableItems.Count * VanillaTradingExpandedMod.settings.amountOfItemsToFluctuate)).ToList();
            foreach (var item in affectedItems)
            {
                AffectPriceRandomly(item);
            }
        }

        public void AffectPriceRandomly(ThingDef item)
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

        private void DoPriceImpactsFromNews()
        {
            for (int num = unProcessedNews.Count - 1; num >= 0; num--)
            {
                var news = unProcessedNews[num];
                if (Find.TickManager.TicksGame >= news.priceImpactStartTick)
                {
                    allNews.Add(news);
                    unProcessedNews.RemoveAt(num);
                    news.def.Worker.AffectPrices(news);
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
            foreach (var priceModifierKvp in thingsAffectedBySoldPurchasedMarketValue)
            {
                var spentMoneyInTransaction = Math.Abs(priceModifierKvp.Value);
                var chance = (spentMoneyInTransaction / 10f) / 100f;
                //Log.Message($"Chance for {priceModifierKvp.Key} is {chance}. amount of spent silver is {priceModifierKvp.Value}");
                if (Rand.Chance(chance))
                {
                    var impactModifier = Mathf.Min(10f, Mathf.Max(1f, spentMoneyInTransaction / 1000f)); 
                    // if amount of silver spent is higher than 1000, start price impact change scalling up to 10x
                    //Log.Message($"Success: Chance for {priceModifierKvp.Key} is {chance}. " +
                    //    $"amount of spent silver is {priceModifierKvp.Value}, " +
                    //    $"impactModifier: {impactModifier}");
                    AffectPrice(priceModifierKvp.Key, priceModifierKvp.Value < 0, Rand.Range(0.01f, 0.1f) 
                        * impactModifier * VanillaTradingExpandedMod.settings.playerTransactionImpactMultiplier);
                }
            }
            thingsAffectedBySoldPurchasedMarketValue.Clear();
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

            foreach (var recorder in priceHistoryRecorders)
            {
                recorder.Value.squeezeOccured = false;
                recorder.Value.crashOccured = false;
            }
        }

        private int prevDay;
        public void AffectPrice(List<ThingDef> thingDefs, bool priceIncrease, Func<float> priceImpactChangeGetter)
        {
            foreach (var thingDef in thingDefs)
            {
                AffectPrice(thingDef, priceIncrease, priceImpactChangeGetter());
            }
        }

        public void AffectPrice(ThingDef thingDef, bool priceIncrease, float priceImpactChange, bool issueMessage = false)
        {
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
            if (priceModifiers[thingDef] < 0.0000001f)
            {
                priceModifiers[thingDef] = 0.0000001f; // we set minimal price to avoid situations with endless minimal prices
            }
            if (issueMessage)
            {
                if (Find.Maps.Where(x => x.IsPlayerHome).Any(x => x.listerThings.ThingsOfDef(VTE_DefOf.VTE_TradingTerminal).Any(x => x.TryGetComp<CompPowerTrader>().PowerOn)))
                {
                    if (priceIncrease)
                    {
                        Messages.Message("VTE.SqueezeMessage".Translate(thingDef.label), MessageTypeDefOf.NeutralEvent);
                    }
                    else
                    {
                        Messages.Message("VTE.CrashMessage".Translate(thingDef.label), MessageTypeDefOf.NeutralEvent);
                    }
                }
            }
            //Log.Message("Affecing price of " + thingDef + ", priceIncrease: " + priceIncrease + ", priceImpactChange: " + priceImpactChange + " - new price: " + priceModifiers[thingDef]);
            //Log.ResetMessageCount();
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
            Scribe_Collections.Look(ref itemsToBeCrashedInTicks, "itemsToBeCrashedByTicks", LookMode.Def, LookMode.Value, ref thingDefsKeys3, ref intValues1);
            Scribe_Collections.Look(ref itemsToBeSqueezedInTicks, "itemsToBeSqueezeByTicks", LookMode.Def, LookMode.Value, ref thingDefsKeys4, ref intValues2);
            Scribe_Collections.Look(ref banksByFaction, "banksByFaction", LookMode.Reference, LookMode.Deep, ref factionKeys, ref bankValues);
            Scribe_Collections.Look(ref allNews, "allNews");
            Scribe_Collections.Look(ref unProcessedNews, "unProcessedNews");
            Scribe_Values.Look(ref prevDay, "prevDay");
            Scribe_Collections.Look(ref priceHistoryRecorders, "priceHistoryRecorders", LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref companies, "companies", LookMode.Deep);
            Scribe_Collections.Look(ref playerSubmittedContracts, "playerSubmittedContracts", LookMode.Deep);
            Scribe_Collections.Look(ref npcSubmittedContracts, "npcSubmittedContracts", LookMode.Deep);
            Scribe_Collections.Look(ref npcContractsToBeCompleted, "npcContractsToBeCompleted", LookMode.Deep);
            InitVars();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                DoCleanup();
                Startup();
            }
        }

        private void DoCleanup()
        {
            companies.RemoveAll(x => x.traderKind is null);
            priceHistoryRecorders.RemoveAll(x => x.Key is null);
            playerSubmittedContracts.RemoveAll(x => x.item is null);
            npcSubmittedContracts.RemoveAll(x => x.item is null);
            npcContractsToBeCompleted.RemoveAll(x => x.item is null);
        }

        private List<ThingDef> thingDefsKeys1;
        private List<float> floatValues1;
        private List<ThingDef> thingDefsKeys2;
        private List<float> floatValues2;
        private List<Faction> factionKeys;
        private List<Bank> bankValues;

        private List<ThingDef> thingDefsKeys3;
        private List<int> intValues1;
        private List<ThingDef> thingDefsKeys4;
        private List<int> intValues2;
    }
}