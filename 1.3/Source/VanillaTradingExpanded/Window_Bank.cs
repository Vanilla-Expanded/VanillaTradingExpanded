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
using Verse.Sound;

namespace VanillaTradingExpanded
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class HotSwappableAttribute : Attribute
	{
	}

	[HotSwappableAttribute]
	public class Window_Bank : Window
	{

		[TweakValue("0TRADING", 0, 1000)] private static float maxWidth = 800;
		[TweakValue("0TRADING", 0, 1000)] private static float maxHeight = 700;

		private Pawn negotiator;
		private Vector2 scrollPosition;
		private Faction faction;
		private Bank bank;
		public Window_Bank(Pawn negotiator, Faction bankFaction)
		{
			this.negotiator = negotiator;
			faction = bankFaction;
			bank = faction.GetBank();
		}

		public override Vector2 InitialSize => new Vector2(maxWidth, Mathf.Min(maxHeight, UI.screenHeight));
		public override void DoWindowContents(Rect inRect)
		{
			var posY = 0f;
			DrawBankStatus(inRect, posY);
			DrawBankFeesInfo(inRect, ref posY);
			DrawDepositAndWithdrawInfo(inRect, ref posY);
			DrawLoanOptions(inRect, ref posY);
		}

		[TweakValue("0TRADING", 0, 1000)] private static float testx = 5;
		[TweakValue("0TRADING", 0, 1000)] private static float testy = 5;
		[TweakValue("0TRADING", 0, 1000)] private static float width = 120;
		[TweakValue("0TRADING", 0, 1000)] private static float height = 70;
		private void DrawBankStatus(Rect inRect, float posY)
        {
			Text.Font = GameFont.Medium;
			var factionNameRect = new Rect(inRect.x, posY, 120, 30);
			Widgets.Label(factionNameRect, Faction.OfPlayer.Name);
			Text.Font = GameFont.Small;

			var localFundsRect = new Rect(factionNameRect.x, factionNameRect.yMax, factionNameRect.width, 24);
			Widgets.Label(localFundsRect, "VTE.LocalFunds".Translate(bank.DepositAmount));

			var bankBalanceRect = new Rect(factionNameRect.x, localFundsRect.yMax - 5, factionNameRect.width, 24);
			Widgets.Label(bankBalanceRect, "VTE.BankBalance".Translate(bank.Balance));
		}

		private void DrawBankFeesInfo(Rect inRect, ref float posY)
        {
			Text.Font = GameFont.Medium;
			var box = new Rect(inRect.xMax - 300, posY, 300, 30);
			var feesInfoRect = new Rect(box.x, box.y, 300, 30);
			Widgets.Label(feesInfoRect, "VTE.BankFees".Translate((bank.Fees * 100f).ToStringDecimalIfSmall()));
			Text.Font = GameFont.Small;
			GUI.color = Color.gray;
			var feesInfoDescRect = new Rect(feesInfoRect.x, feesInfoRect.yMax, 300, 24);
			Widgets.Label(feesInfoDescRect, "VTE.BankFeesBasedOn".Translate(faction.GoodwillWith(Faction.OfPlayer), faction.Named("FACTION")));
			GUI.color = Color.white;
			posY = feesInfoDescRect.yMax + 20;
		}

		public int amountToDepositOrWithdraw;
		public string textEntryBuffer;
		private void DrawDepositAndWithdrawInfo(Rect inRect, ref float posY)
		{
			var depositOrWithdrawLabelRect = new Rect(inRect.x, posY, 300, 24);
			Widgets.Label(depositOrWithdrawLabelRect, "VTE.DepositOrWithdraw".Translate());

			Text.Font = GameFont.Tiny;
			GUI.color = Color.gray;
			var depositOrWithdrawDescRect = new Rect(depositOrWithdrawLabelRect.xMax + 150, posY, 350, 24);
			Widgets.Label(depositOrWithdrawDescRect, "VTE.DepositOrWithdrawDesc".Translate());
			GUI.color = Color.white;
			Text.Font = GameFont.Small;

			var withdrawBox = new Rect(inRect.x, depositOrWithdrawLabelRect.yMax, inRect.width, 24);
			Widgets.DrawLightHighlight(withdrawBox);

			var silverIcon = new Rect(withdrawBox.x + 5, withdrawBox.y, 24, 24);
			Widgets.ThingIcon(silverIcon, ThingDefOf.Silver);

			Text.Anchor = TextAnchor.MiddleCenter;
			var silverLabel = new Rect(silverIcon.xMax + 5, withdrawBox.y, 60, 24);
			Widgets.Label(silverLabel, ThingDefOf.Silver.LabelCap);
			Text.Anchor = TextAnchor.UpperLeft;

			var silverThings = AvailableSilver;
			var withdrawFullyRect = new Rect(silverLabel.xMax + 410, withdrawBox.y, 24, 24);
			if (bank.DepositAmount == 0 && amountToDepositOrWithdraw == 0)
            {
				GUI.color = Widgets.InactiveColor;
			}
			if (Widgets.ButtonText(withdrawFullyRect, "<<"))
            {
				amountToDepositOrWithdraw = -Mathf.Max(bank.DepositAmount, 0);
			}
			var withdrawRect = new Rect(withdrawFullyRect.xMax, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(withdrawRect, "<") && bank.DepositAmount > -amountToDepositOrWithdraw)
			{
				amountToDepositOrWithdraw--;
			}

			GUI.color = Color.white;
			var textEntry = new Rect(withdrawRect.xMax + 5, withdrawBox.y, 60, 24);

			textEntryBuffer = amountToDepositOrWithdraw.ToString();
			Widgets.TextFieldNumeric<int>(textEntry, ref amountToDepositOrWithdraw, ref textEntryBuffer, -999999);

			var depositRect = new Rect(textEntry.xMax + 5, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(depositRect, ">"))
			{
				if (silverThings.Sum(x => x.stackCount) > amountToDepositOrWithdraw)
                {
					amountToDepositOrWithdraw++;
				}
			}
			var depositFullyRect = new Rect(depositRect.xMax, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(depositFullyRect, ">>"))
			{
				amountToDepositOrWithdraw = silverThings.Sum(x => x.stackCount);
			}
			var confirmRect = new Rect(depositFullyRect.xMax + 5, withdrawBox.y, 90, 24);

			if (Widgets.ButtonText(confirmRect, "Confirm".Translate()))
			{
				if (amountToDepositOrWithdraw != 0)
                {
					var warning = amountToDepositOrWithdraw > 0 ? "VTE.AreYouSureYouWantToDeposit".Translate(amountToDepositOrWithdraw)
							: "VTE.AreYouSureYouWantToWithdraw".Translate(amountToDepositOrWithdraw);
					Find.WindowStack.Add(new Dialog_MessageBox(warning, "Yes".Translate(), delegate ()
					{
						if (amountToDepositOrWithdraw > 0)
						{
							bank.DepositSilver(silverThings, amountToDepositOrWithdraw);
						}
						else if (amountToDepositOrWithdraw < 0)
						{
							bank.WithdrawSilver(negotiator, -amountToDepositOrWithdraw);
						}
						amountToDepositOrWithdraw = 0;
						cachedThings = null;
					}, "No".Translate()));
				}
			}
			posY = withdrawBox.yMax + 20;

			var breakdownLabelRect = new Rect(inRect.x, posY, 200, 24);
			Widgets.Label(breakdownLabelRect, "VTE.Breakdown".Translate());
			Text.Font = GameFont.Tiny;
			GUI.color = Color.gray;
			var breakdownLabelDescRect = new Rect(breakdownLabelRect.xMax + 180, posY + 3, 400, 24);
			Widgets.Label(breakdownLabelDescRect, "VTE.BreakdownDesc".Translate());
			GUI.color = Color.white;
			Text.Font = GameFont.Small;

			Text.Anchor = TextAnchor.MiddleLeft;

			posY = breakdownLabelDescRect.yMax + 5;
			var amountAbs = Mathf.Abs(amountToDepositOrWithdraw);
			DrawBreakdownLine(inRect, ref posY, amountToDepositOrWithdraw == 0 ? "VTE.AmountToDepositWithdraw".Translate() : amountToDepositOrWithdraw > 0
							? "VTE.AmountToDeposit".Translate() : "VTE.AmountToWithdraw".Translate(), amountAbs.ToString(), true);

			var amountAfterFees = (int)(amountAbs * (1f - bank.Fees));
			DrawBreakdownLine(inRect, ref posY, "VTE.FeesToPay".Translate(), (amountAbs - amountAfterFees).ToString(), false);
			DrawBreakdownLine(inRect, ref posY, "VTE.Total".Translate(), amountAfterFees.ToString(), true);

			Text.Anchor = TextAnchor.UpperLeft;

		}

		private void DrawBreakdownLine(Rect inRect, ref float posY, string label, string amount, bool highlight)
        {
			var lineBox = new Rect(inRect.x, posY, inRect.width, 30);
			if (highlight)
            {
				Widgets.DrawHighlight(lineBox);
			}
			var labelRect = new Rect(lineBox.x + 100, lineBox.y, Text.CalcSize(label).x, lineBox.height);
			Widgets.Label(labelRect, label);
			var amountRect = new Rect(lineBox.x + 500, lineBox.y, 80, lineBox.height);
			Widgets.Label(amountRect, amount);

			posY = lineBox.yMax;
		}


		public void DrawLoanOptions(Rect inRect, ref float posY)
        {
			var loanOptions = bank.bankExtension.loanOptions;
			posY += 10;
			var titleRect = new Rect(inRect.x, posY, 60, 24);
			Widgets.Label(titleRect, "VTE.Loans".Translate());
			posY = titleRect.yMax;
			var outRect = new Rect(inRect.x, posY, inRect.width, 330);
			var height = Mathf.CeilToInt(loanOptions.Count / 2f) * 160;

			var viewRect = new Rect(outRect.x, posY, inRect.width - 16, height);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			var posX = inRect.x;
			for (var i = 1; i <= loanOptions.Count; i++)
            {
				if (i % 2 == 0)
				{
					posX = inRect.x + 430;
				}
				else
				{
					posX = inRect.x;
				}
				var loanBox = new Rect(posX, posY, 300, 150);
				Widgets.DrawHighlight(loanBox);
				DrawLoanOption(loanBox, loanOptions[i - 1]);
				if (i % 2 == 0)
                {
					posY += 160;
				}
			}
			Widgets.EndScrollView();
        }

		private void DrawLoanOption(Rect rect, LoanOption loanOption)
        {
			var loanTitle = new Rect(rect.x + 10, rect.y + 5, 250, 24);
			Widgets.Label(loanTitle, loanOption.loanNameKey.Translate());

			if (bank.Balance > 0 || loanOption.fixedLoanAmount.HasValue)
            {
				var silverIcon = new Rect(rect.x + 90, loanTitle.yMax, 24, 24);
				Widgets.ThingIcon(silverIcon, ThingDefOf.Silver);

				Text.Anchor = TextAnchor.MiddleLeft;
				Text.Font = GameFont.Medium;
				var silverLabel = new Rect(silverIcon.xMax + 5, silverIcon.y, 60, 24);
				Widgets.Label(silverLabel, loanOption.GetLoanAmountFrom(bank).ToString());
				Text.Anchor = TextAnchor.UpperLeft;
				Text.Font = GameFont.Small;

				var repayLabel = new Rect(loanTitle.x, silverLabel.yMax, 250, 24);
				Widgets.Label(repayLabel, "VTE.Repay".Translate() + " " + loanOption.GetRepayAmountFrom(bank));

				var repayDateLabel = new Rect(repayLabel.x, repayLabel.yMax, 250, 24);
				Widgets.Label(repayDateLabel, "VTE.RepayDate".Translate(loanOption.GetRepayDateFor(negotiator)));

				var takeLoan = new Rect(rect.x + 50, repayDateLabel.yMax + 5, rect.width - 100, 30);
				if (Widgets.ButtonText(takeLoan, "VTE.TakeLoan".Translate()))
                {

                }
			}
			else
			{
				Text.Anchor = TextAnchor.MiddleCenter;
				Text.Font = GameFont.Medium;
				Widgets.Label(rect.ContractedBy(20), "VTE.LoanOptionsUnlockText".Translate());
			}
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
		}

		private List<Thing> cachedThings;
		public List<Thing> AvailableSilver
        {
			get
            {
				if (cachedThings is null)
				{
					cachedThings = negotiator.Map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where((Thing x) => !x.Position.Fogged(x.Map)
						&& (negotiator.Map.areaManager.Home[x.Position] || x.IsInAnyStorage())
						&& negotiator.Map.reachability.CanReach(negotiator.Position, x, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Some)).ToList();
				}
				return cachedThings;
			}
        }
	}
}
