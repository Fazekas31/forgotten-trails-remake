using ForgottenTrail.Gameplay.Interaction;
using ForgottenTrail.Gameplay.Progression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenTrail.Gameplay.Lantern
{
    /// <summary>Luke's lantern pickup, first-person carry prop, and low-cost no-shadow light source.</summary>
    public sealed class HandLanternPickup : PlayerInteractable
    {
        [SerializeField] private float pickupRange = 2.8f;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private Collider pickupCollider;
        [SerializeField] private Light lampLight;
        [SerializeField] private Renderer glassRenderer;
        [SerializeField] private Material litGlass;
        [SerializeField] private Material unlitGlass;
        [SerializeField] private Transform woundedArmAfterHandoff;

        private HandLanternState _state;
        private bool _hasBeenPickedUp;
        private Vector3 _heldLocalScale;
        private bool _hasCapturedHeldScale;

        public override string Prompt => IsHeld ? string.Empty : _hasBeenPickedUp ? "Recuperar o lampião" : "Receber o lampião de Luke";
        public override string JournalEntry => _hasBeenPickedUp ? string.Empty : "Encontrei um homem ferido no portão de entrada. Ele me entregou seu lampião e disse que algo na cidade escuta tudo. Vim buscar Layla, mas parece que Ash Creek já começou a descarregar seu fardo em mim.";
        public bool IsHeld => State.IsHeld;
        public bool IsLit => State.IsLit;

        private HandLanternState State => _state ?? (_state = new HandLanternState(pickupRange));

        public void Configure(float range, Transform holdAnchor, Collider worldCollider, Light lightSource, Renderer glass, Material glassLit, Material glassUnlit, Transform reachingArm = null)
        {
            pickupRange = range;
            handAnchor = holdAnchor;
            pickupCollider = worldCollider;
            lampLight = lightSource;
            glassRenderer = glass;
            litGlass = glassLit;
            unlitGlass = glassUnlit;
            woundedArmAfterHandoff = reachingArm;
            _state = new HandLanternState(pickupRange);
            ApplyVisuals(true);
        }

        public override bool TryInteract(float distance, out string result, out DemoObjective? completedObjective)
        {
            if (!State.TryPickUp(distance))
            {
                result = string.Empty;
                completedObjective = null;
                return false;
            }

            if (pickupCollider != null)
                pickupCollider.enabled = false;
            _hasBeenPickedUp = true;
            if (handAnchor != null)
            {
                if (!_hasCapturedHeldScale)
                {
                    _heldLocalScale = transform.localScale;
                    _hasCapturedHeldScale = true;
                }

                transform.SetParent(handAnchor, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = _heldLocalScale;
            }

            ApplyVisuals(State.IsLit);
            if (woundedArmAfterHandoff != null)
                woundedArmAfterHandoff.localRotation = Quaternion.Euler(0f, 0f, -90f);
            result = "Protagonista: \"Você precisa de ajuda. Deixe-me levantá-lo. Procuro abrigo e uma mulher chamada Layla.\"\nLuke (Homem Ferido): \"Não há camas limpas em Ash Creek, forasteiro. Nem descanso... Se quiser ver o amanhã, fique com isto. (Entrega seu lampião a óleo).\"\nProtagonista: \"O que aconteceu com este lugar?\"\nLuke: \"Eu terminei minha marcha... Agora você vai carregar a escuridão por nós dois. Eles escutam tudo. Não faça barulho.\"";
            completedObjective = DemoObjective.FindLukeAtGate;
            return true;
        }

        public bool ForceDrop(Vector3 position, Quaternion rotation)
        {
            if (!State.TryDrop())
                return false;

            transform.SetParent(null, true);
            transform.position = position;
            transform.rotation = rotation;
            if (pickupCollider != null)
                pickupCollider.enabled = true;
            ApplyVisuals(false);
            return true;
        }

        public void FadeOutForEnding(float duration)
        {
            if (!IsHeld || !IsLit)
                return;

            if (Application.isPlaying)
                StartCoroutine(FadeOutForEndingBeat(duration));
            else
            {
                State.TryToggle();
                ApplyVisuals(false);
            }
        }

        private System.Collections.IEnumerator FadeOutForEndingBeat(float duration)
        {
            if (lampLight == null)
            {
                if (State.IsLit)
                    State.TryToggle();
                ApplyVisuals(false);
                yield break;
            }

            var startingIntensity = lampLight.intensity;
            var fadeDuration = Mathf.Max(0.01f, duration);
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                lampLight.intensity = Mathf.Lerp(startingIntensity, 0f, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            lampLight.intensity = 0f;
            if (State.IsLit)
                State.TryToggle();
            ApplyVisuals(false);
        }

        private void Awake()
        {
            if (_state == null)
                _state = new HandLanternState(pickupRange);
            ApplyVisuals(true);
        }

        private void Update()
        {
            if (!IsHeld || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
                return;

            if (State.TryToggle())
                ApplyVisuals(State.IsLit);
        }

        private void ApplyVisuals(bool isLit)
        {
            if (lampLight != null)
                lampLight.enabled = isLit;
            if (glassRenderer != null)
            {
                var material = isLit ? litGlass : unlitGlass;
                if (material != null)
                    glassRenderer.sharedMaterial = material;
            }
        }

        private void OnGUI()
        {
            if (!IsHeld)
                return;

            var text = IsLit ? "LANTERNA ACESA  ·  [F] APAGAR" : "LANTERNA APAGADA  ·  [F] ACENDER";
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = IsLit ? new Color(1f, 0.72f, 0.39f) : new Color(0.66f, 0.7f, 0.78f) }
            };
            GUI.Label(new Rect(Screen.width - 340f, Screen.height - 54f, 300f, 28f), text, style);
        }
    }
}
