using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Lantern;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenTrail.Gameplay.Barn
{
    /// <summary>Runs the screenplay's barn ambush, Gideon's rescue, mimic fight, and forest reveal.</summary>
    public sealed class BarnEncounterSequence : MonoBehaviour
    {
        private enum Stage
        {
            Locked,
            AtDoor,
            Open,
            AwaitingLanternReveal,
            CreatureRevealed,
            Ambush,
            Combat,
            CreatureDown,
            GideonDialogue,
            ForestOpen,
            Ending,
            Complete
        }

        private const int HitsToDownCreature = 4;
        private const float AmbushDuration = 2.7f;
        private const float CreatureRevealDuration = 0.85f;
        private const float KnifeDefenseWindow = 0.9f;
        private const float FlareDuration = 2.6f;
        private const float FlareInterval = 5.2f;
        private const float GideonWalkDuration = 1.35f;
        private const float EndingStepDuration = 0.95f;
        private const float EndingHardCutAt = 1.2f;
        private const float EndingTitleAt = 1.65f;

        private static readonly string[] GideonDialogue =
        {
            "Protagonista: \"O que... o que era isso?\"",
            "Gideon: (Recarregando a espingarda com calma assustadora) \"O resultado da ganância de Ash Creek. Eles cavaram fundo demais na mina e acordaram os imitadores. Eles vestem nossas vozes como quem veste um casaco.\"",
            "Protagonista: \"Eu vim buscar Layla. O registro dizia que ela estava aqui. Aquele bicho... usou a voz dela.\"",
            "Gideon: \"Ela esteve. Mas sua garota era esperta. Quando ela viu os primeiros doentes mudando, ela percebeu que trancar portas não ia adiantar. Ela fugiu antes que o xerife trancasse esse abatedouro. Mas ela não foi embora pela estrada principal.\"",
            "Protagonista: \"Para onde ela foi?\"",
            "Gideon: \"Ela achou que podia destruir a raiz disso tudo. Ela seguiu o rastro das criaturas de volta para o buraco de onde rastejaram. Ela entrou na Floresta dos Suspiros. O caminho direto para o coração da mina.\"",
            "Jack vai até a beira da porta, fareja o vento que vem das árvores negras e solta um uivo baixo e fúnebre.",
            "Gideon: (Olhando para as árvores escuras) \"A cidade já está morta, cowboy. A floresta é onde eles não precisam mais fingir que são humanos. Se quiser achar a sua Layla... é lá que o verdadeiro pesadelo começa.\""
        };

        [SerializeField] private DemoProgressionComponent progression;
        [SerializeField] private BarnScreenplayAudio audioCues;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private HandLanternPickup lantern;
        [SerializeField] private JackRescueInteractable jack;
        [SerializeField] private BarnKeyInventory barnKey;
        [SerializeField] private CombatKnifeInventory knife;
        [SerializeField] private SavingShotInventory revolver;
        [SerializeField] private GameObject interiorSetpiece;
        [SerializeField] private Transform leftEntranceDoor;
        [SerializeField] private Transform rightEntranceDoor;
        [SerializeField] private GameObject remains;
        [SerializeField] private GameObject mimic;
        [SerializeField] private GameObject gideon;
        [SerializeField] private GameObject breachBoards;
        [SerializeField] private Transform leftForestDoor;
        [SerializeField] private Transform rightForestDoor;
        [SerializeField] private GameObject forestBackdrop;
        [SerializeField] private BarnInteractionPoint doorPoint;
        [SerializeField] private BarnInteractionPoint remainsPoint;
        [SerializeField] private BarnInteractionPoint rafterPoint;
        [SerializeField] private BarnInteractionPoint gideonPoint;
        [SerializeField] private BarnInteractionPoint finalShotPoint;
        [SerializeField] private BarnInteractionPoint forestThresholdPoint;
        [SerializeField] private Transform flareLight;
        [SerializeField] private GameObject revolverVisual;
        [SerializeField] private Transform[] rafterWaypoints;
        [SerializeField] private Transform creatureDownPosition;
        [SerializeField] private Transform jackAftermathPosition;

        private Stage _stage;
        private bool _isSubscribed;
        private bool _flareIsActive;
        private float _ambushStartedAt;
        private float _flareEndsAt;
        private float _nextFlareAt;
        private float _nextRafterPointAt;
        private float _endingStartedAt;
        private Vector3 _endingStepStart;
        private Vector3 _endingStepTarget;
        private bool _endingBellPlayed;
        private bool _knifeDefenseResolved;
        private bool _knifeBrokenInAmbush;
        private bool _lanternThrownInAmbush;
        private bool _jackKnockedOutInAmbush;
        private bool _falsePriestLinePlayed;
        private bool _gideonIsWalking;
        private bool _gideonAtForestDoors;
        private int _dialogueIndex;
        private int _successfulHits;
        private int _rafterWaypointIndex;
        private string _message;
        private float _messageUntil;
        private float _knifeDefenseUntil;

        public bool HasOpenedDoors => _stage != Stage.Locked && _stage != Stage.AtDoor;
        public bool HasStartedBossFight => _stage == Stage.Combat || _stage == Stage.CreatureDown;
        public bool IsShowingEnding => _stage == Stage.Ending;
        public bool CanDefendAgainstMimic => _stage == Stage.Ambush
            && !_knifeDefenseResolved
            && Time.time <= _knifeDefenseUntil
            && knife != null
            && knife.HasKnife;
        public bool IsGideonWalkingToForestDoors => _gideonIsWalking;
        public bool IsGideonAtForestDoors => _gideonAtForestDoors;
        public int SuccessfulHits => _successfulHits;
        public string[] ScriptedGideonDialogue => GideonDialogue;

        public void Configure(
            DemoProgressionComponent demoProgression,
            FirstPersonController investigator,
            PlayerInteractor playerInteractor,
            HandLanternPickup handLantern,
            JackRescueInteractable companion,
            BarnKeyInventory key,
            CombatKnifeInventory combatKnife,
            SavingShotInventory firearm,
            GameObject interior,
            Transform entranceDoorLeft,
            Transform entranceDoorRight,
            GameObject remainsSetpiece,
            GameObject mimicCreature,
            GameObject oldHunter,
            GameObject brokenBoards,
            Transform forestDoorLeft,
            Transform forestDoorRight,
            GameObject forest,
            BarnInteractionPoint door,
            BarnInteractionPoint remainsInteraction,
            BarnInteractionPoint creatureInteraction,
            BarnInteractionPoint gideonInteraction,
            BarnInteractionPoint executionInteraction,
            BarnInteractionPoint forestInteraction,
            Transform flare,
            GameObject firearmModel,
            Transform[] creatureRoute,
            Transform downPoint,
            Transform jackAftermath)
        {
            Unsubscribe();
            progression = demoProgression;
            player = investigator;
            interactor = playerInteractor;
            lantern = handLantern;
            jack = companion;
            barnKey = key;
            knife = combatKnife;
            revolver = firearm;
            interiorSetpiece = interior;
            leftEntranceDoor = entranceDoorLeft;
            rightEntranceDoor = entranceDoorRight;
            remains = remainsSetpiece;
            mimic = mimicCreature;
            gideon = oldHunter;
            breachBoards = brokenBoards;
            leftForestDoor = forestDoorLeft;
            rightForestDoor = forestDoorRight;
            forestBackdrop = forest;
            doorPoint = door;
            remainsPoint = remainsInteraction;
            rafterPoint = creatureInteraction;
            gideonPoint = gideonInteraction;
            finalShotPoint = executionInteraction;
            forestThresholdPoint = forestInteraction;
            audioCues = GetComponent<BarnScreenplayAudio>();
            flareLight = flare;
            revolverVisual = firearmModel;
            rafterWaypoints = creatureRoute;
            creatureDownPosition = downPoint;
            jackAftermathPosition = jackAftermath;
            _stage = Stage.Locked;
            _dialogueIndex = 0;
            _successfulHits = 0;
            _flareIsActive = false;
            _knifeDefenseResolved = false;
            _knifeBrokenInAmbush = false;
            _lanternThrownInAmbush = false;
            _jackKnockedOutInAmbush = false;
            _falsePriestLinePlayed = false;
            _gideonIsWalking = false;
            _gideonAtForestDoors = false;
            Subscribe();
            ApplyObjective(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
            ApplyConfiguredState();
        }

        public string GetPrompt(BarnInteractionKind kind)
        {
            switch (kind)
            {
                case BarnInteractionKind.Door:
                    return _stage == Stage.AtDoor && barnKey != null && barnKey.HasKey
                        ? knife != null && knife.HasKnife ? "Destrancar as portas do celeiro" : "Encontre a faca no saloon antes de abrir o celeiro"
                        : string.Empty;
                case BarnInteractionKind.Remains:
                    return _stage == Stage.Open ? "Examinar os restos" : string.Empty;
                case BarnInteractionKind.RafterCreature:
                    return _stage == Stage.AwaitingLanternReveal ? "Erguer o lampião para as vigas" : string.Empty;
                case BarnInteractionKind.Gideon:
                    return _stage == Stage.GideonDialogue
                        && !_gideonIsWalking
                        && (jack == null || !jack.IsRunningToAftermath)
                        ? "Continuar conversa"
                        : string.Empty;
                case BarnInteractionKind.FinalShot:
                    return _stage == Stage.CreatureDown ? "Executar o tiro final" : string.Empty;
                case BarnInteractionKind.ForestThreshold:
                    return _stage == Stage.ForestOpen ? "Entrar na Floresta dos Suspiros" : string.Empty;
                default:
                    return string.Empty;
            }
        }

        public bool TryInteract(BarnInteractionKind kind, out string result)
        {
            switch (kind)
            {
                case BarnInteractionKind.Door:
                    return OpenBarnDoor(out result);
                case BarnInteractionKind.Remains:
                    return InspectRemains(out result);
                case BarnInteractionKind.RafterCreature:
                    return _stage == Stage.AwaitingLanternReveal
                        ? RevealCreatureFromInteraction(out result)
                        : StartAmbush(out result);
                case BarnInteractionKind.Gideon:
                    return AdvanceGideonDialogue(out result);
                case BarnInteractionKind.FinalShot:
                    return FinishCreature(out result);
                case BarnInteractionKind.ForestThreshold:
                    return BeginEnding(out result);
                default:
                    result = string.Empty;
                    return false;
            }
        }

        private void Awake()
        {
            if (progression == null) progression = FindFirstObjectByType<DemoProgressionComponent>();
            if (player == null) player = FindFirstObjectByType<FirstPersonController>();
            if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
            if (lantern == null) lantern = FindFirstObjectByType<HandLanternPickup>();
            if (jack == null) jack = FindFirstObjectByType<JackRescueInteractable>();
            if (barnKey == null && player != null) barnKey = player.GetComponent<BarnKeyInventory>();
            if (knife == null && player != null) knife = player.GetComponent<CombatKnifeInventory>();
            if (revolver == null && player != null) revolver = player.GetComponent<SavingShotInventory>();
            Subscribe();
            ApplyObjective(progression != null ? progression.CurrentObjective : DemoObjective.Complete);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!_isSubscribed && progression != null)
            {
                progression.ObjectiveChanged += ApplyObjective;
                _isSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_isSubscribed && progression != null)
            {
                progression.ObjectiveChanged -= ApplyObjective;
                _isSubscribed = false;
            }
        }

        private void ApplyObjective(DemoObjective objective)
        {
            if (_stage == Stage.Locked && objective == DemoObjective.ConfrontCreatureInBarn)
                _stage = Stage.AtDoor;
            if (objective == DemoObjective.Complete && _stage != Stage.Ending)
                _stage = Stage.Complete;
        }

        private void ApplyConfiguredState()
        {
            if (interiorSetpiece != null) interiorSetpiece.SetActive(true);
            if (remains != null) remains.SetActive(true);
            if (mimic != null) mimic.SetActive(false);
            var mimicWound = mimic != null ? mimic.transform.Find("Mimic — torn shoulder from Gideon's shotgun") : null;
            if (mimicWound != null) mimicWound.gameObject.SetActive(false);
            if (gideon != null) gideon.SetActive(false);
            if (breachBoards != null) breachBoards.SetActive(true);
            if (forestBackdrop != null) forestBackdrop.SetActive(true);
            if (flareLight != null) flareLight.gameObject.SetActive(false);
            if (revolverVisual != null) revolverVisual.SetActive(false);
            if (leftEntranceDoor != null) leftEntranceDoor.localRotation = Quaternion.identity;
            if (rightEntranceDoor != null) rightEntranceDoor.localRotation = Quaternion.identity;
            if (leftForestDoor != null) leftForestDoor.localRotation = Quaternion.identity;
            if (rightForestDoor != null) rightForestDoor.localRotation = Quaternion.identity;
            if (doorPoint != null) doorPoint.Configure(this, BarnInteractionKind.Door);
            if (remainsPoint != null) remainsPoint.Configure(this, BarnInteractionKind.Remains);
            if (rafterPoint != null) rafterPoint.Configure(this, BarnInteractionKind.RafterCreature);
            if (gideonPoint != null) gideonPoint.Configure(this, BarnInteractionKind.Gideon);
            if (finalShotPoint != null) finalShotPoint.Configure(this, BarnInteractionKind.FinalShot);
            if (forestThresholdPoint != null) forestThresholdPoint.Configure(this, BarnInteractionKind.ForestThreshold);
        }

        private bool OpenBarnDoor(out string result)
        {
            if (_stage != Stage.AtDoor || barnKey == null || !barnKey.HasKey || knife == null || !knife.HasKnife)
            {
                result = string.Empty;
                return false;
            }

            _stage = Stage.Open;
            SetDoorPose(leftEntranceDoor, rightEntranceDoor);
            GetAudioCues().PlayDoorCreak(doorPoint != null ? doorPoint.transform.position : transform.position);
            jack?.HoldAtBarn(new Vector3(-1.65f, 0f, 92.3f));
            result = "Voz distorcida de Layla: \"Você veio... Eu sabia que você carregaria esse peso por mim. Abra a porta.\"";
            ShowMessage("O vento cessa. Jack recua, com o rabo entre as pernas, choramingando e recusando-se a avançar. A fechadura destranca; as portas rangem para uma escuridão absoluta.", 7f);
            return true;
        }

        private bool InspectRemains(out string result)
        {
            if (_stage != Stage.Open)
            {
                result = string.Empty;
                return false;
            }

            _stage = Stage.AwaitingLanternReveal;
            result = "Protagonista: \"Meu Deus... Hale não os trancou aqui para protegê-los. Ele trancou a comida delas.\"\nVoz de Layla: \"Por que você demorou tanto?\"";
            ShowMessage("A voz vem das vigas. Erga o lampião para revelar o que se move no alto.", 6f);
            return true;
        }

        public bool TryRevealCreatureWithLantern()
        {
            if (_stage != Stage.AwaitingLanternReveal
                || player == null
                || player.ViewCamera == null
                || lantern == null
                || !lantern.IsHeld
                || !lantern.IsLit
                || mimic == null)
                return false;

            var target = rafterPoint != null ? rafterPoint.transform.position : mimic.transform.position + Vector3.up * 5f;
            var direction = target - player.ViewCamera.transform.position;
            if (direction.sqrMagnitude < 0.001f
                || direction.magnitude > 40f
                || Vector3.Angle(player.ViewCamera.transform.forward, direction) > 11f)
                return false;

            _stage = Stage.CreatureRevealed;
            _nextRafterPointAt = Time.time + CreatureRevealDuration;
            mimic.SetActive(true);
            SetCreatureVisible(true);
            GetAudioCues().PlayMimicWhisper(mimic.transform.position, 0.6f);
            ShowMessage("A criatura pálida se revela entre as vigas: sem olhos, com membros compridos e a mandíbula escancarada.", 3f);
            return true;
        }

        private bool StartAmbush(out string result)
        {
            if (_stage != Stage.CreatureRevealed)
            {
                result = string.Empty;
                return false;
            }

            _stage = Stage.Ambush;
            _ambushStartedAt = Time.time;
            _knifeDefenseUntil = Time.time + KnifeDefenseWindow;
            _knifeDefenseResolved = false;
            _knifeBrokenInAmbush = false;
            _lanternThrownInAmbush = false;
            _jackKnockedOutInAmbush = false;
            _falsePriestLinePlayed = false;
            GetAudioCues().PlayMimicShriek(mimic != null ? mimic.transform.position : transform.position);
            if (mimic != null)
                mimic.transform.position = player != null ? player.transform.position + player.transform.forward * 1.4f : mimic.transform.position;
            if (player != null)
                player.SetGameplayInputEnabled(false);
            if (Application.isPlaying)
                StartCoroutine(PlayKnockdownPose());

            result = "O mímico solta um guincho ensurdecedor e se lança das vigas sobre o protagonista.";
            ShowMessage(result, AmbushDuration);
            return true;
        }

        private bool RevealCreatureFromInteraction(out string result)
        {
            if (!TryRevealCreatureWithLantern())
            {
                result = string.Empty;
                return false;
            }

            result = "A luz do lampião revela o mímico entre as vigas.";
            return true;
        }

        public bool TryDefendAgainstMimic()
        {
            if (!CanDefendAgainstMimic)
                return false;

            _knifeDefenseResolved = true;
            _knifeBrokenInAmbush = knife.BreakAgainstMimic();
            return _knifeBrokenInAmbush;
        }

        private void CompleteAmbush()
        {
            if (_stage != Stage.Ambush)
                return;

            _stage = Stage.Combat;
            if (!Application.isPlaying && player != null)
                player.SetGameplayInputEnabled(true);
            if (breachBoards != null)
                breachBoards.SetActive(false);
            if (gideon != null)
                gideon.SetActive(true);
            SetCreatureVisible(false);
            GetAudioCues().PlayGunshot(gideon != null ? gideon.transform.position : transform.position, 1f);
            var breachDust = transform.Find("Gideon — breach dust");
            if (breachDust != null)
                breachDust.gameObject.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
            _successfulHits = 0;
            _nextFlareAt = Time.time + 0.4f;
            var thrownRevolver = gideon != null ? gideon.transform.Find("Gideon's .38 — thrown to the investigator") : null;
            if (Application.isPlaying && thrownRevolver != null)
                StartCoroutine(ThrowRevolverToPlayer(thrownRevolver));
            else
            {
                if (revolver != null && !revolver.HasRevolver)
                    revolver.TryAcquireRevolver();
                if (revolverVisual != null)
                    revolverVisual.SetActive(true);
            }
            if (mimic != null)
            {
                var shoulderWound = mimic.transform.Find("Mimic — torn shoulder from Gideon's shotgun");
                if (shoulderWound != null)
                    shoulderWound.gameObject.SetActive(true);
                mimic.SetActive(true);
                SetCreatureVisible(false);
                MoveCreatureToFirstRafter();
            }
            ShowMessage("As tábuas da parede lateral explodem. Um estampido ensurdecedor de espingarda ilumina o escuro. O tiro arranca um pedaço do ombro da criatura, que grita, solta o protagonista e recua para as sombras. Gideon entra com a espingarda fumegante e lança o revólver .38 enferrujado contra o peito do protagonista.\nGideon: \"Levanta, cowboy! O inferno ainda não engoliu você! Pega a arma e mira no som!\"", 8f);
        }

        private System.Collections.IEnumerator ThrowRevolverToPlayer(Transform thrownRevolver)
        {
            var camera = player != null ? player.ViewCamera : null;
            if (thrownRevolver == null || camera == null)
            {
                if (revolver != null && !revolver.HasRevolver)
                    revolver.TryAcquireRevolver();
                if (revolverVisual != null)
                    revolverVisual.SetActive(true);
                yield break;
            }

            var start = thrownRevolver.position;
            var startRotation = thrownRevolver.rotation;
            thrownRevolver.SetParent(null, true);
            var end = camera.transform.position + camera.transform.forward * 0.42f + Vector3.up * 0.18f;
            var arc = (start + end) * 0.5f + Vector3.up * 0.9f;
            const float flightDuration = 0.8f;
            var elapsed = 0f;
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / flightDuration);
                thrownRevolver.position = (1f - t) * (1f - t) * start
                    + 2f * (1f - t) * t * arc
                    + t * t * end;
                thrownRevolver.rotation = startRotation * Quaternion.Euler(0f, t * 540f, t * 360f);
                yield return null;
            }

            thrownRevolver.gameObject.SetActive(false);
            if (revolver != null && !revolver.HasRevolver)
                revolver.TryAcquireRevolver();
            if (revolverVisual == null)
                yield break;

            var heldPosition = revolverVisual.transform.localPosition;
            revolverVisual.transform.localPosition = heldPosition + Vector3.down * 0.55f;
            revolverVisual.SetActive(true);
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / 0.3f);
                revolverVisual.transform.localPosition = Vector3.Lerp(heldPosition + Vector3.down * 0.55f, heldPosition, t);
                yield return null;
            }
            revolverVisual.transform.localPosition = heldPosition;
        }

        private bool FinishCreature(out string result)
        {
            if (_stage != Stage.CreatureDown || revolver == null || !revolver.TryFire())
            {
                result = string.Empty;
                return false;
            }

            var camera = player != null ? player.ViewCamera : null;
            GetAudioCues().PlayGunshot(camera != null ? camera.transform.position : transform.position, 0.9f);
            player?.EmitNoise(camera != null ? camera.transform.position : transform.position, 1f);

            _stage = Stage.GideonDialogue;
            _dialogueIndex = 0;
            if (revolverVisual != null)
                revolverVisual.SetActive(false);
            if (jack != null)
            {
                var playerPosition = player != null
                    ? player.transform.position + player.transform.right * -0.85f
                    : jackAftermathPosition != null ? jackAftermathPosition.position : jack.transform.position;
                var corpsePosition = mimic != null ? mimic.transform.position : transform.position;
                jack.WakeAndRunToAftermath(playerPosition, corpsePosition);
                if (Application.isPlaying)
                    StartCoroutine(PlayJackGrowlAfterAftermathRun());
            }
            result = string.Empty;
            return true;
        }

        private bool AdvanceGideonDialogue(out string result)
        {
            if (_stage != Stage.GideonDialogue
                || _gideonIsWalking
                || _dialogueIndex >= GideonDialogue.Length
                || jack != null && jack.IsRunningToAftermath)
            {
                result = string.Empty;
                return false;
            }

            if (_dialogueIndex == 0 && player != null && gideon != null)
                player.FaceTarget(gideon.transform.position + Vector3.up * 1.25f);

            if (_dialogueIndex == 5 && !_gideonAtForestDoors)
            {
                _gideonIsWalking = true;
                result = string.Empty;
                ShowMessage("Gideon caminha até os fundos do celeiro e empurra uma porta dupla que dá para os limites da cidade. Do outro lado, uma imensidão de árvores negras e retorcidas se ergue sobre a neblina espessa. Um vento gélido entra no celeiro.", 6f);
                if (Application.isPlaying)
                    StartCoroutine(WalkGideonToForestDoors());
                else
                    CompleteGideonWalkToForestDoors();
                return true;
            }

            var currentIndex = _dialogueIndex++;
            result = GideonDialogue[currentIndex];
            if (currentIndex == 6)
            {
                ShowMessage(result, 4f);
                GetAudioCues().PlayJackHowl(jack != null ? jack.transform.position : transform.position);
            }
            if (_dialogueIndex == GideonDialogue.Length)
            {
                _stage = Stage.ForestOpen;
                if (progression != null)
                    progression.TryComplete(DemoObjective.ConfrontCreatureInBarn);
            }
            return true;
        }

        private System.Collections.IEnumerator PlayJackGrowlAfterAftermathRun()
        {
            while (_stage == Stage.GideonDialogue && jack != null && jack.IsRunningToAftermath)
                yield return null;

            if (_stage == Stage.GideonDialogue && jack != null)
                GetAudioCues().PlayJackGrowl(jack.transform.position);
        }

        private System.Collections.IEnumerator WalkGideonToForestDoors()
        {
            var gideonStart = gideon != null ? gideon.transform.position : Vector3.zero;
            var gideonTarget = GetForestDoorInteriorPosition(gideonStart.y);
            var jackStart = jack != null ? jack.transform.position : Vector3.zero;
            var jackTarget = gideonTarget + Vector3.left * 1.2f - Vector3.forward * 0.45f;
            if (jack != null)
            {
                jackTarget.y = jackStart.y;
                jack.HoldAtBarn(jackStart);
            }

            var elapsed = 0f;
            while (elapsed < GideonWalkDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / GideonWalkDuration);
                if (gideon != null)
                {
                    gideon.transform.position = Vector3.Lerp(gideonStart, gideonTarget, t);
                    FaceForestDoors(gideon.transform, t < 0.78f ? gideonTarget : GetForestDoorCenter());
                }
                if (jack != null)
                    jack.transform.position = Vector3.Lerp(jackStart, jackTarget, t);
                yield return null;
            }

            if (gideon != null)
            {
                gideon.transform.position = gideonTarget;
                FaceForestDoors(gideon.transform, GetForestDoorCenter());
            }
            if (jack != null)
                jack.HoldAtBarn(jackTarget);
            CompleteGideonWalkToForestDoors();
        }

        private void CompleteGideonWalkToForestDoors()
        {
            if (gideon != null)
            {
                gideon.transform.position = GetForestDoorInteriorPosition(gideon.transform.position.y);
                FaceForestDoors(gideon.transform, GetForestDoorCenter());
            }
            if (jack != null && forestThresholdPoint != null)
            {
                var jackTarget = forestThresholdPoint.transform.position + Vector3.left * 1.2f - Vector3.forward * 0.45f;
                jackTarget.y = jack.transform.position.y;
                jack.HoldAtBarn(jackTarget);
            }

            SetDoorPose(leftForestDoor, rightForestDoor);
            GetAudioCues().PlayDoorCreak(GetForestDoorCenter());
            _gideonAtForestDoors = true;
            _gideonIsWalking = false;
            if (_dialogueIndex == 5)
            {
                ShowMessage(GideonDialogue[_dialogueIndex], 8f);
                _dialogueIndex++;
            }
        }

        private Vector3 GetForestDoorCenter()
        {
            if (leftForestDoor != null && rightForestDoor != null)
                return (leftForestDoor.position + rightForestDoor.position) * 0.5f;
            return forestThresholdPoint != null ? forestThresholdPoint.transform.position : transform.position + Vector3.forward * 8f;
        }

        private Vector3 GetForestDoorInteriorPosition(float height)
        {
            var position = GetForestDoorCenter() - Vector3.forward * 1.35f;
            position.y = height;
            return position;
        }

        private static void FaceForestDoors(Transform actor, Vector3 target)
        {
            if (actor == null)
                return;

            var direction = Vector3.ProjectOnPlane(target - actor.position, Vector3.up);
            if (direction.sqrMagnitude > 0.001f)
                actor.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private bool BeginEnding(out string result)
        {
            if (_stage != Stage.ForestOpen || progression == null || progression.CurrentObjective != DemoObjective.ReachForest)
            {
                result = string.Empty;
                return false;
            }

            _stage = Stage.Ending;
            _endingStartedAt = Time.time;
            _endingBellPlayed = false;
            _endingStepStart = player != null ? player.transform.position : Vector3.zero;
            _endingStepTarget = _endingStepStart;
            if (player != null)
            {
                var treeFocus = forestThresholdPoint != null
                    ? forestThresholdPoint.transform.position + Vector3.forward * 12f + Vector3.up * 2.6f
                    : player.transform.position + player.transform.forward * 12f + Vector3.up * 2.6f;
                player.FaceTarget(treeFocus);
                var towardForest = forestThresholdPoint != null
                    ? Vector3.ProjectOnPlane(forestThresholdPoint.transform.position - player.transform.position, Vector3.up).normalized
                    : player.transform.forward;
                if (towardForest.sqrMagnitude < 0.001f)
                    towardForest = player.transform.forward;
                _endingStepTarget = forestThresholdPoint != null
                    ? forestThresholdPoint.transform.position + towardForest * 2.2f
                    : player.transform.position + towardForest * 4.5f;
            _endingStepTarget.y = player.transform.position.y;
                GetAudioCues().PlayMineWhispers(forestThresholdPoint != null ? forestThresholdPoint.transform.position : player.transform.position);
            }
            lantern?.FadeOutForEnding(0.95f);
            player?.SetGameplayInputEnabled(false);
            progression.TryComplete(DemoObjective.ReachForest);
            result = string.Empty;
            return true;
        }

        private void FireRevolver()
        {
            if (revolver == null || !revolver.HasRevolver || revolver.RoundsRemaining <= 0)
                return;

            var camera = player != null ? player.ViewCamera : null;
            var target = mimic != null ? mimic.transform.position + Vector3.up * 1.1f : Vector3.zero;
            var aimedAtCreature = camera != null
                && mimic != null
                && Vector3.Distance(camera.transform.position, target) < 38f
                && Vector3.Angle(camera.transform.forward, target - camera.transform.position) < 7f;
            var isIlluminated = _flareIsActive || lantern != null && lantern.IsHeld && lantern.IsLit;

            if (!aimedAtCreature || !isIlluminated)
            {
                ShowMessage(aimedAtCreature
                    ? "A criatura corre pelas paredes e pelas vigas; no escuro, o disparo não a alcança."
                    : "Escute as tábuas e mire na criatura quando a luz a revelar.", 3f);
                return;
            }

            if (!revolver.TryFireAtTarget(aimedAtCreature && isIlluminated))
                return;

            GetAudioCues().PlayGunshot(camera.transform.position);
            player?.EmitNoise(camera.transform.position, 1f);

            _successfulHits++;
            if (_successfulHits >= HitsToDownCreature)
            {
                _stage = Stage.CreatureDown;
                if (mimic != null && creatureDownPosition != null)
                    mimic.transform.position = creatureDownPosition.position;
                SetCreatureVisible(true);
                if (flareLight != null)
                    flareLight.gameObject.SetActive(false);
                _flareIsActive = false;
                ShowMessage("Após quatro tiros, a criatura cai no centro do celeiro, agonizando. Aproxime-se para o tiro final.", 5f);
                return;
            }

            ShowMessage("Tiro certeiro. A criatura cambaleia e volta a se perder nas vigas.", 2.5f);
        }

        private void Update()
        {
            if (_stage == Stage.AwaitingLanternReveal)
                TryRevealCreatureWithLantern();

            if (_stage == Stage.CreatureRevealed && Time.time >= _nextRafterPointAt)
                StartAmbush(out _);

            if (_stage == Stage.Ambush)
            {
                if (CanDefendAgainstMimic && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    TryDefendAgainstMimic();

                UpdateAmbushBeats();
                if (_stage == Stage.Ambush && Time.time >= _ambushStartedAt + AmbushDuration)
                    CompleteAmbush();
            }

            if (_stage == Stage.Combat)
            {
                UpdateRafterMovement();
                UpdateFlare();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    FireRevolver();
            }

            if (_stage == Stage.Ending)
                UpdateEndingBeat();
        }

        private void UpdateAmbushBeats()
        {
            var elapsed = Time.time - _ambushStartedAt;
            if (!_knifeDefenseResolved && Time.time >= _knifeDefenseUntil)
            {
                _knifeDefenseResolved = true;
                _knifeBrokenInAmbush = knife != null && knife.BreakAgainstMimic();
            }

            if (!_lanternThrownInAmbush && elapsed >= 0.95f)
            {
                _lanternThrownInAmbush = true;
                if (player != null && lantern != null && lantern.IsHeld)
                    lantern.ForceDrop(
                        player.transform.position + player.transform.forward * 1.1f + player.transform.right * -1.1f,
                        Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f));

                ShowMessage("A lâmina resvala na pele pálida e se quebra com um golpe. O lampião é arremessado; a luz resta apenas nas frestas da lua.", 2.2f);
            }

            if (!_jackKnockedOutInAmbush && elapsed >= 1.2f)
            {
                _jackKnockedOutInAmbush = true;
                jack?.KnockOutAtBarn(new Vector3(5.15f, 0.08f, 104.9f));
                ShowMessage("Jack morde a perna do monstro, mas é arremessado contra o feno e fica desacordado.", 2.2f);
            }

            if (!_falsePriestLinePlayed && elapsed >= 1.75f)
            {
                _falsePriestLinePlayed = true;
                ShowMessage("Voz do Falso Padre Elias: \"O sino dobrou pra você...\"", 2.7f);
            }
        }

        private System.Collections.IEnumerator PlayKnockdownPose()
        {
            if (player == null || player.ViewCamera == null)
            {
                if (player != null)
                    player.SetGameplayInputEnabled(true);
                yield break;
            }

            var cameraTransform = player.ViewCamera.transform;
            var standingPosition = cameraTransform.localPosition;
            var standingRotation = cameraTransform.localRotation;
            var fallenPosition = new Vector3(standingPosition.x, Mathf.Max(0.52f, standingPosition.y * 0.48f), standingPosition.z);
            var fallenRotation = standingRotation * Quaternion.Euler(17f, 0f, -11f);
            const float fallDuration = 0.38f;
            const float recoverDuration = 0.68f;
            var elapsed = 0f;

            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / fallDuration);
                cameraTransform.localPosition = Vector3.Lerp(standingPosition, fallenPosition, t);
                cameraTransform.localRotation = Quaternion.Slerp(standingRotation, fallenRotation, t);
                yield return null;
            }

            cameraTransform.localPosition = fallenPosition;
            cameraTransform.localRotation = fallenRotation;
            while (_stage == Stage.Ambush)
                yield return null;

            yield return new WaitForSeconds(0.45f);

            elapsed = 0f;
            while (elapsed < recoverDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / recoverDuration);
                cameraTransform.localPosition = Vector3.Lerp(fallenPosition, standingPosition, t);
                cameraTransform.localRotation = Quaternion.Slerp(fallenRotation, standingRotation, t);
                yield return null;
            }

            cameraTransform.localPosition = standingPosition;
            cameraTransform.localRotation = standingRotation;
            player.SetGameplayInputEnabled(true);
        }

        private void UpdateRafterMovement()
        {
            if (mimic == null || rafterWaypoints == null || rafterWaypoints.Length == 0 || _flareIsActive)
                return;

            if (Time.time >= _nextRafterPointAt)
            {
                _rafterWaypointIndex = (_rafterWaypointIndex + 1) % rafterWaypoints.Length;
                _nextRafterPointAt = Time.time + Random.Range(0.55f, 1.05f);
                var cuePosition = rafterWaypoints[_rafterWaypointIndex] != null ? rafterWaypoints[_rafterWaypointIndex].position : mimic.transform.position;
                GetAudioCues().PlayDoorCreak(cuePosition);
                GetAudioCues().PlayMimicWhisper(cuePosition, 0.24f);
            }

            var point = rafterWaypoints[_rafterWaypointIndex];
            if (point != null)
                mimic.transform.position = Vector3.MoveTowards(mimic.transform.position, point.position, 7.5f * Time.deltaTime);
        }

        private void UpdateFlare()
        {
            if (!_flareIsActive && Time.time >= _nextFlareAt)
            {
                _flareIsActive = true;
                _flareEndsAt = Time.time + FlareDuration;
                if (flareLight != null)
                    flareLight.gameObject.SetActive(true);
                SetCreatureVisible(true);
                return;
            }

            if (_flareIsActive && Time.time >= _flareEndsAt)
            {
                _flareIsActive = false;
                _nextFlareAt = Time.time + FlareInterval;
                if (flareLight != null)
                    flareLight.gameObject.SetActive(false);
                SetCreatureVisible(false);
            }
        }

        private void MoveCreatureToFirstRafter()
        {
            if (mimic == null || rafterWaypoints == null || rafterWaypoints.Length == 0 || rafterWaypoints[0] == null)
                return;

            _rafterWaypointIndex = 0;
            mimic.transform.position = rafterWaypoints[0].position;
        }

        private void SetCreatureVisible(bool visible)
        {
            if (mimic == null)
                return;

            foreach (var renderer in mimic.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = visible;
        }

        private void SetDoorPose(Transform left, Transform right)
        {
            if (left == null || right == null)
                return;

            if (!Application.isPlaying)
            {
                left.localRotation = Quaternion.Euler(0f, -92f, 0f);
                right.localRotation = Quaternion.Euler(0f, 92f, 0f);
                return;
            }

            StartCoroutine(OpenDoorLeaves(left, right));
        }

        private System.Collections.IEnumerator OpenDoorLeaves(Transform left, Transform right)
        {
            var startLeft = left.localRotation;
            var startRight = right.localRotation;
            var endLeft = Quaternion.Euler(0f, -92f, 0f);
            var endRight = Quaternion.Euler(0f, 92f, 0f);
            const float duration = 1.1f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                left.localRotation = Quaternion.Slerp(startLeft, endLeft, t);
                right.localRotation = Quaternion.Slerp(startRight, endRight, t);
                yield return null;
            }

            left.localRotation = endLeft;
            right.localRotation = endRight;
        }

        private void UpdateEndingBeat()
        {
            var elapsed = Time.time - _endingStartedAt;
            if (player != null && elapsed < EndingStepDuration)
            {
                var t = Mathf.SmoothStep(0f, 1f, elapsed / EndingStepDuration);
                player.transform.position = Vector3.Lerp(_endingStepStart, _endingStepTarget, t);
            }

            if (_endingBellPlayed || elapsed < EndingHardCutAt)
                return;

            _endingBellPlayed = true;
            var bell = GameObject.Find("Church — bell");
            var bellPosition = bell != null
                ? bell.transform.position
                : forestThresholdPoint != null ? forestThresholdPoint.transform.position : transform.position;
            GetAudioCues().PlayChurchBell(bellPosition);
        }

        private BarnScreenplayAudio GetAudioCues()
        {
            if (audioCues == null)
                audioCues = GetComponent<BarnScreenplayAudio>();
            if (audioCues == null)
                audioCues = gameObject.AddComponent<BarnScreenplayAudio>();
            return audioCues;
        }

        private void ShowMessage(string message, float duration)
        {
            _message = message;
            _messageUntil = Time.time + duration;
        }

        private void OnGUI()
        {
            if (CanDefendAgainstMimic)
            {
                var defenseStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = HudTextScale.FontSize(28),
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.78f, 0.46f) }
                };
                var cueHeight = HudTextScale.Pixels(58f);
                GUI.Label(new Rect(0f, Screen.height * 0.65f, Screen.width, cueHeight), "[E] TENTAR APARAR COM A FACA", defenseStyle);
            }

            if (_stage == Stage.Ending)
            {
                var elapsed = Time.time - _endingStartedAt;
                GUI.color = new Color(0f, 0f, 0f, elapsed >= EndingHardCutAt ? 1f : 0f);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.blackTexture);
                GUI.color = Color.white;
                if (elapsed >= EndingTitleAt)
                {
                    var titleStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = HudTextScale.FontSize(48),
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.9f, 0.84f, 0.71f) }
                    };
                    GUI.Label(new Rect(0f, Screen.height * 0.43f, Screen.width, HudTextScale.Pixels(72f)), "FORGOTTEN TRAIL", titleStyle);
                    titleStyle.fontSize = HudTextScale.FontSize(24);
                    GUI.Label(new Rect(0f, Screen.height * 0.56f, Screen.width, HudTextScale.Pixels(40f)), "FIM DA DEMO", titleStyle);
                }
                return;
            }

            if (string.IsNullOrEmpty(_message) || Time.time >= _messageUntil)
                return;

            var scale = HudTextScale.Factor;
            var panelWidth = Mathf.Min(Screen.width * 0.84f, HudTextScale.Pixels(1200f));
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = HudTextScale.FontSize(24),
                normal = { textColor = new Color(0.93f, 0.87f, 0.76f) }
            };
            var contentHeight = style.CalcHeight(new GUIContent(_message), panelWidth - 48f * scale);
            var panelHeight = Mathf.Clamp(contentHeight + 36f * scale, 96f * scale, Screen.height * 0.45f);
            var panel = new Rect((Screen.width - panelWidth) * 0.5f, Screen.height - panelHeight - 28f * scale, panelWidth, panelHeight);
            GUI.color = new Color(0.055f, 0.06f, 0.075f, 0.92f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f * scale, panel.y + 16f * scale, panel.width - 48f * scale, panel.height - 32f * scale), _message, style);
        }
    }
}
