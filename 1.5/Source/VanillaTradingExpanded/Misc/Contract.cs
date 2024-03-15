using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;
using Verse;

namespace VanillaTradingExpanded
{
    public class Contract : IExposable
    {
        public int reward;
        public float rewardAsFloat;
        public ThingDef item;
        public ThingDef stuff;
        public int amount;
        public int expiresInTicks;
        public int arrivesInTicks;
        public Map mapToTakeItems;
        public string Name => "x" + BaseName;
        public string BaseName => amount + " " + ItemName;
        public string ItemName
        {
            get
            {
                var name = "";
                if (stuff != null)
                {
                    name += stuff.label + " ";
                }
                name += item.label;
                return name;
            }
        }
        public float BaseMarketValue => item.GetStatValueAbstract(StatDefOf.MarketValue, stuff) * amount;
        public void GenerateItem()
        {
            var baseMap = Find.AnyPlayerHomeMap;
            var wealth = baseMap.wealthWatcher.WealthTotal;
            var targetMarketValue = new FloatRange(Mathf.Min(2000, wealth * 0.01f),
                Mathf.Min(VanillaTradingExpandedMod.settings.maximumMarketValueOfItemsInContracts,
                wealth * VanillaTradingExpandedMod.settings.maximumMarketValueOfItemsPerPlayerWealth));
            int tries = 0;
            while (tries < 100)
            {
                tries++;
                Reset();
                item = VanillaTradingExpandedMod.settings.contractBlackListCollectibles ?
                    Utils.craftableOrCollectableItemsBlacklistCollectibles.RandomElement() :
                    Utils.craftableOrCollectableItems.RandomElement();

                stuff = GenStuff.RandomStuffFor(item);
                amount = Mathf.Max(1, (int)(targetMarketValue.RandomInRange / item.GetStatValueAbstract(StatDefOf.MarketValue, stuff)));
                if (targetMarketValue.Includes(BaseMarketValue))
                {
                    break;
                }
            }
            // here we just rounding up amounts so instead of 3421 we get 3500
            if (amount > 500)
            {
                amount = (int)((Math.Round(amount / 100f)) * 100f);
            }
            else if (amount > 250)
            {
                amount = (int)((Math.Round(amount / 50f)) * 50f);
            }
            else if (amount > 10)
            {
                amount = (int)((Math.Round(amount / 5f)) * 5f);
            }
        }
        public void GenerateReward()
        {
            this.reward = (int)(this.BaseMarketValue * Utils.markupRange.RandomElementByWeight(x => Utils.markupCurve.Evaluate(x)));
        }

        public void Reset()
        {
            item = null;
            stuff = null;
            amount = 0;
        }
        public List<Thing> FoundItemsInMap(Map map)
        {
            var candidates = (this.stuff != null ? map.listerThings.ThingsOfDef(this.item).Where(x => x.Stuff == this.stuff) :
                map.listerThings.ThingsOfDef(this.item)).Where(x => x.IsInAnyStorage() || map.areaManager.Home[x.Position]).ToList();
            var things = new List<Thing>();
            var num = 0;
            foreach (var thing in candidates)
            {
                things.Add(thing);
                num += thing.stackCount;
                if (num >= this.amount)
                {
                    return things;
                }
            }
            return things;
        }

        public Thing MakeItem()
        {
            if (this.item.race != null)
            {
                var pawnKindDef = DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault(x => x.race == this.item);
                var pawn = PawnGenerator.GeneratePawn(pawnKindDef, Faction.OfPlayer);
                return pawn;
            }
            else
            {
                var thing = ThingMaker.MakeThing(this.item, this.stuff);
                thing.stackCount = Mathf.Min(this.item.stackLimit, this.amount);
                return thing;
            }
        }

        public float ContractFulfilmentChance()
        {
            var markup = (this.reward / this.BaseMarketValue) * 100f;
            var chance = (markup / this.BaseMarketValue) / 2f;
            if (Utils.nonCraftableItems.Contains(this.item))
            {
                chance /= 2f; // lowering chance for the non-craftable item to be retrieved
            }
            //Log.Message("Chance of completion: " + chance + " - " + this.Name + ", markup: " + markup + ", contract.reward: " + this.reward);
            return chance;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref reward, "reward");
            Scribe_Values.Look(ref rewardAsFloat, "rewardAsFloat");
            Scribe_Defs.Look(ref item, "item");
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref expiresInTicks, "expiredInTicks");
            Scribe_Values.Look(ref arrivesInTicks, "arrivesInTicks");
            Scribe_References.Look(ref mapToTakeItems, "mapToTakeItems");
        }
    }
}