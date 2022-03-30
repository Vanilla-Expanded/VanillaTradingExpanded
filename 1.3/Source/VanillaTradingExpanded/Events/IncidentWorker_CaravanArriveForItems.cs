using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace VanillaTradingExpanded
{
    public class IncidentWorker_CaravanArriveForItems : IncidentWorker_NeutralGroup
	{
		public static Contract contract;
		public override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
		{
			var faction = Find.FactionManager.GetFactions(allowNonHumanlike: false).Where(x => !x.HostileTo(Faction.OfPlayer)).RandomElement();
			return faction == f;
		}

		public override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (!TryResolveParms(parms))
			{
				return false;
			}
			if (parms.faction.HostileTo(Faction.OfPlayer))
			{
				return false;
			}
			parms.traderKind = parms.faction.def.caravanTraderKinds.RandomElement();
			List<Thing> wares = contract.FoundItemsInMap(map);
			List<Pawn> pawns = SpawnPawns(parms, wares);
			if (pawns.Count == 0)
			{
				return false;
			}
			for (int i = 0; i < pawns.Count; i++)
			{
				if (pawns[i].needs != null && pawns[i].needs.food != null)
				{
					pawns[i].needs.food.CurLevel = pawns[i].needs.food.MaxLevel;
				}
			}
			TraderKindDef traderKind = null;
			for (int j = 0; j < pawns.Count; j++)
			{
				Pawn pawn = pawns[j];
				if (pawn.TraderKind != null)
				{
					traderKind = pawn.TraderKind;
					break;
				}
			}
			SendLetter(parms, pawns, traderKind);
			var initialPos = wares.Any() ? wares[0].Position : RCellFinder.TryFindRandomSpotJustOutsideColony(pawns[0], out var res) ? res : map.Center;
			var result = CellFinder.RandomClosewalkCellNear(initialPos, pawns[0].MapHeld, 5, delegate (IntVec3 c)
			{
				for (int k = 0; k < pawns.Count; k++)
				{
					if (!pawns[k].CanReach(c, PathEndMode.OnCell, Danger.Deadly))
					{
						return false;
					}
				}
				return true;
			});
			if (result.IsValid)
            {
				var transferables = new List<TransferableOneWay>();
				var count = 0;
				var transferable = new TransferableOneWay();
				foreach (var thing in wares)
				{
					transferable.things.Add(thing);
					var curCount = Mathf.Min(thing.stackCount, contract.amount - count);
					count += curCount;
				}
				transferable.CountToTransfer = count;
				transferables.Add(transferable);
				LordJob_GrabItemsAndLeave lordJob = new LordJob_GrabItemsAndLeave(parms.faction, result, contract, transferables);
				LordMaker.MakeNewLord(parms.faction, lordJob, map, pawns);
				return true;
			}
			return false;
		}
		protected List<Pawn> SpawnPawns(IncidentParms parms, List<Thing> wares)
		{
			Map map = (Map)parms.target;
			var groupParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDef, parms, ensureCanGenerateAtLeastOnePawn: true);
			var groupMaker = parms.faction.def.pawnGroupMakers.First(x => x.kindDef == PawnGroupKindDefOf.Trader);
			List<Pawn> list = new List<Pawn>();
			GeneratePawns(map, parms, wares, groupParms, groupMaker, list);
			foreach (Pawn item in list)
			{
				IntVec3 loc = CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5);
				GenSpawn.Spawn(item, loc, map);
				parms.storeGeneratedNeutralPawns?.Add(item);
			}
			return list;
		}
		protected void GeneratePawns(Map map, IncidentParms incidentParms, List<Thing> wares, PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns, bool errorOnZeroResults = true)
		{
			PawnGenOption pawnGenOption = groupMaker.traders.FirstOrDefault((PawnGenOption x) => !x.kind.trader);
			if (pawnGenOption != null)
			{
				Log.Error(string.Concat("Cannot generate arriving trader caravan for ", parms.faction, " because there is a pawn kind (") + pawnGenOption.kind.LabelCap + ") who is not a trader but is in a traders list.");
				return;
			}
			PawnGenOption pawnGenOption2 = groupMaker.carriers.FirstOrDefault((PawnGenOption x) => !x.kind.RaceProps.packAnimal);
			if (pawnGenOption2 != null)
			{
				Log.Error(string.Concat("Cannot generate arriving trader caravan for ", parms.faction, " because there is a pawn kind (") + pawnGenOption2.kind.LabelCap + ") who is not a carrier but is in a carriers list.");
				return;
			}
			if (parms.seed.HasValue)
			{
				Log.Warning("Deterministic seed not implemented for this pawn group kind worker. The result will be random anyway.");
			}
			TraderKindDef traderKindDef = ((parms.traderKind != null) ? parms.traderKind : parms.faction.def.caravanTraderKinds.RandomElementByWeight((TraderKindDef traderDef) => traderDef.CalculatedCommonality));
			Pawn pawn = GenerateTrader(parms, groupMaker, traderKindDef);
			outPawns.Add(pawn);
			ThingSetMakerParams parms2 = default(ThingSetMakerParams);
			parms2.traderDef = traderKindDef;
			parms2.tile = parms.tile;
			parms2.makingFaction = parms.faction;
			GenerateCarriers(parms, groupMaker, pawn, wares, outPawns);
			parms.points = outPawns.Count * 100;
			GenerateGuards(parms, groupMaker, pawn, wares, outPawns);
		}
		private Pawn GenerateTrader(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, TraderKindDef traderKind)
		{
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(groupMaker.traders.RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind, parms.faction, PawnGenerationContext.NonPlayer, fixedIdeo: parms.ideo, tile: parms.tile, forceGenerateNewPawn: false, newborn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowFood: true, allowAddictions: true, inhabitant: parms.inhabitants));
			pawn.mindState.wantsToTradeWithColony = true;
			PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, actAsIfSpawned: true);
			pawn.trader.traderKind = traderKind;
			parms.points -= pawn.kindDef.combatPower;
			return pawn;
		}

		private void GenerateCarriers(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, Pawn trader, List<Thing> wares, List<Pawn> outPawns)
		{
			List<Thing> list = wares.Where((Thing x) => !(x is Pawn)).ToList();
			int i = 0;
			int num = Mathf.CeilToInt((float)list.Count / 8f);
			PawnKindDef kind = groupMaker.carriers.Where((PawnGenOption x) => parms.tile == -1 || Find.WorldGrid[parms.tile].biome.IsPackAnimalAllowed(x.kind.race)).RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind;
			List<Pawn> list2 = new List<Pawn>();
			for (int j = 0; j < num; j++)
			{
				Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, parms.faction, PawnGenerationContext.NonPlayer, fixedIdeo: parms.ideo, tile: parms.tile, forceGenerateNewPawn: false, newborn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowFood: true, allowAddictions: true, inhabitant: parms.inhabitants));
				if (i < list.Count)
				{
					i++;
				}
				list2.Add(pawn);
				outPawns.Add(pawn);
			}
		}

		private void GenerateGuards(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, Pawn trader, List<Thing> wares, List<Pawn> outPawns)
		{
			if (!groupMaker.guards.Any())
			{
				return;
			}
			foreach (PawnGenOption item2 in PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints(parms.points, groupMaker.guards, parms))
			{
				PawnGenerationRequest request = PawnGenerationRequest.MakeDefault();
				request.KindDef = item2.kind;
				request.Faction = parms.faction;
				request.Tile = parms.tile;
				request.MustBeCapableOfViolence = true;
				request.Inhabitant = parms.inhabitants;
				request.FixedIdeo = parms.ideo;
				Pawn item = PawnGenerator.GeneratePawn(request);
				outPawns.Add(item);
			}
		}
		protected virtual void SendLetter(IncidentParms parms, List<Pawn> pawns, TraderKindDef traderKind)
		{
			TaggedString letterLabel = "LetterLabelTraderCaravanArrival".Translate(parms.faction.Name, traderKind.label).CapitalizeFirst();
			TaggedString letterText = "VTE.LetterTraderCaravanArrival".Translate(parms.faction.NameColored, traderKind.label).CapitalizeFirst();
			PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref letterLabel, ref letterText, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), informEvenIfSeenBefore: true);
			SendStandardLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, parms, pawns[0]);
		}

		public override void ResolveParmsPoints(IncidentParms parms)
        {
			parms.points = 1000f;
        }
    }
}
