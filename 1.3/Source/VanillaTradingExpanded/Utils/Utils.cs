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
        public static List<ThingDef> cachedItemsForContracts = new List<ThingDef>();
        public static HashSet<ThingDef> tradeableItemsToIgnore = new HashSet<ThingDef>
        {
            ThingDefOf.Silver,
        };

        public static HashSet<ThingDef> craftableItemsSpecific = new HashSet<ThingDef>
        {
            ThingDefOf.Beer
        };

        public static HashSet<ThingDef> collectableThings = new HashSet<ThingDef>();
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
                if (thingDef.tradeability != Tradeability.None && marketValue > 0 && !tradeableItemsToIgnore.Contains(thingDef))
                {
                    if (marketValue > minTradePrice)
                    {
                        minTradePrice = marketValue;
                    }
                    //Log.Message($"Adding: {thingDef}, {thingDef.tradeability}, {thingDef.GetStatValueAbstract(StatDefOf.MarketValue)}, {string.Join(", ", thingDef.tradeTags ?? new List<string>())}");
                    cachedTradeableItems.Add(thingDef);
                }
            }
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.category == ThingCategory.Item && thingDef != ThingDefOf.Silver)
                {
                    if (IsSuitableForContracts(thingDef))
                    {
                        cachedItemsForContracts.Add(thingDef);
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

            bool IsSuitableForContracts(ThingDef thingDef)
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
