using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VanillaTradingExpanded
{
    [StaticConstructorOnStartup]
    public static class Utils
    {
        [DebugAction("General", "Spawn 1x news", allowedGameStates = AllowedGameStates.Playing)]
        private static void Spawn1xNews()
        {
            var newsDefs = DefDatabase<NewsDef>.AllDefs.RandomElementByWeight(x => x.commonality);
            var news = TradingManager.Instance.CreateNews(newsDefs);
            TradingManager.Instance.RegisterNews(news);
        }
        [DebugAction("General", "Spawn 10x news", allowedGameStates = AllowedGameStates.Playing)]
        private static void Spawn10xNews()
        {
            for (var i = 0; i < 10; i++)
            {
                var newsDefs = DefDatabase<NewsDef>.AllDefs.RandomElementByWeight(x => x.commonality);
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

        public static HashSet<ThingDef> craftableOrCollectableItems = new HashSet<ThingDef>();
        public static HashSet<ThingDef> nonCraftableItems = new HashSet<ThingDef>();
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
        static Utils()
        {
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
                }
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
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (IsSuitableForContract(thingDef))
                {
                    if (IsCraftableOrCollectable(thingDef))
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
                    if (!DebugThingPlaceHelper.IsDebugSpawnable(thingDef))
                    {
                        return false;
                    }
                    return true;
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
