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
    [HotSwappableAttribute]
    public class Bank : IExposable
    {
        public const int IndebtednessPeriodDaysUntilWarrant = 180;
        public Faction parentFaction;
        private int depositAmount;
        public List<Loan> loans = new List<Loan>();

        public Bank()
        {

        }
        public Bank(Faction faction)
        {
            this.parentFaction = faction;
            this.bankExtension = faction.def.GetModExtension<BankExtension>();
        }
        public int DepositAmount => depositAmount;
        public float Balance => depositAmount;

        public BankExtension bankExtension;
        public float Fees => bankExtension.feesByGoodwill.Evaluate(this.parentFaction.GoodwillWith(Faction.OfPlayer));
        public void DepositSilver(List<Thing> silvers, int amountToDeposit)
        {
            while (amountToDeposit > 0)
            {
                Thing thing = silvers.RandomElement();
                silvers.Remove(thing);
                if (thing == null)
                {
                    break;
                }
                int num = Math.Min(amountToDeposit, thing.stackCount);
                thing.SplitOff(num).Destroy();
                amountToDeposit -= num;
                depositAmount += (int)(num * (1f - Fees));
            }
        }

        public void WithdrawSilver(Pawn negotiator, int amountToWithwraw)
        {
            List<Thing> thingsToLaunch = new List<Thing>();
            while (amountToWithwraw > 0)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);
                var curAmount = Mathf.Min(thing.def.stackLimit, amountToWithwraw);
                thing.stackCount = (int)(curAmount * (1f - Fees));
                amountToWithwraw -= curAmount;
                depositAmount -= curAmount;
                thingsToLaunch.Add(thing);
            }
            foreach (var thing in thingsToLaunch)
            {
                var cell = DropCellFinder.TradeDropSpot(negotiator.Map);
                TradeUtility.SpawnDropPod(cell, negotiator.Map, thing);
            }
        }

        public void TakeLoan(Pawn negotiator, int loanAmount, int repayAmount, int repayDate, LoanOption loanOption)
        {
            this.loans.Add(new Loan
            {
                repayAmount = repayAmount,
                repayDate = repayDate,
                loanOptionId = this.bankExtension.loanOptions.IndexOf(loanOption),
                mapTile = negotiator.Map.Tile
            });

            List<Thing> thingsToLaunch = new List<Thing>();
            while (loanAmount > 0)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);
                thing.stackCount = Mathf.Min(thing.def.stackLimit, loanAmount);
                loanAmount -= thing.stackCount;
                thingsToLaunch.Add(thing);
            }
            foreach (var thing in thingsToLaunch)
            {
                var cell = DropCellFinder.TradeDropSpot(negotiator.Map);
                TradeUtility.SpawnDropPod(cell, negotiator.Map, thing);
            }
        }

        public void TryRepayFromBalance(Loan loan)
        {
            if (this.Balance > 0)
            {
                var toPay = (int)(Mathf.Min(this.Balance, loan.repayAmount));
                loan.repayAmount -= toPay;
                this.depositAmount -= toPay;
                if (loan.repayAmount == 0)
                {
                    this.loans.Remove(loan);
                }
            }
        }

        public void Tick()
        {
            for (int num = loans.Count - 1; num >= 0; num--)
            {
                var loan = loans[num];
                if (loan.IsOverdue)
                {
                    this.TryRepayFromBalance(loan);
                    if (loans.Contains(loan))
                    {
                        if (!loan.wasOverdue)
                        {
                            loan.wasOverdue = true;
                            this.parentFaction.TryAffectGoodwillWith(Faction.OfPlayer, -100, false, true, VTE_DefOf.VTE_Indebted);
                            Messages.Message("VTE.IndebtedMessage".Translate(this.parentFaction.Named("FACTION")), MessageTypeDefOf.NegativeEvent);
                        }

                        if (Mathf.Abs(loan.DaysUntil) >= IndebtednessPeriodDaysUntilWarrant)
                        {
                            if (!loan.warrantForIndebtednessWarningIssued)
                            {
                                loan.warrantForIndebtednessWarningIssued = true;
                                Find.LetterStack.ReceiveLetter("VTE.DebtCollection".Translate(), "VTE.DebtCollectionDesc".Translate(loan.repayAmount, parentFaction.Named("FACTION")), LetterDefOf.ThreatBig);
                            }
                            if (Rand.MTBEventOccurs(15, 60000f, 1f))
                            {
                                IncidentWorker_Collectors.bankerFaction = this.parentFaction;
                                var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, Find.World);
                                parms.points *= 2;
                                parms.target = Find.AnyPlayerHomeMap;
                                parms.faction = Find.FactionManager.AllFactions.FirstOrDefault(x => x.def.defName == "Pirate")
                                    ?? Find.FactionManager.GetFactions(false, false, false).Where(x => x.HostileTo(Faction.OfPlayer)).RandomElement();
                                VTE_DefOf.VTE_Collectors.Worker.TryExecute(parms);
                                IncidentWorker_Collectors.bankerFaction = null;
                            }
                        }
                    }
                }
            }
        }

        public bool RaidWarrantsActive => loans.Any(x => Mathf.Abs(x.DaysUntil) >= IndebtednessPeriodDaysUntilWarrant);
        public bool LoanIsTaken(LoanOption loanOption, out Loan loan)
        {
            loan = loans.FirstOrDefault(x => x.loanOptionId == this.bankExtension.loanOptions.IndexOf(loanOption));
            return loan != null;
        }
        public void ExposeData()
        {
            Scribe_References.Look(ref parentFaction, "parentFaction");
            Scribe_Values.Look(ref depositAmount, "depositAmount");
            Scribe_Collections.Look(ref loans, "loans", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                loans ??= new List<Loan>();
                bankExtension = parentFaction.def.GetModExtension<BankExtension>();
            }
        }

        public string GetUniqueLoadID()
        {
            return "Bank_" + this.parentFaction.GetUniqueLoadID();
        }
    }
}
