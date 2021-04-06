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

		[TweakValue("0TRADING", 0, 1000)] private static float maxWidth = 800;
		[TweakValue("0TRADING", 0, 1000)] private static float maxHeight = 600;

		[TweakValue("0TRADING", 0, 100)] private static float cellHeight = 35f;

		[TweakValue("0TRADING", 0, 1000)] private static int cachedSortByLabelHeight = 35;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSortByLabelWidth = 50;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSortByButtonHeight = 35;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSortByButtonWidth = 125;
		[TweakValue("0TRADING", 0, 1000)] private static float sortByPosYInitial = 0;
		[TweakValue("0TRADING", 0, 1000)] private static float sortByXOffset = 10;

		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchLabelHeight = 35;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchLabelWidth = 46;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchYOffset = 6;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchXOffset = 16;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchInputHeight = 26;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedSearchInputWidth = 125;

		[TweakValue("0TRADING", 0, 1000)] private static int cachedHeaderHeight = 26;
		[TweakValue("0TRADING", 0, 1000)] private static int cachedHeaderWidth = 80;
		[TweakValue("0TRADING", 0, 1000)] private static float headerPosYInitial = 70;
		[TweakValue("0TRADING", 0, 1000)] private static float tablePosInitialYOffset = 30;

		[TweakValue("0TRADING", 0, 1000)] private static float thingIconXOffset = 30;
		[TweakValue("0TRADING", 0, 1000)] private static float thingIconWidth = 30;

		[TweakValue("0TRADING", 0, 1000)] private static float infoCardXOffset = 5;
		[TweakValue("0TRADING", 0, 1000)] private static float infoCardYOffset = 5;

		[TweakValue("0TRADING", 0, 1000)] private static float thingLabelXOffset = 35;
		[TweakValue("0TRADING", 0, 1000)] private static float thingLabelWidth = 120;

		[TweakValue("0TRADING", 0, 1000)] private static float thingMarketValueXOffset = 35;
		[TweakValue("0TRADING", 0, 1000)] private static float thingMarketValueWidth = 120;

		[TweakValue("0TRADING", 0, 1000)] private static float thingCurrentValueXOffset = 10;
		[TweakValue("0TRADING", 0, 1000)] private static float thingCurrentValueWidth = 120;

		[TweakValue("0TRADING", 0, 1000)] private static float thingTotalChangeXOffset = 25;
		[TweakValue("0TRADING", 0, 1000)] private static float thingTotalChangeWidth = 120;

		[TweakValue("0TRADING", -100, 1000)] private static float thingRecentChangeXOffset = 0;
		[TweakValue("0TRADING", 0, 1000)] private static float thingRecentChangeWidth = 120;
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
					Widgets.Label(thingMarketValueRect, thingDef.GetStatValueAbstract(StatDefOf.MarketValue).ToString());

					var thingCurrentValueRect = new Rect(thingMarketValueRect.xMax + thingCurrentValueXOffset, rect2.y, thingCurrentValueWidth, rect2.height);
					Widgets.Label(thingCurrentValueRect, "35");

					var thingChangeRect = new Rect(thingCurrentValueRect.xMax + thingTotalChangeXOffset, rect2.y, thingTotalChangeWidth, rect2.height);
					Widgets.Label(thingChangeRect, "35");

					var recentChange = 0.25f;
					var thingRecentChangeRect = new Rect(thingChangeRect.xMax + thingRecentChangeXOffset, rect2.y, thingRecentChangeWidth, rect2.height);
					GUI.color = Color.red;
					Widgets.Label(thingRecentChangeRect, (recentChange * 100).ToStringDecimalIfSmall() + "▼");
					GUI.color = Color.white;
				}

				tablePos.y += cellHeight;
				num += cellHeight;
			}
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			Widgets.EndScrollView();
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
			cachedTradeables = SortByColumn(sortByColumn);
		}

		private List<ThingDef> SortByColumn(Column column)
        {
			switch (column)
            {
				case Column.None: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap.ToString()).ToList() : cachedTradeables.OrderBy(x => x.LabelCap.ToString()).ToList();
				case Column.Item: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap.ToString()).ToList() : cachedTradeables.OrderBy(x => x.LabelCap.ToString()).ToList();
				case Column.MarketValue: return sortDescending ? cachedTradeables.OrderByDescending(x => x.GetStatValueAbstract(StatDefOf.MarketValue)).ToList() : cachedTradeables.OrderBy(x => x.GetStatValueAbstract(StatDefOf.MarketValue)).ToList();
				default: return sortDescending ? cachedTradeables.OrderByDescending(x => x.LabelCap).ToList() : cachedTradeables.OrderBy(x => x.LabelCap).ToList();
			}
		}
    }
}
