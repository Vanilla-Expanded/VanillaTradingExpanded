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
        }
    }
    public class VanillaTradingExpandedMod : Mod
    {
        public static Settings settings;
        public VanillaTradingExpandedMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            Utils.InitMarkupValues();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.SliderLabeled("VTE.NewsSpawnOption".Translate(), ref settings.newsSpawnRate,
                "PeriodDays".Translate(settings.newsSpawnRate), 0.01f, 100f); 
            listingStandard.SliderLabeled("VTE.PlayerTransactionImpactMultiplierOption".Translate(), ref settings.playerTransactionImpactMultiplier,
                settings.playerTransactionImpactMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.PlayerContractFulfilmentChanceMultiplier".Translate(), 
                ref settings.playerContractFulfilmentMultiplier, settings.playerContractFulfilmentMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.NewsPriceImpactMultiplier".Translate(), ref settings.newsPriceImpactMultiplier,
                settings.newsPriceImpactMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.NPCContractFulfilmentChanceMultiplier".Translate(),
                ref settings.npcContractFulfilmentMultiplier, settings.npcContractFulfilmentMultiplier.ToStringPercent(), 0.01f, 10f);
            listingStandard.SliderLabeled("VTE.MaxNPCContractCount".Translate(), ref settings.maxNPCContractCount, settings.maxNPCContractCount.ToString(), 1, 200);
            listingStandard.SliderLabeled("VTE.MaxCompanyCount".Translate(), ref settings.maxCompanyCount, settings.maxCompanyCount.ToString(), 1, 200);
            listingStandard.SliderLabeled("VTE.MaximumMarkupOnNPCContract".Translate(), ref settings.maxMarkupOnNPCContract, ((float)(settings.maxMarkupOnNPCContract)).ToStringPercent(), 1, 100);
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
            listingStandard.End();
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
    }
}
