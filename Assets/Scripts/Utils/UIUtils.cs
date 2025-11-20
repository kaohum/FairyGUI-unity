using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FairyGUI.Utils
{
    public static class UIUtils
    {
        private const string JSON_KEY = "gradient";
        private static readonly Regex REGEX = new Regex(@"#([A-Fa-f0-9][A-Fa-f0-9])([A-Fa-f0-9][A-Fa-f0-9])([A-Fa-f0-9][A-Fa-f0-9])");
        
        /// <summary>
        /// 初始化文本渐变色
        /// </summary>
        public static void SetupGradientText(GTextField textField, Dictionary<string, object> jsonObject)
        {
            if (textField == null)
                return;
            
            if (jsonObject == null || !jsonObject.TryGetValue(JSON_KEY, out var jsonValue) || !(jsonValue is string str))
                return;

            var colors = REGEX.Matches(str);
            if (colors.Count != 4)
                return;

            var top = ParseColor32(colors[0]);
            var bottom = ParseColor32(colors[1]);
            var left = ParseColor32(colors[2]);
            var right = ParseColor32(colors[3]);
            
            textField.UpdateGradientColor(top, bottom, left, right);
        }

        private static Color32 ParseColor32(Match match)
        {
            var r = byte.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(match.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
            return  new Color32(r, g, b, 255);
        }
    }
}