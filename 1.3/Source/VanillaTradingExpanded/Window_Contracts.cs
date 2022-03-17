using System.Collections.Generic;
using System;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Linq;
using RimWorld;
using System.Security.Cryptography;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    public class Window_Contracts : Window
    {
        public Pawn negotiator;
        public Window_Contracts(Pawn negotiator)
        {
            this.negotiator = negotiator;
        }
        public override Vector2 InitialSize => new Vector2(1000, Mathf.Min(800, UI.screenHeight));
        private static Dictionary<string, int> headers => new Dictionary<string, int>
        {
            {"VTE.Contract".Translate(), 0 },
            {"VTE.Reward".Translate(), 250 },
            {"VTE.Markup".Translate(), 25 },
            {"VTE.TimeUntilTheEnd".Translate(), 25 },
        };

        public static Vector2 scrollPosition;
        public override void DoWindowContents(Rect inRect)
        {
            Vector2 pos = Vector2.zero;
            Text.Font = GameFont.Medium;
            var contractsTitle = new Rect(pos.x, pos.y, 120, 30);
            Widgets.Label(contractsTitle, "VTE.Contracts".Translate());
            pos.y += 30;

            var contractsExplanation = new Rect(pos.x, pos.y, 780, 60);
            Text.Font = GameFont.Small;
            GUI.color = Color.grey;
            Widgets.Label(contractsExplanation, "VTE.ContractsExplanation".Translate());
            GUI.color = Color.white;
            pos.y += 70;
            pos.x += 10;
            var headersKeys = headers.Keys.ToList();
            Text.Font = GameFont.Tiny;
            for (int i = 0; i < headersKeys.Count; i++)
            {
                Rect labelRect = new Rect(pos.x + headers[headersKeys[i]], pos.y, 120, 24);
                MouseoverSounds.DoRegion(labelRect);
                Widgets.Label(labelRect, headersKeys[i]);
                pos.x += 120 + headers[headersKeys[i]];
            }
            Text.Font = GameFont.Small;
            pos.y += 20;
            pos.x = 10;
            var npcSubmittedContracts = TradingManager.Instance.npcSubmittedContracts;

            float listHeight = npcSubmittedContracts.Count * 26;
			Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 10, 400);
			Rect scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, listHeight);
			Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollRect);
			GUI.color = new Color(1f, 1f, 1f, 0.2f);
			Widgets.DrawLineHorizontal(0f, pos.y, viewRect.width);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Small;

			for (int j = 0; j < npcSubmittedContracts.Count; j++)
            {
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleLeft;
                var contract = npcSubmittedContracts[j];
                if (j % 2 == 1)
                {
                    var wholeEntryRect = new Rect(pos.x, pos.y, inRect.width, 24);
                    Widgets.DrawLightHighlight(wholeEntryRect);
                }
                var thingIconRect = new Rect(pos.x, pos.y, 24, 24);
                Widgets.ThingIcon(thingIconRect, contract.item, contract.stuff);
                var infoCardRect = new Rect(thingIconRect.xMax, pos.y, 24, 24);
                Widgets.InfoCardButton(infoCardRect, contract.item);
                var nameRect = new Rect(infoCardRect.xMax, pos.y, 270, 24);
                Widgets.Label(nameRect, contract.Name);

                Text.Anchor = TextAnchor.MiddleCenter;
                var rewardRect = new Rect(nameRect.xMax + 50, pos.y, 50, 24);
                Widgets.Label(rewardRect, contract.reward.ToStringMoney());

                var markupRect = new Rect(rewardRect.xMax + 95, pos.y, 50, 24);
                Widgets.Label(markupRect, (contract.reward / contract.BaseMarketValue).ToStringPercent());

                var timeUntilEndRect = new Rect(markupRect.xMax + 125, pos.y, 50, 24);
                Widgets.Label(timeUntilEndRect, (contract.expiresInTicks - contract.creationTick).ToStringTicksToPeriod());

                var contractsButton = new Rect(timeUntilEndRect.xMax + 40, pos.y + 1, 150, 22);
                var playerHasEnoughItem = PlayerHasEnoughItemFor(contract, out var things);
                if (!playerHasEnoughItem)
                {
                    GUI.color = Color.grey;
                }
                var label = playerHasEnoughItem ? "VTE.CompleteContract".Translate() : "VTE.InsufficientAsset".Translate();
                if (Widgets.ButtonText(contractsButton, label, active: playerHasEnoughItem))
                {
                    contract.mapToTakeItems = negotiator.Map;
                    TradingManager.Instance.CompleteContract(contract);
                }
				pos.y += 26;
            }
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;
			Widgets.EndScrollView();
		}

        private bool PlayerHasEnoughItemFor(Contract contract, out List<Thing> things)
        {
            things = contract.FoundItemsInMap(negotiator.Map);
            return things.Sum(x => x.stackCount) >= contract.amount;
        }
    }
}
