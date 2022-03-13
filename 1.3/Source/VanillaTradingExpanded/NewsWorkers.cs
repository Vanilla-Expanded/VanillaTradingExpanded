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
    public class NewsContext : IExposable
    {
        public Faction faction;
        public Settlement settlement;
        public Company company;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref company, "company");
        }
    }
	public abstract class NewsWorker
    {
        public abstract NewsContext GenerateContext();
        public abstract GrammarRequest GetGrammarRequest(NewsContext context);
        public abstract void AffectPrices(News news);

        public virtual bool VisibleToPlayer(News news)
        {
            return true;
        }
	}

    public abstract class NewsWorker_TradeItemsImpact : NewsWorker
    {
        public override void AffectPrices(News news)
        {
            var priceImpactChange = Mathf.Abs(news.priceImpact);
            foreach (var thingDef in news.AffectedThingDefs())
            {
                TradingManager.Instance.AffectPrice(thingDef, news.priceImpact > 0, priceImpactChange);
            }
        }
    }

    public class NewsWorker_FactionAction : NewsWorker_TradeItemsImpact
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

    public class NewsWorker_SettlementAction : NewsWorker_TradeItemsImpact
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

    public class NewsWorker_CompanyAction : NewsWorker
    {
        public override bool VisibleToPlayer(News news)
        {
            return news.newsContext.company.playerFollowsNews;
        }
        public override void AffectPrices(News news)
        {

            if (news.priceImpact >= 0)
            {
                news.newsContext.company.currentValue *= 1f + news.priceImpact;
            }
            else
            {
                news.newsContext.company.currentValue /= 1f + Mathf.Abs(news.priceImpact);
            }
        }
        public override NewsContext GenerateContext()
        {
            return new NewsContext
            {
                company = TradingManager.Instance.companies.RandomElement()
            };
        }
        public override GrammarRequest GetGrammarRequest(NewsContext context)
        {
            var grammarRequest = default(GrammarRequest);
            grammarRequest.Rules.Add(new Rule_String("COMPANY", context.company.name));
            return grammarRequest;
        }
    }
}
