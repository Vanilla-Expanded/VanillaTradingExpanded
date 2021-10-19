using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Grammar;

namespace VanillaTradingExpanded
{
    public class NewsContext
    {
        public Faction faction;
        public Settlement settlement;
    }
	public abstract class NewsWorker
    {
        public abstract NewsContext GenerateContext();

        public abstract GrammarRequest GetGrammarRequest(NewsContext context);
	}

    public class NewsWorker_FactionAction : NewsWorker
    {
        public override NewsContext GenerateContext()
        {
            return new NewsContext
            {
                faction = Find.FactionManager.AllFactions.Where(x => !x.defeated && !x.Hidden && x.def.humanlikeFaction && !x.IsPlayer).RandomElement()
            };
        }
        public override GrammarRequest GetGrammarRequest(NewsContext context)
        {
            var grammarRequest = default(GrammarRequest);
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION", context.faction, grammarRequest.Constants));
            return grammarRequest;
        }
    }

    public class NewsWorker_SettlementAction : NewsWorker
    {
        public override NewsContext GenerateContext()
        {
            return new NewsContext
            {
                settlement = Find.World.worldObjects.Settlements.Where(x => !x.Faction.defeated && !x.Faction.Hidden && x.Faction.def.humanlikeFaction && !x.Faction.IsPlayer).RandomElement()
            };
        }
        public override GrammarRequest GetGrammarRequest(NewsContext context)
        {
            var grammarRequest = default(GrammarRequest);
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForWorldObject("SETTLEMENT", context.settlement));
            return grammarRequest;
        }
    }
}
