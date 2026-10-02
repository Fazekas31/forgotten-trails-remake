using System;
using UnityEngine;

namespace ForgottenTrail.Gameplay.Combat
{
    /// <summary>Player-facing inventory for the revolver and its single saving shot.</summary>
    public sealed class SavingShotInventory : MonoBehaviour
    {
        private SavingShotInventoryState _state = new SavingShotInventoryState();

        public event Action ShotFired;

        public bool HasRevolver => State.HasRevolver;
        public int RoundsRemaining => State.RoundsRemaining;
        private SavingShotInventoryState State => _state ?? (_state = new SavingShotInventoryState());

        public bool TryAcquireRevolver() => State.TryAcquireRevolver();

        public bool TryFire()
        {
            if (!State.TryFire())
                return false;

            ShotFired?.Invoke();
            return true;
        }

        private void OnGUI()
        {
            if (!HasRevolver)
                return;

            var label = RoundsRemaining > 0 ? "REVÓLVER .38  ·  1 TIRO" : "REVÓLVER .38  ·  VAZIO";
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = RoundsRemaining > 0 ? new Color(0.91f, 0.75f, 0.47f) : new Color(0.66f, 0.68f, 0.72f) }
            };
            GUI.Label(new Rect(Screen.width - 340f, Screen.height - 82f, 300f, 26f), label, style);
        }
    }
}
