using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_PerformTransactionGains : Window
	{
		public Window_StockMarket parent;
        public override Vector2 InitialSize => new Vector2(450, 430);
        public Window_PerformTransactionGains(Window_StockMarket parent)
        {
			this.parent = parent;
			this.absorbInputAroundWindow = false;
			this.forcePause = true;
			this.parent.amountToTransfer = new Dictionary<Bank, int>();
			foreach (var bank in parent.allBanks)
            {
				this.parent.amountToTransfer[bank] = 0;
			}
		}

		public string textEntryBuffer;
		private Vector2 scrollPosition;
		private float allMoneyToGain;
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			allMoneyToGain = this.parent.amountToTransfer.Sum(x => x.Value);
			var pos = new Vector2(inRect.x, inRect.y);
			var bankDeposits = new Rect(pos.x, pos.y, inRect.width, 24f);
			Widgets.Label(bankDeposits, "VTE.BankDepositsToGain".Translate());
			pos.y += 30;
			float listHeight = parent.allBanks.Count * 28f;
			Rect viewRect = new Rect(pos.x, pos.y, inRect.width, (inRect.height - pos.y) - 90);
			Rect scrollRect = new Rect(pos.x, pos.y, inRect.width - 16f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			for (var i = 0; i < parent.allBanks.Count; i++)
            {
				var bank = parent.allBanks[i];
				Text.Anchor = TextAnchor.MiddleLeft;
				var entryRect = new Rect(pos.x, pos.y, inRect.width, 24);
				if (i % 2 == 1)
				{
					Widgets.DrawLightHighlight(entryRect);
				}
				var silverIcon = new Rect(pos.x, pos.y, 24, 24);
				GUI.color = bank.parentFaction.Color;
				GUI.DrawTexture(silverIcon, bank.parentFaction.def.FactionIcon);
				GUI.color = Color.white;

				var silverLabel = new Rect(silverIcon.xMax + 10, pos.y, 130, 24);
				Widgets.Label(silverLabel, bank.Name);

				var depositAmountRect = new Rect(silverLabel.xMax, pos.y, 55, 24);
				Widgets.Label(depositAmountRect, bank.DepositAmount.ToString());
				var withdrawFullyRect = new Rect(depositAmountRect.xMax + 10, pos.y, 24, 24);
				var amountMoneyExceptThisBank = this.parent.amountToTransfer.Where(x => x.Key != bank).Sum(x => x.Value);
				if (Widgets.ButtonText(withdrawFullyRect, "<<") && parent.transactionGain > this.parent.amountToTransfer.Where(x => x.Key != bank).Sum(x => x.Value))
				{
					this.parent.amountToTransfer[bank] = parent.transactionGain - amountMoneyExceptThisBank;
				}
				var withdrawRect = new Rect(withdrawFullyRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(withdrawRect, "<") && this.parent.transactionGain - allMoneyToGain > 0)
				{
					this.parent.amountToTransfer[bank]++;
				}

				var textEntry = new Rect(withdrawRect.xMax + 5, pos.y, 60, 24);
				textEntryBuffer = this.parent.amountToTransfer[bank].ToString();
				var value = this.parent.amountToTransfer[bank];
				Widgets.TextFieldNumeric<int>(textEntry, ref value, ref textEntryBuffer, 0, (parent.transactionGain - amountMoneyExceptThisBank));
				this.parent.amountToTransfer[bank] = value;

				var depositRect = new Rect(textEntry.xMax + 5, pos.y, 24, 24);
				if (Widgets.ButtonText(depositRect, ">") && this.parent.amountToTransfer[bank] > 0)
				{
					this.parent.amountToTransfer[bank]--;
				}
				var depositFullyRect = new Rect(depositRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(depositFullyRect, ">>"))
				{
					this.parent.amountToTransfer[bank] = 0;
				}
				GUI.color = Color.white;
				pos.y += 28f;
			}
			Widgets.EndScrollView();

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;

			pos.y = inRect.height - 60;
			var transactionGainToExtract = new Rect(pos.x, pos.y, 250, 24);
			Widgets.Label(transactionGainToExtract, "VTE.TransactionGainToTransfer".Translate(parent.transactionGain));
			
			var moneyToBePaid = new Rect(transactionGainToExtract.xMax, pos.y, 250, 24);
			Widgets.Label(moneyToBePaid, "VTE.MoneyToTransfer".Translate(allMoneyToGain));

			pos.y += 30;
			bool canPay = allMoneyToGain == parent.transactionGain;
			GUI.color = canPay ? Color.white : Color.grey;
			var confirmButtonRect = new Rect(pos.x + 15, pos.y, 170, 32f);
			if (Widgets.ButtonText(confirmButtonRect, "Confirm".Translate(), active: canPay))
			{
				this.Close();
				if (this.parent.transactionCost > 0)
                {
					Find.WindowStack.Add(new Window_PerformTransactionCosts(this.parent));
				}
				else
                {
					this.parent.PerformTransaction();
                }
			}
			GUI.color = Color.white;
			var closeButtonRect = new Rect(confirmButtonRect.xMax + 25, confirmButtonRect.y, confirmButtonRect.width, confirmButtonRect.height);
			if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
			{
				this.Close();
			}

		}
    }
}
