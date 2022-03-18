using Verse;

namespace VanillaTradingExpanded
{
    public class Share : IExposable
    {
        public float valueBought;
        public void ExposeData()
        {
            Scribe_Values.Look(ref valueBought, "valueBought");
        }
    }
}