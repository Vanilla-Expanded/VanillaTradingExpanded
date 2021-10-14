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
    public class LoanOption
    {
        public string loanNameKey;
        public float? fixedLoanAmount;
        public float? fixedRepayAmount;
        public float? loanAmountPerDeposit;
        public float? repayAmountPerDeposit;
        public bool transactionFeesIncluded;
        public float loanRepayPeriodDays;
    }
    public class BankExtension : DefModExtension
    {
        public string bankNameKey;
        public FloatRange startingFunds;
        public SimpleCurve feesByGoodwill;
        public List<LoanOption> loanOptions;
    }
}
