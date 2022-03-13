using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VanillaTradingExpanded
{
    public class Company : IExposable, ILoadReferenceable
    {
        public Faction faction;

        public TraderKindDef traderKind;

        public PriceHistoryAutoRecorderCompany recorder;

        public string name;

        public float currentValue;

        public List<Share> sharesHeldByPlayer;

        public bool playerFollowsNews;

        public int loadID;

        private static List<string> tmpExtantNames = new List<string>();
        public Company()
        {

        }
        public Company(Faction faction, TraderKindDef traderKind, int id)
        {
            this.faction = faction;
            this.traderKind = traderKind;
            tmpExtantNames.Clear();

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                tmpExtantNames.AddRange(maps[i].passingShipManager.passingShips.Select((PassingShip x) => x.name));
            }
            foreach (var company in TradingManager.Instance.companies)
            {
                tmpExtantNames.Add(company.name);
            }
            name = NameGenerator.GenerateName(RulePackDefOf.NamerTraderGeneral, tmpExtantNames);
            if (faction != null)
            {
                name = string.Format("{0} {1} {2}", name, "OfLower".Translate(), faction.Name);
            }
            this.loadID = id;
            this.currentValue = Rand.RangeInclusive(5, 1000);
            this.recorder = new PriceHistoryAutoRecorderCompany
            {
                company = this
            };
            this.recorder.RecordCurrentPrice();
            this.sharesHeldByPlayer = new List<Share>();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Defs.Look(ref traderKind, "traderKind");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref currentValue, "currentValue");
            Scribe_Values.Look(ref loadID, "loadID");
            Scribe_Collections.Look(ref sharesHeldByPlayer, "sharesHeldByPlayer", LookMode.Deep);
            Scribe_Deep.Look(ref recorder, "recorder");
            Scribe_Values.Look(ref playerFollowsNews, "playerFollowsNews");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.sharesHeldByPlayer ??= new List<Share>();
                //currentValue *= 1.01f; // for debugging
            }
        }

        public string GetUniqueLoadID()
        {
            return "Company_" + loadID;
        }
    }
}