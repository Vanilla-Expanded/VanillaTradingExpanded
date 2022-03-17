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
    [DefOf]
    public static class VTE_DefOf
    {
        public static JobDef VTE_ViewPrices;
        public static JobDef VTE_ContactBank;
        public static JobDef VTE_ViewNews;
        public static JobDef VTE_OpenStockMarket;
        public static JobDef VTE_OpenContracts;
        public static HistoryEventDef VTE_Indebted;
        public static IncidentDef VTE_Collectors;
        public static ThingDef VTE_TradingTerminal;
        public static IncidentDef VTE_CaravanArriveForItems;
    }
}
