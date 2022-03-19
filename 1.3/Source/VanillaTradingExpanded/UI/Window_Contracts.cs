using System.Collections.Generic;
using System;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Verse;
using Verse.Sound;
using System.Linq;
using RimWorld;
using System.Security.Cryptography;
using System.Diagnostics.Contracts;

namespace VanillaTradingExpanded
{
    [HotSwappable]
    public class Window_Contracts : Window
    {
        public Pawn negotiator;
        public Window_Contracts(Pawn negotiator)
        {
            this.negotiator = negotiator;
            this.forcePause = true;
            this.closeOnClickedOutside = true;
            curPlayerContract = new Contract
            {
                amount = 1
            };
        }
        public override Vector2 InitialSize => new Vector2(1000, 880);
        private static Dictionary<string, int> headers => new Dictionary<string, int>
        {
            {"VTE.Contract".Translate(), 0 },
            {"VTE.Reward".Translate(), 250 },
            {"VTE.Markup".Translate(), 25 },
            {"VTE.TimeUntilTheEnd".Translate(), 25 },
        };

        public static Vector2 scrollPosition;
        public static Vector2 scrollPosition2;
        public Contract curPlayerContract;
        public override void DoWindowContents(Rect inRect)
        {
            Vector2 pos = Vector2.zero;
            Text.Font = GameFont.Medium;
            var contractsTitle = new Rect(pos.x, pos.y, 200, 30);
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
                var rewardRect = new Rect(nameRect.xMax + 40, pos.y, 60, 24);
                Widgets.Label(rewardRect, contract.reward.ToStringMoney());

                var markupRect = new Rect(rewardRect.xMax + 95, pos.y, 50, 24);
                Widgets.Label(markupRect, (contract.reward / contract.BaseMarketValue).ToStringPercent());

                var timeUntilEndRect = new Rect(markupRect.xMax + 125, pos.y, 60, 24);
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

            pos.y = viewRect.yMax + 30;
            Text.Font = GameFont.Medium;
            var yourContractsTitle = new Rect(pos.x, pos.y, 170, 30);
            Widgets.Label(yourContractsTitle, "VTE.YourContracts".Translate());
            Text.Font = GameFont.Small;

            var moneyStatus = "VTE.MoneyInBanks".Translate(TradingManager.Instance.Banks.Sum(x => x.DepositAmount));
            var size = Text.CalcSize(moneyStatus);
            var moneyInBanksRect = new Rect(yourContractsTitle.xMax + 10, yourContractsTitle.y + (yourContractsTitle.height / 2f) - (size.y / 2f) + 5f, size.x, size.y);
            Widgets.Label(moneyInBanksRect, moneyStatus);

            pos.y += 30;
            pos.x = 10;
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

            var playerSubmittedContracts = TradingManager.Instance.playerSubmittedContracts;
            listHeight = playerSubmittedContracts.Count * 26;
            viewRect = new Rect(pos.x, pos.y, inRect.width - 10, 100);
            scrollRect = new Rect(pos.x, pos.y, viewRect.width - 16f, listHeight);
            Widgets.BeginScrollView(viewRect, ref scrollPosition2, scrollRect);
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            Widgets.DrawLineHorizontal(0f, pos.y, viewRect.width);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;

            Contract contractToCancel = null;
            for (int j = 0; j < playerSubmittedContracts.Count; j++)
            {
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.MiddleLeft;
                var contract = playerSubmittedContracts[j];
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
                var rewardRect = new Rect(nameRect.xMax + 40, pos.y, 60, 24);
                Widgets.Label(rewardRect, contract.reward.ToStringMoney());

                var markupRect = new Rect(rewardRect.xMax + 95, pos.y, 50, 24);
                Widgets.Label(markupRect, (contract.rewardAsFloat / contract.BaseMarketValue).ToStringPercent());

                var timeUntilEndRect = new Rect(markupRect.xMax + 125, pos.y, 60, 24);
                Widgets.Label(timeUntilEndRect, (contract.expiresInTicks - contract.creationTick).ToStringTicksToPeriod());

                var contractsButton = new Rect(timeUntilEndRect.xMax + 40, pos.y + 1, 150, 22);
                if (Widgets.ButtonText(contractsButton, "VTE.CancelContract".Translate()))
                {
                    contractToCancel = contract;
                }
                pos.y += 26;
            }
            Widgets.EndScrollView();

            if (contractToCancel != null)
            {
                TradingManager.Instance.playerSubmittedContracts.Remove(contractToCancel);
            }
            pos.x = 10;
            pos.y = viewRect.yMax + 10;
            var amountDecreaseRect = new Rect(pos.x, pos.y, 24, 24);
            if (Widgets.ButtonText(amountDecreaseRect, "<") && curPlayerContract.amount > 0)
            {
                curPlayerContract.amount--;
            }

            GUI.color = Color.white;
            var textEntry = new Rect(amountDecreaseRect.xMax + 5, pos.y, 60, 24);

            contractAmountBuffer = curPlayerContract.amount.ToString();
            Widgets.TextFieldNumeric<int>(textEntry, ref curPlayerContract.amount, ref contractAmountBuffer);
            var amountIncreaseRect = new Rect(textEntry.xMax + 5, pos.y, 24, 24);
            if (Widgets.ButtonText(amountIncreaseRect, ">"))
            {
                curPlayerContract.amount++;
            }

            bool itemIsSetAndMadeFromStuff = curPlayerContract.item != null && curPlayerContract.item.MadeFromStuff;
            var selectItemRect = new Rect(amountIncreaseRect.xMax, pos.y, itemIsSetAndMadeFromStuff ? 170 : 200, 24);
            var label2 = curPlayerContract.item != null ? curPlayerContract.ItemName.CapitalizeFirst() : "VTE.SelectItem".Translate().ToString();
            if (Widgets.ButtonText(selectItemRect, label2))
            {
                Find.WindowStack.Add(new Window_SelectItemForContract(curPlayerContract));
            }

            if (itemIsSetAndMadeFromStuff)
            {
                var selectStuffRect = new Rect(selectItemRect.xMax, pos.y, 80, 24);
                if (Widgets.ButtonText(selectStuffRect, "VTE.SelectStuff".Translate()))
                {
                    var floatList = new List<FloatMenuOption>();
                    foreach (var stuff in GenStuff.AllowedStuffsFor(curPlayerContract.item))
                    {
                        floatList.Add(new FloatMenuOption(stuff.LabelCap, delegate
                        {
                            curPlayerContract.stuff = stuff;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(floatList));
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            var rewardRect2 = new Rect(amountIncreaseRect.xMax + 240, pos.y, 60, 24);
            var rewardForContract = curPlayerContract.item != null ? curPlayerContract.BaseMarketValue * (fixedMarkupForPlayerContract / 100f) : 0;
            Widgets.Label(rewardRect2, curPlayerContract.item != null ? ((int)rewardForContract).ToStringMoney() : "-");

            var markupRect2 = new Rect(rewardRect2.xMax + 100, pos.y, 50, 24);
            Widgets.TextFieldNumeric<float>(markupRect2, ref fixedMarkupForPlayerContract, ref fixedMarkupBuffer);

            var timeUntilEndRect2 = new Rect(markupRect2.xMax + 130, pos.y, 50, 24);
            Widgets.TextFieldNumeric<float>(timeUntilEndRect2, ref fixedDurationInDays, ref fixedDurationBuffer);

            var contractsButton2 = new Rect(timeUntilEndRect2.xMax + 40, pos.y, 150, 24);
            if (Widgets.ButtonText(contractsButton2, "VTE.SubmitContract".Translate()) && curPlayerContract.item != null)
            {
                curPlayerContract.reward = (int)rewardForContract;
                curPlayerContract.rewardAsFloat = rewardForContract;
                curPlayerContract.creationTick = Find.TickManager.TicksGame;
                curPlayerContract.expiresInTicks = Find.TickManager.TicksGame + (int)(GenDate.TicksPerDay * fixedDurationInDays);
                TradingManager.Instance.playerSubmittedContracts.Add(curPlayerContract);
                curPlayerContract = new Contract
                {
                    amount = 1
                };
                fixedMarkupForPlayerContract = 100f;
                fixedDurationInDays = 7f;
                contractAmountBuffer = curPlayerContract.amount.ToString();
                fixedMarkupBuffer = fixedMarkupForPlayerContract.ToString();
                fixedDurationBuffer = fixedDurationInDays.ToString();
            }

            var yourContractsExplanation = new Rect(pos.x, contractsButton2.yMax + 10, 780, 60);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.grey;
            Widgets.Label(yourContractsExplanation, "VTE.YourContractsExplanation".Translate());
            GUI.color = Color.white;

            var closeButtonRect = new Rect(inRect.width - 200, yourContractsExplanation.yMax, 180, CloseButSize.y);
            if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
            {
                this.Close();
            }
        }

        public float fixedMarkupForPlayerContract = 100f;
        public float fixedDurationInDays = 7f;
        public string contractAmountBuffer;
        public string fixedMarkupBuffer;
        public string fixedDurationBuffer;
        private bool PlayerHasEnoughItemFor(Contract contract, out List<Thing> things)
        {
            things = contract.FoundItemsInMap(negotiator.Map);
            return things.Sum(x => x.stackCount) >= contract.amount;
        }
    }
}
