using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

namespace VanillaTradingExpanded
{
	public class LordToil_PrepareCaravan_GatherItemsNPC : LordToil
	{
		private IntVec3 meetingPoint;

		public override float? CustomWakeThreshold => 0.5f;

		public override bool AllowRestingInBed => true;

		public LordToil_PrepareCaravan_GatherItemsNPC(IntVec3 meetingPoint)
		{
			this.meetingPoint = meetingPoint;
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn.RaceProps.Humanlike)
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherItems);
				}
				else
				{
					pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint);
				}
			}
		}

		public override void LordToilTick()
		{
			TryUpdateTransferrables();
			base.LordToilTick();
			if (Find.TickManager.TicksGame % 120 != 0)
			{
				return;
			}
			bool flag = true;
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn = lord.ownedPawns[i];
				if (pawn.RaceProps.Humanlike && pawn.mindState.lastJobTag != JobTag.WaitingForOthersToFinishGatheringItems)
				{
					flag = false;
					break;
				}
			}
			List<Pawn> allPawnsSpawned = base.Map.mapPawns.AllPawnsSpawned;
			for (int j = 0; j < allPawnsSpawned.Count; j++)
			{
				if (allPawnsSpawned[j].CurJob != null && allPawnsSpawned[j].jobs.curDriver is JobDriver_PrepareCaravan_GatherItems && allPawnsSpawned[j].CurJob.lord == lord)
				{
					flag = false;
					break;
				}
			}

			if (!flag)
			{
				return;
			}

			foreach (Pawn ownedPawn in lord.ownedPawns)
			{
				ownedPawn.inventory.ClearHaulingCaravanCache();
			}

			lord.ReceiveMemo("AllItemsGathered");
		}

		public void TryUpdateTransferrables()
        {
			var lordJob = this.lord.LordJob as LordJob_GrabItemsAndLeave;
			if (lordJob != null)
            {
				var transferable = lordJob.transferables.First();
				if (transferable != null && transferable.things.Where(x => !x.Destroyed).Sum(x => x.stackCount) < lordJob.contract.amount)
                {
					Log.Message("Refreshing transferable");
					lordJob.transferables.Clear();
					var count = 0;
					transferable = new TransferableOneWay();
					foreach (var pawn in lord.ownedPawns)
					{
						foreach (var thing in pawn.inventory.innerContainer)
						{
							if (thing.def == lordJob.contract.item && thing.Stuff == lordJob.contract.stuff)
							{
								var curCount = Mathf.Min(thing.stackCount, lordJob.contract.amount - count);
								if (curCount > 0)
								{
									transferable.things.Add(thing);
									count += curCount;
								}
							}
						}
					}

					List<Thing> wares = lordJob.contract.FoundItemsInMap(this.lord.Map);
					foreach (var thing in wares)
					{
						var curCount = Mathf.Min(thing.stackCount, lordJob.contract.amount - count);
						if (curCount > 0)
						{
							count += curCount;
							transferable.things.Add(thing);
						}
					}
					transferable.CountToTransfer = count;
					lordJob.transferables.Add(transferable);
				}
			}
		}
	}
	internal class LordToil_DefendTraderCaravan : LordToil_DefendPoint
	{
		public override bool AllowSatisfyLongNeeds => false;

		public override float? CustomWakeThreshold => 0.5f;

		public LordToil_DefendTraderCaravan()
		{
		}

		public LordToil_DefendTraderCaravan(IntVec3 defendPoint)
			: base(defendPoint)
		{
		}

		public override void UpdateAllDuties()
		{
			LordToilData_DefendPoint lordToilData_DefendPoint = base.Data;
			Pawn pawn = TraderCaravanUtility.FindTrader(lord);
			if (pawn == null)
			{
				return;
			}
			pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, lordToilData_DefendPoint.defendPoint, lordToilData_DefendPoint.defendRadius);
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				Pawn pawn2 = lord.ownedPawns[i];
				switch (pawn2.GetTraderCaravanRole())
				{
					case TraderCaravanRole.Carrier:
						pawn2.mindState.duty = new PawnDuty(DutyDefOf.Follow, pawn, 5f);
						pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
						break;
					case TraderCaravanRole.Chattel:
						pawn2.mindState.duty = new PawnDuty(DutyDefOf.Escort, pawn, 5f);
						pawn2.mindState.duty.locomotion = LocomotionUrgency.Walk;
						break;
					case TraderCaravanRole.Guard:
						pawn2.mindState.duty = new PawnDuty(DutyDefOf.Defend, lordToilData_DefendPoint.defendPoint, lordToilData_DefendPoint.defendRadius);
						break;
				}
			}
		}
	}
	public class LordJob_GrabItemsAndLeave : LordJob_FormAndSendCaravan
	{
		public override bool AllowStartNewGatherings => false;
		public override bool NeverInRestraints => true;
		public override bool AddFleeToil => false;
		public override bool ManagesRopableAnimals => true;

		public Faction faction;

		public Contract contract;
		public LordJob_GrabItemsAndLeave()
		{
		}
		public LordJob_GrabItemsAndLeave(Faction faction, IntVec3 meetingSpot, Contract contract, List<TransferableOneWay> transferables)
		{
			this.meetingPoint = meetingSpot;
			this.faction = faction;
			this.transferables = transferables;
			this.contract = contract;
			this.downedPawns = new List<Pawn>();
		}
		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph();
			LordToil_Travel lordToil_Travel = (LordToil_Travel)(stateGraph.StartingToil = new LordToil_Travel(meetingPoint));
			LordToil_DefendTraderCaravan lordToil_DefendTraderCaravan = new LordToil_DefendTraderCaravan();
			stateGraph.AddToil(lordToil_DefendTraderCaravan);
			LordToil_DefendTraderCaravan lordToil_DefendTraderCaravan2 = new LordToil_DefendTraderCaravan(meetingPoint);
			stateGraph.AddToil(lordToil_DefendTraderCaravan2);
			LordToil_ExitMapAndEscortCarriers lordToil_ExitMapAndEscortCarriers = new LordToil_ExitMapAndEscortCarriers();
			stateGraph.AddToil(lordToil_ExitMapAndEscortCarriers);
			LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap();
			stateGraph.AddToil(lordToil_ExitMap);
			LordToil_ExitMap lordToil_ExitMap2 = new LordToil_ExitMap(LocomotionUrgency.Walk, canDig: true);
			stateGraph.AddToil(lordToil_ExitMap2);
			LordToil_ExitMapTraderFighting lordToil_ExitMapTraderFighting = new LordToil_ExitMapTraderFighting();
			stateGraph.AddToil(lordToil_ExitMapTraderFighting);
			Transition transition = new Transition(lordToil_Travel, lordToil_ExitMapAndEscortCarriers);
			transition.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2);
			transition.AddPreAction(new TransitionAction_Message("MessageVisitorsDangerousTemperature".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
			transition.AddPostAction(new TransitionAction_EndAllJobs());
			transition.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
			//transition.AddPreAction(new TransitionAction_Message("Transition"));
			stateGraph.AddTransition(transition);

			Transition transition2 = new Transition(lordToil_Travel, lordToil_ExitMap2);
			transition2.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap);
			transition2.AddTrigger(new Trigger_PawnCannotReachMapEdge());
			transition2.AddPostAction(new TransitionAction_Message("MessageVisitorsTrappedLeaving".Translate(faction.def.pawnsPlural.CapitalizeFirst(), faction.Name)));
			//transition2.AddPostAction(new TransitionAction_Message("Transition2"));
			transition2.AddPostAction(new TransitionAction_WakeAll());
			transition2.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transition2);

			Transition transition3 = new Transition(lordToil_Travel, lordToil_ExitMapTraderFighting);
			transition3.AddSources(lordToil_DefendTraderCaravan, lordToil_DefendTraderCaravan2, lordToil_ExitMapAndEscortCarriers, lordToil_ExitMap);
			transition3.AddTrigger(new Trigger_FractionPawnsLost(0.2f));
			//transition3.AddPreAction(new TransitionAction_Message("Transition3"));
			transition3.AddPostAction(new TransitionAction_EndAllJobs());
			stateGraph.AddTransition(transition3);

			Transition transition4 = new Transition(lordToil_Travel, lordToil_DefendTraderCaravan);
			transition4.AddTrigger(new Trigger_PawnHarmed());
			transition4.AddPreAction(new TransitionAction_SetDefendTrader());
			transition4.AddPostAction(new TransitionAction_WakeAll());
			transition4.AddPostAction(new TransitionAction_EndAllJobs());
			//transition4.AddPreAction(new TransitionAction_Message("Transition4"));

			stateGraph.AddTransition(transition4);
			Transition transition5 = new Transition(lordToil_DefendTraderCaravan, lordToil_Travel);
			transition5.AddTrigger(new Trigger_TicksPassedWithoutHarm(60));
			//transition5.AddPreAction(new TransitionAction_Message("Transition5"));

			stateGraph.AddTransition(transition5);
			gatherItems = new LordToil_PrepareCaravan_GatherItemsNPC(meetingPoint);
			stateGraph.AddToil(gatherItems);
			gatherItems_pause = new LordToil_PrepareCaravan_Pause();
			stateGraph.AddToil(gatherItems_pause);
			leave = new LordToil_PrepareCaravan_Leave(exitSpot);
			stateGraph.AddToil(leave);
			LordToil_End lordToil_End = new LordToil_End();
			stateGraph.AddToil(lordToil_End);
			Transition transition6 = new Transition(lordToil_Travel, gatherItems);
			transition6.AddTrigger(new Trigger_Memo("TravelArrived"));
			//transition6.AddPreAction(new TransitionAction_Message("Transition6"));
			stateGraph.AddTransition(transition6);
			Transition transition7 = new Transition(gatherItems, leave);
			transition7.AddTrigger(new Trigger_Memo("AllItemsGathered"));
			transition7.AddPostAction(new TransitionAction_EndAllJobs());
			//transition7.AddPreAction(new TransitionAction_Message("Transition7"));
			stateGraph.AddTransition(transition7);
			Transition transition8 = new Transition(leave, lordToil_End);
			transition8.AddTrigger(new Trigger_Memo("ReadyToExitMap"));
			//transition8.AddPreAction(new TransitionAction_Message("Transition8"));
			transition8.AddPreAction(new TransitionAction_Custom(ExitCaravan));
			stateGraph.AddTransition(transition8);
			Transition transition11 = PauseTransition(gatherItems, gatherItems_pause);
			//transition11.AddPreAction(new TransitionAction_Message("Transition11"));
			stateGraph.AddTransition(transition11);
			Transition transition12 = UnpauseTransition(gatherItems_pause, gatherItems);
			//transition12.AddPreAction(new TransitionAction_Message("Transition12"));
			stateGraph.AddTransition(transition12);
			return stateGraph;
		}
		public void ExitCaravan()
        {
			caravanSent = true;
			var collectedAmount = 0;
			foreach (var pawn in lord.ownedPawns)
            {
				foreach (var item in pawn.inventory.innerContainer)
				{
					if (item.def == contract.item && item.Stuff == contract.stuff)
					{
						collectedAmount += item.stackCount;
					}
				}
				pawn.DeSpawn();
				Find.WorldPawns.PassToWorld(pawn);
            }
			//Log.Message("Final Collected amount: " + collectedAmount);
			if (collectedAmount >= contract.amount)
            {
				Find.WindowStack.Add(new Window_PerformTransactionGains("VTE.BankDepositsToPutContractReward".Translate(), new TransactionProcess
				{
					transactionGain = contract.reward
				}));
			}
			else
            {
				Find.WindowStack.Add(new Dialog_MessageBox("VTE.CaravanDidNotCollectAllItems".Translate(collectedAmount, contract.BaseName)));
            }
        }

        public override void PostCleanup()
        {
            base.PostCleanup();
			if (TradingManager.Instance.npcSubmittedContracts.Count < VanillaTradingExpandedMod.settings.maxNPCContractCount)
            {
				TradingManager.Instance.npcSubmittedContracts.Add(TradingManager.Instance.GenerateRandomContract());
			}
		}
		public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
		{

		}
		public override void ExposeData()
        {
            base.ExposeData();
			Scribe_References.Look(ref faction, "faction");
			Scribe_Deep.Look(ref contract, "contract");
        }
        public override bool CanOpenAnyDoor(Pawn p)
		{
			return true;
		}
	}
}
