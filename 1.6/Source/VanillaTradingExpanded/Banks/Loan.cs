using RimWorld;
using Verse;

namespace VanillaTradingExpanded
{
    public class Loan : IExposable
    {

        public int initialRepayAmount;
        public int curRepayAmount;
        public int repayDate;
        public int loanOptionId;
        public int mapTile;
        public bool wasOverdue;
        public bool warrantForIndebtednessWarningIssued;
        public bool IsOverdue => DaysUntil < 0;
        public float DaysUntil => (this.repayDate - Find.TickManager.TicksAbs).TicksToDays();

        public LoanOption GetLoanOption(Bank bank)
        {
            return bank.bankExtension.loanOptions[loanOptionId];
        }
        public void ExposeData()
        {
            Scribe_Values.Look(ref initialRepayAmount, "initialRepayAmount");
            Scribe_Values.Look(ref curRepayAmount, "curRepayAmount");
            Scribe_Values.Look(ref repayDate, "repayTick");
            Scribe_Values.Look(ref loanOptionId, "loanOptionId");
            Scribe_Values.Look(ref wasOverdue, "wasOverdue");
            Scribe_Values.Look(ref mapTile, "mapTile");
        }
    }
}
