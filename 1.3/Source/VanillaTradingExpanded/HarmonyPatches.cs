using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
		public static bool outputOnlyVanilla;
		private static bool Prefix(StatWorker __instance, StatDef ___stat, StatRequest request, ref float __result)
		{
			if (___stat == StatDefOf.MarketValue && !outputOnlyVanilla && request.BuildableDef is ThingDef thingDef)
			{
				var priceModifiers = TradingManager.Instance?.priceModifiers;
				if (priceModifiers != null && priceModifiers.TryGetValue(thingDef, out float value))
                {
					__result = value;
					Log.Message("Result for " + thingDef + ": " + __result);
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch]
	public static class GiveSoldThingToTrader_Patch
	{
		[HarmonyTargetMethods]
		public static IEnumerable<MethodBase> TargetMethods()
		{
			var interfaceType = typeof(ITrader);
			foreach (var type in GenTypes.AllTypes)
            {
				if (type != interfaceType && interfaceType.IsAssignableFrom(type))
				{
					var method = AccessTools.DeclaredMethod(type, "GiveSoldThingToTrader", new Type[] { typeof(Thing), typeof(int), typeof(Pawn) });
					if (method != null)
					{
						yield return method;
					}
				}
			}
		}
		private static void Prefix(Thing __0, int __1, Pawn __2) // (Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterSoldThing(__0, __1);
		}
	}


	[HarmonyPatch]
	public static class GiveSoldThingToPlayer_Patch
	{
		[HarmonyTargetMethods]
		public static IEnumerable<MethodBase> TargetMethods()
		{
			var interfaceType = typeof(ITrader);
			foreach (var type in GenTypes.AllTypes)
			{
				if (type != interfaceType && interfaceType.IsAssignableFrom(type))
				{
					var method = AccessTools.DeclaredMethod(type, "GiveSoldThingToPlayer", new Type[] { typeof(Thing), typeof(int), typeof(Pawn) });
					if (method != null)
					{
						yield return method;
					}
				}
			}
		}
		private static void Prefix(Thing __0, int __1, Pawn __2) // (Thing toGive, int countToGive, Pawn playerNegotiator)
		{
			TradingManager.Instance.RegisterPurchasedThing(__0, __1);
		}
	}
}
