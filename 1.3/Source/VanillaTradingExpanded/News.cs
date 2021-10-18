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