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
	public class Building_TradingTerminal : Building
	{
		private CompPowerTrader powerComp;
		public bool CanUseTerminalNow
		{
			get
			{
				if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled)
				{
					return false;
				}
				if (powerComp != null)
				{
					return powerComp.PowerOn;
				}
				return true;
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
		}
		private FloatMenuOption GetFailureReason(Pawn myPawn)
		{
			if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
			{
				return new FloatMenuOption("CannotUseNoPath".Translate(), null);
			}
			if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled)
			{
				return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
			}
			if (powerComp != null && !powerComp.PowerOn)
			{
				return new FloatMenuOption("CannotUseNoPower".Translate(), null);
			}
			if (!myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
			{
				return new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label, myPawn.Named("PAWN"))), null);
			}
			if (!CanUseTerminalNow)
			{
				Log.Error(string.Concat(myPawn, " could not use comm console for unknown reason."));
				return new FloatMenuOption("Cannot use now", null);
			}
			return null;
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
		{
			FloatMenuOption failureReason = GetFailureReason(myPawn);
			if (failureReason != null)
			{
				yield return failureReason;
			}
			else
			{
				yield return GetViewMarketPricesOption(myPawn);
				yield return GetViewNewsOption(myPawn);
				yield return GetOpenStockMarketOption(myPawn);
				foreach (var floatOption2 in GetBankOptions(myPawn))
                {
					yield return floatOption2;
                }
				yield return GetOpenContractsOption(myPawn);
			}
		}
		private FloatMenuOption GetViewMarketPricesOption(Pawn negotiator)
        {
			string text = "VTE.ViewMarketPrices".Translate();
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
			{
				Job job = JobMaker.MakeJob(VTE_DefOf.VTE_ViewPrices, this);
				negotiator.jobs.TryTakeOrderedJob(job);
			}, MenuOptionPriority.Default), negotiator, this);
		}

		private FloatMenuOption GetViewNewsOption(Pawn negotiator)
		{
			string text = "VTE.ViewNews".Translate();
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
			{
				Job job = JobMaker.MakeJob(VTE_DefOf.VTE_ViewNews, this);
				negotiator.jobs.TryTakeOrderedJob(job);
			}, MenuOptionPriority.Default), negotiator, this);
		}

		private FloatMenuOption GetOpenStockMarketOption(Pawn negotiator)
		{
			string text = "VTE.OpenStockMarket".Translate();
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
			{
				Job job = JobMaker.MakeJob(VTE_DefOf.VTE_OpenStockMarket, this);
				negotiator.jobs.TryTakeOrderedJob(job);
			}, MenuOptionPriority.Default), negotiator, this);
		}

		private FloatMenuOption GetOpenContractsOption(Pawn negotiator)
		{
			string text = "VTE.OpenContracts".Translate();
			return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
			{
				Job job = JobMaker.MakeJob(VTE_DefOf.VTE_OpenContracts, this);
				negotiator.jobs.TryTakeOrderedJob(job);
			}, MenuOptionPriority.Default), negotiator, this);
		}

		public Faction currentVisitableFactionBank;
		private IEnumerable<FloatMenuOption> GetBankOptions(Pawn negotiator)
		{
			foreach (var kvp in TradingManager.Instance.banksByFaction)
            {
				yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("VTE.Contact".Translate(kvp.Value.Name), delegate
				{
					Job job = JobMaker.MakeJob(VTE_DefOf.VTE_ContactBank, this);
					currentVisitableFactionBank = kvp.Key;
					negotiator.jobs.TryTakeOrderedJob(job);
				}, MenuOptionPriority.Default), negotiator, this);
			}
		}

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_References.Look(ref currentVisitableFactionBank, "currentVisitableFactionBank");
        }
    }
}
