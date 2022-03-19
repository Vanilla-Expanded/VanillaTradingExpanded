using HarmonyLib;
using NAudio.Codecs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace VanillaTradingExpanded
{

    [HotSwappable]
	[StaticConstructorOnStartup]
	public class Window_StockMarket : Window
	{
		private static List<CurveMark> marks = new List<CurveMark>();
		public enum Column
        {
			None,
			Company,
			Value,
			SharesHeld, 
			Profit,
			RecentChange
        }

		private static Dictionary<Column, string> sortByButtons = new Dictionary<Column, string>
		{
			{Column.Company, "VTE.Name".Translate()},
			{Column.Value, "VTE.Value".Translate()},
			{Column.SharesHeld, "VTE.SharesHeld".Translate()},
			{Column.Profit, "VTE.Profit".Translate()},
			{Column.RecentChange, "VTE.RecentChange".Translate()},
		};

		public class HeaderData
        {
			public float xOffset;
			public string name;
			public Color color;
			public float headerWidth = 80;
		}

		private static List<HeaderData> headers => new List<HeaderData>
		{
			new HeaderData
			{
				xOffset = 25,
				name = "VTE.Company".Translate(),
				color = Color.white
			},
			new HeaderData
			{
				xOffset = 165,
				name = "VTE.Value".Translate(),
				color = Color.white
			},
			new HeaderData
			{
				xOffset = 5,
				name = "VTE.Shares".Translate(),
				color = Color.white
			},
			new HeaderData
			{
				xOffset = 5,
				name = "VTE.ProfitLoss".Translate(),
				color = Color.white
			},
			new HeaderData
			{
				xOffset = 25,
				name = "VTE.RecentChange".Translate(),
				color = Color.white
			},
			new HeaderData
			{
				xOffset = 50,
				name = "VTE.SharesTransactionHeader".Translate(),
				color = Color.grey,
				headerWidth = 130
			},
			new HeaderData
			{
				xOffset = 5,
				name = "VTE.FollowNews".Translate(),
				color = Color.white
			},
		};

		private bool dirty;

		private Vector2 scrollPosition;

		private bool sortDescending;
		public bool SortingDescending
		{
			get
			{
				if (SortingBy != Column.None)
				{
					return sortDescending;
				}
				return false;
			}
		}

		public TransactionProcess transactionProcess;
		public Window_StockMarket()
		{
			SetDirty();
			this.forcePause = true;
			this.closeOnClickedOutside = true;
			transactionProcess = new TransactionProcess();
		}

		private static float maxWidth = 960;
		private static float maxHeight = 700;
		private static float cellHeight = 32f;

		private static int cachedSortByLabelHeight = 35;
		private static int cachedSortByLabelWidth = 50;
		private static int cachedSortByButtonHeight = 35;
		private static int cachedSortByButtonWidth = 125;
		private static float sortByXOffset = 10;
		private static float thingMarketValueXOffset = 35;
		private static float entryWidth = 50;
		private static float thingRecentChangeWidth = 60;
		public override Vector2 InitialSize => new Vector2(maxWidth, Mathf.Min(maxHeight, UI.screenHeight));
		public override void DoWindowContents(Rect inRect)
		{
			RecacheIfDirty();
			Vector2 pos = Vector2.zero;
			pos.x = 15f;
			Text.Font = GameFont.Medium;
			var colonyNameSize = Text.CalcSize(Faction.OfPlayer.Name);
			var colonyNameRect = new Rect(pos.x, pos.y, colonyNameSize.x, 40);
			Widgets.Label(colonyNameRect, Faction.OfPlayer.Name);

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;

			transactionProcess.allMoneyInBanks = transactionProcess.allBanks.Sum(x => x.DepositAmount);
			transactionProcess.transactionGain = (int)Mathf.Abs(transactionProcess.companySharesToBuyOrSell.Where(x => x.Value < 0).Sum(x => x.Value * x.Key.currentValue));
			transactionProcess.transactionCost = (int)transactionProcess.companySharesToBuyOrSell.Where(x => x.Value > 0).Sum(x => x.Value * x.Key.currentValue);
			transactionProcess.totalTransaction = transactionProcess.transactionGain - transactionProcess.transactionCost;

			var moneyStatus = "VTE.MoneyInBanks".Translate(transactionProcess.allMoneyInBanks);
			var size = Text.CalcSize(moneyStatus);
			var moneyInBanksRect = new Rect(colonyNameRect.xMax + 20, colonyNameRect.y + (colonyNameRect.height / 2f) - (size.y / 2f) - 3f, size.x, size.y);
			Widgets.Label(moneyInBanksRect, moneyStatus);
			pos.y += 40;

			var sortByKeys = sortByButtons.Keys.ToList();
			var sortyByLabelRect = new Rect(pos.x, pos.y, cachedSortByLabelWidth, cachedSortByLabelHeight);
			Widgets.Label(sortyByLabelRect, "VTE.SortBy".Translate());
			pos.x += sortyByLabelRect.width + sortByXOffset;

			for (int i = 0; i < sortByKeys.Count; i++)
			{
				Rect buttonRect = new Rect(pos.x, pos.y, cachedSortByButtonWidth, cachedSortByButtonHeight);
				if (Widgets.ButtonText(buttonRect, sortByButtons[sortByKeys[i]]))
                {
					SortBy(sortByKeys[i], !sortDescending);
                }
				pos.x += cachedSortByButtonWidth + sortByXOffset;
			}
			pos.x = 15f;
			pos.y += 40;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.LowerCenter;

			for (int i = 0; i < headers.Count; i++)
			{
				Rect labelRect = new Rect(pos.x + headers[i].xOffset, pos.y, headers[i].headerWidth, 32);
				MouseoverSounds.DoRegion(labelRect);
				GUI.color = headers[i].color;
				Widgets.Label(labelRect, headers[i].name);
				GUI.color = Color.white;
				pos.x += headers[i].headerWidth + headers[i].xOffset;
			}
			Text.Anchor = TextAnchor.MiddleCenter;

			pos.x = 15f;
			pos.y += 35;

			float listHeight = cachedCompanies.Count * cellHeight;
			Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 15f, 300);
			Rect scrollRect = new Rect(pos.x, pos.y, inRect.width - 31f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			GUI.color = new Color(1f, 1f, 1f, 0.2f);
			Widgets.DrawLineHorizontal(0f, pos.y, viewRect.width);
			GUI.color = Color.white;
			Text.Font = GameFont.Small;
			float num = 0f;

			for (int j = 0; j < cachedCompanies.Count; j++)
			{
				if (num > scrollPosition.y - cellHeight && num < scrollPosition.y + viewRect.height)
                {
					Text.Anchor = TextAnchor.MiddleLeft;
					var company = cachedCompanies[j];
					Rect rect2 = new Rect(pos.x, pos.y, viewRect.width, cellHeight);

					if (j % 2 == 1)
					{
						Widgets.DrawLightHighlight(rect2);
					}

					var chartIconRect = new Rect(rect2.x, rect2.y, rect2.height, rect2.height);
					GUI.DrawTexture(chartIconRect, GuiHelper.ChartIcon);
					if (Mouse.IsOver(chartIconRect))
                    {
						marks.Clear();
						var graphRect = new Rect(UI.MousePositionOnUIInverted.x + 15, UI.MousePositionOnUIInverted.y + 15, 800, 400);
						Find.WindowStack.ImmediateWindow(company.GetHashCode(), graphRect, WindowLayer.Dialog, delegate
						{
							Text.Font = GameFont.Small;
							Rect rect = graphRect.AtZero();
							Widgets.DrawWindowBackground(rect);
							Rect position = rect.ContractedBy(10f);
							GUI.BeginGroup(position);
							Rect legendRect = new Rect(0, 0, graphRect.width, graphRect.height);
							var recorder = company.recorder;
							var graphSection = new FloatRange(0f, Mathf.Min(60f, recorder.records.Count));
							SimpleCurveDrawer_Patch.modify = true;
							recorder.DrawGraph(position, legendRect, graphSection, marks);
							SimpleCurveDrawer_Patch.modify = false;
							GUI.EndGroup();
						}, doBackground: false);
					}

					var companyNameRect = new Rect(chartIconRect.xMax + 10, rect2.y, 210, rect2.height);
					Widgets.Label(companyNameRect, company.name);

					Text.Anchor = TextAnchor.MiddleCenter;

					var companyValueRect = new Rect(companyNameRect.xMax + thingMarketValueXOffset, rect2.y, entryWidth, rect2.height);
					Widgets.Label(companyValueRect, company.currentValue.ToStringDecimalIfSmall());
					
					var sharesHeldRect = new Rect(companyValueRect.xMax + thingMarketValueXOffset, rect2.y, entryWidth, rect2.height);
					Widgets.Label(sharesHeldRect, company.sharesHeldByPlayer.Count.ToString());

					var profitLossRect = new Rect(sharesHeldRect.xMax + thingMarketValueXOffset, rect2.y, entryWidth, rect2.height);
					var profit = GetProfit(company);
					if (profit > 0)
                    {
						GUI.color = Color.green;
					}
					else if (profit < 0)
                    {
						GUI.color = Color.red;
					}
					Widgets.Label(profitLossRect, profit.ToString());
					GUI.color = Color.white;

					var recentChange = GetRecentChangeFor(company);
					var profitLossChangeRect = new Rect(profitLossRect.xMax + 50, rect2.y, thingRecentChangeWidth, rect2.height);
					var recentChangeString = recentChange.ToStringDecimalIfSmall() + "%";
					if (recentChange > 0)
					{
						GUI.color = Color.green;
						recentChangeString += "▲";
					}
					else if (recentChange < 0)
					{
						GUI.color = Color.red;
						recentChangeString += "▼";
					}
					Widgets.Label(profitLossChangeRect, recentChangeString);
					GUI.color = Color.white;

					if (!transactionProcess.companySharesToBuyOrSell.TryGetValue(company, out var amount))
                    {
						transactionProcess.companySharesToBuyOrSell[company] = amount = 0;
					}
					var shareBuyRect = new Rect(profitLossChangeRect.xMax + 55, rect2.y + 1, rect2.height - 2, rect2.height - 2);
					if (Widgets.ButtonText(shareBuyRect, "<"))
					{
						amount--;
					}

					GUI.color = Color.white;
					var textEntry = new Rect(shareBuyRect.xMax + 5, rect2.y + 1, 60, rect2.height - 2);

					textEntryBuffer = amount.ToString();
					Widgets.TextFieldNumeric<int>(textEntry, ref amount, ref textEntryBuffer, -company.sharesHeldByPlayer.Count);
					var shareSellRect = new Rect(textEntry.xMax + 5, rect2.y + 1, rect2.height - 2, rect2.height - 2);
					if (Widgets.ButtonText(shareSellRect, ">"))
					{
						amount++;
					}
					var followNewsBox = new Rect(shareSellRect.xMax + 40, rect2.y + 2.5f, rect2.height - 5, rect2.height - 5);
					GUI.DrawTexture(followNewsBox, checkBox);
					Widgets.Checkbox(new Vector2(followNewsBox.x + 3f, followNewsBox.y + 3f), ref company.playerFollowsNews, size: followNewsBox.height - 5, paintable: true);
					transactionProcess.companySharesToBuyOrSell[company] = amount;
					GUI.color = Color.white;
				}
				pos.y += cellHeight;
				num += cellHeight;
			}
			Widgets.EndScrollView();
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Small;
			pos.y = 450;
			var breakDownTitleRect = new Rect(pos.x, pos.y, 200, 30);
			Widgets.Label(breakDownTitleRect, "VTE.Breakdown".Translate());
			pos.y += 30;

			var transactionCostRect = new Rect(pos.x, pos.y, inRect.width - 30f, 30);
			Widgets.DrawLightHighlight(transactionCostRect);
			var transactionCostLabelRect = new Rect(transactionCostRect.x + 100, transactionCostRect.y, 150, transactionCostRect.height);
			Widgets.Label(transactionCostLabelRect, "VTE.TransactionCost".Translate());
			var transactionCostAmountRect = new Rect(500, transactionCostRect.y, 120, transactionCostRect.height);
			Widgets.Label(transactionCostAmountRect, transactionProcess.transactionCost.ToString());
			pos.y += 30;

			var transactionGainRect = new Rect(pos.x, pos.y, inRect.width - 30f, 30);
			var transactionGainLabelRect = new Rect(transactionGainRect.x + 100, transactionGainRect.y, 150, transactionGainRect.height);
			Widgets.Label(transactionGainLabelRect, "VTE.TransactionGain".Translate());
			var transactionGainAmountRect = new Rect(500, transactionGainRect.y, 120, transactionGainRect.height);
			Widgets.Label(transactionGainAmountRect, transactionProcess.transactionGain.ToString());
			pos.y += 30;

			var totalRect = new Rect(pos.x, pos.y, inRect.width - 30f, 30);
			Widgets.DrawLightHighlight(totalRect);
			var totalLabelRect = new Rect(totalRect.x + 100, totalRect.y, 150, totalRect.height);
			Widgets.Label(totalLabelRect, "VTE.Total".Translate());
			var totalAmountRect = new Rect(500, totalRect.y, 120, totalRect.height);
			Widgets.Label(totalAmountRect, transactionProcess.totalTransaction.ToString());
			pos.y += 30;

			var moneyInBanksAfterRect = new Rect(pos.x, pos.y, inRect.width - 30f, 30);
			var moneyInBanksAfterLabelRect = new Rect(moneyInBanksAfterRect.x + 100, moneyInBanksAfterRect.y, 150, moneyInBanksAfterRect.height);
			Widgets.Label(moneyInBanksAfterLabelRect, "VTE.MoneyInBanksAfter".Translate());
			var moneyInBanksAfterAmountRect = new Rect(500, moneyInBanksAfterRect.y, 120, moneyInBanksAfterRect.height);
			Widgets.Label(moneyInBanksAfterAmountRect, (transactionProcess.totalTransaction + transactionProcess.allMoneyInBanks).ToString());
			pos.y += 30;

			GUI.color = Color.grey;
			Text.Font = GameFont.Tiny;
			var breakdownDescriptionRect = new Rect(pos.x, inRect.height - 50, 380, 50);
			Widgets.Label(breakdownDescriptionRect, "VTE.BreakdownStockMarketDesc".Translate());

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			bool canPay = (transactionProcess.totalTransaction + transactionProcess.allMoneyInBanks) >= 0;
			GUI.color = canPay ? Color.white : Color.grey;
			var confirmButtonRect = new Rect(breakdownDescriptionRect.xMax + 150, breakdownDescriptionRect.y + 10, 170, 32f);
			if (Widgets.ButtonText(confirmButtonRect, "Confirm".Translate(), active: canPay))
            {
				if (transactionProcess.transactionGain > 0)
                {
					Find.WindowStack.Add(new Window_PerformTransactionGains("VTE.BankDepositsToGain".Translate(), transactionProcess));
				}
				else if (transactionProcess.transactionCost > 0)
                {
					Find.WindowStack.Add(new Window_PerformTransactionCosts("VTE.BankDepositsToSpend".Translate(), transactionProcess));
				}
                else
                {
					this.Close();
                }
			}
			GUI.color = Color.white;
			var closeButtonRect = new Rect(confirmButtonRect.xMax + 25, confirmButtonRect.y, confirmButtonRect.width, confirmButtonRect.height);
			if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
			{
				this.Close();
			}
		}

		public static readonly Texture2D checkBox = ContentFinder<Texture2D>.Get("UI/Widgets/WorkBoxBG_Bad");
		public string textEntryBuffer;
		private float GetRecentChangeFor(Company company)
        {
			var previousPrice = company.recorder.GetPriceInPreviousDays(3);
			var change = company.currentValue - previousPrice;
			return (change / previousPrice) * 100f;
		}

		private float GetProfit(Company company)
        {
			if (company.sharesHeldByPlayer.Count > 0)
            {
				return Mathf.FloorToInt((company.currentValue * company.sharesHeldByPlayer.Count) - company.sharesHeldByPlayer.Sum(x => x.valueBought));
			}
			return 0f;
        }
		public void SetDirty()
		{
			dirty = true;
		}

		private Column sortByColumn;
		private Column SortingBy => sortByColumn;
		private void SortBy(Column column, bool descending)
		{
			if (column != sortByColumn)
            {
				sortDescending = false;
            }
			else
            {
				sortDescending = descending;
			}
			sortByColumn = column;
			SetDirty();
		}
		private void RecacheIfDirty()
		{
			if (dirty)
			{
				dirty = false;
				RecacheTradeables();
			}
		}

		private List<Company> cachedCompanies = new List<Company>();
		private void RecacheTradeables()
		{
			cachedCompanies.Clear();
			cachedCompanies.AddRange(TradingManager.Instance.companies);
			StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = true;
			cachedCompanies = SortByColumn(sortByColumn);
			StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = false;
		}
		private List<Company> SortByColumn(Column column)
        {
			switch (column)
            {
				case Column.None: return sortDescending ? cachedCompanies.OrderByDescending(x => x.name).ToList() : cachedCompanies.OrderBy(x => x.name).ToList();
				case Column.Company: return sortDescending ? cachedCompanies.OrderByDescending(x => x.name).ToList() : cachedCompanies.OrderBy(x => x.name).ToList();
				case Column.Value: return sortDescending ? cachedCompanies.OrderByDescending(x => x.currentValue).ToList() : cachedCompanies.OrderBy(x => x.currentValue).ToList();
				case Column.RecentChange: return sortDescending ? cachedCompanies.OrderByDescending(x => GetRecentChangeFor(x)).ToList() : cachedCompanies.OrderBy(x => GetRecentChangeFor(x)).ToList();
				case Column.SharesHeld: return sortDescending ? cachedCompanies.OrderByDescending(x => x.sharesHeldByPlayer.Count).ToList() : cachedCompanies.OrderBy(x => x.sharesHeldByPlayer.Count).ToList();
				case Column.Profit: return sortDescending ? cachedCompanies.OrderByDescending(x => GetProfit(x)).ToList() : cachedCompanies.OrderBy(x => GetProfit(x)).ToList();
				default: return sortDescending ? cachedCompanies.OrderByDescending(x => x.name).ToList() : cachedCompanies.OrderBy(x => x.name).ToList();
			}
		}
    }
}
