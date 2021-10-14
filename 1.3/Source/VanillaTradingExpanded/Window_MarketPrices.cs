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

	public class Window_MarketPrices : Window
	{
		private enum Column
        {
			None,
			Item,
			MarketValue,
			CurrentValue, 
			Change,
			RecentChange
        }

		private static Dictionary<Column, string> sortByButtons = new Dictionary<Column, string>
		{
			{Column.Item, "VTE.Name".Translate()},
			{Column.MarketValue, "VTE.MarketValue".Translate()},
			{Column.CurrentValue, "VTE.CurrentValue".Translate()},
			{Column.Change, "VTE.Change".Translate()},
			{Column.RecentChange, "VTE.RecentChange".Translate()},
		};

		private static Dictionary<Column, Tuple<int, string>> headers = new Dictionary<Column, Tuple<int, string>>
		{
			{Column.Item, new Tuple<int, string>(70, "VTE.Item".Translate())},
			{Column.MarketValue, new Tuple<int, string>(100, "VTE.MarketValue".Translate())},
			{Column.CurrentValue, new Tuple<int, string>(50, "VTE.CurrentValue".Translate())},
			{Column.Change, new Tuple<int, string>(50, "VTE.Change".Translate())},
			{Column.RecentChange, new Tuple<int, string>(50, "VTE.RecentChange".Translate())},
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
		public Window_MarketPrices()
		{
			SetDirty();
		}

		private static float maxWidth = 800;
		private static float maxHeight = 600;

		private static float cellHeight = 35f;

		private static int cachedSortByLabelHeight = 35;
		private static int cachedSortByLabelWidth = 50;
		private static int cachedSortByButtonHeight = 35;
		private static int cachedSortByButtonWidth = 125;
		private static float sortByPosYInitial = 0;
		private static float sortByXOffset = 10;

		private static int cachedSearchLabelHeight = 35;
		private static int cachedSearchLabelWidth = 46;
		private static int cachedSearchYOffset = 6;
		private static int cachedSearchXOffset = 16;
		private static int cachedSearchInputHeight = 26;
		private static int cachedSearchInputWidth = 125;

		private static int cachedHeaderHeight = 26;
		private static int cachedHeaderWidth = 80;
		private static float headerPosYInitial = 70;
		private static float tablePosInitialYOffset = 30;

		private static float thingIconXOffset = 30;
		private static float thingIconWidth = 30;

		private static float infoCardXOffset = 5;
		private static float infoCardYOffset = 5;

		private static float thingLabelXOffset = 35;
		private static float thingLabelWidth = 120;

		private static float thingMarketValueXOffset = 35;
		private static float thingMarketValueWidth = 120;

		private static float thingCurrentValueXOffset = 10;
		private static float thingCurrentValueWidth = 120;

		private static float thingTotalChangeXOffset = 25;
		private static float thingTotalChangeWidth = 120;

		private static float thingRecentChangeXOffset = 0;
		private static float thingRecentChangeWidth = 120;
		public override Vector2 InitialSize => new Vector2(maxWidth, Mathf.Min(maxHeight, UI.screenHeight));

		private string textFilter;
		public override void DoWindowContents(Rect inRect)
		{
			RecacheIfDirty();
			Text.Anchor = TextAnchor.MiddleCenter;
			Vector2 initialPos = Vector2.zero;
			Vector2 sortByPos = new Vector2(initialPos.x, 0);
			var sortByKeys = sortByButtons.Keys.ToList();
			var sortyByLabelRect = new Rect(initialPos.x, sortByPos.y, cachedSortByLabelWidth, cachedSortByLabelHeight);
			Widgets.Label(sortyByLabelRect, "VTE.SortBy".Translate());
			sortByPos.x += sortyByLabelRect.width + sortByXOffset;

			for (int i = 0; i < sortByKeys.Count; i++)
			{
				Rect buttonRect = new Rect(sortByPos.x, sortByPosYInitial, cachedSortByButtonWidth, cachedSortByButtonHeight);
				if (Widgets.ButtonText(buttonRect, sortByButtons[sortByKeys[i]]))
                {
					SortBy(sortByKeys[i], !sortDescending);
                }
				sortByPos.x += cachedSortByButtonWidth + sortByXOffset;
			}

			var searchLabelRect = new Rect(initialPos.x, sortByPos.y + cachedSortByButtonHeight + cachedSearchYOffset, cachedSearchLabelWidth, cachedSearchLabelHeight);
			Widgets.Label(searchLabelRect, "VTE.Search".Translate());

			var searchInputRect = new Rect(searchLabelRect.xMax + cachedSearchXOffset, searchLabelRect.y, cachedSearchInputWidth, cachedSearchInputHeight);
			var oldValue = textFilter;
			textFilter = Widgets.TextField(searchInputRect, textFilter);
			if (oldValue != textFilter)
            {
				SetDirty();
			}
			Vector2 headerPos = Vector2.zero;
			var headersKeys = headers.Keys.ToList();

			Text.Font = GameFont.Tiny;
			for (int i = 0; i < headersKeys.Count; i++)
			{
				Rect labelRect = new Rect(headerPos.x + headers[headersKeys[i]].Item1, headerPosYInitial, cachedHeaderWidth, cachedHeaderHeight);
				MouseoverSounds.DoRegion(labelRect);
				Widgets.Label(labelRect, headers[headersKeys[i]].Item2);
				headerPos.x += cachedHeaderWidth + headers[headersKeys[i]].Item1;
			}



			Vector2 tablePos = Vector2.zero;
			tablePos.y = headerPosYInitial + tablePosInitialYOffset;

			float listHeight = cachedTradeables.Count * cellHeight;
			Rect viewRect = new Rect(tablePos.x, tablePos.y, inRect.width, (inRect.height - tablePos.y) - 20);
			Rect scrollRect = new Rect(tablePos.x, tablePos.y, inRect.width - 16f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			GUI.color = new Color(1f, 1f, 1f, 0.2f);
			Widgets.DrawLineHorizontal(0f, tablePos.y, viewRect.width);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Small;
			float num = 0f;
			StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = true;

			for (int j = 0; j < cachedTradeables.Count; j++)
			{
				if (num > scrollPosition.y - cellHeight && num < scrollPosition.y + viewRect.height)
                {
					var thingDef = cachedTradeables[j];
					Rect rect2 = new Rect(0f, tablePos.y, viewRect.width, cellHeight);

					if (j % 2 == 1)
					{
						Widgets.DrawLightHighlight(rect2);
					}

					if (Mouse.IsOver(rect2))
					{
						Widgets.DrawBox(rect2);
						GUI.DrawTexture(rect2, TexUI.HighlightTex);
					}
					var thingIconRect = new Rect(rect2.x + thingIconXOffset, rect2.y, thingIconWidth, rect2.height);
					GuiHelper.ThingIcon(thingIconRect, thingDef);
					Widgets.InfoCardButton(thingIconRect.xMax + infoCardXOffset, rect2.y + infoCardYOffset, thingDef);
					var thingLabelRect = new Rect(thingIconRect.xMax + thingLabelXOffset, rect2.y, thingLabelWidth, rect2.height);
					Widgets.Label(thingLabelRect, thingDef.LabelCap);
					var thingMarketValueRect = new Rect(thingLabelRect.xMax + thingMarketValueXOffset, rect2.y, thingMarketValueWidth, rect2.height);

					var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
					Widgets.Label(thingMarketValueRect, baseMarketValue.ToString());

					var thingCurrentValueRect = new Rect(thingMarketValueRect.xMax + thingCurrentValueXOffset, rect2.y, thingCurrentValueWidth, rect2.height);
					var currentPrice = TradingManager.Instance.priceModifiers.TryGetValue(thingDef, out var curPrice) ? curPrice : baseMarketValue;
					Widgets.Label(thingCurrentValueRect, currentPrice.ToString());

					var totalChange = GetTotalChangeFor(currentPrice, baseMarketValue);
					var thingChangeRect = new Rect(thingCurrentValueRect.xMax + thingTotalChangeXOffset, rect2.y, thingTotalChangeWidth, rect2.height);
					var totalChangeString = totalChange.ToStringDecimalIfSmall() + "%";
					if (totalChange > 0)
                    {
						GUI.color = Color.green;
						totalChangeString += "▲";
					}
					else if (totalChange < 0)
                    {
						GUI.color = Color.red;
						totalChangeString += "▼";
					}

					Widgets.Label(thingChangeRect, totalChangeString);
					GUI.color = Color.white;
					var recentChange = GetRecentChangeFor(thingDef, currentPrice, baseMarketValue);

					var thingRecentChangeRect = new Rect(thingChangeRect.xMax + thingRecentChangeXOffset, rect2.y, thingRecentChangeWidth, rect2.height);
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
					Widgets.Label(thingRecentChangeRect, recentChangeString);
					GUI.color = Color.white;
				}
				tablePos.y += cellHeight;
				num += cellHeight;
			}
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			Widgets.EndScrollView();
			StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = false;
		}
		private float GetTotalChangeFor(float currentPrice, float baseMarketValue)
		{
			var change = currentPrice - baseMarketValue;
			return (change / baseMarketValue) * 100f;
		}
		private float GetRecentChangeFor(ThingDef thingDef, float currentPrice, float baseMarketValue)
        {
			var previousPrice = TradingManager.Instance.previousPriceModifiers.TryGetValue(thingDef, out var prevPrice) ? prevPrice : baseMarketValue;
			var change = currentPrice - previousPrice;
			return (change / previousPrice) * 100f;
		}

		private float GetTotalChangeFor(ThingDef thingDef)
		{
			var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
			var currentPrice = TradingManager.Instance.priceModifiers.TryGetValue(thingDef, out var curPrice) ? curPrice : baseMarketValue;
			var change = currentPrice - baseMarketValue;
			return (change / baseMarketValue) * 100f;
		}
		private float GetRecentChangeFor(ThingDef thingDef)
		{
			var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
			var currentPrice = TradingManager.Instance.priceModifiers.TryGetValue(thingDef, out var curPrice) ? curPrice : baseMarketValue;
			var previousPrice = TradingManager.Instance.previousPriceModifiers.TryGetValue(thingDef, out var prevPrice) ? prevPrice : baseMarketValue;
			var change = currentPrice - previousPrice;
			return (change / previousPrice) * 100f;
		}
		private float GetCurrentValueFor(ThingDef thingDef)
		{
			var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
			var currentPrice = TradingManager.Instance.priceModifiers.TryGetValue(thingDef, out var curPrice) ? curPrice : baseMarketValue;
			return currentPrice;;
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

		private List<ThingDef> cachedTradeables = new List<ThingDef>();
		private void RecacheTradeables()
		{
			cachedTradeables.Clear();
			cachedTradeables.AddRange(TradingManager.Instance.cachedTradeables);
			if (!textFilter.NullOrEmpty())
			{
				cachedTradeables = cachedTradeables.Where(x => x.LabelCap.ToLower().RawText.Contains(textFilter)).ToList();
			}
			StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = true;
			cachedTradeables = SortByColumn(sortByColumn);
			StatWorker_GetBaseValueFor_Patch.showOnlyVanilla = false;

		}

		private List<ThingDef> SortByColumn(Column column)
        {
			switch (column)
            {
				case Column.None: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap.ToString()).ToList() : cachedTradeables.OrderBy(x => x.LabelCap.ToString()).ToList();
				case Column.Item: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap.ToString()).ToList() : cachedTradeables.OrderBy(x => x.LabelCap.ToString()).ToList();
				case Column.MarketValue: return sortDescending ? cachedTradeables.OrderByDescending(x => x.GetStatValueAbstract(StatDefOf.MarketValue)).ToList() : cachedTradeables.OrderBy(x => x.GetStatValueAbstract(StatDefOf.MarketValue)).ToList();
				case Column.CurrentValue: return sortDescending ? cachedTradeables.OrderByDescending(x => GetCurrentValueFor(x)).ToList() : cachedTradeables.OrderBy(x => GetCurrentValueFor(x)).ToList();
				case Column.Change: return sortDescending ? cachedTradeables.OrderByDescending(x => GetTotalChangeFor(x)).ToList() : cachedTradeables.OrderBy(x => GetTotalChangeFor(x)).ToList();
				case Column.RecentChange: return sortDescending ? cachedTradeables.OrderByDescending(x => GetRecentChangeFor(x)).ToList() : cachedTradeables.OrderBy(x => GetRecentChangeFor(x)).ToList();
				default: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap).ToList() : cachedTradeables.OrderBy(x => x.LabelCap).ToList();
			}
		}
    }
}
