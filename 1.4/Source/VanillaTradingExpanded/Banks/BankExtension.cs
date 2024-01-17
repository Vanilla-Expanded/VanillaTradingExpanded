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
    public class LoanOption
    {
        public string loanNameKey;
        public int? fixedLoanAmount;
        public int? fixedRepayAmount;
        public float? loanAmountPerDeposit;
        public float? repayAmountPerDeposit;
        public bool transactionFeesIncluded;
        public float loanRepayPeriodDays;
        public float overdueInterestEveryDay;
        public int GetLoanAmountFrom(Bank bank)
        {
            if (fixedLoanAmount.HasValue)
            {
                return fixedLoanAmount.Value;
            }
            return Mathf.CeilToInt(bank.DepositAmount * loanAmountPerDeposit.Value);
        }

        public int GetRepayAmountFrom(Bank bank)
        {
            if (fixedRepayAmount.HasValue)
            {
                return fixedRepayAmount.Value;
            }
            float loanAmount = GetLoanAmountFrom(bank);
            if (transactionFeesIncluded)
            {
                loanAmount *= (1f + bank.Fees);
            }
            return Mathf.CeilToInt(loanAmount);
        }

        public int GetRepayDateTicks()
        {
            return (int)(GenDate.TicksPerDay * loanRepayPeriodDays);
        }
    }
    public class BankExtension : DefModExtension
    {
        public string bankNameKey;
        public FloatRange startingFunds;
        public SimpleCurve feesByGoodwill;
        public List<LoanOption> loanOptions;
    }
}
