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
using Verse.AI.Group;

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

	[HarmonyPatch(typeof(LordJob_FormAndSendCaravan), "GatheringItemsNow", MethodType.Getter)]
	public class GatheringItemsNowPatch
	{
		private static bool Prefix(LordJob_FormAndSendCaravan __instance)
		{
			if (__instance is LordJob_GrabItemsAndLeave)
			{
				return false;
			}
			return true;
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
				if (TradingManager.Instance?.TryGetModifiedPriceFor(thingDef, out __result) ?? false)
                {
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

	[HarmonyPatch(typeof(SimpleCurveDrawer), "DrawCurvesLegend")]
	public class SimpleCurveDrawer_Patch
	{
		public static bool modify;
		private static bool Prefix(Rect rect, List<SimpleCurveDrawInfo> curves)
		{
			if (modify)
			{
				DrawCurvesLegend(rect, curves);
				return false;
			}
			return true;
		}

		public static void DrawCurvesLegend(Rect rect, List<SimpleCurveDrawInfo> curves)
		{
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			Text.WordWrap = false;
			GUI.BeginGroup(rect);
			float num = 0f;
			float num2 = 0f;
			int num3 = (int)(rect.width / 140f);
			int num4 = 0;
			foreach (SimpleCurveDrawInfo curf in curves)
			{
				GUI.color = curf.color;
				GUI.DrawTexture(new Rect(num, num2 + 2f, 15f, 15f), BaseContent.WhiteTex);
				GUI.color = Color.white;
				num += 20f;
				if (curf.label != null)
				{
					Widgets.Label(new Rect(num, num2, 300, 100f), curf.label);
				}
				num4++;
				if (num4 == num3)
				{
					num4 = 0;
					num = 0f;
					num2 += 20f;
				}
				else
				{
					num += 300;
				}
			}
			GUI.EndGroup();
			GUI.color = Color.white;
			Text.WordWrap = true;
		}
	}
}
