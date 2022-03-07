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
    [StaticConstructorOnStartup]
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

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int N)
        {
            return source.Skip(Math.Max(0, source.Count() - N));
        }
    }
}
