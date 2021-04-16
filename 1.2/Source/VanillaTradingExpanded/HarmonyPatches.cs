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

namespace VanillaTradingExpanded
{
	[StaticConstructorOnStartup]
	internal static class HarmonyPatches
	{
		public static Harmony harmony;
		static HarmonyPatches()
		{
			harmony = new Harmony("OskarPotocki.VanillaTradingExpanded");
			harmony.PatchAll();
		}
	}

	[HarmonyPatch(typeof(StatWorker), "GetBaseValueFor")]
	public class StatWorker_GetBaseValueFor_Patch
	{
		public static bool showOnlyVanilla;
		private static bool Prefix(StatWorker __instance, StatDef ___stat, StatRequest request, ref float __result)
		{
			if (___stat == StatDefOf.MarketValue && !showOnlyVanilla && request.BuildableDef is ThingDef thingDef && TradingManager.Instance.priceModifiers.TryGetValue(thingDef, out float value))
            {
				__result = value;
				Log.Message("Result for " + thingDef + ": " + __result);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn_TraderTracker), "GiveSoldThingToTrader")]
	public class Pawn_TraderTracker_GiveSoldThingToTrader_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterSoldThing(toGive, countToGive);
		}
	}


	[HarmonyPatch(typeof(Pawn_TraderTracker), "GiveSoldThingToPlayer")]
	public class Pawn_TraderTracker_GiveSoldThingToPlayer_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterPurchasedThing(toGive, countToGive);
		}
	}

	[HarmonyPatch(typeof(TradeShip), "GiveSoldThingToTrader")]
	public class TradeShip_GiveSoldThingToTrader_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterSoldThing(toGive, countToGive);
		}
	}


	[HarmonyPatch(typeof(TradeShip), "GiveSoldThingToPlayer")]
	public class TradeShip_GiveSoldThingToPlayer_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterPurchasedThing(toGive, countToGive);
		}
	}

	[HarmonyPatch(typeof(Caravan), "GiveSoldThingToTrader")]
	public class Caravan_GiveSoldThingToTrader_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterSoldThing(toGive, countToGive);
		}
	}


	[HarmonyPatch(typeof(Caravan), "GiveSoldThingToPlayer")]
	public class Caravan_GiveSoldThingToPlayer_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterPurchasedThing(toGive, countToGive);
		}
	}

	[HarmonyPatch(typeof(Settlement), "GiveSoldThingToTrader")]
	public class Settlement_GiveSoldThingToTrader_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterSoldThing(toGive, countToGive);
		}
	}


	[HarmonyPatch(typeof(Settlement), "GiveSoldThingToPlayer")]
	public class Settlement_GiveSoldThingToPlayer_Patch
	{
		private static void Prefix(Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterPurchasedThing(toGive, countToGive);
		}
	}
}
