using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Linq;
using RimWorld;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    public class Window_SelectItemForContract : Window
    {
        private Contract contract;
        private Vector2 scrollPosition;
        public override Vector2 InitialSize => new Vector2(620f, 500f);

        public List<ThingDef> allItems;
        public Window_SelectItemForContract(Contract parent)
        {
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            allItems = Utils.cachedItemsForContracts.ToList();
            this.contract = parent;
        }

        string searchKey;
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleLeft;
            var searchLabel = new Rect(inRect.x, inRect.y, 60, 24);
            Widgets.Label(searchLabel, "VTE.Search".Translate());
            var searchRect = new Rect(searchLabel.xMax + 5, searchLabel.y, 200, 24f);
            searchKey = Widgets.TextField(searchRect, searchKey);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect outRect = new Rect(inRect);
            outRect.y = searchRect.yMax + 5;
            outRect.yMax -= 70f;
            outRect.width -= 16f;

            var thingDefs = searchKey.NullOrEmpty() ? allItems : allItems.Where(x => x.label.ToLower().Contains(searchKey.ToLower())).ToList();

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (float)thingDefs.Count() * 35f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            try
            {
                float num = 0f;
                foreach (ThingDef thingDef in thingDefs.OrderBy(x => x.FirstThingCategory?.index ?? 0).ThenBy(x => x.label))
                {
                    Rect iconRect = new Rect(0f, num, 24, 32);
                    Widgets.InfoCardButton(iconRect, thingDef);
                    iconRect.x += 24;
                    Widgets.ThingIcon(iconRect, thingDef);
                    Rect rect = new Rect(iconRect.xMax + 5, num, viewRect.width * 0.7f, 32f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(rect, thingDef.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;
                    rect.x = rect.xMax + 10;
                    rect.width = 100;
                    if (Widgets.ButtonText(rect, "VTE.Select".Translate()))
                    {
                        this.contract.item = thingDef;
                        this.contract.stuff = null;
                        SoundDefOf.Click.PlayOneShotOnCamera();
                        this.Close();
                    }
                    num += 35f;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }
    }
}
