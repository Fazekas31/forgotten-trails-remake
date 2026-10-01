using ForgottenTrail.Gameplay.Progression;
using UnityEngine;

namespace ForgottenTrail.Gameplay
{
    public sealed class DemoProgressionComponent : MonoBehaviour
    {
        private DemoProgression _progression;

        public DemoObjective CurrentObjective => Progression.CurrentObjective;
        public bool IsComplete => Progression.IsComplete;
        private DemoProgression Progression => _progression ?? (_progression = new DemoProgression());

        public bool TryComplete(DemoObjective objective)
        {
            return Progression.TryComplete(objective);
        }

        private void OnGUI()
        {
            if (IsComplete)
                return;

            var label = CurrentObjective.ToString();
            label = System.Text.RegularExpressions.Regex.Replace(label, "([a-z])([A-Z])", "$1 $2");
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.91f, 0.75f, 0.47f) }
            };
            GUI.Label(new Rect(28, 26, 520, 28), "OBJETIVO  ·  " + label, style);
        }
    }
}
