using RimWorld;
using System.Collections.Generic;
using Verse;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    public class News : IExposable
    {
        public string text;
        public int creationTick;
        public float priceImpact;
        public int priceImpactStartTick;
        public NewsDef def;
        public NewsContext newsContext;
        public string Date => GenDate.DateFullStringAt(creationTick, Find.WorldGrid.LongLatOf(Find.CurrentMap?.Tile ?? Find.Maps.FirstOrFallback().Tile));
        
        public HashSet<ThingDef> AffectedThingDefs()
        {
            var thingDefs = new HashSet<ThingDef>();
            if (this.def.thingCategories != null)
            {
                foreach (var category in this.def.thingCategories)
                {
                    thingDefs.AddRange(category.DescendantThingDefs);
                }
            }
            if (this.def.thingDefs != null)
            {
                thingDefs.AddRange(this.def.thingDefs);
            }
            if (def.tradeTags != null)
            {
                foreach (var tag in def.tradeTags)
                {
                    if (Utils.itemsByTradeTags.TryGetValue(tag, out var list))
                    {
                        thingDefs.AddRange(list);
                    }
                }
            }

            if (def.thingSetMakerTags != null)
            {
                foreach (var tag in def.thingSetMakerTags)
                {
                    if (Utils.itemsByThingSetMakerTags.TryGetValue(tag, out var list))
                    {
                        thingDefs.AddRange(list);
                    }
                }
            }

            return thingDefs;
        }

        public bool MatchesCategory(ThingCategoryDef categoryDef)
        {
            return this.def.thingCategories?.Contains(categoryDef) == true || this.def.thingDefs?.Any(thingDef => thingDef?.thingCategories?.Contains(categoryDef) ?? false) == true;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref creationTick, "creationTick");
            Scribe_Values.Look(ref priceImpact, "priceImpact");
            Scribe_Values.Look(ref priceImpactStartTick, "priceImpactStartTick");
            Scribe_Defs.Look(ref def, "def");
            Scribe_Deep.Look(ref newsContext, "newsContext");
        }
    }
}