using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VanillaTradingExpanded
{
    [HarmonyPatch]
    public static class Initializer
    {
        public static MethodBase targetMethod;
        
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(x => x.Name == "BetterLoading"))
            {
                return AccessTools.Method("BetterLoading.BetterLoadingMain:CreateTimingReport");
            }
            return AccessTools.Method(typeof(StaticConstructorOnStartupUtility), "CallAll");
        }
        public static void Postfix()
        {
            Utils.Initialize();
        }
    }

    public static class Utils
    {
        [DebugAction("General", "Spawn 1x news", allowedGameStates = AllowedGameStates.Playing)]
        private static void Spawn1xNews()
        {
            var newsDefs = DefDatabase<NewsDef>.AllDefs.Where(x => x.CanOccur).RandomElementByWeight(x => x.commonality);
            var news = TradingManager.Instance.CreateNews(newsDefs);
            TradingManager.Instance.RegisterNews(news);
        }
        [DebugAction("General", "Spawn 10x news", allowedGameStates = AllowedGameStates.Playing)]
        private static void Spawn10xNews()
        {
            for (var i = 0; i < 10; i++)
            {
                var newsDefs = DefDatabase<NewsDef>.AllDefs.Where(x => x.CanOccur).RandomElementByWeight(x => x.commonality);
                var news = TradingManager.Instance.CreateNews(newsDefs);
                TradingManager.Instance.RegisterNews(news);
            }
        }

        [DebugAction("General", "Fluctuate prices of 10 items", allowedGameStates = AllowedGameStates.Playing)]
        private static void FluctuatePrices10()
        {
            var affectedItems = cachedTradeableItems.InRandomOrder().Take(10).ToList();
            foreach (var item in affectedItems)
            {
                TradingManager.Instance.AffectPriceRandomly(item);
            }
        }

        [DebugAction("General", "Fluctuate prices of 20 items", allowedGameStates = AllowedGameStates.Playing)]
        private static void FluctuatePrices20()
        {
            var affectedItems = cachedTradeableItems.InRandomOrder().Take(20).ToList();
            foreach (var item in affectedItems)
            {
                TradingManager.Instance.AffectPriceRandomly(item);
            }
        }

        [DebugAction("General", "Regenerate contracts", allowedGameStates = AllowedGameStates.Playing)]
        private static void RegenerateContracts()
        {
            TradingManager.Instance.npcSubmittedContracts.Clear();
            TradingManager.Instance.GenerateAllStartingContracts();
        }

        [DebugAction("General", "Spawn 100000 silver", allowedGameStates = AllowedGameStates.Playing)]
        private static void Spawn100000Silver()
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = 100000;
            IntVec3 intVec = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            DropPodUtility.DropThingsNear(intVec, Find.CurrentMap, new List<Thing> { silver }, 110, canInstaDropDuringInit: false, leaveSlag: true, forbid: false);
        }

        [DebugAction("General", "Add bank", allowedGameStates = AllowedGameStates.Playing)]
        private static void AddBank()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (var faction in Find.FactionManager.AllFactions.Where(x => x.def.humanlikeFaction && !x.IsPlayer
                && !TradingManager.Instance.banksByFaction.ContainsKey(x)))
            {
                list.Add(new DebugMenuOption(faction.name, DebugMenuOptionMode.Action, delegate
                {
                    TradingManager.Instance.CreateNewBank(faction, TradingManager.Instance.GetNewBankExtensionFor(faction));
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        public static HashSet<ThingDef> craftableOrCollectableItems = new HashSet<ThingDef>();
        public static HashSet<ThingDef> nonCraftableItems = new HashSet<ThingDef>();
        public static HashSet<ThingDef> animals = new HashSet<ThingDef>();
        public static HashSet<ThingDef> tradeableItemsToIgnore = new HashSet<ThingDef>
        {
            ThingDefOf.Silver,
        };

        private static HashSet<ThingDef> craftableItemsSpecific = new HashSet<ThingDef>
        {
            ThingDefOf.Beer
        };

        private static HashSet<ThingDef> collectableThings = new HashSet<ThingDef>();
        public static List<ThingDef> cachedTradeableItems = new List<ThingDef>();
        public static List<ThingDef> cachedFoodItems = new List<ThingDef>();
        public static Dictionary<string, List<ThingDef>> itemsByThingSetMakerTags = new Dictionary<string, List<ThingDef>>();
        public static Dictionary<string, List<ThingDef>> itemsByTradeTags = new Dictionary<string, List<ThingDef>>();
        public static float minTradePrice;
        public static void Initialize()
        {
            InitMarkupValues();
            minTradePrice = float.MaxValue;
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.thingSetMakerTags != null)
                {
                    foreach (var tag in thingDef.thingSetMakerTags)
                    {
                        if (!itemsByThingSetMakerTags.TryGetValue(tag, out List<ThingDef> items))
                        {
                            itemsByThingSetMakerTags[tag] = items = new List<ThingDef>();
                        }
                        items.Add(thingDef);
                    }
                }

                if (thingDef.tradeTags != null)
                {
                    foreach (var tag in thingDef.tradeTags)
                    {
                        if (!itemsByTradeTags.TryGetValue(tag, out List<ThingDef> items))
                        {
                            itemsByTradeTags[tag] = items = new List<ThingDef>();
                        }
                        items.Add(thingDef);
                    }
                }

                if (thingDef.plant?.harvestedThingDef != null)
                {
                    collectableThings.Add(thingDef.plant.harvestedThingDef);
                }
                if (thingDef.building?.mineableThing != null)
                {
                    collectableThings.Add(thingDef.building.mineableThing);
                }
                var milkableProps = thingDef.GetCompProperties<CompProperties_Milkable>();
                if (milkableProps != null)
                {
                    collectableThings.Add(milkableProps.milkDef);
                }
                var spawnerProps = thingDef.GetCompProperties<CompProperties_Spawner>();
                if (spawnerProps != null)
                {
                    collectableThings.Add(spawnerProps.thingToSpawn);
                }
                var eggLayer = thingDef.GetCompProperties<CompProperties_EggLayer>();
                if (eggLayer != null)
                {
                    collectableThings.Add(eggLayer.eggFertilizedDef);
                    collectableThings.Add(eggLayer.eggUnfertilizedDef);
                }
                var shearable = thingDef.GetCompProperties<CompProperties_Shearable>();
                if (shearable != null)
                {
                    collectableThings.Add(shearable.woolDef);
                }
                if (thingDef.race != null)
                {
                    collectableThings.Add(thingDef.race.leatherDef);
                    collectableThings.Add(thingDef.race.meatDef);
                    if (thingDef.race.Animal && !thingDef.race.Dryad)
                    {
                        animals.Add(thingDef);
                    }
                }

                try
                {
                    var marketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
                    if (CanBeSoldOrBought(thingDef, marketValue))
                    {
                        if (marketValue > minTradePrice)
                        {
                            minTradePrice = marketValue;
                        }
                        //Log.Message($"Adding: {thingDef}, thingDef.tradeability: {thingDef.tradeability}, marketValue: {marketValue}, {string.Join(", ", thingDef.tradeTags ?? new List<string>())}");
                        cachedTradeableItems.Add(thingDef);
                    }
                }
                catch (NullReferenceException exception)
                {

                }
            }
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (IsSuitableForContract(thingDef))
                {
                    if (IsCraftableOrCollectable(thingDef) && CanBeUsedInNPCContracts(thingDef))
                    {
                        craftableOrCollectableItems.Add(thingDef);
                    }
                    else
                    {
                        nonCraftableItems.Add(thingDef);
                    }
                }
            }

            foreach (var thing in cachedTradeableItems)
            {
                if (thing.IsNutritionGivingIngestible)
                {
                    cachedFoodItems.Add(thing);
                }
            }

            bool CanBeUsedInNPCContracts(ThingDef thingDef)
            {
                if (IsChunk(thingDef))
                {
                    return false;
                }
                if (thingDef == ThingDefOf.Dye) // players somehow get lots of dye contracts, so I'm excluding it
                {
                    return false;
                }
                return true;
            }
            bool CanBeSoldOrBought(ThingDef thingDef, float marketValue)
            {
                bool result = thingDef.tradeability != Tradeability.None && marketValue > 0 && !tradeableItemsToIgnore.Contains(thingDef);
                if (result)
                {
                    if (thingDef.plant != null && !thingDef.Minifiable)
                    {
                        //Log.Message("Cannot sell " + thingDef + " - " + thingDef.label);
                        return false;
                    }
                    if (thingDef.building != null && !thingDef.Minifiable)
                    {
                        //Log.Message("Cannot sell " + thingDef + " - " + thingDef.label);
                        return false;
                    }
                    return true;
                }
                return false;
            }

            bool IsSuitableForContract(ThingDef thingDef)
            {
                if (thingDef.category == ThingCategory.Item && thingDef != ThingDefOf.Silver)
                {
                    try
                    {
                        if (!DebugThingPlaceHelper.IsDebugSpawnable(thingDef))
                        {
                            return false;
                        }
                    }
                    catch (NullReferenceException exception)
                    {
                        return false; // if the thingdef is erroring like that, it's probably a weird thing, not suitable for contracts
                    }
                    if (thingDef.BaseMarketValue <= 0)
                    {
                        return false;
                    }
                    if (thingDef.tradeTags != null && thingDef.tradeTags.Contains("NonContractable")) // we skip any thing with the NonContractable tag
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            bool IsChunk(ThingDef thingDef)
            {
                if (thingDef.thingCategories != null)
                {
                    foreach (var category in thingDef.thingCategories)
                    {
                        if (category == ThingCategoryDefOf.Chunks || category == ThingCategoryDefOf.StoneChunks)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            bool IsCraftableOrCollectable(ThingDef thingDef)
            {
                foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
                {
                    if (recipe.products != null)
                    {
                        foreach (var product in recipe.products)
                        {
                            if (product.thingDef == thingDef)
                            {
                                return true;
                            }
                        }
                    }
                }
                if (craftableItemsSpecific.Contains(thingDef) || collectableThings.Contains(thingDef))
                {
                    return true;
                }
                return false;
            }
        }
        public static SimpleCurve markupCurve;
        public static float[] markupRange;
        public static void InitMarkupValues()
        {
            var maxMarkupValue = VanillaTradingExpandedMod.settings.maxMarkupOnNPCContract;
            markupRange = new float[1000];
            for (var i = 0; i < 1000; i++)
            {
                markupRange[i] = Rand.Range(1f, (float)maxMarkupValue);
            }
            markupCurve = new SimpleCurve();
            float value = 1;
            for (var i = 1; i < maxMarkupValue + 1; i++)
            {
                var curvePoint = new CurvePoint(i, value);
                markupCurve.Add(curvePoint);
                value *= 0.7f;
            }
        }
        public static string ToStringMoney(this int f, string format = null)
        {
            if (format == null)
            {
                format = ((!(f >= 10f) && f != 0f) ? "F2" : "F0");
            }
            return "MoneyFormat".Translate(f.ToString(format));
        }
        public static Bank GetBank(this Faction faction)
        {
            if (TradingManager.Instance.banksByFaction.TryGetValue(faction, out var bank))
            {
                return bank;
            }
            return null;
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
