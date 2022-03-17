using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace VanillaTradingExpanded
{

    public class IncidentWorker_Collectors : IncidentWorker_RaidEnemy
    {
        public override string GetLetterLabel(IncidentParms parms)
        {
            return "VTE.CollectorsLabel".Translate();
        }

        public static Faction bankerFaction;
        public override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            string text = "VTE.CollectorsText".Translate(bankerFaction.Named("FACTION"));
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextEnemy;
            text += "\n\n";
            text += "VTE.CollectorsTextPartTwo".Translate();
            return text;
        }
    }
}
