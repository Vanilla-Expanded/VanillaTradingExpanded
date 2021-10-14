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

namespace VanillaTradingExpanded
{
	[StaticConstructorOnStartup]
	public static class GuiHelper
	{
		private static readonly Rect DefaultTexCoords;
		private static readonly Rect LinkedTexCoords;
		static GuiHelper()
        {
			DefaultTexCoords = new Rect(0f, 0f, 1f, 1f);
			LinkedTexCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
		}
		public static void ThingIcon(Rect rect, ThingDef thingDef, ThingDef stuffDef = null, float scale = 1f)
		{
			if (!(thingDef.uiIcon == null) && !(thingDef.uiIcon == BaseContent.BadTex))
			{
				Texture2D resolvedIcon = thingDef.uiIcon;
				Graphic_Appearances graphic_Appearances;
				if ((graphic_Appearances = (thingDef.graphic as Graphic_Appearances)) != null)
				{
					resolvedIcon = (Texture2D)graphic_Appearances.SubGraphicFor(stuffDef ?? GenStuff.DefaultStuffFor(thingDef)).MatAt(thingDef.defaultPlacingRot).mainTexture;
				}
				GUI.color = (thingDef.MadeFromStuff ? Color.white : thingDef.uiIconColor);
				ThingIconWorker(rect, thingDef, resolvedIcon, thingDef.uiIconAngle, scale);
				GUI.color = Color.white;
			}
		}

		private static void ThingIconWorker(Rect rect, ThingDef thingDef, Texture resolvedIcon, float resolvedIconAngle, float scale = 1f)
		{
			Vector2 texProportions = new Vector2(resolvedIcon.width, resolvedIcon.height);
			Rect texCoords = DefaultTexCoords;
			if (thingDef.graphicData != null)
			{
				texProportions = thingDef.graphicData.drawSize.RotatedBy(thingDef.defaultPlacingRot);
				if (thingDef.uiIconPath.NullOrEmpty() && thingDef.graphicData.linkFlags != 0)
				{
					texCoords = LinkedTexCoords;
				}
			}
			Widgets.DrawTextureFitted(rect, resolvedIcon, GenUI.IconDrawScale(thingDef) * scale, texProportions, texCoords, resolvedIconAngle);
		}
	}
}
