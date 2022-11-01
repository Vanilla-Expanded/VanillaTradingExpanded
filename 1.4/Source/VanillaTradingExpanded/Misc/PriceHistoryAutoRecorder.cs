using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace VanillaTradingExpanded
{
	public class PriceHistoryAutoRecorderThing : PriceHistoryAutoRecorder
    {
		public ThingDef thingDef;
		public bool squeezeOccured;
		public bool crashOccured;
        public override float GetCurrentValue()
        {
			var baseMarketValue = thingDef.GetStatValueAbstract(StatDefOf.MarketValue);
			var currentPrice = TradingManager.Instance.TryGetModifiedPriceFor(thingDef, out var curPrice) ? curPrice : baseMarketValue;
			return currentPrice;
		}
		public override void RecordCurrentPrice()
        {
			records.Add(thingDef.GetStatValueAbstract(StatDefOf.MarketValue));
		}

        public override string Name()
        {
			return thingDef.label;
		}

        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Defs.Look(ref thingDef, "thingDef");
			Scribe_Values.Look(ref squeezeOccured, "squeezeOccured");
			Scribe_Values.Look(ref crashOccured, "crashOccured");
		}

        public override string TitleName()
        {
			return "VTE.PriceHistory".Translate(Name());
		}
    }

	public class PriceHistoryAutoRecorderCompany : PriceHistoryAutoRecorder
	{
		public Company company;
		public bool squeezeOccured;
		public bool crashOccured;
		public override float GetCurrentValue()
		{
			return company.currentValue;
		}
		public override void RecordCurrentPrice()
		{
			records.Add(company.currentValue);
		}

		public override string Name()
		{
			return company.name;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look(ref company, "thingDef");
			Scribe_Values.Look(ref squeezeOccured, "squeezeOccured");
			Scribe_Values.Look(ref crashOccured, "crashOccured");
		}

		public override string TitleName()
		{
			return "VTE.ChartHistory".Translate(Name());
		}
	}

	public abstract class PriceHistoryAutoRecorder : IExposable
	{
		public List<float> records = new List<float>();
		public SimpleCurveDrawerStyle curveDrawerStyle;
		public PriceHistoryAutoRecorder()
        {
			records = new List<float>();
			curveDrawerStyle = new SimpleCurveDrawerStyle();
			curveDrawerStyle.DrawMeasures = true;
			curveDrawerStyle.DrawPoints = false;
			curveDrawerStyle.DrawBackground = true;
			curveDrawerStyle.DrawBackgroundLines = false;
			curveDrawerStyle.DrawLegend = true;
			curveDrawerStyle.DrawCurveMousePoint = true;
			curveDrawerStyle.OnlyPositiveValues = true;
			curveDrawerStyle.UseFixedSection = true;
			curveDrawerStyle.UseAntiAliasedLines = true;
			curveDrawerStyle.PointsRemoveOptimization = true;
			curveDrawerStyle.MeasureLabelsXCount = 10;
			curveDrawerStyle.MeasureLabelsYCount = 5;
			curveDrawerStyle.XIntegersOnly = true;
			curveDrawerStyle.LabelX = "Day".Translate();
		}
		public abstract void RecordCurrentPrice();
		public abstract string Name();
		public float GetPriceInPreviousDays(int lastDay, bool returnCurrentValueAsFallback = true)
        {
			if (records.Count >= lastDay)
            {
				return records[records.Count - lastDay];
            }
			if (returnCurrentValueAsFallback)
            {
				return records.Last();
			}
			return -1f;
		}

		private List<SimpleCurveDrawInfo> curves = new List<SimpleCurveDrawInfo>();

		private int cachedGraphTickCount = -1;

		public abstract string TitleName();
		public abstract float GetCurrentValue();
		public void DrawGraph(Rect graphRect, Rect legendRect, FloatRange section, List<CurveMark> marks)
		{
			int ticksGame = Find.TickManager.TicksGame;
			if (ticksGame != cachedGraphTickCount)
			{
				cachedGraphTickCount = ticksGame;
				curves.Clear();

				SimpleCurveDrawInfo simpleCurveDrawInfo = new SimpleCurveDrawInfo();
				simpleCurveDrawInfo.color = new Color(1, 1, 0, 1);
				simpleCurveDrawInfo.label = TitleName();
				simpleCurveDrawInfo.valueFormat = "${0}";
				simpleCurveDrawInfo.curve = new SimpleCurve();
				var recordsLast59 = records.TakeLast(59).ToList();
				for (int j = 0; j < recordsLast59.Count; j++)
				{
					simpleCurveDrawInfo.curve.Add(new CurvePoint(j, recordsLast59[j]), sort: false);
				}
				simpleCurveDrawInfo.curve.Add(new CurvePoint(recordsLast59.Count, GetCurrentValue()), sort: false);
				simpleCurveDrawInfo.curve.SortPoints();
				if (recordsLast59.Count == 1)
				{
					simpleCurveDrawInfo.curve.Add(new CurvePoint(1.66666669E-05f, recordsLast59[0]));
				}
				curves.Add(simpleCurveDrawInfo);
			}
			if (Mathf.Approximately(section.min, section.max))
			{
				section.max += 1.66666669E-05f;
			}
			SimpleCurveDrawerStyle curveDrawerStyle = Find.History.curveDrawerStyle;
			curveDrawerStyle.FixedSection = section;
			curveDrawerStyle.UseFixedScale = false;
			curveDrawerStyle.FixedScale = Vector2.zero;
			curveDrawerStyle.YIntegersOnly = false;
			curveDrawerStyle.OnlyPositiveValues = false;
			SimpleCurveDrawer.DrawCurves(graphRect, curves, curveDrawerStyle, marks, legendRect);
			Text.Anchor = TextAnchor.UpperLeft;
		}

		public virtual void ExposeData()
		{
			byte[] arr = null;
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				arr = RecordsToBytes();
			}
			try
			{
                DataExposeUtility.ByteArray(ref arr, "records");
            }
			catch (Exception ex)
			{
				Log.Error("Exception saving in Vanilla Trading Expanded: " + ex);
			}
            if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				SetRecordsFromBytes(arr);
			}
		}

		private byte[] RecordsToBytes()
		{
			byte[] array = new byte[records.Count * 4];
			for (int i = 0; i < records.Count; i++)
			{
				byte[] bytes = BitConverter.GetBytes(records[i]);
				for (int j = 0; j < 4; j++)
				{
					array[i * 4 + j] = bytes[j];
				}
			}
			return array;
		}

		private void SetRecordsFromBytes(byte[] bytes)
		{
			int num = bytes.Length / 4;
			records.Clear();
			for (int i = 0; i < num; i++)
			{
				float item = BitConverter.ToSingle(bytes, i * 4);
				records.Add(item);
			}
		}
	}
}