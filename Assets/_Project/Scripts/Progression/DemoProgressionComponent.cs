using System;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.UI;
using UnityEngine;

namespace ForgottenTrail.Gameplay
{
    public sealed class DemoProgressionComponent : MonoBehaviour
    {
        private DemoProgression _progression;

        public event Action<DemoObjective> ObjectiveChanged;

        public DemoObjective CurrentObjective => Progression.CurrentObjective;
        public bool IsComplete => Progression.IsComplete;
        private DemoProgression Progression => _progression ?? (_progression = new DemoProgression());

        public bool TryComplete(DemoObjective objective)
        {
            if (!Progression.TryComplete(objective))
                return false;

            ObjectiveChanged?.Invoke(CurrentObjective);
            return true;
        }

        private void OnGUI()
        {
            if (IsComplete)
                return;

            var label = ObjectiveLabel(CurrentObjective);
            var scale = HudTextScale.Factor;
            var panel = HudTextScale.ObjectivePanelRect(Screen.width, Screen.height);
            var style = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = HudTextScale.FontSize(26),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.97f, 0.82f, 0.56f) }
            };
            var objectiveText = "OBJETIVO  ·  " + label;
            var textHeight = style.CalcHeight(new GUIContent(objectiveText), panel.width - 36f * scale);
            panel.height = Mathf.Max(panel.height, textHeight + 20f * scale);
            GUI.color = new Color(0.035f, 0.04f, 0.05f, 0.84f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 18f * scale, panel.y + 7f * scale, panel.width - 36f * scale, panel.height - 14f * scale), objectiveText, style);
        }

        private static string ObjectiveLabel(DemoObjective objective)
        {
            switch (objective)
            {
                case DemoObjective.FindLukeAtGate: return "Encontre Luke no portão";
                case DemoObjective.FollowBootprintsToSaloon: return "Siga as pegadas pela cidade";
                case DemoObjective.InvestigateSaloonClues: return "Investigue o saloon";
                case DemoObjective.ExamineSaloonKnife: return "Desça e examine a faca no balcão";
                case DemoObjective.ReceiveSheriffMission: return "Ouça o pedido do padre Elias";
                case DemoObjective.DiscoverChurchTruth: return "Descubra a verdade na igreja";
                case DemoObjective.FollowChesterAndJack: return "Siga Chester e Jack pelo beco do ferreiro";
                case DemoObjective.SearchSheriffOffice: return "Procure o escritório do xerife";
                case DemoObjective.ReturnToChurch: return "Volte à igreja";
                case DemoObjective.ConfrontCreatureInBarn: return "Enfrente a criatura no celeiro";
                case DemoObjective.ReachForest: return "Siga até a Floresta dos Suspiros";
                default: return string.Empty;
            }
        }
    }
}
