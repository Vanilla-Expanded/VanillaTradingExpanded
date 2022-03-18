using RimWorld;
using Verse;

namespace VanillaTradingExpanded
{
    public class Loan : IExposable
    {
        public int repayAmount;
        public int repayDate;
        public int loanOptionId;
        public int mapTile;
        public bool wasOverdue;
        public bool warrantForIndebtednessWarningIssued;
        public bool IsOverdue => DaysUntil < 0;
        public int DaysUntil => (int)(this.repayDate - Find.TickManager.TicksAbs).TicksToDays();
        public void ExposeData()
        {
            Scribe_Values.Look(ref repayAmount, "repayAmount");
            Scribe_Values.Look(ref repayDate, "repayTick");
            Scribe_Values.Look(ref loanOptionId, "loanOptionId");
            Scribe_Values.Look(ref wasOverdue, "wasOverdue");
            Scribe_Values.Look(ref mapTile, "mapTile");
        }
    }
}
