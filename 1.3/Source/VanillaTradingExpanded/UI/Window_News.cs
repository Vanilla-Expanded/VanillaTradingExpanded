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
    [HotSwappable]
	public class Window_News : Window
	{
		private enum FilterBy
        {
			All,
			OnlySpecificCategory,
			OnlyTradeShips,
			OnlyBullish, 
			OnlyBearish,
        }

		private static Dictionary<FilterBy, string> filterByButtons = new Dictionary<FilterBy, string>
		{
			{FilterBy.All, "VTE.All".Translate()},
			{FilterBy.OnlySpecificCategory, "VTE.OnlySpecificCategory".Translate()},
			{FilterBy.OnlyTradeShips, "VTE.OnlyTradeShips".Translate()},
			{FilterBy.OnlyBullish, "VTE.OnlyBullish".Translate()},
			{FilterBy.OnlyBearish, "VTE.OnlyBearish".Translate()},
		};

		private bool dirty;

		private Vector2 scrollPosition;
		public Window_News()
		{
			SetDirty();
			this.forcePause = true;
			this.closeOnClickedOutside = true;
		}

		private static float cellHeight = 35f;
		private static int cachedFilterByLabelHeight = 35;
		private static int cachedFilterByButtonHeight = 35;
		private static float sortByXOffset = 10;

		private static float headerPosYInitial = 70;
		private static float tablePosInitialYOffset = 30;
		public override Vector2 InitialSize => new Vector2(1000, Mathf.Min(800, UI.screenHeight));
		public override void DoWindowContents(Rect inRect)
		{
			RecacheIfDirty();
			Text.Anchor = TextAnchor.MiddleCenter;
			Vector2 initialPos = Vector2.zero;
			var localNews = new Rect(initialPos.x, initialPos.y - 15, 120, 50);
			Text.Font = GameFont.Medium;
			Widgets.Label(localNews, "VTE.LocalNews".Translate());
			Text.Font = GameFont.Small;
			initialPos.y = localNews.yMax - 5;
			Vector2 filterByPos = new Vector2(initialPos.x, initialPos.y);
			var filterByKeys = filterByButtons.Keys.ToList();
			var filterByLabelRect = new Rect(initialPos.x, filterByPos.y, 60, cachedFilterByLabelHeight);
			Widgets.Label(filterByLabelRect, "VTE.FilterBy".Translate());
			filterByPos.x += filterByLabelRect.width + sortByXOffset;

			for (int i = 0; i < filterByKeys.Count; i++)
			{
				Rect buttonRect = new Rect(filterByPos.x, filterByPos.y, 160, cachedFilterByButtonHeight);
				var label = filterByButtons[filterByKeys[i]];
				if (filterByKeys[i] == FilterBy.OnlySpecificCategory && specificCategory != null)
                {
					label = specificCategory.LabelCap;
				}
				if (Widgets.ButtonText(buttonRect, label))
                {
					Filter(filterByKeys[i]);
                }
				filterByPos.x += 160 + sortByXOffset;
			}

			Vector2 tablePos = Vector2.zero;
			tablePos.y = headerPosYInitial + tablePosInitialYOffset;
			Text.Anchor = TextAnchor.MiddleLeft;

			var newsTitle = new Rect(15, filterByLabelRect.yMax + 10, 120, 24);
			Text.Font = GameFont.Tiny;
			Widgets.Label(newsTitle, "VTE.News".Translate());
			Text.Font = GameFont.Small;

			var dateTitle = new Rect(785, filterByLabelRect.yMax + 10, 120, 24);
			Text.Font = GameFont.Tiny;
			Widgets.Label(dateTitle, "VTE.Date".Translate());
			Text.Font = GameFont.Small;

			float listHeight = cachedNews.Count * cellHeight;
			Rect viewRect = new Rect(tablePos.x, tablePos.y, inRect.width, (inRect.height - tablePos.y) - 20);
			Rect scrollRect = new Rect(tablePos.x, tablePos.y, inRect.width - 16f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			GUI.color = new Color(1f, 1f, 1f, 0.2f);
			Widgets.DrawLineHorizontal(0f, tablePos.y, viewRect.width);
			GUI.color = Color.white;

			Text.Font = GameFont.Small;
			float num = 0f;

			for (int j = 0; j < cachedNews.Count; j++)
			{
				if (num > scrollPosition.y - cellHeight && num < scrollPosition.y + viewRect.height)
                {
					var news = cachedNews[j];
					Rect entryBox = new Rect(0f, tablePos.y, viewRect.width, cellHeight);

					if (j % 2 == 1)
					{
						Widgets.DrawLightHighlight(entryBox);
					}

					var textBox = new Rect(entryBox.x + 15, entryBox.y, entryBox.width * 0.8f, entryBox.height);
					Widgets.Label(textBox, news.text);

					var dateBox = new Rect(textBox.xMax, entryBox.y, entryBox.width * 0.2f, entryBox.height);
					Text.Font = GameFont.Tiny;
					GUI.color = Color.gray;
					Widgets.Label(dateBox, news.Date);
					Text.Font = GameFont.Small;
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

		private FilterBy filterBy;

		private ThingCategoryDef specificCategory;
		private void Filter(FilterBy filterBy)
		{
			this.filterBy = filterBy;
			if (filterBy == FilterBy.OnlySpecificCategory)
            {
				var floatList = new List<FloatMenuOption>();
				foreach (var child in ThingCategoryDefOf.Root.childCategories)
                {
					floatList.Add(new FloatMenuOption(child.LabelCap, delegate
					{
						specificCategory = child;
						SetDirty();
					}));
                }
				Find.WindowStack.Add(new FloatMenu(floatList));
            }
			else
            {
				SetDirty();
			}
		}
		private void RecacheIfDirty()
		{
			if (dirty)
			{
				dirty = false;
				RecacheNews();
			}
		}

		private List<News> cachedNews = new List<News>();
		private void RecacheNews()
		{
			cachedNews.Clear();
			cachedNews.AddRange(TradingManager.Instance.AllNews);
			StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = true;
			cachedNews = FilteredNews(filterBy);
			StatWorker_GetBaseValueFor_Patch.outputOnlyVanilla = false;
		}

		private List<News> FilteredNews(FilterBy filter)
        {
			switch (filter)
            {
				case FilterBy.All: return cachedNews;
				case FilterBy.OnlySpecificCategory: return cachedNews.Where(x => x.MatchesCategory(specificCategory)).ToList();
				case FilterBy.OnlyBullish: return cachedNews.Where(x => x.newsContext.company is null && x.priceImpact > 0).ToList();
				case FilterBy.OnlyBearish: return cachedNews.Where(x => x.newsContext.company is null && x.priceImpact < 0).ToList();
				case FilterBy.OnlyTradeShips: return cachedNews.Where(x => x.newsContext.company != null).ToList();
			}
			return cachedNews;
		}
    }
}
