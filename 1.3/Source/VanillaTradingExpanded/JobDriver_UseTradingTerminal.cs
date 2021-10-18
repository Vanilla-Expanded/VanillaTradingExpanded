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
	public abstract class JobDriver_UseTradingTerminal : JobDriver
	{
		public Building_TradingTerminal TradingTerminal => this.TargetA.Thing as Building_TradingTerminal;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn((Toil to) => !TradingTerminal.CanUseTerminalNow);
			Toil openTradingTerminal = new Toil();
			openTradingTerminal.initAction = delegate
			{
				Pawn actor = openTradingTerminal.actor;
				DoAction(actor);
			};
			yield return openTradingTerminal;
		}

		protected abstract void DoAction(Pawn actor);
	}

    public class JobDriver_ViewPrices : JobDriver_UseTradingTerminal
    {
        protected override void DoAction(Pawn actor)
        {
			Find.WindowStack.Add(new Window_MarketPrices());
		}
	}

    public class JobDriver_ContactBank : JobDriver_UseTradingTerminal
    {
        protected override void DoAction(Pawn actor)
        {
			Find.WindowStack.Add(new Window_Bank(actor, TradingTerminal.currentVisitableFactionBank));
		}
	}

	public class JobDriver_ViewNews : JobDriver_UseTradingTerminal
	{
		protected override void DoAction(Pawn actor)
		{
			Find.WindowStack.Add(new Window_News());
		}
	}
}
