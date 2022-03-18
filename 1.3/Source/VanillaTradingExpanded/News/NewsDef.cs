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
    public class NewsDef : Def
    {
        public RulePackDef textRulePack;
        public List<ThingCategoryDef> thingCategories;
        public List<ThingDef> thingDefs;
        public List<string> thingSetMakerTags;
        public List<string> tradeTags;
        public FloatRange priceImpactRandomInRange;
        public IntRange priceImpactTicksDelay;
        public Type newsWorker = typeof(NewsWorker);
        [Unsaved(false)]
        private NewsWorker workerInt;
        public NewsWorker Worker
        {
            get
            {
                if (workerInt == null)
                {
                    workerInt = (NewsWorker)Activator.CreateInstance(newsWorker);
                }
                return workerInt;
            }
        }

        public float commonality;
    }
}
