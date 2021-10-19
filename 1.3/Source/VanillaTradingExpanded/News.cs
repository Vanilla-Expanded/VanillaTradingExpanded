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

        public List<ThingCategoryDef> affectedThingCategories;
        public List<ThingDef> affectedThingDefs;
        public string Date => GenDate.DateFullStringAt(creationTick, Find.WorldGrid.LongLatOf(Find.CurrentMap?.Tile ?? Find.Maps.FirstOrFallback().Tile));
        
        public HashSet<ThingDef> AffectedThingDefs()
        {
            var thingDefs = new HashSet<ThingDef>();
            if (affectedThingCategories != null)
            {
                foreach (var category in affectedThingCategories)
                {
                    thingDefs.AddRange(category.DescendantThingDefs);
                }
            }
            if (affectedThingDefs != null)
            {
                thingDefs.AddRange(affectedThingDefs);
            }
            return thingDefs;
        }

        public bool MatchesCategory(ThingCategoryDef categoryDef)
        {
            return affectedThingCategories?.Contains(categoryDef) == true || affectedThingDefs?.Any(thingDef => thingDef?.thingCategories?.Contains(categoryDef) ?? false) == true;
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref text, "text");
            Scribe_Values.Look(ref creationTick, "creationTick");
            Scribe_Values.Look(ref priceImpact, "priceImpact");
            Scribe_Values.Look(ref priceImpactStartTick, "priceImpactStartTick");
            Scribe_Collections.Look(ref affectedThingCategories, "affectedThingCategories", LookMode.Def);
            Scribe_Collections.Look(ref affectedThingDefs, "affectedThingDefs", LookMode.Def);
        }
    }
}