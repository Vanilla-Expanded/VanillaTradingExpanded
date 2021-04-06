using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VanillaTradingExpanded
{
    public class TradingManager : GameComponent
    {

        public static TradingManager Instance;

        public Dictionary<ThingDef, float> priceModifiers;

        public HashSet<ThingDef> cachedTradeables;
        public TradingManager()
        {

        }

        public TradingManager(Game game)
        {

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Instance = this;
            cachedTradeables = new HashSet<ThingDef>();
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (thingDef.tradeability != Tradeability.None && thingDef.GetStatValueAbstract(StatDefOf.MarketValue) > 0)
                {
                    Log.Message($"Adding: {thingDef}, {thingDef.tradeability}, {thingDef.GetStatValueAbstract(StatDefOf.MarketValue)}");
                    cachedTradeables.Add(thingDef);
                }
            }
        }

        public void PreInit()
        {
            if (priceModifiers is null)
            {
                priceModifiers = new Dictionary<ThingDef, float>();
            }
        }
        public override void StartedNewGame()
        {
            PreInit();
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            PreInit();
            base.LoadedGame();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
        }


        public void RemoveDestroyedPawn(Pawn key)
        {

        }
        public override void ExposeData()
        {
            base.ExposeData();

        }

    }
}