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
    public class Loan : IExposable
    {
        public int amount;
        public void ExposeData()
        {
            
        }
    }
    public class Bank : IExposable
    {
        public Faction parentFaction;
        private int depositAmount;
        public Bank(Faction faction)
        {
            this.parentFaction = faction;
        }
        public List<Loan> loans = new List<Loan>();

        public int DepositAmount => depositAmount;
        public float Balance => depositAmount + loans.Sum(x => x.amount);
        public float Fees => this.parentFaction.def.GetModExtension<BankExtension>().feesByGoodwill.Evaluate(this.parentFaction.GoodwillWith(Faction.OfPlayer));
        
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
                depositAmount += num;
            }
        }

        public void WithdrawSilver(Pawn negotiator, int amountToWithwraw)
        {
            List<Thing> thingsToLaunch = new List<Thing>();
            while (amountToWithwraw > 0)
            {
                Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);
                thing.stackCount = Mathf.Min(thing.def.stackLimit, amountToWithwraw);
                amountToWithwraw -= thing.stackCount;
                depositAmount -= thing.stackCount;
                thingsToLaunch.Add(thing);
            }
            foreach (var thing in thingsToLaunch)
            {
                var cell = DropCellFinder.TradeDropSpot(negotiator.Map);
                TradeUtility.SpawnDropPod(cell, negotiator.Map, thing);
            }
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref parentFaction, "parentFaction");
            Scribe_Values.Look(ref depositAmount, "depositAmount");
            Scribe_Collections.Look(ref loans, "loans", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                loans ??= new List<Loan>();
            }
        }
    }
}
