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

        public virtual void OnCreate(News news) { }
        public abstract bool CanOccur();
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
                faction = GetFaction()
            };
        }
        private static Faction GetFaction()
        {
            if (Find.FactionManager.AllFactions.Where(x => !x.defeated && !x.Hidden && x.def.humanlikeFaction && !x.IsPlayer).TryRandomElement(out var faction))
            {
                return faction;
            }
            if (Find.FactionManager.AllFactions.Where(x => x.def.humanlikeFaction && !x.IsPlayer).TryRandomElement(out var faction2))
            {
                return faction2;
            }
            return null;
        }

        public override GrammarRequest GetGrammarRequest(NewsContext context)
        {
            var grammarRequest = default(GrammarRequest);
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION", context.faction, grammarRequest.Constants));
            return grammarRequest;
        }

        public override bool CanOccur()
        {
            return GetFaction() != null;
        }
    }
    public class NewsWorker_SettlementAction : NewsWorker_TradeItemsImpact
    {
        public override NewsContext GenerateContext()
        {
            return new NewsContext
            {
                settlement = GetSettlement()
            };
        }
        public Settlement GetSettlement()
        {
            if (Find.World.worldObjects.Settlements.Where(x => !x.Faction.defeated && !x.Faction.Hidden && x.Faction.def.humanlikeFaction && !x.Faction.IsPlayer).TryRandomElement(out var settlement))
            {
                return settlement;
            }
            return Find.World.worldObjects.Settlements.RandomElement();
        }
        public override GrammarRequest GetGrammarRequest(NewsContext context)
        {
            var grammarRequest = default(GrammarRequest);
            grammarRequest.Rules.AddRange(GrammarUtility.RulesForWorldObject("SETTLEMENT", context.settlement));
            return grammarRequest;
        }

        public override bool CanOccur()
        {
            return GetSettlement() != null;
        }
    }

    public class NewsWorker_CompanyAction : NewsWorker
    {
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

        public override bool CanOccur()
        {
            return TradingManager.Instance.companies.Any();
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

        public override void OnCreate(News news)
        {
            base.OnCreate(news);
            if (news.newsContext.company.playerFollowsNews)
            {
                Messages.Message(news.text, MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}
