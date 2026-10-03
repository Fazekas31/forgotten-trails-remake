using UnityEngine;

namespace ForgottenTrail.Gameplay.UI
{
    /// <summary>Scales screen-space labels against the demo's 1080p design resolution.</summary>
    public static class HudTextScale
    {
        public static float Factor => ScaleForHeight(Screen.height);

        public static float ScaleForHeight(int screenHeight)
        {
            return Mathf.Clamp(screenHeight / 1080f, 0.85f, 1.35f);
        }

        public static float Pixels(float designPixels)
        {
            return designPixels * Factor;
        }

        public static Rect ObjectivePanelRect(int screenWidth, int screenHeight)
        {
            var scale = ScaleForHeight(screenHeight);
            var left = 24f * scale;
            var alertLeft = screenWidth - 360f * scale - 24f * scale;
            var spaceBeforeAlert = alertLeft - left - 16f * scale;
            var width = Mathf.Min(920f * scale, screenWidth - 48f * scale, spaceBeforeAlert);
            return new Rect(left, 20f * scale, Mathf.Max(1f, width), 58f * scale);
        }

        public static Rect AlertPanelRect(int screenWidth, int screenHeight)
        {
            var scale = ScaleForHeight(screenHeight);
            var width = 360f * scale;
            var height = 78f * scale;
            return new Rect(screenWidth - width - 24f * scale, 38f * scale, width, height);
        }

        public static int FontSize(int designFontSize)
        {
            return FontSize(designFontSize, Screen.height);
        }

        public static int FontSize(int designFontSize, int screenHeight)
        {
            return Mathf.Max(1, Mathf.RoundToInt(designFontSize * ScaleForHeight(screenHeight)));
        }
    }
}
