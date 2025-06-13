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
		public TransactionProcess transactionProcess;
		public override Vector2 InitialSize => new Vector2(500, 430);

		public string title;
        public Window_PerformTransactionCosts(string title, TransactionProcess parent)
        {
			this.title = title;
			this.transactionProcess = parent;
			this.forcePause = true;
			this.transactionProcess.amountToSpend = new Dictionary<Bank, int>();
			foreach (var bank in parent.allBanks)
            {
				this.transactionProcess.amountToSpend[bank] = 0;
			}
		}

		public string textEntryBuffer;
		private Vector2 scrollPosition;
		private float allMoneyToSpend;

		public override void OnCancelKeyPressed()
		{

		}
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
			allMoneyToSpend = this.transactionProcess.amountToSpend.Sum(x => x.Value);
			var pos = new Vector2(inRect.x, inRect.y);
			var titleRect = new Rect(pos.x, pos.y, inRect.width, 48f);
			Widgets.Label(titleRect, title);
			pos.y += 48f;
			float listHeight = transactionProcess.allBanks.Count * 28f;
			Rect viewRect = new Rect(pos.x, pos.y, inRect.width, (inRect.height - pos.y) - 90);
			Rect scrollRect = new Rect(pos.x, pos.y, inRect.width - 16f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			for (var i = 0; i < transactionProcess.allBanks.Count; i++)
            {
				var bank = transactionProcess.allBanks[i];
				Text.Anchor = TextAnchor.MiddleLeft;
				var entryRect = new Rect(pos.x, pos.y, inRect.width, 24);
				if (i % 2 == 1)
				{
					Widgets.DrawLightHighlight(entryRect);
				}
				var bankIcon = new Rect(pos.x, pos.y, 24, 24);
				GUI.color = bank.parentFaction.Color;
				GUI.DrawTexture(bankIcon, bank.parentFaction.def.FactionIcon);
				GUI.color = Color.white;

				var bankLabel = new Rect(bankIcon.xMax + 10, pos.y, 200, 24);
				Widgets.Label(bankLabel, bank.Name);

				var bankMoney = bank.DepositAmount;
				if (this.transactionProcess.amountToTransfer != null && this.transactionProcess.amountToTransfer.TryGetValue(bank, out var amount))
                {
					bankMoney += amount;
				}

				var depositAmountRect = new Rect(bankLabel.xMax, pos.y, 65, 24);
				Widgets.Label(depositAmountRect, bankMoney.ToStringMoney());

				var withdrawFullyRect = new Rect(depositAmountRect.xMax, pos.y, 24, 24);
				if (bankMoney == 0 && this.transactionProcess.amountToSpend[bank] == 0)
				{
					GUI.color = Widgets.InactiveColor;
				}
				if (Widgets.ButtonText(withdrawFullyRect, "<<"))
				{
					this.transactionProcess.amountToSpend[bank] = 0;
				}
				var withdrawRect = new Rect(withdrawFullyRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(withdrawRect, "<") && this.transactionProcess.amountToSpend[bank] > 0)
				{
					this.transactionProcess.amountToSpend[bank] -= 1 * GenUI.CurrentAdjustmentMultiplier();
				}
				var textEntry = new Rect(withdrawRect.xMax + 5, pos.y, 60, 24);
				textEntryBuffer = this.transactionProcess.amountToSpend[bank].ToString();
				var value = this.transactionProcess.amountToSpend[bank];
				Widgets.TextFieldNumeric<int>(textEntry, ref value, ref textEntryBuffer, 0, Mathf.Min(bankMoney, transactionProcess.transactionCost));
				this.transactionProcess.amountToSpend[bank] = value;

				var depositRect = new Rect(textEntry.xMax + 5, pos.y, 24, 24);
				if (Widgets.ButtonText(depositRect, ">") && bankMoney > 0 && transactionProcess.transactionCost > allMoneyToSpend)
				{
					this.transactionProcess.amountToSpend[bank] += 1 * GenUI.CurrentAdjustmentMultiplier();
				}
				var depositFullyRect = new Rect(depositRect.xMax, pos.y, 24, 24);
				if (Widgets.ButtonText(depositFullyRect, ">>") && bankMoney > 0 && transactionProcess.transactionCost > this.transactionProcess.amountToSpend.Where(x => x.Key != bank).Sum(x => x.Value))
				{
					this.transactionProcess.amountToSpend[bank] = (int)Mathf.Min(bankMoney, transactionProcess.transactionCost - this.transactionProcess.amountToSpend.Where(x => x.Key != bank).Sum(x => x.Value));
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
			Widgets.Label(transactionCostToPay, "VTE.TransactionCostToPay".Translate(transactionProcess.transactionCost));
			
			var moneyToBePaid = new Rect(transactionCostToPay.xMax + 40, pos.y, 250, 24);
			Widgets.Label(moneyToBePaid, "VTE.MoneyToSpend".Translate(allMoneyToSpend));

			pos.y += 30;
			bool canPay = allMoneyToSpend == transactionProcess.transactionCost;
			GUI.color = canPay ? Color.white : Color.grey;
			var confirmButtonRect = new Rect(pos.x + 15, pos.y, 170, 32f);
			if (Widgets.ButtonText(confirmButtonRect, "Confirm".Translate(), active: canPay))
			{
				this.transactionProcess.PerformTransaction();
				this.Close();
			}
			GUI.color = Color.white;
			var closeButtonRect = new Rect(confirmButtonRect.xMax + 85, confirmButtonRect.y, confirmButtonRect.width, confirmButtonRect.height);
			if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
			{
				transactionProcess.PostCancel();
				this.Close();
			}
		}

        public override void PostClose()
        {
            base.PostClose();
			transactionProcess.PostClose();
        }
    }
}
