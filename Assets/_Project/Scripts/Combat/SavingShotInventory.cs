using System;
using ForgottenTrail.Gameplay.UI;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Combat
{
    /// <summary>Player-facing inventory for Gideon's .38 revolver and the barn encounter rounds.</summary>
    public sealed class SavingShotInventory : MonoBehaviour
    {
        private SavingShotInventoryState _state = new SavingShotInventoryState();

        public event Action ShotFired;

        public bool HasRevolver => State.HasRevolver;
        public int RoundsRemaining => State.RoundsRemaining;
        private SavingShotInventoryState State => _state ?? (_state = new SavingShotInventoryState());

        public bool TryAcquireRevolver(int rounds = SavingShotInventoryState.BarnRevolverRoundCount) => State.TryAcquireRevolver(rounds);

        public bool TryFire()
        {
            if (!State.TryFire())
                return false;

            ShotFired?.Invoke();
            return true;
        }

        public bool TryFireAtTarget(bool targetIsReady)
        {
            if (!State.TryFireAtTarget(targetIsReady))
                return false;

            ShotFired?.Invoke();
            return true;
        }

        private void OnGUI()
        {
            if (!HasRevolver)
                return;

            var label = RoundsRemaining > 0
                ? "REVÓLVER .38  ·  " + RoundsRemaining + (RoundsRemaining == 1 ? " TIRO" : " TIROS")
                : "REVÓLVER .38  ·  VAZIO";
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = HudTextScale.FontSize(24),
                fontStyle = FontStyle.Bold,
                normal = { textColor = RoundsRemaining > 0 ? new Color(0.91f, 0.75f, 0.47f) : new Color(0.66f, 0.68f, 0.72f) }
            };
            var scale = HudTextScale.Factor;
            var panel = new Rect(Screen.width - HudTextScale.Pixels(500f), Screen.height - HudTextScale.Pixels(108f), HudTextScale.Pixels(476f), HudTextScale.Pixels(42f));
            GUI.color = new Color(0.055f, 0.06f, 0.075f, 0.88f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12f * scale, panel.y + 4f * scale, panel.width - 24f * scale, panel.height - 8f * scale), label, style);
        }
    }
}
