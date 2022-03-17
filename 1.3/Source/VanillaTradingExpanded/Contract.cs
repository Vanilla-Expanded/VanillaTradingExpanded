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
        public ThingDef item;
        public ThingDef stuff;
        public int amount;
        public int creationTick;
        public int expiresInTicks;
        public int arrivesInTicks;
        public Map mapToTakeItems;
        public string Name
        {
            get
            {
                var name = "x" + BaseName;
                return name;
            }
        }

        public string BaseName
        {
            get
            {
                var name = amount + " ";
                if (stuff != null)
                {
                    name += stuff.label + " ";
                }
                name += item.label;
                return name;
            }
        }
        public float BaseMarketValue => item.GetStatValueAbstract(StatDefOf.MarketValue, stuff) * amount;

        public void GenerateItem(FloatRange targetMarketValue)
        {
            int tries = 0;
            while (tries < 100)
            {
                tries++;
                Reset();
                item = Utils.cachedItemsForContracts.RandomElement();
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

        private static float[] range;
        public void GenerateReward()
        {
            if (range is null)
            {
                range = new float[1000];
                for (var i = 0; i < 1000; i++)
                {
                    range[i] = Rand.Range(1f, 10f);
                }
            }
            this.reward = (int)(this.BaseMarketValue * range.RandomElementByWeight(x => markupCurve.Evaluate(x)));
        }

        public SimpleCurve markupCurve = new SimpleCurve
        {
            {1f, 1f },
            {2f, 0.9f },
            {3f, 0.5f },
            {4f, 0.3f },
            {5f,  0.15f },
            {6f,  0.07f },
            {7f, 0.004f },
            {8f,  0.001f },
            {9f, 0.0001f },
            {10f,  0.0001f },
        };
        public void Reset()
        {
            item = null;
            stuff = null;
            amount = 0;
        }
        public List<Thing> FoundItemsInMap(Map map)
        {
            var candidates = (this.stuff != null ? map.listerThings.ThingsOfDef(this.item).Where(x => x.Stuff == this.stuff) : map.listerThings.ThingsOfDef(this.item)).Where(x => x.IsInAnyStorage()).ToList();
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
        public void ExposeData()
        {
            Scribe_Values.Look(ref reward, "reward");
            Scribe_Defs.Look(ref item, "item");
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_Values.Look(ref amount, "amount");
            Scribe_Values.Look(ref creationTick, "creationTick");
            Scribe_Values.Look(ref expiresInTicks, "expiredInTicks");
            Scribe_Values.Look(ref arrivesInTicks, "arrivesInTicks");
            Scribe_References.Look(ref mapToTakeItems, "mapToTakeItems");
        }
    }
}