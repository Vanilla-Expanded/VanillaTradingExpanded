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
		[TweakValue("0TRADING", 0, 1000)] private static float maxHeight = 600;

		private Pawn negotiator;
		private Vector2 scrollPosition;
		public Faction faction;
		public Bank bank;
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
			DrawWithdrawBox(inRect, ref posY);
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

		public int amountToDepositOrWithDraw;
		public string textEntryBuffer;
		private void DrawWithdrawBox(Rect inRect, ref float posY)
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
			if (bank.DepositAmount == 0 && amountToDepositOrWithDraw == 0)
            {
				GUI.color = Widgets.InactiveColor;
			}
			if (Widgets.ButtonText(withdrawFullyRect, "<<"))
            {
				amountToDepositOrWithDraw = -Mathf.Max(bank.DepositAmount, 0);
			}
			var withdrawRect = new Rect(withdrawFullyRect.xMax, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(withdrawRect, "<") && bank.DepositAmount > -amountToDepositOrWithDraw)
			{
				amountToDepositOrWithDraw--;
			}

			GUI.color = Color.white;
			var textEntry = new Rect(withdrawRect.xMax + 5, withdrawBox.y, 60, 24);

			textEntryBuffer = amountToDepositOrWithDraw.ToString();
			Widgets.TextFieldNumeric<int>(textEntry, ref amountToDepositOrWithDraw, ref textEntryBuffer, -999999);

			var depositRect = new Rect(textEntry.xMax + 5, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(depositRect, ">"))
			{
				if (silverThings.Sum(x => x.stackCount) > amountToDepositOrWithDraw)
                {
					amountToDepositOrWithDraw++;
				}
			}
			var depositFullyRect = new Rect(depositRect.xMax, withdrawBox.y, 24, 24);
			if (Widgets.ButtonText(depositFullyRect, ">>"))
			{
				amountToDepositOrWithDraw = silverThings.Sum(x => x.stackCount);
			}
			var confirmRect = new Rect(depositFullyRect.xMax + 5, withdrawBox.y, 90, 24);

			if (Widgets.ButtonText(confirmRect, "Confirm".Translate()))
			{
				if (amountToDepositOrWithDraw != 0)
                {
					var warning = amountToDepositOrWithDraw > 0 ? "VTE.AreYouSureYouWantToDeposit".Translate(amountToDepositOrWithDraw)
							: "VTE.AreYouSureYouWantToWithdraw".Translate(amountToDepositOrWithDraw);
					Find.WindowStack.Add(new Dialog_MessageBox(warning, "Yes".Translate(), delegate ()
					{
						if (amountToDepositOrWithDraw > 0)
						{
							bank.DepositSilver(silverThings, amountToDepositOrWithDraw);
						}
						else if (amountToDepositOrWithDraw < 0)
						{
							bank.WithdrawSilver(negotiator, -amountToDepositOrWithDraw);
						}
						amountToDepositOrWithDraw = 0;
						cachedThings = null;
					}, "No".Translate()));
				}
			}

			posY = withdrawBox.yMax;
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
