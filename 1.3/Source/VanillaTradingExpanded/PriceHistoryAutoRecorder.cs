using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace VanillaTradingExpanded
{
	public class PriceHistoryAutoRecorder : IExposable
	{
		public ThingDef thingDef;
		public List<float> records = new List<float>();
		public SimpleCurveDrawerStyle curveDrawerStyle;
		public PriceHistoryAutoRecorder()
        {
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
		public void Record()
		{
			records.Add(thingDef.GetStatValueAbstract(StatDefOf.MarketValue));
		}

		private List<SimpleCurveDrawInfo> curves = new List<SimpleCurveDrawInfo>();

		private int cachedGraphTickCount = -1;

		public void DrawGraph(Rect graphRect, Rect legendRect, FloatRange section, List<CurveMark> marks)
		{
			int ticksGame = Find.TickManager.TicksGame;
			if (ticksGame != cachedGraphTickCount)
			{
				cachedGraphTickCount = ticksGame;
				curves.Clear();

				SimpleCurveDrawInfo simpleCurveDrawInfo = new SimpleCurveDrawInfo();
				simpleCurveDrawInfo.color = new Color(1, 1, 0, 1);
				simpleCurveDrawInfo.label = "VTE.PriceHistory".Translate(thingDef.label);
				simpleCurveDrawInfo.valueFormat = "${0}";
				simpleCurveDrawInfo.curve = new SimpleCurve();
				var recordsLast60 = records.TakeLast(60).ToList();
				for (int j = 0; j < recordsLast60.Count; j++)
				{
					simpleCurveDrawInfo.curve.Add(new CurvePoint(j, recordsLast60[j]), sort: false);
				}
				simpleCurveDrawInfo.curve.SortPoints();
				if (recordsLast60.Count == 1)
				{
					simpleCurveDrawInfo.curve.Add(new CurvePoint(1.66666669E-05f, recordsLast60[0]));
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

		public void ExposeData()
		{
			Scribe_Defs.Look(ref thingDef, "thingDef");
			byte[] arr = null;
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				arr = RecordsToBytes();
			}
			DataExposeUtility.ByteArray(ref arr, "records");
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