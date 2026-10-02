using ForgottenTrail.Gameplay.Enemies;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Lantern;
using ForgottenTrail.Gameplay.Player;
using UnityEngine;

namespace ForgottenTrail.Gameplay.SheriffOffice
{
    /// <summary>Starts the office breach after the Red Book and lets the player slip past the blind creature.</summary>
    public sealed class SheriffOfficeEscapeSequence : MonoBehaviour
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private HandLanternPickup lantern;
        [SerializeField] private GameObject blindCreature;
        [SerializeField] private GameObject closedFrontDoor;
        [SerializeField] private GameObject breachedFrontDoor;
        [SerializeField] private GameObject rearExitHandle;
        [SerializeField] private GameObject rearExitBarrier;
        [SerializeField] private JackRescueInteractable jack;
        [SerializeField] private Light haleMuzzleFlash;

        private SheriffOfficeEscapeState _state = new SheriffOfficeEscapeState();
        private float _messageUntil;
        private float _muzzleFlashUntil;
        private string _message;
        private bool _jackIsQuiet;

        public bool IsActive => State.HasStarted && !State.HasEscaped;
        public bool HasEscaped => State.HasEscaped;
        public bool JackIsQuiet => _jackIsQuiet;
        public SheriffOfficeEscapeState State => _state ?? (_state = new SheriffOfficeEscapeState());
        public string CurrentMessage => _message;

        public void Configure(
            FirstPersonController investigator,
            HandLanternPickup handLantern,
            GameObject creature,
            GameObject intactDoor,
            GameObject breachedDoor,
            GameObject exitHandle,
            GameObject exitBarrier,
            JackRescueInteractable companion,
            Light muzzleFlash)
        {
            player = investigator;
            lantern = handLantern;
            blindCreature = creature;
            closedFrontDoor = intactDoor;
            breachedFrontDoor = breachedDoor;
            rearExitHandle = exitHandle;
            rearExitBarrier = exitBarrier;
            jack = companion;
            haleMuzzleFlash = muzzleFlash;
            _state = new SheriffOfficeEscapeState();
            _jackIsQuiet = false;
            if (jack != null)
                jack.ConfigureSheriffEscape(this);

            if (blindCreature != null)
                blindCreature.SetActive(false);
            if (closedFrontDoor != null)
                closedFrontDoor.SetActive(true);
            if (breachedFrontDoor != null)
                breachedFrontDoor.SetActive(false);
            if (rearExitHandle != null)
                rearExitHandle.SetActive(false);
            if (rearExitBarrier != null)
                rearExitBarrier.SetActive(false);
            if (haleMuzzleFlash != null)
                haleMuzzleFlash.enabled = false;
        }

        public bool Begin()
        {
            if (!State.Begin())
                return false;

            if (closedFrontDoor != null)
                closedFrontDoor.SetActive(false);
            if (breachedFrontDoor != null)
                breachedFrontDoor.SetActive(true);
            if (blindCreature != null)
                blindCreature.SetActive(true);
            if (rearExitHandle != null)
                rearExitHandle.SetActive(true);
            if (rearExitBarrier != null)
                rearExitBarrier.SetActive(true);

            _message = "A porta da delegacia é arrombada. A criatura cega entra farejando e imita a voz rouca do padre: \"O sino ainda toca pelos vivos...\" Apague o lampião [F], comande Jack para manter o silêncio, agache-se [Ctrl] e contorne as mesas destruídas até os fundos.";
            _messageUntil = Time.time + 8f;
            return true;
        }

        public bool TryLeaveQuietly(out string result)
        {
            if (!IsActive)
            {
                result = State.HasEscaped
                    ? "Você já está do lado de fora. Volte à igreja e confronte o falso Elias."
                    : "O caminho ainda não está livre.";
                return false;
            }

            var crouching = player != null && player.IsCrouching;
            var lanternLit = lantern != null && lantern.IsLit;
            if (!State.TryEscape(crouching, lanternLit, JackIsQuiet))
            {
                result = !JackIsQuiet
                    ? "Jack ainda pode choramingar. Aproxime-se dele e comande-o a ficar em silêncio."
                    : lanternLit
                    ? "A luz denuncia sua posição. Apague o lampião [F] e agache [Ctrl] antes de cruzar a porta."
                    : "Os passos ecoam pela sala. Agache [Ctrl] para cruzar a porta sem alertar a criatura.";
                player?.EmitNoise(transform.position, 0.82f);
                return true;
            }

            if (rearExitBarrier != null)
                rearExitBarrier.SetActive(false);

            if (haleMuzzleFlash != null)
            {
                haleMuzzleFlash.enabled = true;
                _muzzleFlashUntil = Time.time + 0.16f;
            }

            _message = "O tiro de Hale corta o silêncio. Você escapou — volte à igreja.";
            _messageUntil = Time.time + 6f;
            result = "Você sai agachado e deixa o lampião apagado. O tiro de Hale atinge a criatura. Volte à igreja.";
            return true;
        }

        public bool CommandJackToStayQuiet()
        {
            if (!IsActive || JackIsQuiet)
                return false;

            _jackIsQuiet = true;
            return true;
        }

        private void Update()
        {
            if (haleMuzzleFlash != null && haleMuzzleFlash.enabled && Time.time >= _muzzleFlashUntil)
                haleMuzzleFlash.enabled = false;
        }

        private void OnGUI()
        {
            if (!IsActive && Time.time >= _messageUntil)
                return;

            var text = IsActive
                ? "FUGA  ·  [F] APAGAR LAMPIÃO  ·  [CTRL] AGACHAR  ·  [E] SAIR PELOS FUNDOS"
                : _message;
            if (string.IsNullOrEmpty(text))
                return;

            var panel = new Rect(Screen.width * 0.5f - 300f, 92f, 600f, 48f);
            GUI.color = new Color(0.07f, 0.075f, 0.09f, 0.9f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.82f, 0.7f) }
            };
            GUI.Label(new Rect(panel.x + 12f, panel.y + 4f, panel.width - 24f, panel.height - 8f), text, style);
        }
    }
}
