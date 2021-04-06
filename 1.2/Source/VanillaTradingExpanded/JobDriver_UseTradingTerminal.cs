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
	public class JobDriver_UseTradingTerminal : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn((Toil to) => !((Building_TradingTerminal)to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseTerminalNow);
			Toil openTradingTerminal = new Toil();
			openTradingTerminal.initAction = delegate
			{
				Pawn actor = openTradingTerminal.actor;
				if (((Building_TradingTerminal)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseTerminalNow)
				{
					Find.WindowStack.Add(new Window_MarketPrices());
				}
			};
			yield return openTradingTerminal;
		}
	}
}
