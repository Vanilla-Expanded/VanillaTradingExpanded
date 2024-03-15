using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace VanillaTradingExpanded
{
    public class Settings : ModSettings
    {
        public float newsSpawnRate = 3f;
        public float playerTransactionImpactMultiplier = 1f;
        public float npcContractFulfilmentMultiplier = 1f;
        public float playerContractFulfilmentMultiplier = 1f;
        public int maxNPCContractCount = 30;
        public int maxCompanyCount = 30;
        public int maxMarkupOnNPCContract = 10;
        public float newsPriceImpactMultiplier = 1f;
        public float amountOfItemsToFluctuate = 0.2f;
        public bool contractBlackListCollectibles = false;
        public bool caravanLessContractItemPickup = false;
        public float maximumMarketValueOfItemsInContracts = 10000;
        public float maximumMarketValueOfItemsPerPlayerWealth = 0.01f;
        public bool enablePriceFluctuationRestriction;
        public float minPriceFluctuation = 0.95f;
        public float maxPriceFluctuation = 2f;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref newsSpawnRate, "newsSpawnRate", 3f);
            Scribe_Values.Look(ref playerTransactionImpactMultiplier, "playerTransactionImpactMultiplier", 1f);
            Scribe_Values.Look(ref npcContractFulfilmentMultiplier, "npcContractFulfilmentMultiplier", 1f);
            Scribe_Values.Look(ref playerContractFulfilmentMultiplier, "playerContractFulfilmentMultiplier", 1f);
            Scribe_Values.Look(ref maxNPCContractCount, "maxNPCContractCount", 30);
            Scribe_Values.Look(ref maxCompanyCount, "maxCompanyCount", 30);
            Scribe_Values.Look(ref maxMarkupOnNPCContract, "maxMarkupOnNPCContract", 10);
            Scribe_Values.Look(ref newsPriceImpactMultiplier, "newsPriceImpactMultiplier", 1f);
            Scribe_Values.Look(ref amountOfItemsToFluctuate, "itemsToFluctuate", 0.2f);
            Scribe_Values.Look(ref contractBlackListCollectibles, "contractBlackListCollectibles", false);
            Scribe_Values.Look(ref caravanLessContractItemPickup, "caravanLessContractItemPickup", false);
            Scribe_Values.Look(ref maximumMarketValueOfItemsInContracts, "maximumMarketValueOfItemsInContracts", 10000);
            Scribe_Values.Look(ref maximumMarketValueOfItemsPerPlayerWealth, "maximumMarketValueOfItemsPerPlayerWealth", 0.01f);
            Scribe_Values.Look(ref enablePriceFluctuationRestriction, "enablePriceFluctuationRestriction", false);
            Scribe_Values.Look(ref minPriceFluctuation, "minPriceFluctuation", 0.95f);
            Scribe_Values.Look(ref maxPriceFluctuation, "maxPriceFluctuation", 2f);
        }
    }
    public class VanillaTradingExpandedMod : Mod
    {
        public static Settings settings;
        public VanillaTradingExpandedMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
            var earlyHarmony = new Harmony("VanillaTradingExpandedEarly.Mod");
            earlyHarmony.Patch(Initializer.TargetMethod(), 
                postfix: new HarmonyMethod(AccessTools.Method(typeof(Initializer), nameof(Initializer.Postfix))));
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Utils.InitMarkupValues();
        }
        string fieldBuf1, fieldBuf2;
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.SliderLabeled("VTE.NewsSpawnOption".Translate(), ref settings.newsSpawnRate,
                "PeriodDays".Translate(settings.newsSpawnRate), 0.01f, 100f); 
            listingStandard.SliderLabeled("VTE.PlayerTransactionImpactMultiplierOption".Translate(), ref settings.playerTransactionImpactMultiplier,
                settings.playerTransactionImpactMultiplier.ToStringPercent(), 0f, 10f);

            var rect = new Rect(listingStandard.curX, listingStandard.curY, 200, 24f);
            Widgets.Label(rect, "VTE.MaximumPriceFluctuations".Translate());
            Widgets.Checkbox(rect.xMax, rect.y, ref settings.enablePriceFluctuationRestriction);
            GUI.color = settings.enablePriceFluctuationRestriction ? Color.white : Color.gray;
            var minValueSliderRect = new Rect(rect.xMax + 50, rect.y, 500, 24);
            var tmp1 = Widgets.HorizontalSlider(minValueSliderRect, settings.minPriceFluctuation, 0f, 1f, 
                label: "VTE.MinimumOfMarketValue".Translate("-" + settings.minPriceFluctuation.ToStringPercent()));
            var inputRect = new Rect(minValueSliderRect.xMax + 15, minValueSliderRect.y, 80, 24);
            try
            {
                if (!fieldBuf1.NullOrEmpty() && fieldBuf1 != (tmp1 * 100f).ToString())
                {
                    fieldBuf1 = (tmp1 * 100f).ToString();
                }
            } catch { }
            Widgets.TextFieldPercent(inputRect, ref tmp1, ref fieldBuf1, 0f, 1f);
            if (settings.enablePriceFluctuationRestriction)
            {
                settings.minPriceFluctuation = tmp1;
            }

            var maxValueSliderRect = new Rect(minValueSliderRect.x, minValueSliderRect.yMax, 500, 24);
            var tmp2 = Widgets.HorizontalSlider(maxValueSliderRect, settings.maxPriceFluctuation, 0f, 3f, 
                    label: "VTE.MaximumOfMarketValue".Translate(settings.maxPriceFluctuation.ToStringPercent()));
            inputRect = new Rect(maxValueSliderRect.xMax + 15, maxValueSliderRect.y, 80, 24);
            try
            {
                if (!fieldBuf2.NullOrEmpty() && fieldBuf2 != (tmp2 * 100f).ToString())
                {
                    fieldBuf2 = (tmp2 * 100f).ToString();
                }
            } catch { }
            
            Widgets.TextFieldPercent(inputRect, ref tmp2, ref fieldBuf2, 0f, 3f);
            if (settings.enablePriceFluctuationRestriction)
            {
                settings.maxPriceFluctuation = tmp2;
            }


            GUI.color = Color.white;
            listingStandard.Gap(48f);
            listingStandard.SliderLabeled("VTE.PlayerContractFulfilmentChanceMultiplier".Translate(), 
                ref settings.playerContractFulfilmentMultiplier, settings.playerContractFulfilmentMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.NewsPriceImpactMultiplier".Translate(), ref settings.newsPriceImpactMultiplier,
                settings.newsPriceImpactMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.NPCContractFulfilmentChanceMultiplier".Translate(),
                ref settings.npcContractFulfilmentMultiplier, settings.npcContractFulfilmentMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.MaxNPCContractCount".Translate(), ref settings.maxNPCContractCount, settings.maxNPCContractCount.ToString(), 1, 200);
            listingStandard.SliderLabeled("VTE.MaxCompanyCount".Translate(), ref settings.maxCompanyCount, settings.maxCompanyCount.ToString(), 1, 200);
            listingStandard.SliderLabeled("VTE.MaximumMarkupOnNPCContract".Translate(), ref settings.maxMarkupOnNPCContract, ((float)(settings.maxMarkupOnNPCContract)).ToStringPercent(), 1, 100);
            listingStandard.Label("VTE.MaximumMarketValueOfItemsInContracts".Translate());
            buf1 = settings.maximumMarketValueOfItemsInContracts.ToString();
            Widgets.TextFieldNumeric(new Rect(inRect.width - 200, listingStandard.curY - 24, 200, 24), ref settings.maximumMarketValueOfItemsInContracts, ref buf1);
            listingStandard.SliderLabeled("VTE.MaximumMarketValueOfItemsPerPlayerWealth".Translate(), ref settings.maximumMarketValueOfItemsPerPlayerWealth, settings.maximumMarketValueOfItemsPerPlayerWealth.ToStringPercent());
            listingStandard.CheckboxLabeled("VTE.ContractBlackListCollectibles".Translate(), ref settings.contractBlackListCollectibles);
            listingStandard.CheckboxLabeled("VTE.CaravanLessContractItemPickup".Translate(), ref settings.caravanLessContractItemPickup);
            listingStandard.SliderLabeled("VTE.AmountOfRandomItemsToFluctuate".Translate(), ref settings.amountOfItemsToFluctuate, ((float)(settings.amountOfItemsToFluctuate)).ToStringPercent(), 0.01f, 1f);
            if (Find.World != null)
            {
                if (listingStandard.ButtonText("VTE.ResetPriceChanges".Translate()))
                {
                    TradingManager.Instance.priceModifiers.Clear();
                    TradingManager.Instance.thingsAffectedBySoldPurchasedMarketValue.Clear();
                    TradingManager.Instance.priceHistoryRecorders.Clear();
                    TradingManager.Instance.GenerateAllPriceRecorders();
                }
            }

            if (listingStandard.ButtonText("Reset".Translate()))
            {
                settings.newsSpawnRate = 3f;
                settings.playerTransactionImpactMultiplier = 1f;
                settings.npcContractFulfilmentMultiplier = 1f;
                settings.playerContractFulfilmentMultiplier = 1f;
                settings.maxNPCContractCount = 30;
                settings.maxCompanyCount = 30;
                settings.maxMarkupOnNPCContract = 10;
                settings.newsPriceImpactMultiplier = 1f;
                settings.amountOfItemsToFluctuate = 0.2f;
                settings.contractBlackListCollectibles = false;
                settings.caravanLessContractItemPickup = false;
                settings.maximumMarketValueOfItemsInContracts = 10000;
                settings.maximumMarketValueOfItemsPerPlayerWealth = 0.01f;
                settings.minPriceFluctuation = 0.95f;
                settings.maxPriceFluctuation = 2f;
                settings.enablePriceFluctuationRestriction = false;
            }
            listingStandard.End();
        }

        string buf1;
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
    }
}
