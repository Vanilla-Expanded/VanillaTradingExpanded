using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_PerformTransactionCosts : Window
	{
		public Window_StockMarket parent;
        public override Vector2 InitialSize => new Vector2(450, 430);
        public Window_PerformTransactionCosts(Window_StockMarket parent)
        {
			this.parent = parent;
			this.absorbInputAroundWindow = false;
			this.forcePause = true;
			this.parent.amountToSpend = new Dictionary<Bank, int>();
			foreach (var bank in parent.allBanks)
            {
				this.parent.amountToSpend[bank] = 0;
			}
		}

		public string textEntryBuffer;
		private Vector2 scrollPosition;
		private float allMoneyToSpend;
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			allMoneyToSpend = this.parent.amountToSpend.Sum(x => x.Value);
			var pos = new Vector2(inRect.x, inRect.y);
			var bankDeposits = new Rect(pos.x, pos.y, inRect.width, 24f);
			Widgets.Label(bankDeposits, "VTE.BankDepositsToSpend".Translate());
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

				var bankMoney = bank.DepositAmount;
				if (this.parent.amountToTransfer != null && this.parent.amountToTransfer.TryGetValue(bank, out var amount))
                {
					bankMoney += amount;
				}

				var depositAmountRect = new Rect(silverLabel.xMax, pos.y, 55, 24);
				Widgets.Label(depositAmountRect, bankMoney.ToString());

				var withdrawFullyRect = new Rect(depositAmountRect.xMax + 10, pos.y, 24, 24);
				if (bankMoney == 0 && this.parent.amountToSpend[bank] == 0)
				{
					GUI.color = Widgets.InactiveColor;
				}
				if (Widgets.ButtonText(withdrawFullyRect, "<<"))
				{
					this.parent.amountToSpend[bank] = 0;
				}
				var withdrawRect = new Rect(withdrawFullyRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(withdrawRect, "<") && this.parent.amountToSpend[bank] > 0)
				{
					this.parent.amountToSpend[bank]--;
				}
				var textEntry = new Rect(withdrawRect.xMax + 5, pos.y, 60, 24);
				textEntryBuffer = this.parent.amountToSpend[bank].ToString();
				var value = this.parent.amountToSpend[bank];
				Widgets.TextFieldNumeric<int>(textEntry, ref value, ref textEntryBuffer, 0, Mathf.Min(bankMoney, parent.transactionCost));
				this.parent.amountToSpend[bank] = value;

				var depositRect = new Rect(textEntry.xMax + 5, pos.y, 24, 24);
				if (Widgets.ButtonText(depositRect, ">") && bankMoney > 0 && parent.transactionCost > allMoneyToSpend)
				{
					this.parent.amountToSpend[bank]++;
				}
				var depositFullyRect = new Rect(depositRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(depositFullyRect, ">>") && bankMoney > 0 && parent.transactionCost > this.parent.amountToSpend.Where(x => x.Key != bank).Sum(x => x.Value))
				{
					this.parent.amountToSpend[bank] = (int)Mathf.Min(bankMoney, parent.transactionCost - this.parent.amountToSpend.Where(x => x.Key != bank).Sum(x => x.Value));
				}
				GUI.color = Color.white;
				pos.y += 28f;
			}
			Widgets.EndScrollView();

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;

			pos.y = inRect.height - 60;
			var transactionCostToPay = new Rect(pos.x, pos.y, 250, 24);
			Widgets.Label(transactionCostToPay, "VTE.TransactionCostToPay".Translate(parent.transactionCost));
			
			var moneyToBePaid = new Rect(transactionCostToPay.xMax, pos.y, 250, 24);
			Widgets.Label(moneyToBePaid, "VTE.MoneyToSpend".Translate(allMoneyToSpend));

			pos.y += 30;
			bool canPay = allMoneyToSpend == parent.transactionCost;
			GUI.color = canPay ? Color.white : Color.grey;
			var confirmButtonRect = new Rect(pos.x + 15, pos.y, 170, 32f);
			if (Widgets.ButtonText(confirmButtonRect, "Confirm".Translate(), active: canPay))
			{
				this.parent.PerformTransaction();
				this.Close();
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
