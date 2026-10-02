using System;
using ForgottenTrail.Gameplay.Progression;
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
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.91f, 0.75f, 0.47f) }
            };
            GUI.Label(new Rect(28, 26, 520, 28), "OBJETIVO  ·  " + label, style);
        }

        private static string ObjectiveLabel(DemoObjective objective)
        {
            switch (objective)
            {
                case DemoObjective.FindLukeAtGate: return "Encontre Luke no portão";
                case DemoObjective.FollowBootprintsToSaloon: return "Siga as pegadas pela cidade";
                case DemoObjective.InvestigateSaloonClues: return "Investigue o saloon";
                case DemoObjective.ExamineSaloonKnife: return "Desça e examine a faca no balcão";
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
