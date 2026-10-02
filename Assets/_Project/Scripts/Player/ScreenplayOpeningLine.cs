using UnityEngine;

namespace ForgottenTrail.Gameplay.Player
{
    /// <summary>Displays the protagonist's opening line from the screenplay at the start of the demo.</summary>
    public sealed class ScreenplayOpeningLine : MonoBehaviour
    {
        public const string ScriptedText = "Protagonista: \"Ash Creek... A última carta de Layla veio daqui.\"";

        [SerializeField] private float displayDuration = 6f;
        private float _visibleUntil;

        public void Begin(float duration = 6f)
        {
            displayDuration = Mathf.Max(0f, duration);
            _visibleUntil = Time.time + displayDuration;
        }

        private void Start()
        {
            Begin(displayDuration);
        }

        private void OnGUI()
        {
            if (Time.time >= _visibleUntil)
                return;

            var width = Mathf.Min(Screen.width * 0.62f, 760f);
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 17,
                normal = { textColor = new Color(0.91f, 0.86f, 0.74f) }
            };
            var height = style.CalcHeight(new GUIContent(ScriptedText), width - 30f) + 20f;
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.72f, width, height);
            GUI.color = new Color(0.08f, 0.075f, 0.065f, 0.82f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 15f, panel.y + 8f, panel.width - 30f, panel.height - 16f), ScriptedText, style);
        }
    }
}
