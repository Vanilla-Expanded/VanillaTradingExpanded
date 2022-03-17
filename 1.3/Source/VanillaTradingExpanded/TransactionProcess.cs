using System.Collections.Generic;
using System.Linq;
using Verse;

namespace VanillaTradingExpanded
{
    public class TransactionProcess
    {
		public List<Bank> allBanks;
		public Dictionary<Bank, int> amountToTransfer;
		public Dictionary<Bank, int> amountToSpend;
		public int allMoneyInBanks;
		public int totalTransaction;
		public int transactionGain;
		public int transactionCost;
		public Dictionary<Company, int> companySharesToBuyOrSell = new Dictionary<Company, int>();
		public TransactionProcess()
        {
			allBanks = TradingManager.Instance.Banks;
		}
		public void PerformTransaction()
		{
			Log.Message("Performing transaction");
			Log.Message("this.sharesToBuyOrSell: " + this.companySharesToBuyOrSell.Count);
			if (this.amountToTransfer != null)
			{
				if (this.companySharesToBuyOrSell != null)
				{
					var companySharesToSell = this.companySharesToBuyOrSell.Where(x => x.Value < 0).Select(x => x.Key).ToList();
					foreach (var company in companySharesToSell)
					{
						company.sharesHeldByPlayer = company.sharesHeldByPlayer.InRandomOrder().ToList(); // so the profit loss rate won't be off,
																										  // since it will calculate based on bought value in the past
						var shareAmount = -this.companySharesToBuyOrSell[company];
						for (var i = shareAmount - 1; i >= 0; i--)
						{
							company.sharesHeldByPlayer.RemoveAt(i);
							Log.Message("Sold share from " + company.name + " for " + company.currentValue);
						}
						this.companySharesToBuyOrSell.Remove(company);
					}
				}

				foreach (var kvp in this.amountToTransfer)
				{
					kvp.Key.DepositAmount += kvp.Value;
				}
				this.amountToTransfer.Clear();
			}

			if (this.amountToSpend != null)
			{
				if (this.companySharesToBuyOrSell != null)
				{
					var companySharesToBuy = this.companySharesToBuyOrSell.Where(x => x.Value > 0).Select(x => x.Key).ToList();
					foreach (var company in companySharesToBuy)
					{
						var shareAmount = this.companySharesToBuyOrSell[company];
						for (var i = 0; i < shareAmount; i++)
						{
							var share = new Share
							{
								valueBought = company.currentValue,
							};
							company.sharesHeldByPlayer.Add(share);
							Log.Message("Bought share from " + company.name + " for " + company.currentValue);
						}
						this.companySharesToBuyOrSell.Remove(company);
					}
					foreach (var kvp in this.amountToSpend)
					{
						kvp.Key.DepositAmount -= kvp.Value;
					}
					this.amountToSpend.Clear();
				}

			}
			Log.Message("this.sharesToBuyOrSell: " + this.companySharesToBuyOrSell.Count);
		}
	}
}
