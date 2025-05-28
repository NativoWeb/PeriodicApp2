// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2018/07/13

#if true // MODULE_MARKER

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Options;
using Outline = UnityEngine.UI.Outline;
using Text = UnityEngine.UI.Text;

#pragma warning disable 1591
namespace DG.Tweening
{
	public static class DOTweenModuleUI
    {
        public static class Utils
        {
            public static Vector2 SwitchToRectTransform(RectTransform from, RectTransform to)
            {
                Vector2 localPoint;
                Vector2 fromPivotDerivedOffset = new Vector2(from.rect.width * 0.5f + from.rect.xMin, from.rect.height * 0.5f + from.rect.yMin);
                Vector2 screenP = RectTransformUtility.WorldToScreenPoint(null, from.position);
                screenP += fromPivotDerivedOffset;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenP, null, out localPoint);
                Vector2 pivotDerivedOffset = new Vector2(to.rect.width * 0.5f + to.rect.xMin, to.rect.height * 0.5f + to.rect.yMin);
                return to.anchoredPosition + localPoint - pivotDerivedOffset;
            }
        }
	}
}
#endif
