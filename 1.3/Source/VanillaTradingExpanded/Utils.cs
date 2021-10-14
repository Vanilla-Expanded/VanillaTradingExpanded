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
    public static class Utils
    {
        public static Bank GetBank(this Faction faction)
        {
            if (TradingManager.Instance.banksByFaction.TryGetValue(faction, out var bank))
            {
                return bank;
            }
            return null;
        }
    }
}
