using System.IO;
using ForgottenTrail.Gameplay.Alley;
using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Barn;
using ForgottenTrail.Gameplay.Combat;
using ForgottenTrail.Gameplay.Church;
using ForgottenTrail.Gameplay.Enemies;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Lantern;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.Saloon;
using ForgottenTrail.Gameplay.SheriffOffice;
using ForgottenTrail.Gameplay.World;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Editor
{
    public static class AshCreekSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AshCreekApproach.unity";
        private const string MaterialsPath = "Assets/_Project/Materials";
        private const float MainStreetWidth = 12f;
        private const float MainStreetThickness = 0.08f;
        private const float MainStreetSurfaceY = 0.055f;
        private const float MainStreetWestEdge = -MainStreetWidth * 0.5f;
        private const float GroundLightmapScaleInLightmap = 0.25f;
        private const float MainStreetLightmapScaleInLightmap = 0.5f;
        private const string EntranceForestFrameName = "Entrance approach — forest frame";
        private const string MapOrientationMarkerName = "Map layout orientation applied";
        private const string LegacyPrototypeTownLayoutMarkerName = "Godot prototype town composition applied";
        private const string PreviousPrototypeTownLayoutMarkerName = "Godot prototype town composition v2 applied";
        private const string PrototypeTownLayoutMarkerName = "Ash Creek map composition v3 applied";
        private static readonly Vector3 SaloonLayoutOffset = new Vector3(-3f, 0f, 9f);
        private static readonly Vector3 SheriffsOfficeLayoutOffset = new Vector3(6f, 0f, 59f);
        private static readonly Vector3 ChurchLayoutOffset = new Vector3(-14f, 0f, 16f);
        private static readonly Vector3 BlacksmithAlleyLayoutOffset = new Vector3(6f, 0f, 42f);
        private static readonly Vector3 BarnLayoutOffset = new Vector3(-25f, 0f, 24f);
        private static readonly Vector3 ForestLayoutOffset = new Vector3(-25f, 0f, 31f);
        private const string CarmenNoteText = "Carmen jurou que viu o Diabo descendo pela estrada da mina. Eu disse a ela que era apenas a febre de Luke esperando o Juízo Final. Mas quando os sussurros começaram na janela, Carmen trancou as portas e me deixou para trás com os doentes. Todos aqui descarregam seu medo no próximo e fogem para o celeiro. Se você está lendo isso, o peso agora é seu.";
        private const string RenderingPath = "Assets/_Project/Rendering";

        [MenuItem("Forgotten Trail/Build Ash Creek Graybox")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialsPath);
            Directory.CreateDirectory(RenderingPath);

            var materials = CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AshCreekApproach";
            ConfigureEnvironment();
            BuildGround(materials);
            BuildStreetAndGate(materials);
            var player = BuildPlayer();
            BuildTown(materials);
            BuildTrees(materials);
            BuildLocalMist();
            BuildLukeAndArrivalTrail(materials, player);
            BuildEnemy(materials);
            BuildVolume();
            ApplyPrototypeTownComposition(scene);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Ash Creek graybox scene created at " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Update Saloon Evidence")]
        public static void UpdateSaloonEvidence()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mud = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Clue_-_boot_marks_in_mud.mat");
            var blood = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Clue_-_dried_blood.mat");
            if (mud == null || blood == null)
                throw new System.InvalidOperationException("Saloon clue materials are missing. Build Ash Creek Graybox first.");

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Clue — blood trail by well"
                    || rootObject.name == "Clue — dragged boot print"
                    || rootObject.name == "Saloon — dried blood stain")
                    Object.DestroyImmediate(rootObject);
            }

            var materials = new MaterialSet { Mud = mud, Blood = blood };
            BuildBootTrailByWell(materials);
            BuildSaloonDriedBloodStains(materials);

            scene = SceneManager.GetActiveScene();
            var knifeClue = FindClueById(scene, "saloon.knife");
            var noteClue = FindClueById(scene, SaloonApparitionState.NoteInteractionId);
            if (knifeClue == null || noteClue == null)
                throw new System.InvalidOperationException("The saloon note or knife clue was not found in the scene.");
            ConfigureSaloonClues(noteClue, knifeClue);
            ApplyPrototypeTownComposition(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Saloon evidence updated in " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Update Saloon Investigation")]
        public static void UpdateSaloonInvestigation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var materials = CreateMaterials();
            var knife = FindClueById(scene, "saloon.knife");
            var note = FindClueById(scene, SaloonApparitionState.NoteInteractionId);
            if (knife == null || note == null)
                throw new System.InvalidOperationException("The existing saloon note or knife clue was not found.");

            var playerObject = GameObject.Find("Player — Investigator");
            var cameraObject = playerObject != null ? playerObject.transform.Find("First person camera") : null;
            if (cameraObject == null)
                throw new System.InvalidOperationException("The existing player camera was not found.");
            if (cameraObject.GetComponent<AudioListener>() == null)
                cameraObject.gameObject.AddComponent<AudioListener>();

            ConfigureSaloonClues(note, knife);
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Saloon — dried blood stain"
                    || rootObject.name == "Clue — overturned furniture"
                    || rootObject.name == "Clue — warning on wall"
                    || rootObject.name == "Saloon — window apparition"
                    || rootObject.name == "Saloon — double doors"
                    || rootObject.name == "Saloon — investigation progress"
                    || rootObject.name == "Saloon — apparition sequence"
                    || rootObject.name.StartsWith("Clue — saloon footprint", System.StringComparison.Ordinal))
                    Object.DestroyImmediate(rootObject);
            }

            BuildSaloonDriedBloodStains(materials);
            BuildSaloonInvestigationSetpiece(materials);
            var player = GameObject.Find("Player — Investigator");
            var interactor = player != null ? player.GetComponent<PlayerInteractor>() : null;
            var knifeInventory = player != null ? player.GetComponent<CombatKnifeInventory>() : null;
            var keyInventory = player != null ? player.GetComponent<BarnKeyInventory>() : null;
            if (interactor != null && knifeInventory != null && keyInventory != null)
            {
                var grants = player.GetComponent<ScreenplayItemGrantTracker>() ?? player.AddComponent<ScreenplayItemGrantTracker>();
                grants.Configure(interactor, knifeInventory, keyInventory, knife.gameObject, null);
            }
            ConfigureSaloonWindows(scene, materials.SaloonWindow);
            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("The saloon investigation, apparition, and exit beat were updated in " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Update Church Investigation")]
        public static void UpdateChurchInvestigation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var materials = CreateMaterials();
            var playerObject = GameObject.Find("Player — Investigator");
            var playerInteractor = playerObject != null ? playerObject.GetComponent<PlayerInteractor>() : null;
            var playerController = playerObject != null ? playerObject.GetComponent<FirstPersonController>() : null;
            var badgeInventory = playerObject != null ? playerObject.GetComponent<SheriffBadgeInventory>() : null;
            var knifeInventory = playerObject != null ? playerObject.GetComponent<CombatKnifeInventory>() : null;
            var keyInventory = playerObject != null ? playerObject.GetComponent<BarnKeyInventory>() : null;
            var journal = playerObject != null ? playerObject.GetComponent<PlayerJournalComponent>() : null;
            var progression = Object.FindFirstObjectByType<DemoProgressionComponent>();
            if (playerInteractor == null || playerController == null || journal == null || progression == null)
                throw new System.InvalidOperationException("The Ash Creek scene is missing its investigator, journal, or progression component.");
            if (badgeInventory == null)
                badgeInventory = playerObject.AddComponent<SheriffBadgeInventory>();
            if (knifeInventory == null) knifeInventory = playerObject.AddComponent<CombatKnifeInventory>();
            if (keyInventory == null) keyInventory = playerObject.AddComponent<BarnKeyInventory>();
            if (playerObject.GetComponent<SavingShotInventory>() == null) playerObject.AddComponent<SavingShotInventory>();
            if (playerObject.GetComponent<ScreenplayItemGrantTracker>() == null) playerObject.AddComponent<ScreenplayItemGrantTracker>();

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Church investigation — setpiece"
                    || rootObject.name == "Blacksmith alley — Chester and Jack's trail"
                    || rootObject.name == "Blacksmith alley — stealth route")
                    Object.DestroyImmediate(rootObject);
            }

            var setpiece = new GameObject("Church investigation — setpiece");
            BuildChurchInvestigationSetpiece(setpiece.transform, materials, playerController, progression, out var falseElias, out var returnReveal);
            BuildChesterAndJackTrail(materials);
            var alleyRoot = new GameObject("Blacksmith alley — stealth route");
            var alleyParts = BuildBlacksmithStealthRoute(alleyRoot.transform, materials, playerController);
            var tracker = setpiece.AddComponent<ChurchInvestigationTracker>();
            var badge = falseElias.transform.Find("Church — Elias's sheriff badge");
            tracker.Configure(playerInteractor, progression, falseElias, returnReveal, badgeInventory, badge != null ? badge.gameObject : null);

            var alleyTracker = alleyRoot.AddComponent<ChesterJackRouteTracker>();
            alleyTracker.Configure(playerInteractor, progression, journal, alleyParts.Jack, alleyParts.Chester);
            alleyParts.Gate.Configure(alleyTracker, alleyParts.GateLeaf, playerController);
            alleyParts.Chester.Configure(alleyTracker, alleyParts.LivingChester, alleyParts.FallenChester);
            alleyParts.Jack.Configure(alleyTracker, playerController, alleyParts.JackAudio);

            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("The screenplay's church mission, return revelation, and Chester route were added to " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Update Sheriff's Office Investigation")]
        public static void UpdateSheriffOfficeInvestigation()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var materials = CreateMaterials();
            var playerObject = GameObject.Find("Player — Investigator");
            var playerInteractor = playerObject != null ? playerObject.GetComponent<PlayerInteractor>() : null;
            var playerController = playerObject != null ? playerObject.GetComponent<FirstPersonController>() : null;
            var badgeInventory = playerObject != null ? playerObject.GetComponent<SheriffBadgeInventory>() : null;
            var knifeInventory = playerObject != null ? playerObject.GetComponent<CombatKnifeInventory>() : null;
            var keyInventory = playerObject != null ? playerObject.GetComponent<BarnKeyInventory>() : null;
            var journal = playerObject != null ? playerObject.GetComponent<PlayerJournalComponent>() : null;
            var lantern = Object.FindFirstObjectByType<HandLanternPickup>();
            var progression = Object.FindFirstObjectByType<DemoProgressionComponent>();
            var jackCompanion = Object.FindFirstObjectByType<JackRescueInteractable>();
            if (playerObject == null || playerInteractor == null || playerController == null || journal == null || lantern == null || progression == null || jackCompanion == null)
                throw new System.InvalidOperationException("The Ash Creek scene is missing its investigator, lantern, journal, or progression component.");
            if (badgeInventory == null)
                badgeInventory = playerObject.AddComponent<SheriffBadgeInventory>();
            if (knifeInventory == null) knifeInventory = playerObject.AddComponent<CombatKnifeInventory>();
            if (keyInventory == null) keyInventory = playerObject.AddComponent<BarnKeyInventory>();
            if (playerObject.GetComponent<SavingShotInventory>() == null) playerObject.AddComponent<SavingShotInventory>();
            if (playerObject.GetComponent<ScreenplayItemGrantTracker>() == null) playerObject.AddComponent<ScreenplayItemGrantTracker>();

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Sheriff office — investigation"
                    || rootObject.name == "Sheriff office — rear approach")
                    Object.DestroyImmediate(rootObject);
            }

            BuildSheriffRearEntrance(materials);

            var officeRoot = new GameObject("Sheriff office — investigation");
            BuildSheriffOfficeInterior(officeRoot.transform, materials, playerInteractor, playerController, lantern, progression, jackCompanion, badgeInventory);

            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("The sheriff's cells, evidence room, second floor, Red Book, and escape sequence were added to " + ScenePath);
        }

        private static void BuildSheriffRearEntrance(MaterialSet materials)
        {
            var generatedNames = new System.Collections.Generic.HashSet<string>
            {
                "Sheriff office — rear wall",
                "Sheriff office — rear wall left",
                "Sheriff office — rear wall right",
                "Sheriff office — rear door lintel",
                "Sheriff office — rear entrance trim left",
                "Sheriff office — rear entrance trim right",
                "Sheriff office — rear entrance step",
                "Sheriff office — rear door standing open",
                "Sign — ENTRADA DOS FUNDOS"
            };
            foreach (var rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (generatedNames.Contains(rootObject.name))
                    Object.DestroyImmediate(rootObject);
            }

            const float centerX = 17f;
            const float wallHeight = 6f;
            const float wallThickness = 0.32f;
            const float doorwayWidth = 2.1f;
            const float rearWallZ = 44.84f;
            const float wallCenterY = 3.24f;
            const float wallSegmentWidth = (12f - doorwayWidth) * 0.5f;

            var leftWall = Box("Sheriff office — rear wall left",
                new Vector3(centerX - (doorwayWidth + wallSegmentWidth) * 0.5f, wallCenterY, rearWallZ),
                new Vector3(wallSegmentWidth, wallHeight, wallThickness), materials.WoodLight);
            var rightWall = Box("Sheriff office — rear wall right",
                new Vector3(centerX + (doorwayWidth + wallSegmentWidth) * 0.5f, wallCenterY, rearWallZ),
                new Vector3(wallSegmentWidth, wallHeight, wallThickness), materials.WoodLight);
            var lintel = Box("Sheriff office — rear door lintel",
                new Vector3(centerX, wallHeight - 0.1f, rearWallZ),
                new Vector3(doorwayWidth, 0.4f, wallThickness), materials.WoodLight);
            MarkStatic(leftWall);
            MarkStatic(rightWall);
            MarkStatic(lintel);

            foreach (var side in new[] { -1f, 1f })
            {
                var trim = Box(side < 0f ? "Sheriff office — rear entrance trim left" : "Sheriff office — rear entrance trim right",
                    new Vector3(centerX + side * doorwayWidth * 0.5f, wallCenterY, rearWallZ - 0.03f),
                    new Vector3(0.16f, wallHeight, 0.19f), materials.Wood);
                MarkStatic(trim);
            }

            var step = Box("Sheriff office — rear entrance step", new Vector3(centerX, 0.12f, 45.34f), new Vector3(2.7f, 0.24f, 1.0f), materials.Wood);
            MarkStatic(step);
            var openDoor = Box("Sheriff office — rear door standing open", new Vector3(17.54f, 1.3f, 44.25f), new Vector3(0.12f, 2.2f, 1.18f), materials.Wood);
            MarkStatic(openDoor);
            BuildSheriffRearApproach(materials);
        }

        private static void BuildSheriffRearApproach(MaterialSet materials)
        {
            var route = new GameObject("Sheriff office — rear approach");
            var pawPositions = new[]
            {
                new Vector3(10.1f, 0.055f, 35.55f),
                new Vector3(9.92f, 0.055f, 36.9f),
                new Vector3(9.88f, 0.055f, 38.35f),
                new Vector3(9.88f, 0.055f, 39.8f),
                new Vector3(9.9f, 0.055f, 41.25f),
                new Vector3(10.05f, 0.055f, 42.65f),
                new Vector3(10.55f, 0.055f, 43.95f),
                new Vector3(11.25f, 0.055f, 45.25f),
                new Vector3(12.65f, 0.055f, 45.42f),
                new Vector3(14.05f, 0.055f, 45.42f),
                new Vector3(15.45f, 0.055f, 45.42f),
                new Vector3(16.65f, 0.055f, 45.42f)
            };

            for (var i = 0; i < pawPositions.Length; i++)
            {
                var paw = Sphere("Jack's paw print — rear trail " + (i + 1), pawPositions[i], new Vector3(0.32f, 0.055f, 0.22f), materials.Mud);
                paw.transform.rotation = Quaternion.Euler(0f, i < 7 ? 0f : 90f, 0f);
                paw.transform.SetParent(route.transform, true);
                Object.DestroyImmediate(paw.GetComponent<Collider>());
            }

            var bootTrailClue = GameObject.Find("Alley trail — torn red collar");
            var routeClue = bootTrailClue != null ? bootTrailClue.GetComponent<InteractableClue>() : null;
            if (routeClue != null)
                routeClue.Configure(
                    ChesterJackRouteState.SheriffOfficeExitInteractionId,
                    "Seguir as pegadas de Jack até os fundos da delegacia",
                    "Pegadas de botas e patas contornam a parede oeste e chegam à porta aberta dos fundos. Jack fareja o rastro de Layla lá dentro.",
                    2.8f,
                    false,
                    DemoObjective.FollowChesterAndJack,
                    string.Empty);
        }

        [MenuItem("Forgotten Trail/Update Gate Arrival")]
        public static void UpdateGateArrival()
        {
            Directory.CreateDirectory(MaterialsPath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var materials = CreateMaterials();
            var playerObject = GameObject.Find("Player — Investigator") ?? GameObject.Find("Player — Luke");
            var cameraObject = playerObject != null ? playerObject.transform.Find("First person camera") : null;
            var progression = Object.FindFirstObjectByType<DemoProgressionComponent>();
            if (playerObject == null || cameraObject == null || progression == null)
                throw new System.InvalidOperationException("The existing Ash Creek scene is missing its player, camera, or progression component.");
            playerObject.name = "Player — Investigator";
            if (playerObject.GetComponent<SheriffBadgeInventory>() == null)
                playerObject.AddComponent<SheriffBadgeInventory>();
            if (playerObject.GetComponent<CombatKnifeInventory>() == null)
                playerObject.AddComponent<CombatKnifeInventory>();
            if (playerObject.GetComponent<BarnKeyInventory>() == null)
                playerObject.AddComponent<BarnKeyInventory>();
            if (playerObject.GetComponent<SavingShotInventory>() == null)
                playerObject.AddComponent<SavingShotInventory>();
            if (playerObject.GetComponent<ScreenplayOpeningLine>() == null)
                playerObject.AddComponent<ScreenplayOpeningLine>();
            var openingLine = playerObject.GetComponent<ScreenplayOpeningLine>();
            var firstPersonController = playerObject.GetComponent<FirstPersonController>();
            if (firstPersonController == null)
                firstPersonController = playerObject.AddComponent<FirstPersonController>();
            firstPersonController.ConfigureViewCamera(cameraObject.GetComponent<Camera>());

            var journal = playerObject.GetComponent<PlayerJournalComponent>();
            if (journal == null)
                journal = playerObject.AddComponent<PlayerJournalComponent>();
            journal.Configure(string.Empty);

            var handAnchor = cameraObject.Find("Lantern hand anchor");
            if (handAnchor == null)
                handAnchor = new GameObject("Lantern hand anchor").transform;
            handAnchor.SetParent(cameraObject, false);
            handAnchor.localPosition = new Vector3(0.34f, -0.34f, 0.52f);
            handAnchor.localRotation = Quaternion.Euler(22f, 0f, 0f);
            handAnchor.localScale = Vector3.one * 0.72f;

            var camera = cameraObject.GetComponent<Camera>();
            var interactor = playerObject.GetComponent<PlayerInteractor>();
            if (interactor == null)
                interactor = playerObject.AddComponent<PlayerInteractor>();
            interactor.Configure(camera, progression, journal);
            var grants = playerObject.GetComponent<ScreenplayItemGrantTracker>() ?? playerObject.AddComponent<ScreenplayItemGrantTracker>();
            grants.Configure(
                interactor,
                playerObject.GetComponent<CombatKnifeInventory>(),
                playerObject.GetComponent<BarnKeyInventory>(),
                FindClueById(scene, "saloon.knife")?.gameObject,
                null);
            EditorUtility.SetDirty(journal);
            EditorUtility.SetDirty(interactor);

            var rootsToReplace = new System.Collections.Generic.HashSet<string>
            {
                "Luke — wounded at the gate",
                "Luke — blood on the road",
                "Luke's oil lantern",
                "Clue — bootprints from gate to well"
            };
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootsToReplace.Contains(rootObject.name))
                    Object.DestroyImmediate(rootObject);
            }

            BuildLukeAndArrivalTrail(materials, new PlayerRig
            {
                LanternHandAnchor = handAnchor,
                PlayerRoot = playerObject.transform,
                Controller = firstPersonController,
                OpeningLine = openingLine
            });

            var wellTrailClue = FindClueById(scene, "saloon.boot-trail");
            if (wellTrailClue == null)
                throw new System.InvalidOperationException("The existing well-to-saloon boot trail was not found.");
            wellTrailClue.Configure("saloon.boot-trail", "Examinar as marcas de botas", "Marcas de botas de garimpeiros foram arrastadas do poço pela rua principal, na direção do saloon.", 2.8f, true, DemoObjective.FollowBootprintsToSaloon, string.Empty);
            EditorUtility.SetDirty(wellTrailClue);

            var saloonNoteClue = FindClueById(scene, "saloon.torn-note");
            if (saloonNoteClue == null)
                throw new System.InvalidOperationException("The existing saloon note clue was not found.");
            saloonNoteClue.Configure("saloon.torn-note", "Ler a anotação", CarmenNoteText, 2.8f, false, DemoObjective.InvestigateSaloonClues, string.Empty);
            EditorUtility.SetDirty(saloonNoteClue);

            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Luke, the portable lantern, the arrival trail, and journal progression were added to " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Update Barn and Forest Climax")]
        public static void UpdateBarnEncounter()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ConfigureEnvironment();
            BuildVolume();
            ApplyExistingSceneReadability(scene);
            var materials = CreateMaterials();
            var playerObject = GameObject.Find("Player — Investigator");
            var player = playerObject != null ? playerObject.GetComponent<FirstPersonController>() : null;
            var interactor = playerObject != null ? playerObject.GetComponent<PlayerInteractor>() : null;
            var progression = Object.FindFirstObjectByType<DemoProgressionComponent>();
            var lantern = Object.FindFirstObjectByType<HandLanternPickup>();
            var jack = Object.FindFirstObjectByType<JackRescueInteractable>();
            if (playerObject == null || player == null || interactor == null || progression == null || lantern == null || jack == null)
                throw new System.InvalidOperationException("The Ash Creek scene is missing its player, lantern, Jack, or progression component.");

            var knife = playerObject.GetComponent<CombatKnifeInventory>() ?? playerObject.AddComponent<CombatKnifeInventory>();
            var barnKey = playerObject.GetComponent<BarnKeyInventory>() ?? playerObject.AddComponent<BarnKeyInventory>();
            var revolver = playerObject.GetComponent<SavingShotInventory>() ?? playerObject.AddComponent<SavingShotInventory>();
            var grants = playerObject.GetComponent<ScreenplayItemGrantTracker>() ?? playerObject.AddComponent<ScreenplayItemGrantTracker>();

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name.StartsWith("Barn —", System.StringComparison.Ordinal)
                    || rootObject.name == "Sign — CELEIRO"
                    || rootObject.name == "Barn encounter — Act I climax")
                    Object.DestroyImmediate(rootObject);
            }

            BuildWesternBuilding("Barn", new Vector3(0f, 0f, 104f), 20f, 9f, 20f, materials.Barn, materials.Roof, "CELEIRO", materials);
            var barnRoot = new GameObject("Barn encounter — Act I climax");
            var firearmModel = BuildFirstPersonRevolver(playerObject.transform.Find("First person camera"), materials);
            BuildBarnPlayableSetpiece(
                barnRoot.transform,
                materials,
                progression,
                player,
                interactor,
                lantern,
                jack,
                barnKey,
                knife,
                revolver,
                firearmModel,
                out var keyhole,
                out var remainsPoint,
                out var rafterPoint,
                out var gideonPoint,
                out var finalShotPoint,
                out var forestPoint);

            var knifeClue = FindClueById(scene, "saloon.knife");
            var hales = Object.FindObjectsByType<SheriffHaleEncounterInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var hale = hales.Length > 0 ? hales[0] : null;
            var key = hale != null ? hale.transform.Find("Hale — barn key") : null;
            if (hale != null && key == null)
                key = BuildHaleBarnKey(hale.transform, materials);
            grants.Configure(interactor, knife, barnKey, knifeClue != null ? knifeClue.gameObject : null, key != null ? key.gameObject : null);
            var tracker = barnRoot.GetComponent<BarnEncounterSequence>();
            tracker.Configure(
                progression,
                player,
                interactor,
                lantern,
                jack,
                barnKey,
                knife,
                revolver,
                barnRoot.transform.Find("Barn interior — straw, remains, and rafters").gameObject,
                barnRoot.transform.Find("Barn entrance — left leaf"),
                barnRoot.transform.Find("Barn entrance — right leaf"),
                barnRoot.transform.Find("Barn interior — straw, remains, and rafters/Remains pile").gameObject,
                barnRoot.transform.Find("Mimic — rafter stalker").gameObject,
                barnRoot.transform.Find("Gideon — breach entrance").gameObject,
                barnRoot.transform.Find("Gideon — broken side boards").gameObject,
                barnRoot.transform.Find("Barn forest — left door"),
                barnRoot.transform.Find("Barn forest — right door"),
                barnRoot.transform.Find("Floresta dos Suspiros — black trees").gameObject,
                keyhole,
                remainsPoint,
                rafterPoint,
                gideonPoint,
                finalShotPoint,
                forestPoint,
                barnRoot.transform.Find("Gideon's flare cue"),
                firearmModel,
                FindRafterWaypoints(barnRoot.transform),
                barnRoot.transform.Find("Mimic — center of the barn"),
                barnRoot.transform.Find("Jack — aftermath position"));

            AlignSceneToMap(scene);
            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            BakeStaticLighting(scene);
            Debug.Log("The screenplay's playable barn fight and Forest of Sighs ending were added to " + ScenePath);
        }

        [MenuItem("Forgotten Trail/Improve Demo Readability")]
        public static void ImproveDemoReadability()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyExistingSceneReadability(scene);
            MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f);

            var volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null || volume.sharedProfile == null
                || !volume.sharedProfile.TryGet(out ColorAdjustments color))
                throw new System.InvalidOperationException("Ash Creek's global color adjustment profile was not found.");
            color.postExposure.Override(0.35f);
            EditorUtility.SetDirty(volume.sharedProfile);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("On-screen text, Luke's lantern and entrance visibility were improved.");
        }

        [MenuItem("Forgotten Trail/Update Entrance Composition")]
        public static void UpdateEntranceComposition()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var materials = LoadEntranceForestMaterials();
            var player = FindRoot(scene, "Player — Investigator");
            var mapOriented = player != null && player.transform.Find(MapOrientationMarkerName) != null;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == EntranceForestFrameName)
                    Object.DestroyImmediate(root);
            }

            BuildEntranceForestFrame(materials, mapOriented);
            ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("The entrance now has layered forest silhouettes framing the trail while keeping the road and Luke's sightline open.");
        }

        [MenuItem("Forgotten Trail/Recompose Town From Godot Prototype")]
        public static void RecomposeTownFromGodotPrototype()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var mud = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Clue_-_boot_marks_in_mud.mat");
            if (mud == null)
                throw new System.InvalidOperationException("The bootprint material was not found. Build Ash Creek Graybox first.");

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == "Clue — dragged boot print")
                    Object.DestroyImmediate(rootObject);
            }

            BuildBootTrailByWell(new MaterialSet { Mud = mud });
            var movedObjects = ApplyPrototypeTownComposition(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            BakeStaticLighting(scene);
            Debug.Log("The town was recomposed around the open well plaza; moved " + movedObjects + " scene groups, including the Chester route.");
        }

        private static MaterialSet LoadEntranceForestMaterials()
        {
            var materials = new MaterialSet
            {
                Wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Wood_-_weathered_walnut.mat"),
                Foliage = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Foliage_-_night_pine.mat")
            };

            if (materials.Wood == null || materials.Foliage == null)
                throw new System.InvalidOperationException("Ash Creek's entrance forest materials were not found.");

            return materials;
        }

        [MenuItem("Forgotten Trail/Restore Amber Lamp Glow")]
        public static void RestoreAmberLampGlow()
        {
            MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f);
            AssetDatabase.SaveAssets();
            Debug.Log("Ash Creek's amber emissive material was restored.");
        }

        [MenuItem("Forgotten Trail/Bake Ash Creek Static Lighting")]
        public static void BakeAshCreekStaticLighting()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BakeStaticLighting(scene);
        }

        private static void BakeStaticLighting(Scene scene)
        {
            EditorSceneManager.SetActiveScene(scene);
            ConfigureStaticPracticalLights(scene);
            var lightingSettings = Lightmapping.GetLightingSettingsForScene(scene);
            if (lightingSettings == null)
            {
                lightingSettings = new LightingSettings();
                Lightmapping.SetLightingSettingsForScene(scene, lightingSettings);
            }
            lightingSettings.autoGenerate = false;
            lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            lightingSettings.lightmapResolution = 16f;
            lightingSettings.lightmapMaxSize = 1024;
            lightingSettings.directSampleCount = 32;
            lightingSettings.indirectSampleCount = 128;
            lightingSettings.environmentSampleCount = 64;
            lightingSettings.lightProbeSampleCountMultiplier = 1f;
            lightingSettings.directionalityMode = LightmapsMode.NonDirectional;
            EditorUtility.SetDirty(lightingSettings);
            if (!Lightmapping.Bake())
                throw new System.InvalidOperationException("Unity could not bake Ash Creek's static lighting.");

            MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Ash Creek static practical lighting was baked into the scene.");
        }

        private static void AlignSceneToMap(Scene scene)
        {
            const string markerName = "Map layout orientation applied";
            var mapRotation = Quaternion.Euler(0f, 90f, 0f);
            foreach (var root in scene.GetRootGameObjects())
            {
                var existingMarker = root.transform.Find(markerName);
                if (existingMarker != null)
                {
                    existingMarker.gameObject.hideFlags |= HideFlags.DontSaveInBuild;
                    continue;
                }

                root.transform.SetPositionAndRotation(
                    mapRotation * root.transform.position,
                    mapRotation * root.transform.rotation);
                var marker = new GameObject(markerName)
                {
                    hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild
                };
                marker.transform.SetParent(root.transform, false);
            }
        }

        private static int ApplyPrototypeTownComposition(Scene scene)
        {
            EnsureExtendedRoadRuts(scene);
            AlignSceneToMap(scene);

            var mapRotation = Quaternion.Euler(0f, 90f, 0f);
            ConfigureExtendedTownGround(scene, mapRotation);

            var movedObjects = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                var marker = root.transform.Find(PrototypeTownLayoutMarkerName);
                if (marker != null)
                {
                    marker.gameObject.hideFlags |= HideFlags.DontSaveInBuild;
                    continue;
                }

                if (!TryGetPrototypeLayoutZone(root.name, out var zone))
                    continue;

                var sourceOffset = GetPrototypeLayoutOffset(zone, LayoutVersion.Current);
                var previousMarker = root.transform.Find(PreviousPrototypeTownLayoutMarkerName);
                var legacyMarker = root.transform.Find(LegacyPrototypeTownLayoutMarkerName);
                var previousOffset = previousMarker != null
                    ? GetPrototypeLayoutOffset(zone, LayoutVersion.V2)
                    : legacyMarker != null
                        ? GetPrototypeLayoutOffset(zone, LayoutVersion.V1)
                        : Vector3.zero;

                var worldOffset = mapRotation * (sourceOffset - previousOffset);
                root.transform.position += worldOffset;
                if (previousMarker != null)
                    Object.DestroyImmediate(previousMarker.gameObject);
                if (legacyMarker != null)
                    Object.DestroyImmediate(legacyMarker.gameObject);

                marker = new GameObject(PrototypeTownLayoutMarkerName)
                {
                    hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild
                }.transform;
                marker.SetParent(root.transform, false);
                if (worldOffset.sqrMagnitude > 0.0001f)
                    movedObjects++;
            }

            return movedObjects;
        }

        private enum TownLayoutZone
        {
            Saloon,
            SheriffOffice,
            Church,
            BlacksmithAlley,
            Barn,
            Forest
        }

        private enum LayoutVersion
        {
            V1,
            V2,
            Current
        }

        private static bool TryGetPrototypeLayoutZone(string objectName, out TownLayoutZone zone)
        {
            var comparison = System.StringComparison.OrdinalIgnoreCase;
            if (objectName.StartsWith("Saloon —", comparison)
                || objectName.StartsWith("Clue — saloon", comparison)
                || objectName == "Clue — knife"
                || objectName == "Clue — Carmen and Miss Moses note"
                || objectName == "Clue — overturned furniture"
                || objectName == "Clue — warning on wall")
            {
                zone = TownLayoutZone.Saloon;
                return true;
            }

            if (objectName.StartsWith("Sheriff office —", comparison))
            {
                zone = TownLayoutZone.SheriffOffice;
                return true;
            }

            if (objectName.StartsWith("Church —", comparison)
                || objectName.StartsWith("Church investigation —", comparison))
            {
                zone = TownLayoutZone.Church;
                return true;
            }

            if (objectName.StartsWith("Blacksmith alley —", comparison)
                || objectName == "Blacksmith alley sign"
                || objectName.StartsWith("Watcher — blacksmith alley", comparison)
                || objectName.IndexOf("BECO DO FERREIRO", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                zone = TownLayoutZone.BlacksmithAlley;
                return true;
            }

            if (objectName == "Forest — localized layers")
            {
                zone = TownLayoutZone.Forest;
                return true;
            }

            if (objectName.StartsWith("Barn —", comparison)
                || objectName.StartsWith("Barn encounter —", comparison))
            {
                zone = TownLayoutZone.Barn;
                return true;
            }

            zone = default;
            return false;
        }

        private static Vector3 GetPrototypeLayoutOffset(TownLayoutZone zone, LayoutVersion version)
        {
            var isCurrent = version == LayoutVersion.Current;
            switch (zone)
            {
                case TownLayoutZone.Saloon: return isCurrent ? SaloonLayoutOffset : new Vector3(-5f, 0f, 7f);
                case TownLayoutZone.SheriffOffice: return isCurrent ? SheriffsOfficeLayoutOffset : new Vector3(7f, 0f, 15f);
                case TownLayoutZone.Church: return isCurrent ? ChurchLayoutOffset : new Vector3(-8f, 0f, 15f);
                case TownLayoutZone.BlacksmithAlley: return isCurrent ? BlacksmithAlleyLayoutOffset : new Vector3(4f, 0f, 9f);
                case TownLayoutZone.Barn:
                    return isCurrent ? BarnLayoutOffset
                        : version == LayoutVersion.V1 ? new Vector3(-12f, 0f, 14f)
                        : new Vector3(0f, 0f, 14f);
                case TownLayoutZone.Forest:
                    return isCurrent ? ForestLayoutOffset
                        : version == LayoutVersion.V1 ? new Vector3(-12f, 0f, 14f)
                        : new Vector3(0f, 0f, 14f);
                default: return Vector3.zero;
            }
        }

        private static void ConfigureExtendedTownGround(Scene scene, Quaternion mapRotation)
        {
            var ground = FindRoot(scene, "Ash Creek ground");
            if (ground != null)
            {
                ground.transform.SetPositionAndRotation(
                    mapRotation * new Vector3(0f, -0.3f, 75f),
                    mapRotation);
                ground.transform.localScale = new Vector3(100f, 0.6f, 240f);
                SetLightmapScaleInLightmap(ground, GroundLightmapScaleInLightmap);
            }

            var street = FindRoot(scene, "Main street");
            if (street != null)
            {
                street.transform.SetPositionAndRotation(
                    mapRotation * new Vector3(0f, MainStreetSurfaceY - MainStreetThickness * 0.5f, 63f),
                    mapRotation);
                street.transform.localScale = new Vector3(MainStreetWidth, MainStreetThickness, 120f);
                SetLightmapScaleInLightmap(street, MainStreetLightmapScaleInLightmap);
            }
        }

        private static void SetLightmapScaleInLightmap(GameObject gameObject, float scale)
        {
            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.scaleInLightmap = scale;
        }

        private static void EnsureExtendedRoadRuts(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var rutCount = 0;
            foreach (var root in roots)
            {
                if (root.name == "Road rut")
                    rutCount++;
            }

            if (rutCount == 12)
                return;

            var groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + "/Ground_-_wet_earth.mat");
            if (groundMaterial == null)
                throw new System.InvalidOperationException("Ash Creek's road material was not found. Build the graybox first.");

            foreach (var root in roots)
            {
                if (root.name == "Road rut")
                    Object.DestroyImmediate(root);
            }

            for (var i = 0; i < 12; i++)
            {
                var z = 14f + i * 9.2f;
                var rut = Cylinder("Road rut", new Vector3((i % 2 == 0 ? -1f : 1f) * 2.3f, 0.075f, z), new Vector3(0.18f, 0.012f, 1.1f), groundMaterial);
                rut.transform.rotation = Quaternion.Euler(0f, (i * 23f) % 40f, 0f);
                MarkStatic(rut);
            }
        }

        private static void ConfigureStaticPracticalLights(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    var name = light.gameObject.name;
                    if (!name.StartsWith("Warm practical", System.StringComparison.Ordinal)
                        && !name.StartsWith("Sheriff office — amber desk practical", System.StringComparison.Ordinal)
                        && !name.StartsWith("Sheriff office — lantern above the ledger", System.StringComparison.Ordinal)
                        && !name.StartsWith("Blacksmith — localized amber forge light", System.StringComparison.Ordinal))
                        continue;

                    light.lightmapBakeType = LightmapBakeType.Baked;
                    light.shadows = LightShadows.None;
                    if (name.StartsWith("Warm practical", System.StringComparison.Ordinal))
                    {
                        light.intensity = 0.88f;
                        light.range = 8f;
                    }
                    else
                    {
                        light.intensity = Mathf.Min(light.intensity, 1.5f);
                    }
                    EditorUtility.SetDirty(light);
                }
            }
        }

        private static void ApplyExistingSceneReadability(Scene scene)
        {
            const string currentTextSizeMarker = "Readable text size v3 applied";
            const string previousTextSizeMarker = "Readable text size v2 applied";
            const string legacyTextSizeMarker = "Readable text size applied";
            const string orientationMarker = "Map layout orientation applied";
            var mapRotation = Quaternion.Euler(0f, 90f, 0f);
            var playerRoot = FindRoot(scene, "Player — Investigator");
            var sceneIsMapOriented = playerRoot != null && playerRoot.transform.Find(orientationMarker) != null;

            var lukeRoot = FindRoot(scene, "Luke — wounded at the gate");
            if (lukeRoot != null)
            {
                lukeRoot.transform.position = ScenePosition(new Vector3(-1.35f, 0f, 9.3f));
                var lanternRoot = FindRoot(scene, "Luke's oil lantern");
                if (lanternRoot != null)
                {
                    lanternRoot.transform.position = ScenePosition(new Vector3(-0.25f, 0.48f, 10f));
                    var lamp = lanternRoot.GetComponentInChildren<Light>(true);
                    ConfigureLukeLanternLight(lamp, lukeRoot.transform);
                }

                var bloodRoot = FindRoot(scene, "Luke — blood on the road");
                if (bloodRoot != null)
                    bloodRoot.transform.position = ScenePosition(new Vector3(-1.1f, MainStreetSurfaceY + 0.008f, 9.55f));
            }

            Vector3 ScenePosition(Vector3 unrotatedPosition)
            {
                return sceneIsMapOriented ? mapRotation * unrotatedPosition : unrotatedPosition;
            }

            var obsoleteSigns = new System.Collections.Generic.HashSet<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
                {
                    if (text.text.Contains("ENTRADA DOS FUNDOS"))
                        obsoleteSigns.Add(text.transform.root.gameObject);
                }
            }

            foreach (var sign in obsoleteSigns)
                Object.DestroyImmediate(sign);

            foreach (var root in scene.GetRootGameObjects())
            {
                var warningRoot = root.name == "Clue — warning on wall";
                foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
                {
                    if (warningRoot)
                    {
                        text.characterSize = 0.042f;
                        text.fontSize = 48;
                    }
                    else if (root.name.StartsWith("Sign — ", System.StringComparison.Ordinal)
                        && text.transform.Find(currentTextSizeMarker) == null)
                    {
                        var previousMarker = text.transform.Find(previousTextSizeMarker);
                        var legacyMarker = text.transform.Find(legacyTextSizeMarker);
                        if (previousMarker != null)
                            text.characterSize = text.characterSize / 0.8f * 0.62f;
                        else if (legacyMarker == null)
                            text.characterSize *= 0.62f;
                        text.fontSize = 48;
                        if (previousMarker != null)
                            Object.DestroyImmediate(previousMarker.gameObject);
                        if (legacyMarker != null)
                            Object.DestroyImmediate(legacyMarker.gameObject);
                        var marker = new GameObject(currentTextSizeMarker)
                        {
                            hideFlags = HideFlags.HideInHierarchy
                        };
                        marker.transform.SetParent(text.transform, false);
                    }
                }

                if (warningRoot)
                {
                    var collider = root.GetComponent<BoxCollider>();
                    if (collider != null)
                        collider.size = new Vector3(0.12f, 0.74f, 0.94f);
                }
            }
        }

        private static GameObject BuildFirstPersonRevolver(Transform camera, MaterialSet materials)
        {
            if (camera == null)
                throw new System.InvalidOperationException("The investigator's camera is missing.");

            for (var i = camera.childCount - 1; i >= 0; i--)
            {
                var child = camera.GetChild(i);
                if (child.name == "Gideon's .38 — first-person model")
                    Object.DestroyImmediate(child.gameObject);
            }

            var pistol = new GameObject("Gideon's .38 — first-person model");
            pistol.transform.SetParent(camera, false);
            pistol.transform.localPosition = new Vector3(0.37f, -0.34f, 0.65f);
            pistol.transform.localRotation = Quaternion.Euler(2f, 5f, -4f);
            PrimitiveChild(pistol.transform, ".38 — walnut grip", PrimitiveType.Cube, new Vector3(0f, -0.16f, 0f), new Vector3(0.12f, 0.27f, 0.16f), Quaternion.Euler(-18f, 0f, -8f), materials.Wood);
            PrimitiveChild(pistol.transform, ".38 — iron frame", PrimitiveType.Cube, new Vector3(0f, -0.025f, 0.02f), new Vector3(0.15f, 0.13f, 0.28f), Quaternion.identity, materials.Metal);
            PrimitiveChild(pistol.transform, ".38 — cylinder", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0.1f), new Vector3(0.09f, 0.13f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.Metal);
            PrimitiveChild(pistol.transform, ".38 — short barrel", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.29f), new Vector3(0.07f, 0.075f, 0.26f), Quaternion.identity, materials.Metal);
            PrimitiveChild(pistol.transform, ".38 — front sight", PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.4f), new Vector3(0.025f, 0.035f, 0.045f), Quaternion.identity, materials.SheriffGold);
            pistol.SetActive(false);
            return pistol;
        }

        private static void BuildBarnPlayableSetpiece(
            Transform root,
            MaterialSet materials,
            DemoProgressionComponent progression,
            FirstPersonController player,
            PlayerInteractor interactor,
            HandLanternPickup lantern,
            JackRescueInteractable jack,
            BarnKeyInventory barnKey,
            CombatKnifeInventory knife,
            SavingShotInventory revolver,
            GameObject revolverVisual,
            out BarnInteractionPoint doorPoint,
            out BarnInteractionPoint remainsPoint,
            out BarnInteractionPoint rafterPoint,
            out BarnInteractionPoint gideonPoint,
            out BarnInteractionPoint finalShotPoint,
            out BarnInteractionPoint forestPoint)
        {
            var sequence = root.gameObject.AddComponent<BarnEncounterSequence>();
            root.gameObject.AddComponent<BarnScreenplayAudio>();
            var origin = new Vector3(0f, 0f, 104f);
            var frontZ = origin.z - 10f + 0.16f;
            var backZ = origin.z + 10f - 0.16f;

            var interior = new GameObject("Barn interior — straw, remains, and rafters");
            interior.transform.SetParent(root, true);
            BuildBarnRafters(interior.transform, materials, origin);
            BuildBarnHayAndRemains(interior.transform, materials, origin, out var remainsPile);

            var leftEntranceDoor = CreateBarnDoorLeaf(root, "Barn entrance — left leaf", new Vector3(-2.7f, 0f, frontZ), 2.7f, 5.7f, materials.Barn, materials.WoodLight, extendsRight: true);
            var rightEntranceDoor = CreateBarnDoorLeaf(root, "Barn entrance — right leaf", new Vector3(2.7f, 0f, frontZ), 2.7f, 5.7f, materials.Barn, materials.WoodLight, extendsRight: false);
            doorPoint = CreateBarnInteractionPoint(root, "Barn door — iron lock", BarnInteractionKind.Door, new Vector3(0f, 1.4f, frontZ - 1.2f), new Vector3(0.72f, 0.72f, 0.4f));

            var leftForestDoor = CreateBarnDoorLeaf(root, "Barn forest — left door", new Vector3(-2.4f, 0f, backZ), 2.4f, 5.6f, materials.Wood, materials.WoodLight, extendsRight: true);
            var rightForestDoor = CreateBarnDoorLeaf(root, "Barn forest — right door", new Vector3(2.4f, 0f, backZ), 2.4f, 5.6f, materials.Wood, materials.WoodLight, extendsRight: false);
            forestPoint = CreateBarnInteractionPoint(root, "Forest threshold — quiet path", BarnInteractionKind.ForestThreshold, new Vector3(0f, 1.35f, backZ - 1.45f), new Vector3(0.75f, 0.95f, 0.38f));

            BuildBarnForestBackdrop(root, materials);
            var forestMist = new GameObject("Forest — cold mist at the door");
            forestMist.transform.position = new Vector3(0f, 0.2f, 134f);
            forestMist.transform.SetParent(root, true);
            ConfigureBarnMist(forestMist, new Vector3(32f, 5f, 28f));

            var brokenBoards = new GameObject("Gideon — broken side boards");
            brokenBoards.transform.SetParent(root, true);
            for (var i = 0; i < 7; i++)
            {
                var plank = Box("Gideon — nailed breach plank", new Vector3(-9.65f, 0.7f + i * 0.82f, 104f + (i % 2) * 0.22f), new Vector3(0.16f, 0.68f, 1.95f), i % 2 == 0 ? materials.Barn : materials.WoodLight);
                plank.transform.SetParent(brokenBoards.transform, true);
                plank.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1f : 1f) * (4f + i));
                Object.DestroyImmediate(plank.GetComponent<Collider>());
            }
            var breachDust = new GameObject("Gideon — breach dust");
            breachDust.transform.SetParent(root, true);
            breachDust.transform.position = new Vector3(-8.9f, 2.6f, 104f);
            ConfigureBarnDust(breachDust);

            var mimic = BuildBarnMimic(root, materials);
            var rafterRoute = new[]
            {
                new Vector3(-7.2f, 7.1f, 100.5f), new Vector3(6.8f, 7.0f, 100.8f),
                new Vector3(7.3f, 7.2f, 108.7f), new Vector3(-6.9f, 7.1f, 108.1f),
                new Vector3(0f, 7.8f, 105.1f)
            };
            for (var i = 0; i < rafterRoute.Length; i++)
            {
                var waypoint = new GameObject("Mimic — rafter route point " + (i + 1).ToString("00"));
                waypoint.transform.SetParent(root, false);
                waypoint.transform.position = rafterRoute[i];
            }
            var gideon = BuildGideon(root, materials, new Vector3(-7.25f, 0f, 104.8f));
            var gideonPointObject = new GameObject("Gideon — interaction point");
            gideonPointObject.transform.SetParent(gideon.transform, false);
            gideonPointObject.transform.localPosition = new Vector3(0f, 1.25f, 0.95f);
            var gideonCollider = gideonPointObject.AddComponent<BoxCollider>();
            gideonCollider.size = new Vector3(0.9f, 1.1f, 0.45f);
            gideonPoint = gideonPointObject.AddComponent<BarnInteractionPoint>();
            gideonPoint.Configure(sequence, BarnInteractionKind.Gideon);

            remainsPoint = CreateBarnInteractionPoint(interior.transform, "Remains pile — inspect", BarnInteractionKind.Remains, new Vector3(0f, 0.92f, 102.3f), new Vector3(0.9f, 1.1f, 0.75f));
            rafterPoint = CreateBarnInteractionPoint(interior.transform, "Rafter — creature voice", BarnInteractionKind.RafterCreature, new Vector3(0f, 5.8f, 103.6f), new Vector3(1.2f, 1.4f, 0.8f));
            finalShotPoint = CreateBarnInteractionPoint(interior.transform, "Mimic — final shot", BarnInteractionKind.FinalShot, new Vector3(0f, 1.05f, 104.9f), new Vector3(0.95f, 0.9f, 0.95f));

            var flareCue = new GameObject("Gideon's flare cue");
            flareCue.transform.SetParent(root, true);
            flareCue.transform.position = new Vector3(0f, 6.1f, 104f);
            var flareLight = flareCue.AddComponent<Light>();
            flareLight.type = LightType.Point;
            flareLight.color = new Color(1f, 0.52f, 0.22f);
            flareLight.intensity = 12f;
            flareLight.range = 22f;
            flareLight.shadows = LightShadows.None;
            var flareGlow = Sphere("Signal flare — brief amber core", flareCue.transform.position, new Vector3(0.22f, 0.22f, 0.22f), materials.WarmGlow);
            flareGlow.transform.SetParent(flareCue.transform, true);
            Object.DestroyImmediate(flareGlow.GetComponent<Collider>());

            var downPosition = new GameObject("Mimic — center of the barn").transform;
            downPosition.SetParent(root, false);
            downPosition.position = new Vector3(0f, 0.2f, 104.7f);
            var jackAftermath = new GameObject("Jack — aftermath position").transform;
            jackAftermath.SetParent(root, false);
            jackAftermath.position = new Vector3(1.25f, 0f, 104.1f);

            sequence.Configure(
                progression,
                player,
                interactor,
                lantern,
                jack,
                barnKey,
                knife,
                revolver,
                interior,
                leftEntranceDoor,
                rightEntranceDoor,
                remainsPile,
                mimic,
                gideon,
                brokenBoards,
                leftForestDoor,
                rightForestDoor,
                forestMist,
                doorPoint,
                remainsPoint,
                rafterPoint,
                gideonPoint,
                finalShotPoint,
                forestPoint,
                flareCue.transform,
                revolverVisual,
                FindRafterWaypoints(root),
                downPosition,
                jackAftermath);
        }

        private static Transform CreateBarnDoorLeaf(Transform parent, string name, Vector3 hingePosition, float width, float height, Material boards, Material trim, bool extendsRight)
        {
            var hinge = new GameObject(name);
            hinge.transform.SetParent(parent, true);
            hinge.transform.position = hingePosition;
            var leaf = Box(name + " — oak planks", Vector3.zero, new Vector3(width, height, 0.22f), boards);
            leaf.transform.SetParent(hinge.transform, false);
            leaf.transform.localPosition = new Vector3(extendsRight ? width * 0.5f : -width * 0.5f, height * 0.5f, 0f);
            leaf.layer = LayerMask.NameToLayer("Ignore Raycast");
            for (var i = -1; i <= 1; i++)
            {
                var brace = Box(name + " — iron-braced crosspiece", Vector3.zero, new Vector3(width * 0.9f, 0.14f, 0.28f), trim);
                brace.transform.SetParent(leaf.transform, false);
                brace.transform.localPosition = new Vector3(0f, height * (0.25f + (i + 1) * 0.25f), -0.025f);
                Object.DestroyImmediate(brace.GetComponent<Collider>());
            }
            return hinge.transform;
        }

        private static BarnInteractionPoint CreateBarnInteractionPoint(Transform parent, string name, BarnInteractionKind kind, Vector3 position, Vector3 colliderSize)
        {
            var point = new GameObject(name);
            point.transform.SetParent(parent, true);
            point.transform.position = position;
            var collider = point.AddComponent<BoxCollider>();
            collider.size = colliderSize;
            var interaction = point.AddComponent<BarnInteractionPoint>();
            interaction.Configure(null, kind);
            return interaction;
        }

        private static void BuildBarnRafters(Transform parent, MaterialSet materials, Vector3 origin)
        {
            var ridge = Box("Barn interior — ridge beam", new Vector3(0f, 8.35f, origin.z), new Vector3(0.38f, 0.42f, 18.6f), materials.Wood);
            ridge.transform.SetParent(parent, true);
            MarkStatic(ridge);
            for (var row = 0; row < 6; row++)
            {
                var z = origin.z - 8.1f + row * 3.25f;
                var left = Box("Barn interior — left rafter", new Vector3(-4.6f, 6.4f, z), new Vector3(10.1f, 0.28f, 0.34f), materials.WoodLight);
                left.transform.rotation = Quaternion.Euler(0f, 0f, 22.5f);
                left.transform.SetParent(parent, true);
                var right = Box("Barn interior — right rafter", new Vector3(4.6f, 6.4f, z), new Vector3(10.1f, 0.28f, 0.34f), materials.WoodLight);
                right.transform.rotation = Quaternion.Euler(0f, 0f, -22.5f);
                right.transform.SetParent(parent, true);
                MarkStatic(left);
                MarkStatic(right);
            }
        }

        private static void BuildBarnHayAndRemains(Transform parent, MaterialSet materials, Vector3 origin, out GameObject remainsPile)
        {
            var balePositions = new[]
            {
                new Vector3(-7f, 0.55f, 99.2f), new Vector3(-5.7f, 1.5f, 99.2f), new Vector3(7.2f, 0.55f, 100f),
                new Vector3(8f, 0.55f, 107.8f), new Vector3(-8f, 0.55f, 109f), new Vector3(-6.4f, 1.5f, 109.1f),
                new Vector3(5.7f, 0.55f, 108.8f), new Vector3(7.1f, 1.5f, 108.8f)
            };
            for (var i = 0; i < balePositions.Length; i++)
            {
                var bale = Box("Barn interior — compacted hay bale", balePositions[i], new Vector3(1.5f, 1.05f, 1.1f), materials.Hay);
                bale.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 8f : -11f, i % 3 == 0 ? 3f : 0f);
                bale.transform.SetParent(parent, true);
                MarkStatic(bale);
                for (var strand = -1; strand <= 1; strand++)
                {
                    var tie = Box("Barn interior — bale twine", bale.transform.position + new Vector3(strand * 0.42f, 0f, -0.56f), new Vector3(0.035f, 0.96f, 0.025f), materials.Leather);
                    tie.transform.SetParent(parent, true);
                    Object.DestroyImmediate(tie.GetComponent<Collider>());
                }
            }

            var remains = new GameObject("Remains pile");
            remains.transform.SetParent(parent, true);
            remains.transform.position = new Vector3(0f, 0f, 102.2f);
            remainsPile = remains;
            foreach (var corner in new[] { new Vector3(-8.3f, 0f, 101f), new Vector3(8.6f, 0f, 103.6f), new Vector3(-7.8f, 0f, 107.2f) })
            {
                var bundle = new GameObject("Victim remains — torn clothes beneath the straw");
                bundle.transform.position = corner;
                bundle.transform.SetParent(parent, true);
                PrimitiveChild(bundle.transform, "Torn coat", PrimitiveType.Capsule, new Vector3(0f, 0.24f, 0f), new Vector3(0.5f, 0.22f, 0.36f), Quaternion.Euler(0f, 0f, 82f), materials.GideonCloth);
                PrimitiveChild(bundle.transform, "Pale hand under the coat", PrimitiveType.Capsule, new Vector3(0.36f, 0.11f, 0.1f), new Vector3(0.24f, 0.075f, 0.08f), Quaternion.identity, materials.Skin);
                var blood = Cylinder("Barn interior — dark soaked straw", corner + Vector3.up * 0.018f, new Vector3(1.25f, 0.014f, 0.92f), materials.Blood);
                blood.transform.SetParent(parent, true);
                Object.DestroyImmediate(blood.GetComponent<Collider>());
            }
            var body = new GameObject("Bundled remains beneath the hay");
            body.transform.SetParent(remains.transform, false);
            PrimitiveChild(body.transform, "Torn clothes", PrimitiveType.Capsule, new Vector3(0f, 0.26f, 0f), new Vector3(0.72f, 0.28f, 0.4f), Quaternion.Euler(0f, 0f, 74f), materials.GideonCloth);
            PrimitiveChild(body.transform, "Pale arm", PrimitiveType.Capsule, new Vector3(0.54f, 0.12f, 0.08f), new Vector3(0.42f, 0.08f, 0.075f), Quaternion.Euler(0f, 0f, 9f), materials.Skin);
            var pool = Cylinder("Remains — soaked dark straw", new Vector3(0f, 0.015f, 102.3f), new Vector3(1.35f, 0.02f, 0.88f), materials.Blood);
            pool.transform.SetParent(parent, true);
            Object.DestroyImmediate(pool.GetComponent<Collider>());
        }

        private static GameObject BuildBarnMimic(Transform parent, MaterialSet materials)
        {
            var mimic = new GameObject("Mimic — rafter stalker");
            mimic.transform.SetParent(parent, true);
            mimic.transform.position = new Vector3(0f, 7.25f, 103.8f);
            PrimitiveChild(mimic.transform, "Mimic — long pale torso", PrimitiveType.Capsule, new Vector3(0f, -0.2f, 0f), new Vector3(0.35f, 0.88f, 0.3f), Quaternion.Euler(12f, 0f, -7f), materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — eyeless skull", PrimitiveType.Sphere, new Vector3(0f, 0.86f, 0.09f), new Vector3(0.48f, 0.52f, 0.42f), Quaternion.Euler(0f, 0f, 9f), materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — open black jaw", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0.32f), new Vector3(0.38f, 0.24f, 0.13f), Quaternion.Euler(12f, 0f, 0f), materials.Enemy);
            for (var tooth = -2; tooth <= 2; tooth++)
                PrimitiveChild(mimic.transform, "Mimic — exposed tooth", PrimitiveType.Cube, new Vector3(tooth * 0.06f, 0.66f, 0.4f), new Vector3(0.035f, 0.09f, 0.03f), Quaternion.identity, materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — impossibly long left arm", PrimitiveType.Capsule, new Vector3(-0.65f, -0.2f, 0.06f), new Vector3(0.13f, 1.36f, 0.13f), Quaternion.Euler(0f, 0f, 38f), materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — impossibly long right arm", PrimitiveType.Capsule, new Vector3(0.64f, -0.36f, 0.07f), new Vector3(0.13f, 1.55f, 0.13f), Quaternion.Euler(0f, 0f, -49f), materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — trailing left leg", PrimitiveType.Capsule, new Vector3(-0.2f, -1.25f, -0.06f), new Vector3(0.12f, 0.94f, 0.12f), Quaternion.Euler(0f, 0f, -21f), materials.PaleSkin);
            PrimitiveChild(mimic.transform, "Mimic — trailing right leg", PrimitiveType.Capsule, new Vector3(0.24f, -1.3f, 0.02f), new Vector3(0.12f, 1.06f, 0.12f), Quaternion.Euler(0f, 0f, 24f), materials.PaleSkin);
            var shoulderWound = PrimitiveChild(mimic.transform, "Mimic — torn shoulder from Gideon's shotgun", PrimitiveType.Sphere, new Vector3(-0.38f, 0.15f, 0.22f), new Vector3(0.22f, 0.15f, 0.08f), Quaternion.Euler(7f, 0f, -18f), materials.Blood);
            shoulderWound.SetActive(false);
            var collider = mimic.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0f, 0f);
            collider.radius = 0.62f;
            collider.height = 3.1f;
            mimic.SetActive(false);
            return mimic;
        }

        private static GameObject BuildGideon(Transform parent, MaterialSet materials, Vector3 position)
        {
            var gideon = new GameObject("Gideon — breach entrance");
            gideon.transform.SetParent(parent, true);
            gideon.transform.position = position;
            gideon.transform.rotation = Quaternion.Euler(0f, 36f, 0f);
            PrimitiveChild(gideon.transform, "Gideon — muddy wolf-skin coat", PrimitiveType.Capsule, new Vector3(0f, 0.91f, 0f), new Vector3(0.82f, 1.08f, 0.64f), Quaternion.identity, materials.GideonCloth);
            PrimitiveChild(gideon.transform, "Gideon — weathered face", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0.05f), new Vector3(0.37f, 0.4f, 0.33f), Quaternion.Euler(0f, 0f, -8f), materials.GideonSkin);
            PrimitiveChild(gideon.transform, "Gideon — gray hair", PrimitiveType.Sphere, new Vector3(0f, 1.94f, -0.02f), new Vector3(0.39f, 0.18f, 0.35f), Quaternion.identity, materials.GideonCloth);
            PrimitiveChild(gideon.transform, "Gideon — cheek scar", PrimitiveType.Cube, new Vector3(0.11f, 1.7f, 0.34f), new Vector3(0.035f, 0.17f, 0.025f), Quaternion.Euler(0f, 0f, -27f), materials.Blood);
            PrimitiveChild(gideon.transform, "Gideon — shotgun stock", PrimitiveType.Cube, new Vector3(0.36f, 1.11f, 0.18f), new Vector3(0.12f, 0.12f, 0.38f), Quaternion.Euler(12f, -18f, -26f), materials.Wood);
            PrimitiveChild(gideon.transform, "Gideon — smoking shotgun barrel", PrimitiveType.Cube, new Vector3(0.48f, 1.34f, 0.35f), new Vector3(0.07f, 0.07f, 0.86f), Quaternion.Euler(11f, -21f, -12f), materials.Metal);
            var thrownRevolver = new GameObject("Gideon's .38 — thrown to the investigator");
            thrownRevolver.transform.SetParent(gideon.transform, false);
            thrownRevolver.transform.localPosition = new Vector3(0.48f, 1.14f, 0.42f);
            thrownRevolver.transform.localRotation = Quaternion.Euler(4f, 12f, -18f);
            PrimitiveChild(thrownRevolver.transform, ".38 — walnut grip", PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.2f, 0.12f), Quaternion.Euler(-18f, 0f, -8f), materials.Wood);
            PrimitiveChild(thrownRevolver.transform, ".38 — iron frame", PrimitiveType.Cube, new Vector3(0f, -0.015f, 0.02f), new Vector3(0.11f, 0.1f, 0.22f), Quaternion.identity, materials.Metal);
            PrimitiveChild(thrownRevolver.transform, ".38 — cylinder", PrimitiveType.Cylinder, new Vector3(0f, 0.025f, 0.08f), new Vector3(0.07f, 0.1f, 0.07f), Quaternion.Euler(90f, 0f, 0f), materials.Metal);
            PrimitiveChild(thrownRevolver.transform, ".38 — short barrel", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0.24f), new Vector3(0.05f, 0.055f, 0.2f), Quaternion.identity, materials.Metal);
            var collider = gideon.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.95f, 0f);
            collider.size = new Vector3(0.95f, 1.95f, 0.75f);
            gideon.SetActive(false);
            return gideon;
        }

        private static void BuildBarnForestBackdrop(Transform parent, MaterialSet materials)
        {
            var forest = new GameObject("Floresta dos Suspiros — black trees");
            forest.transform.SetParent(parent, true);
            var random = new System.Random(31);
            for (var i = 0; i < 34; i++)
            {
                var x = -17f + (float)random.NextDouble() * 34f;
                var z = 121f + (float)random.NextDouble() * 35f;
                var height = 7.5f + (float)random.NextDouble() * 4.5f;
                var trunk = Cylinder("Forest — twisted black trunk", new Vector3(x, height * 0.5f, z), new Vector3(0.48f, height * 0.5f, 0.48f), materials.Wood);
                trunk.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 24f - 12f);
                trunk.transform.SetParent(forest.transform, true);
                Object.DestroyImmediate(trunk.GetComponent<Collider>());
                for (var branchIndex = 0; branchIndex < 3; branchIndex++)
                {
                    var branch = Box("Forest — broken reaching bough", new Vector3(x + (float)random.NextDouble() * 2f - 1f, height * (0.55f + branchIndex * 0.1f), z), new Vector3(0.16f, 0.16f, 2.9f + (float)random.NextDouble() * 1.8f), materials.Wood);
                    branch.transform.rotation = Quaternion.Euler(18f + branchIndex * 13f, (float)random.NextDouble() * 360f, -30f + (float)random.NextDouble() * 60f);
                    branch.transform.SetParent(forest.transform, true);
                    Object.DestroyImmediate(branch.GetComponent<Collider>());
                }
                if (i % 2 == 0)
                {
                    var foliage = Sphere("Forest — sparse black canopy", new Vector3(x, height * 0.82f, z), new Vector3(4.6f, 2.8f, 4f), materials.Foliage);
                    foliage.transform.SetParent(forest.transform, true);
                    Object.DestroyImmediate(foliage.GetComponent<Collider>());
                }
            }
        }

        private static void ConfigureBarnMist(GameObject mist, Vector3 boxSize)
        {
            var system = mist.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.duration = 18f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.4f, 3.3f);
            main.startColor = new Color(0.53f, 0.62f, 0.76f, 0.1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 18;
            var emission = system.emission;
            emission.rateOverTime = 2.6f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.02f);
            var renderer = mist.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
                renderer.sharedMaterial = new Material(shader) { name = mist.name + " material", color = new Color(0.53f, 0.62f, 0.76f, 0.1f) };
        }

        private static void ConfigureBarnDust(GameObject dust)
        {
            var system = dust.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new Color(0.39f, 0.32f, 0.23f, 0.32f);
            main.maxParticles = 55;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 42) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.2f, 2.4f, 2.1f);
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
                renderer.sharedMaterial = new Material(shader) { name = dust.name + " material", color = new Color(0.39f, 0.32f, 0.23f, 0.32f) };
        }

        private static Transform[] FindRafterWaypoints(Transform barnRoot)
        {
            var points = new System.Collections.Generic.List<Transform>();
            foreach (var transform in barnRoot.GetComponentsInChildren<Transform>(true))
                if (transform.name.StartsWith("Mimic — rafter route point ", System.StringComparison.Ordinal))
                    points.Add(transform);
            points.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return points.ToArray();
        }

        private static InteractableClue FindClueById(Scene scene, string clueId)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var clue in rootObject.GetComponentsInChildren<InteractableClue>(true))
                {
                    if (clue.Id == clueId)
                        return clue;
                }
            }

            return null;
        }

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                    return root;
            }

            return null;
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.21f, 0.24f, 0.30f);
            RenderSettings.ambientIntensity = 0.98f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.21f, 0.24f, 0.31f);
            RenderSettings.fogDensity = 0.0055f;

            var moonObject = GameObject.Find("Moonlight — cool key");
            var moon = moonObject != null ? moonObject.GetComponent<Light>() : null;
            if (moon == null)
                moon = new GameObject("Moonlight — cool key").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.58f, 0.67f, 0.80f);
            moon.intensity = 0.56f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.48f;
            moon.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                Ground = MakeMaterial("Ground — wet earth", new Color(0.18f, 0.17f, 0.15f), 0f, 0.18f),
                Road = MakeMaterial("Main street — damp gravel", new Color(0.24f, 0.23f, 0.21f), 0f, 0.28f),
                Wood = MakeMaterial("Wood — weathered walnut", new Color(0.29f, 0.20f, 0.14f), 0f, 0.3f),
                WoodLight = MakeMaterial("Wood — sun-worn trim", new Color(0.43f, 0.31f, 0.21f), 0f, 0.35f),
                Roof = MakeMaterial("Roof — tarred wood", new Color(0.13f, 0.14f, 0.16f), 0.05f, 0.3f),
                Barn = MakeMaterial("Barn — faded red boards", new Color(0.36f, 0.12f, 0.08f), 0f, 0.3f),
                Stone = MakeMaterial("Stone — cold gray", new Color(0.31f, 0.33f, 0.35f), 0f, 0.42f),
                Church = MakeMaterial("Church — aged plaster", new Color(0.49f, 0.46f, 0.38f), 0f, 0.42f),
                Window = MakeMaterial("Window — dark glass", new Color(0.06f, 0.10f, 0.15f), 0.2f, 0.55f),
                SaloonWindow = MakeTransparentSaloonWindow(),
                Metal = MakeMaterial("Metal — oxidized iron", new Color(0.20f, 0.22f, 0.23f), 0.6f, 0.42f),
                Blood = MakeMaterial("Clue — dried blood", new Color(0.26f, 0.045f, 0.035f), 0f, 0.24f),
                Linen = MakeMaterial("Clue — Layla's aged linen", new Color(0.51f, 0.46f, 0.36f), 0f, 0.16f),
                Mud = MakeMaterial("Clue — boot marks in mud", new Color(0.095f, 0.085f, 0.075f), 0f, 0.08f),
                Foliage = MakeMaterial("Foliage — night pine", new Color(0.09f, 0.14f, 0.13f), 0f, 0.36f),
                WarmGlow = MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f),
                ChurchRobe = MakeMaterial("Church — preacher's faded ivory robe", new Color(0.52f, 0.48f, 0.38f), 0f, 0.28f),
                SheriffGold = MakeMaterial("Sheriff — tarnished brass badge", new Color(0.62f, 0.39f, 0.095f), 0.68f, 0.58f),
                Enemy = MakeMaterial("Figure — near-black cloth", new Color(0.08f, 0.085f, 0.10f), 0f, 0.25f),
                LukeCloth = MakeMaterial("Luke — dusty work coat", new Color(0.25f, 0.20f, 0.15f), 0f, 0.32f),
                Skin = MakeMaterial("Luke — pale skin", new Color(0.39f, 0.29f, 0.23f), 0f, 0.36f),
                Leather = MakeMaterial("Luke — worn leather", new Color(0.16f, 0.12f, 0.09f), 0f, 0.28f),
                Hay = MakeMaterial("Barn — dry straw", new Color(0.48f, 0.35f, 0.19f), 0f, 0.76f),
                PaleSkin = MakeMaterial("Mimic — cold pale hide", new Color(0.68f, 0.66f, 0.59f), 0f, 0.31f),
                GideonCloth = MakeMaterial("Gideon — mud-stained wolf coat", new Color(0.14f, 0.15f, 0.16f), 0f, 0.84f),
                GideonSkin = MakeMaterial("Gideon — weathered skin", new Color(0.34f, 0.25f, 0.20f), 0f, 0.72f),
                WarmGlassDim = MakeMaterial("Lantern — unlit amber glass", new Color(0.19f, 0.095f, 0.04f), 0f, 0.3f)
            };
        }

        private static Material MakeMaterial(string name, Color color, float metallic, float smoothness)
        {
            var assetPath = MaterialsPath + "/" + MakeSafeName(name) + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            var isNew = material == null;
            if (isNew)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
            }

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (isNew) AssetDatabase.CreateAsset(material, assetPath);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material MakeEmissive(string name, Color color, float intensity)
        {
            var material = MakeMaterial(name, color, 0f, 0.2f);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * intensity);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material MakeTransparentSaloonWindow()
        {
            var material = MakeMaterial("Saloon — smoky transparent glass", new Color(0.07f, 0.11f, 0.18f, 0.24f), 0.08f, 0.72f);
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string MakeSafeName(string name)
        {
            return name.Replace(' ', '_').Replace('—', '-');
        }

        private static void BuildGround(MaterialSet materials)
        {
            var ground = Box("Ash Creek ground", new Vector3(0f, -0.3f, 65f), new Vector3(100f, 0.6f, 180f), materials.Ground);
            MarkStatic(ground);
            SetLightmapScaleInLightmap(ground, GroundLightmapScaleInLightmap);

            var street = Box("Main street", new Vector3(0f, MainStreetSurfaceY - MainStreetThickness * 0.5f, 63f), new Vector3(MainStreetWidth, MainStreetThickness, 120f), materials.Road);
            MarkStatic(street);
            SetLightmapScaleInLightmap(street, MainStreetLightmapScaleInLightmap);

            for (var i = 0; i < 12; i++)
            {
                var z = 14f + i * 9.2f;
                var rut = Cylinder("Road rut", new Vector3((i % 2 == 0 ? -1f : 1f) * 2.3f, 0.075f, z), new Vector3(0.18f, 0.012f, 1.1f), materials.Ground);
                rut.transform.rotation = Quaternion.Euler(0f, (i * 23f) % 40f, 0f);
                MarkStatic(rut);
            }
        }

        private static void BuildStreetAndGate(MaterialSet materials)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var post = Box("Wooden entrance gate — post", new Vector3(side * 8f, 2.3f, 13f), new Vector3(0.42f, 4.6f, 0.42f), materials.WoodLight);
                MarkStatic(post);
                var gateLeaf = Box("Wooden gate — slats", new Vector3(side * 5.5f, 1.2f, 13f), new Vector3(4.6f, 2.4f, 0.25f), materials.Wood);
                MarkStatic(gateLeaf);
                for (var i = 0; i < 5; i++)
                {
                    var slat = Box("Gate slat", new Vector3(side * 5.5f + (i - 2) * 0.86f, 1.25f, 12.82f), new Vector3(0.12f, 2.5f, 0.1f), materials.WoodLight);
                    MarkStatic(slat);
                }
            }

            var lintel = Box("Entrance gate — lintel", new Vector3(0f, 4.7f, 13f), new Vector3(17f, 0.35f, 0.38f), materials.Wood);
            MarkStatic(lintel);
            AddText("ASH CREEK", new Vector3(0f, 4.1f, 12.72f), 0.26f, new Color(0.78f, 0.67f, 0.48f), 180f);
            CreateLamp(new Vector3(-7.3f, 0f, 19f), materials);
            CreateLamp(new Vector3(7.3f, 0f, 19f), materials);

            for (var i = 0; i < 8; i++)
            {
                var x = (i % 2 == 0 ? -1f : 1f) * 6.3f;
                var z = 22f + i * 9f;
                var post = Box("Street fence post", new Vector3(x, 0.75f, z), new Vector3(0.2f, 1.5f, 0.2f), materials.WoodLight);
                MarkStatic(post);
            }
        }

        private static void BuildTown(MaterialSet materials)
        {
            BuildWesternBuilding("Saloon", new Vector3(-17f, 0f, 40f), 14f, 8.5f, 11f, materials.Wood, materials.Roof, "SALÃO", materials);
            BuildWesternBuilding("Sheriff office", new Vector3(17f, 0f, 40f), 12f, 6f, 10f, materials.WoodLight, materials.Roof, "XERIFE", materials);
            BuildWesternBuilding("Church", new Vector3(0f, 0f, 61f), 14f, 7f, 17f, materials.Church, materials.Roof, "IGREJA", materials);
            BuildWesternBuilding("Barn", new Vector3(0f, 0f, 104f), 20f, 9f, 20f, materials.Barn, materials.Roof, "CELEIRO", materials);

            var wellBase = Cylinder("Central well — stone rim", new Vector3(0f, 0.45f, 33f), new Vector3(1.65f, 0.45f, 1.65f), materials.Stone);
            MarkStatic(wellBase);
            var wellOpening = Cylinder("Central well — dark opening", new Vector3(0f, 0.94f, 33f), new Vector3(1.12f, 0.045f, 1.12f), materials.Enemy);
            MarkStatic(wellOpening);
            for (var side = -1; side <= 1; side += 2)
            {
                var support = Box("Well roof support", new Vector3(side * 1.15f, 2.15f, 33f), new Vector3(0.16f, 2.5f, 0.16f), materials.Wood);
                MarkStatic(support);
            }
            var wellRoof = Box("Well roof", new Vector3(0f, 3.25f, 33f), new Vector3(3f, 0.22f, 2.4f), materials.Roof);
            MarkStatic(wellRoof);

            var blacksmithSign = Box("Blacksmith alley sign", new Vector3(7.5f, 3.5f, 36f), new Vector3(0.3f, 0.8f, 3f), materials.Wood);
            MarkStatic(blacksmithSign);
            AddText("BECO DO FERREIRO", new Vector3(7.28f, 3.5f, 36f), 0.12f, new Color(0.78f, 0.67f, 0.48f), 90f);

            BuildSaloonInteriorAndClues(materials);
            CreateLamp(new Vector3(-6.7f, 0f, 38f), materials);
            CreateLamp(new Vector3(6.7f, 0f, 53f), materials);
            CreateLamp(new Vector3(-7f, 0f, 69f), materials);

            for (var i = 0; i < 7; i++)
            {
                var barrel = Cylinder("Street barrel", new Vector3((i % 2 == 0 ? -1f : 1f) * (9.3f + (i % 3) * 1.7f), 0.65f, 27f + i * 7.4f), new Vector3(0.48f, 0.65f, 0.48f), materials.Wood);
                MarkStatic(barrel);
            }
        }

        private static void BuildWesternBuilding(string name, Vector3 origin, float width, float height, float depth, Material wall, Material roof, string sign, MaterialSet materials)
        {
            var floor = Box(name + " — floor", new Vector3(origin.x, 0.12f, origin.z), new Vector3(width, 0.24f, depth), materials.Wood);
            MarkStatic(floor);

            var wallThickness = 0.32f;
            var wallCenterY = height * 0.5f + 0.24f;
            GameObject back;
            GameObject backLeft = null;
            GameObject backRight = null;
            GameObject backLintel = null;
            var backZ = origin.z + depth * 0.5f - wallThickness * 0.5f;
            if (name == "Sheriff office" || name == "Barn")
            {
                var rearDoorWidth = name == "Barn" ? 4.8f : 2.1f;
                var rearWallWidth = (width - rearDoorWidth) * 0.5f;
                backLeft = Box(name + " — rear wall left", new Vector3(origin.x - (rearDoorWidth + rearWallWidth) * 0.5f, wallCenterY, backZ), new Vector3(rearWallWidth, height, wallThickness), wall);
                backRight = Box(name + " — rear wall right", new Vector3(origin.x + (rearDoorWidth + rearWallWidth) * 0.5f, wallCenterY, backZ), new Vector3(rearWallWidth, height, wallThickness), wall);
                backLintel = Box(name + " — rear door lintel", new Vector3(origin.x, height - 0.1f, backZ), new Vector3(rearDoorWidth, 0.4f, wallThickness), materials.WoodLight);
                back = null;
            }
            else
            {
                back = Box(name + " — rear wall", new Vector3(origin.x, wallCenterY, backZ), new Vector3(width, height, wallThickness), wall);
            }
            var left = Box(name + " — left wall", new Vector3(origin.x - width * 0.5f + wallThickness * 0.5f, wallCenterY, origin.z), new Vector3(wallThickness, height, depth), wall);
            var right = Box(name + " — right wall", new Vector3(origin.x + width * 0.5f - wallThickness * 0.5f, wallCenterY, origin.z), new Vector3(wallThickness, height, depth), wall);
            var frontZ = origin.z - depth * 0.5f + wallThickness * 0.5f;
            var doorWidth = name == "Church" ? 2.2f : name == "Barn" ? 5.4f : 2.0f;
            var frontWidth = (width - doorWidth) * 0.5f;
            var frontLeft = Box(name + " — front wall left", new Vector3(origin.x - (doorWidth + frontWidth) * 0.5f, wallCenterY, frontZ), new Vector3(frontWidth, height, wallThickness), wall);
            var frontRight = Box(name + " — front wall right", new Vector3(origin.x + (doorWidth + frontWidth) * 0.5f, wallCenterY, frontZ), new Vector3(frontWidth, height, wallThickness), wall);
            foreach (var part in new[] { back, backLeft, backRight, backLintel, left, right, frontLeft, frontRight })
                if (part != null) MarkStatic(part);

            var lintel = Box(name + " — door lintel", new Vector3(origin.x, height - 0.1f, frontZ), new Vector3(doorWidth, 0.4f, wallThickness), materials.WoodLight);
            MarkStatic(lintel);
            for (var i = -1; i <= 1; i += 2)
            {
                var trim = Box(name + " — door frame", new Vector3(origin.x + i * doorWidth * 0.5f, height * 0.5f + 0.24f, frontZ - 0.03f), new Vector3(0.18f, height, 0.2f), materials.WoodLight);
                MarkStatic(trim);
            }

            var porch = Box(name + " — porch", new Vector3(origin.x, 0.23f, origin.z - depth * 0.5f - 1.15f), new Vector3(width * 0.82f, 0.22f, 2.3f), materials.Wood);
            MarkStatic(porch);
            var awning = Box(name + " — awning", new Vector3(origin.x, height * 0.7f, origin.z - depth * 0.5f - 1.1f), new Vector3(width * 0.88f, 0.22f, 2.8f), roof);
            MarkStatic(awning);
            for (var i = -1; i <= 1; i++)
            {
                var post = Box(name + " — porch post", new Vector3(origin.x + i * width * 0.34f, height * 0.35f, origin.z - depth * 0.5f - 2.1f), new Vector3(0.18f, height * 0.7f, 0.18f), materials.WoodLight);
                MarkStatic(post);
            }

            var roofLeft = Box(name + " — roof left", new Vector3(origin.x - width * 0.24f, height + 1f, origin.z), new Vector3(width * 0.58f, 0.32f, depth + 1.2f), roof);
            roofLeft.transform.rotation = Quaternion.Euler(0f, 0f, -13f);
            var roofRight = Box(name + " — roof right", new Vector3(origin.x + width * 0.24f, height + 1f, origin.z), new Vector3(width * 0.58f, 0.32f, depth + 1.2f), roof);
            roofRight.transform.rotation = Quaternion.Euler(0f, 0f, 13f);
            MarkStatic(roofLeft); MarkStatic(roofRight);

            var signBoard = Box(name + " — sign board", new Vector3(origin.x, height - 0.7f, frontZ - 0.22f), new Vector3(Mathf.Min(width * 0.62f, 5.4f), 0.7f, 0.18f), materials.Roof);
            MarkStatic(signBoard);
            AddText(sign, new Vector3(origin.x, height - 0.69f, frontZ - 0.34f), Mathf.Min(0.16f, 2.6f / sign.Length), new Color(0.85f, 0.74f, 0.55f), 180f);

            for (var side = -1; side <= 1; side += 2)
            {
                var windowMaterial = name == "Saloon" ? materials.SaloonWindow : materials.Window;
                var window = Box(name + " — front window", new Vector3(origin.x + side * width * 0.34f, height * 0.58f, frontZ - 0.08f), new Vector3(1.15f, 1.35f, 0.08f), windowMaterial);
                MarkStatic(window);
                var sill = Box(name + " — window sill", new Vector3(origin.x + side * width * 0.34f, height * 0.58f - 0.78f, frontZ - 0.12f), new Vector3(1.55f, 0.14f, 0.32f), materials.WoodLight);
                MarkStatic(sill);
            }

            if (name == "Church")
            {
                var tower = Box("Church — bell tower", new Vector3(origin.x, height + 3.2f, origin.z - depth * 0.22f), new Vector3(3.1f, 6.2f, 3.1f), wall);
                MarkStatic(tower);
                var towerRoof = Box("Church — tower roof", new Vector3(origin.x, height + 6.6f, origin.z - depth * 0.22f), new Vector3(4.2f, 0.42f, 4.2f), roof);
                towerRoof.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                MarkStatic(towerRoof);
                var bell = Sphere("Church — bell", new Vector3(origin.x, height + 2.4f, origin.z - depth * 0.22f), new Vector3(0.8f, 1.05f, 0.8f), materials.Metal);
                MarkStatic(bell);
            }

            if (name == "Saloon")
            {
                var upperBand = Box("Saloon — second floor band", new Vector3(origin.x, height * 0.56f, origin.z - depth * 0.5f - 0.1f), new Vector3(width, 0.22f, 0.48f), materials.WoodLight);
                MarkStatic(upperBand);
                BuildBootTrailByWell(materials);
            }

            if (name == "Barn")
            {
                for (var i = -2; i <= 2; i++)
                {
                    if (Mathf.Abs(i * 2.4f) < doorWidth * 0.5f + 0.3f)
                        continue;
                    var slat = Box("Barn — front timber", new Vector3(origin.x + i * 2.4f, 3.4f, frontZ - 0.15f), new Vector3(0.22f, 6.8f, 0.25f), materials.WoodLight);
                    MarkStatic(slat);
                }
            }
        }

        private static void BuildSheriffOfficeInterior(
            Transform root,
            MaterialSet materials,
            PlayerInteractor interactor,
            FirstPersonController playerController,
            HandLanternPickup lantern,
            DemoProgressionComponent progression,
            JackRescueInteractable jackCompanion,
            SheriffBadgeInventory badgeInventory)
        {
            var tracker = root.gameObject.AddComponent<SheriffOfficeInvestigationTracker>();

            OfficeBox(root, "Evidence room — scarred oak desk", new Vector3(17.7f, 0.99f, 38.45f), new Vector3(4.0f, 0.2f, 1.25f), materials.WoodLight);
            for (var side = -1; side <= 1; side += 2)
            for (var frontBack = -1; frontBack <= 1; frontBack += 2)
                OfficeBox(root, "Evidence room — desk leg", new Vector3(17.7f + side * 1.7f, 0.52f, 38.45f + frontBack * 0.47f), new Vector3(0.16f, 0.92f, 0.16f), materials.Wood);

            var handkerchief = Box("Evidence — Layla's bloodied handkerchief", new Vector3(16.85f, 1.14f, 38.37f), new Vector3(0.42f, 0.035f, 0.34f), materials.Linen);
            handkerchief.transform.rotation = Quaternion.Euler(0f, 16f, 0f);
            handkerchief.transform.SetParent(root, true);
            foreach (var stain in new[]
            {
                Sphere("Handkerchief — soaked blood", new Vector3(16.79f, 1.162f, 38.34f), new Vector3(0.16f, 0.012f, 0.11f), materials.Blood),
                Sphere("Handkerchief — dried blood spot", new Vector3(16.96f, 1.162f, 38.42f), new Vector3(0.075f, 0.012f, 0.06f), materials.Blood)
            })
            {
                stain.transform.rotation = Quaternion.Euler(0f, 16f, 0f);
                stain.transform.SetParent(root, true);
                Object.DestroyImmediate(stain.GetComponent<Collider>());
            }
            var handkerchiefClue = handkerchief.AddComponent<InteractableClue>();
            handkerchiefClue.Configure(
                SheriffOfficeInvestigationState.LaylaEvidenceInteractionId,
                "Examinar o lenço de Layla",
                "Jack fareja e indica o lenço ensanguentado de Layla sobre a mesa.",
                2.8f,
                false,
                DemoObjective.SearchSheriffOffice,
                string.Empty);

            BuildSheriffCell(root, materials, out var hale);
            BuildSheriffStairAndLanding(root, materials, out var redBook);

            var lampBase = Cylinder("Sheriff office — lamp base", new Vector3(17.75f, 1.2f, 38.4f), new Vector3(0.15f, 0.045f, 0.15f), materials.Metal);
            lampBase.transform.SetParent(root, true);
            var lampGlass = Sphere("Sheriff office — lamp glass", new Vector3(17.75f, 1.52f, 38.4f), new Vector3(0.2f, 0.32f, 0.2f), materials.WarmGlow);
            lampGlass.transform.SetParent(root, true);
            var firstFloorLamp = new GameObject("Sheriff office — amber desk practical");
            firstFloorLamp.transform.position = new Vector3(17.75f, 2.05f, 38.4f);
            firstFloorLamp.transform.SetParent(root, true);
            var warmLight = firstFloorLamp.AddComponent<Light>();
            warmLight.type = LightType.Point;
            warmLight.color = new Color(1f, 0.55f, 0.28f);
            warmLight.intensity = 1.35f;
            warmLight.range = 7f;
            warmLight.shadows = LightShadows.None;
            warmLight.lightmapBakeType = LightmapBakeType.Baked;

            var secondFloorLamp = new GameObject("Sheriff office — lantern above the ledger");
            secondFloorLamp.transform.position = new Vector3(19.1f, 5.15f, 43.0f);
            secondFloorLamp.transform.SetParent(root, true);
            var upperLight = secondFloorLamp.AddComponent<Light>();
            upperLight.type = LightType.Point;
            upperLight.color = new Color(1f, 0.62f, 0.34f);
            upperLight.intensity = 0.9f;
            upperLight.range = 5f;
            upperLight.shadows = LightShadows.None;
            upperLight.lightmapBakeType = LightmapBakeType.Baked;

            BuildSheriffOfficeEscape(root, materials, playerController, lantern, jackCompanion);

            var escapeSequence = root.GetComponent<SheriffOfficeEscapeSequence>();
            tracker.Configure(interactor, progression, escapeSequence, hale.gameObject, badgeInventory);
            hale.Configure(tracker, badgeInventory, hale.transform.Find("Hale — pointed revolver"));
            redBook.Configure(tracker);
            var player = interactor.gameObject;
            var grants = player.GetComponent<ScreenplayItemGrantTracker>() ?? player.AddComponent<ScreenplayItemGrantTracker>();
            var knife = FindClueById(SceneManager.GetActiveScene(), "saloon.knife");
            var barnKey = hale.transform.Find("Hale — barn key");
            grants.Configure(
                interactor,
                player.GetComponent<CombatKnifeInventory>(),
                player.GetComponent<BarnKeyInventory>(),
                knife != null ? knife.gameObject : null,
                barnKey != null ? barnKey.gameObject : null);
        }

        private static void BuildSheriffOfficeEscape(
            Transform root,
            MaterialSet materials,
            FirstPersonController player,
            HandLanternPickup lantern,
            JackRescueInteractable jackCompanion)
        {
            var intactDoor = Box("Sheriff office — front door intact", new Vector3(17f, 1.38f, 35.16f), new Vector3(2f, 2.55f, 0.16f), materials.Wood);
            intactDoor.transform.SetParent(root, true);

            var brokenDoor = new GameObject("Sheriff office — front door breached");
            brokenDoor.transform.SetParent(root, true);
            for (var i = 0; i < 5; i++)
            {
                var splinter = Box("Door splinter " + (i + 1), new Vector3(16.3f + i * 0.35f, 0.68f + (i % 2) * 0.62f, 35.3f - (i % 2) * 0.34f),
                    new Vector3(0.72f, 0.12f, 0.1f), materials.WoodLight);
                splinter.transform.rotation = Quaternion.Euler(0f, -14f + i * 8f, -8f + (i % 3) * 9f);
                splinter.transform.SetParent(brokenDoor.transform, true);
                Object.DestroyImmediate(splinter.GetComponent<Collider>());
            }

            var creature = new GameObject("Sheriff office — blind mimic creature");
            creature.transform.position = new Vector3(17f, 0.24f, 37.3f);
            PrimitiveChild(creature.transform, "Mimic — hunched body", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 1.2f, 0.62f), Quaternion.Euler(8f, 0f, 0f), materials.Enemy);
            PrimitiveChild(creature.transform, "Mimic — blind face", PrimitiveType.Sphere, new Vector3(0f, 1.86f, 0.18f), new Vector3(0.62f, 0.48f, 0.5f), Quaternion.identity, materials.Enemy);
            PrimitiveChild(creature.transform, "Mimic — reaching arm left", PrimitiveType.Capsule, new Vector3(-0.64f, 0.94f, 0.12f), new Vector3(0.22f, 0.92f, 0.25f), Quaternion.Euler(0f, 0f, -48f), materials.Enemy);
            PrimitiveChild(creature.transform, "Mimic — reaching arm right", PrimitiveType.Capsule, new Vector3(0.64f, 0.94f, 0.12f), new Vector3(0.22f, 0.92f, 0.25f), Quaternion.Euler(0f, 0f, 48f), materials.Enemy);
            var ears = new GameObject("Mimic — listening point").transform;
            ears.SetParent(creature.transform, false);
            ears.localPosition = new Vector3(0f, 1.9f, 0.22f);
            var awareness = creature.AddComponent<EnemyAwarenessAgent>();
            awareness.ConfigureHearingOnly(player, ears);
            creature.SetActive(false);

            var muzzleFlashObject = new GameObject("Sheriff office — Hale's muzzle flash");
            muzzleFlashObject.transform.position = new Vector3(13.78f, 1.55f, 43.1f);
            muzzleFlashObject.transform.SetParent(root, true);
            var muzzleFlash = muzzleFlashObject.AddComponent<Light>();
            muzzleFlash.type = LightType.Point;
            muzzleFlash.color = new Color(1f, 0.66f, 0.32f);
            muzzleFlash.intensity = 5f;
            muzzleFlash.range = 3.2f;
            muzzleFlash.shadows = LightShadows.None;
            muzzleFlash.enabled = false;

            var exitHandle = Box("Sheriff office — rear exit handle", new Vector3(16.15f, 1.42f, 44.28f), new Vector3(0.17f, 0.18f, 0.16f), materials.SheriffGold);
            exitHandle.transform.SetParent(root, true);
            var exitInteraction = exitHandle.AddComponent<SheriffOfficeEscapeInteractable>();
            var exitBarrier = new GameObject("Sheriff office — temporary rear exit barrier");
            exitBarrier.transform.position = new Vector3(17f, 1.3f, 44.84f);
            exitBarrier.transform.SetParent(root, true);
            exitBarrier.AddComponent<BoxCollider>().size = new Vector3(2.02f, 2.5f, 0.24f);
            var escapeSequence = root.gameObject.AddComponent<SheriffOfficeEscapeSequence>();
            escapeSequence.Configure(player, lantern, creature, intactDoor, brokenDoor, exitHandle, exitBarrier, jackCompanion, muzzleFlash);
            exitInteraction.Configure(escapeSequence);
        }

        private static void BuildSheriffCell(Transform root, MaterialSet materials, out SheriffHaleEncounterInteractable hale)
        {
            const float cellFrontZ = 41.0f;
            const float barCenterY = 1.35f;
            for (var i = 0; i <= 9; i++)
            {
                var x = 11.9f + i * 0.43f;
                var bar = OfficeBox(root, "Jail cell — front iron bar", new Vector3(x, barCenterY, cellFrontZ), new Vector3(0.075f, 2.5f, 0.075f), materials.Metal);
                bar.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            foreach (var y in new[] { 0.24f, 1.28f, 2.34f })
            {
                var bar = OfficeBox(root, "Jail cell — front crossbar", new Vector3(13.84f, y, cellFrontZ), new Vector3(4.0f, 0.08f, 0.08f), materials.Metal);
                bar.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            for (var i = 0; i <= 8; i++)
            {
                var z = 41.2f + i * 0.43f;
                var bar = OfficeBox(root, "Jail cell — side iron bar", new Vector3(15.88f, barCenterY, z), new Vector3(0.075f, 2.5f, 0.075f), materials.Metal);
                bar.layer = LayerMask.NameToLayer("Ignore Raycast");
            }

            var eastCrossbar = OfficeBox(root, "Jail cell — east crossbar", new Vector3(15.88f, 1.28f, 42.92f), new Vector3(0.08f, 0.08f, 3.5f), materials.Metal);
            eastCrossbar.layer = LayerMask.NameToLayer("Ignore Raycast");
            OfficeBox(root, "Jail cell — bench", new Vector3(13.3f, 0.56f, 44.0f), new Vector3(2.8f, 0.18f, 0.65f), materials.Wood);
            OfficeBox(root, "Jail cell — bench leg", new Vector3(12.1f, 0.36f, 44.0f), new Vector3(0.14f, 0.42f, 0.16f), materials.WoodLight);
            OfficeBox(root, "Jail cell — bench leg", new Vector3(14.5f, 0.36f, 44.0f), new Vector3(0.14f, 0.42f, 0.16f), materials.WoodLight);

            var haleRoot = new GameObject("Sheriff Hale — behind the cell bars");
            haleRoot.transform.position = new Vector3(13.52f, 0.24f, 43.0f);
            haleRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            haleRoot.transform.SetParent(root, true);
            PrimitiveChild(haleRoot.transform, "Hale — worn duster", PrimitiveType.Capsule, new Vector3(0f, 0.91f, 0f), new Vector3(0.72f, 0.95f, 0.48f), Quaternion.identity, materials.LukeCloth);
            PrimitiveChild(haleRoot.transform, "Hale — tired face", PrimitiveType.Sphere, new Vector3(0f, 1.57f, 0.02f), new Vector3(0.34f, 0.38f, 0.32f), Quaternion.identity, materials.Skin);
            PrimitiveChild(haleRoot.transform, "Hale — sheriff hat", PrimitiveType.Cylinder, new Vector3(0f, 1.79f, 0.02f), new Vector3(0.42f, 0.08f, 0.38f), Quaternion.identity, materials.Wood);
            PrimitiveChild(haleRoot.transform, "Hale — tarnished badge", PrimitiveType.Cylinder, new Vector3(0.12f, 1.06f, 0.25f), new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), materials.SheriffGold);
            PrimitiveChild(haleRoot.transform, "Hale — shaking forearm", PrimitiveType.Capsule, new Vector3(0.39f, 1.16f, 0.17f), new Vector3(0.15f, 0.55f, 0.15f), Quaternion.Euler(0f, 0f, -54f), materials.LukeCloth);
            PrimitiveChild(haleRoot.transform, "Hale — pointed revolver", PrimitiveType.Cube, new Vector3(0.5f, 1.28f, 0.33f), new Vector3(0.08f, 0.12f, 0.28f), Quaternion.identity, materials.Metal);
            BuildHaleBarnKey(haleRoot.transform, materials);
            var haleCollider = haleRoot.AddComponent<BoxCollider>();
            haleCollider.center = new Vector3(0f, 0.97f, 0.05f);
            haleCollider.size = new Vector3(0.85f, 1.95f, 0.72f);
            hale = haleRoot.AddComponent<SheriffHaleEncounterInteractable>();
            haleRoot.SetActive(false);

        }

        private static Transform BuildHaleBarnKey(Transform haleRoot, MaterialSet materials)
        {
            var existingKey = haleRoot.Find("Hale — barn key");
            if (existingKey != null)
                return existingKey;

            var key = new GameObject("Hale — barn key").transform;
            key.SetParent(haleRoot, false);
            key.localPosition = new Vector3(-0.27f, 0.88f, 0.28f);
            PrimitiveChild(key, "Hale's key — iron ring", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.045f, 0.018f, 0.045f), Quaternion.Euler(90f, 0f, 0f), materials.SheriffGold);
            PrimitiveChild(key, "Hale's key — shaft", PrimitiveType.Cube, new Vector3(0.035f, -0.08f, 0f), new Vector3(0.025f, 0.12f, 0.018f), Quaternion.Euler(0f, 0f, 14f), materials.Metal);
            PrimitiveChild(key, "Hale's key — bit", PrimitiveType.Cube, new Vector3(0.064f, -0.12f, 0f), new Vector3(0.06f, 0.022f, 0.018f), Quaternion.identity, materials.Metal);
            return key;
        }

        private static void BuildSheriffStairAndLanding(Transform root, MaterialSet materials, out RedBookInteractable redBook)
        {
            const float floorTop = 0.24f;
            const int stepCount = 14;
            const float risePerStep = 0.22f;
            const float runPerStep = 0.29f;
            const float stepWidth = 1.45f;
            for (var i = 0; i < stepCount; i++)
            {
                var step = Box("Sheriff office — second floor stair step " + (i + 1),
                    new Vector3(20.65f, floorTop + i * risePerStep + 0.12f, 36.0f + i * runPerStep),
                    new Vector3(stepWidth, 0.24f, 0.43f), materials.WoodLight);
                step.transform.SetParent(root, true);
                MarkStatic(step);
            }

            OfficeBox(root, "Sheriff office — second floor landing", new Vector3(19.55f, 3.22f, 42.45f), new Vector3(7.1f, 0.24f, 5.1f), materials.WoodLight);
            OfficeBox(root, "Sheriff office — landing rail west", new Vector3(16.06f, 3.73f, 42.25f), new Vector3(0.12f, 0.78f, 4.65f), materials.Wood);
            OfficeBox(root, "Sheriff office — landing rail back", new Vector3(19.55f, 3.73f, 44.94f), new Vector3(7.0f, 0.78f, 0.12f), materials.Wood);
            for (var i = 0; i < 4; i++)
                OfficeBox(root, "Sheriff office — second floor joist", new Vector3(16.2f + i * 2.1f, 2.74f, 42.45f), new Vector3(0.16f, 0.16f, 5.05f), materials.Wood);

            OfficeBox(root, "Second floor — Red Book stand", new Vector3(19.05f, 3.78f, 43.0f), new Vector3(2.2f, 0.16f, 1.25f), materials.Wood);
            OfficeBox(root, "Second floor — book stand leg", new Vector3(18.25f, 3.52f, 42.65f), new Vector3(0.13f, 0.48f, 0.13f), materials.WoodLight);
            OfficeBox(root, "Second floor — book stand leg", new Vector3(19.85f, 3.52f, 42.65f), new Vector3(0.13f, 0.48f, 0.13f), materials.WoodLight);
            OfficeBox(root, "Second floor — book stand leg", new Vector3(18.25f, 3.52f, 43.35f), new Vector3(0.13f, 0.48f, 0.13f), materials.WoodLight);
            OfficeBox(root, "Second floor — book stand leg", new Vector3(19.85f, 3.52f, 43.35f), new Vector3(0.13f, 0.48f, 0.13f), materials.WoodLight);

            var cover = Box("Red Book — worn crimson cover", new Vector3(19.03f, 3.92f, 42.96f), new Vector3(0.56f, 0.09f, 0.4f), materials.Barn);
            cover.transform.SetParent(root, true);
            var pages = Box("Red Book — exposed page edges", new Vector3(19.03f, 3.977f, 42.96f), new Vector3(0.49f, 0.035f, 0.34f), materials.WoodLight);
            pages.transform.SetParent(cover.transform, true);
            Object.DestroyImmediate(pages.GetComponent<Collider>());
            var clasp = Box("Red Book — brass clasp", new Vector3(19.03f, 4.01f, 42.78f), new Vector3(0.09f, 0.03f, 0.06f), materials.SheriffGold);
            clasp.transform.SetParent(cover.transform, true);
            Object.DestroyImmediate(clasp.GetComponent<Collider>());
            redBook = cover.AddComponent<RedBookInteractable>();

            var rafters = new[] { 40.65f, 42.7f, 44.55f };
            for (var i = 0; i < rafters.Length; i++)
                OfficeBox(root, "Sheriff office — exposed roof rafter", new Vector3(19.1f, 5.85f, rafters[i]), new Vector3(10.6f, 0.18f, 0.2f), materials.Wood);
        }

        private static GameObject OfficeBox(Transform root, string name, Vector3 position, Vector3 size, Material material, bool hasCollider = true, bool isStatic = true)
        {
            var piece = Box(name, position, size, material);
            piece.transform.SetParent(root, true);
            if (!hasCollider)
                Object.DestroyImmediate(piece.GetComponent<Collider>());
            if (isStatic)
                MarkStatic(piece);
            return piece;
        }

        private static void BuildBootTrailByWell(MaterialSet materials)
        {
            const int footprintCount = 15;
            for (var i = 0; i < footprintCount; i++)
            {
                var progress = i / (float)(footprintCount - 1);
                var x = Mathf.Lerp(-2.2f, -22f, progress);
                var side = i % 2 == 0 ? -0.12f : 0.12f;
                var size = i == 0 ? new Vector3(0.28f, 0.03f, 0.55f) : new Vector3(0.19f, 0.025f, 0.43f);
                var rotationY = i % 2 == 0 ? -57f : -43f;
                var rotationRadians = rotationY * Mathf.Deg2Rad;
                var projectedHalfWidth = Mathf.Abs(Mathf.Cos(rotationRadians)) * size.x * 0.5f
                    + Mathf.Abs(Mathf.Sin(rotationRadians)) * size.z * 0.5f;
                const float roadEdgeClearance = 0.025f;
                if (x < MainStreetWestEdge && x + projectedHalfWidth > MainStreetWestEdge)
                    x = MainStreetWestEdge - projectedHalfWidth - roadEdgeClearance;

                var supportHeight = x >= MainStreetWestEdge ? MainStreetSurfaceY : 0f;
                var position = new Vector3(x, supportHeight + size.y * 0.5f, Mathf.Lerp(33.45f, 41.6f, progress) + side);
                var footprint = Box("Clue — dragged boot print", position, size, materials.Mud);
                footprint.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
                MarkStatic(footprint);

                if (i != 0)
                    continue;

                var clue = footprint.AddComponent<InteractableClue>();
                clue.Configure("saloon.boot-trail", "Examinar as marcas de botas", "Marcas de botas de garimpeiros foram arrastadas do poço pela rua principal, na direção do saloon.", 2.8f, true, DemoObjective.FollowBootprintsToSaloon, "RASTRO — DO POÇO AO SALOON\nAs marcas de botas deixam o poço e seguem pela rua principal, em direção ao saloon.");
            }
        }

        private static void BuildSaloonDriedBloodStains(MaterialSet materials)
        {
            var bloodStains = new[]
            {
                new Vector3(-18.3f, 0.257f, 39.1f),
                new Vector3(-12.8f, 0.257f, 42.3f),
                new Vector3(-17.2f, 0.257f, 35.9f)
            };
            for (var i = 0; i < bloodStains.Length; i++)
            {
                var stain = Cylinder("Saloon — dried blood stain", bloodStains[i], new Vector3(0.35f + i * 0.06f, 0.025f, 0.7f), materials.Blood);
                stain.transform.rotation = Quaternion.Euler(0f, 16f + i * 17f, 0f);
                MarkStatic(stain);
                Object.DestroyImmediate(stain.GetComponent<Collider>());
            }
        }

        private static void ConfigureSaloonClues(InteractableClue note, InteractableClue knife)
        {
            note.Configure(
                SaloonApparitionState.NoteInteractionId,
                "Ler a anotação",
                CarmenNoteText,
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                string.Empty);
            knife.Configure(
                "saloon.knife",
                "Examinar a faca",
                "Uma faca está cravada sobre o balcão destruído. Ela desbloqueia o ataque corpo a corpo emergencial.",
                2.8f,
                true,
                DemoObjective.ExamineSaloonKnife);
            EditorUtility.SetDirty(note);
            EditorUtility.SetDirty(knife);
        }

        private static void BuildSaloonInvestigationSetpiece(MaterialSet materials)
        {
            BuildSaloonFootprints(materials);
            BuildSaloonBrokenFurniture(materials);
            BuildSaloonWallWarning(materials);
            BuildSaloonApparitionAndExitBeat(materials);
        }

        private static void BuildSaloonFootprints(MaterialSet materials)
        {
            const int footprintCount = 9;
            for (var i = 0; i < footprintCount; i++)
            {
                var progress = i / (float)(footprintCount - 1);
                var position = new Vector3(
                    Mathf.Lerp(-16.8f, -21.1f, progress),
                    0.258f,
                    Mathf.Lerp(36f, 39.4f, progress));
                var print = Box("Clue — saloon footprint", position, new Vector3(0.17f, 0.018f, 0.34f), materials.Mud);
                print.transform.rotation = Quaternion.Euler(0f, (i % 2 == 0 ? -18f : 22f), 0f);
                if (i == 0)
                {
                    var clue = print.AddComponent<InteractableClue>();
                    clue.Configure(
                        "saloon.footprints",
                        "Examinar pegadas no assoalho",
                        "Marcas de botas de garimpeiros arrastadas seguem em direção à rua principal e ao saloon.",
                        2.8f,
                        false,
                        DemoObjective.InvestigateSaloonClues,
                        string.Empty);
                }
                else
                {
                    Object.DestroyImmediate(print.GetComponent<Collider>());
                }
            }
        }

        private static void BuildSaloonBrokenFurniture(MaterialSet materials)
        {
            var table = Box("Clue — overturned furniture", new Vector3(-13.3f, 0.72f, 40.25f), new Vector3(2.1f, 0.18f, 1.35f), materials.WoodLight);
            table.transform.rotation = Quaternion.Euler(0f, -18f, 71f);
            var clue = table.AddComponent<InteractableClue>();
            clue.Configure(
                "saloon.broken-furniture",
                "Examinar os móveis quebrados",
                "Mesas viradas, copos quebrados e poças de sangue seco indicam uma debandada violenta.",
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                string.Empty);

            PrimitiveChild(table.transform, "Broken furniture — snapped leg", PrimitiveType.Cube, new Vector3(0.56f, -0.3f, 0.38f), new Vector3(0.16f, 0.88f, 0.16f), Quaternion.Euler(0f, 0f, -37f), materials.Wood);
            PrimitiveChild(table.transform, "Broken furniture — loose plank", PrimitiveType.Cube, new Vector3(-0.45f, -0.2f, -0.47f), new Vector3(1.15f, 0.12f, 0.14f), Quaternion.Euler(0f, 24f, 31f), materials.Wood);
            MarkStatic(table);
        }

        private static void BuildSaloonWallWarning(MaterialSet materials)
        {
            var warning = new GameObject("Clue — warning on wall");
            warning.transform.position = new Vector3(-10.97f, 2.25f, 38.7f);
            var collider = warning.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.12f, 0.74f, 0.94f);
            var wallScrawl = Box("Warning — aged plaster beneath the writing", warning.transform.position, new Vector3(0.13f, 0.76f, 0.96f), materials.Church);
            wallScrawl.transform.SetParent(warning.transform, true);
            var clue = warning.AddComponent<InteractableClue>();
            clue.Configure(
                "saloon.warning",
                "Ler o aviso na parede",
                "Não façam barulho. Eles não enxergam como nós, mas escutam tudo. Os sobreviventes foram levados para o celeiro.",
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                string.Empty);

            var writingObject = new GameObject("Warning — dried red writing");
            writingObject.transform.SetParent(warning.transform, false);
            writingObject.transform.localPosition = new Vector3(-0.071f, 0f, 0f);
            writingObject.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
            var writing = writingObject.AddComponent<TextMesh>();
            writing.text = "Não façam barulho.\nEles não enxergam como nós,\nmas escutam tudo.\nOs sobreviventes foram\nlevados para o celeiro.";
            writing.anchor = TextAnchor.MiddleCenter;
            writing.alignment = TextAlignment.Center;
            writing.characterSize = 0.042f;
            writing.fontSize = 48;
            writing.color = new Color(0.35f, 0.045f, 0.035f);
            writing.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (writing.font != null)
                writingObject.GetComponent<MeshRenderer>().sharedMaterial = writing.font.material;
            MarkStatic(warning);
        }

        private static void BuildSaloonApparitionAndExitBeat(MaterialSet materials)
        {
            var apparition = new GameObject("Saloon — window apparition");
            apparition.transform.position = new Vector3(-21.76f, 3.72f, 33.55f);
            apparition.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            PrimitiveChild(apparition.transform, "Apparition — elongated torso", PrimitiveType.Capsule, new Vector3(0f, 1.34f, 0f), new Vector3(0.52f, 1.48f, 0.44f), Quaternion.identity, materials.Enemy);
            PrimitiveChild(apparition.transform, "Apparition — featureless head", PrimitiveType.Sphere, new Vector3(0f, 2.36f, 0f), new Vector3(0.38f, 0.48f, 0.38f), Quaternion.identity, materials.Enemy);
            PrimitiveChild(apparition.transform, "Apparition — left arm", PrimitiveType.Capsule, new Vector3(-0.42f, 1.43f, 0f), new Vector3(0.15f, 1.08f, 0.16f), Quaternion.Euler(0f, 0f, -7f), materials.Enemy);
            PrimitiveChild(apparition.transform, "Apparition — right arm", PrimitiveType.Capsule, new Vector3(0.42f, 1.39f, 0f), new Vector3(0.15f, 1.02f, 0.16f), Quaternion.Euler(0f, 0f, 8f), materials.Enemy);
            PrimitiveChild(apparition.transform, "Apparition — left leg", PrimitiveType.Capsule, new Vector3(-0.14f, 0.39f, 0f), new Vector3(0.18f, 0.8f, 0.18f), Quaternion.identity, materials.Enemy);
            PrimitiveChild(apparition.transform, "Apparition — right leg", PrimitiveType.Capsule, new Vector3(0.14f, 0.39f, 0f), new Vector3(0.18f, 0.8f, 0.18f), Quaternion.identity, materials.Enemy);
            apparition.SetActive(false);

            var doorsRoot = new GameObject("Saloon — double doors");
            doorsRoot.transform.position = new Vector3(-17f, 0f, 34.63f);
            var leftDoor = CreateDoubleDoorLeaf(doorsRoot.transform, "Saloon — left door", -1f, 83f, materials.WoodLight);
            var rightDoor = CreateDoubleDoorLeaf(doorsRoot.transform, "Saloon — right door", 1f, -83f, materials.WoodLight);

            var sequenceObject = new GameObject("Saloon — apparition sequence");
            sequenceObject.transform.position = new Vector3(-17f, 0f, 35.5f);
            var trackerObject = new GameObject("Saloon — investigation progress");
            var tracker = trackerObject.AddComponent<SaloonInvestigationTracker>();
            var playerInteractor = Object.FindFirstObjectByType<PlayerInteractor>();
            var progression = Object.FindFirstObjectByType<DemoProgressionComponent>();
            tracker.Configure(playerInteractor, progression);

            var soundSource = sequenceObject.AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 1f;
            soundSource.rolloffMode = AudioRolloffMode.Linear;
            soundSource.minDistance = 2.5f;
            soundSource.maxDistance = 18f;
            soundSource.dopplerLevel = 0f;
            var flashObject = new GameObject("Saloon — amber slam flash");
            flashObject.transform.SetParent(sequenceObject.transform, false);
            flashObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var flash = flashObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.64f, 0.37f);
            flash.range = 8f;
            flash.intensity = 0f;
            flash.shadows = LightShadows.None;
            flash.enabled = false;

            var sequence = sequenceObject.AddComponent<SaloonApparitionSequence>();
            sequence.Configure(
                Object.FindFirstObjectByType<FirstPersonController>(),
                progression,
                Object.FindFirstObjectByType<PlayerJournalComponent>(),
                tracker,
                apparition.transform,
                leftDoor,
                rightDoor,
                soundSource,
                flash);
        }

        private static Transform CreateDoubleDoorLeaf(Transform root, string name, float side, float openYaw, Material material)
        {
            var hinge = new GameObject(name + " — hinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(side, 1.12f, 0f);
            hinge.localRotation = Quaternion.Euler(0f, openYaw, 0f);
            PrimitiveChild(hinge, name + " — leaf", PrimitiveType.Cube, new Vector3(-side * 0.5f, 0f, 0f), new Vector3(1f, 2.24f, 0.16f), Quaternion.identity, material);
            return hinge;
        }

        private static void BuildChurchInvestigationSetpiece(
            Transform root,
            MaterialSet materials,
            FirstPersonController player,
            DemoProgressionComponent progression,
            out GameObject falseElias,
            out GameObject returnReveal)
        {
            for (var row = 0; row < 3; row++)
            {
                var z = 56.5f + row * 2.25f;
                for (var side = -1; side <= 1; side += 2)
                {
                    var x = side * 3.45f;
                    var seat = Box("Church — pew seat", new Vector3(x, 0.53f, z), new Vector3(2.9f, 0.18f, 0.58f), materials.Wood);
                    seat.transform.SetParent(root, true);
                    MarkStatic(seat);
                    var back = Box("Church — pew back", new Vector3(x, 1.02f, z + 0.22f), new Vector3(2.9f, 0.88f, 0.14f), materials.WoodLight);
                    back.transform.SetParent(root, true);
                    MarkStatic(back);
                    for (var end = -1; end <= 1; end += 2)
                    {
                        var leg = Box("Church — pew leg", new Vector3(x + end * 1.12f, 0.29f, z), new Vector3(0.14f, 0.5f, 0.48f), materials.Wood);
                        leg.transform.SetParent(root, true);
                        MarkStatic(leg);
                    }
                }
            }

            var altarBase = Box("Church — altar base", new Vector3(0f, 0.62f, 67.25f), new Vector3(4.4f, 1.2f, 1.2f), materials.Church);
            altarBase.transform.SetParent(root, true);
            MarkStatic(altarBase);
            var altarTop = Box("Church — altar top", new Vector3(0f, 1.32f, 66.8f), new Vector3(5.2f, 0.2f, 1.9f), materials.WoodLight);
            altarTop.transform.SetParent(root, true);
            MarkStatic(altarTop);
            var crossStem = Box("Church — altar cross", new Vector3(0f, 2.55f, 68.25f), new Vector3(0.18f, 2.2f, 0.18f), materials.WoodLight);
            crossStem.transform.SetParent(root, true);
            MarkStatic(crossStem);
            var crossBeam = Box("Church — altar cross beam", new Vector3(0f, 2.92f, 68.25f), new Vector3(1.3f, 0.18f, 0.18f), materials.WoodLight);
            crossBeam.transform.SetParent(root, true);
            MarkStatic(crossBeam);

            var altarFlame = Sphere("Church — amber altar flame", new Vector3(-4.25f, 1.48f, 64.1f), new Vector3(0.3f, 0.43f, 0.3f), materials.WarmGlow);
            altarFlame.transform.SetParent(root, true);
            var altarLightObject = new GameObject("Church — first-visit amber altar light");
            altarLightObject.transform.position = new Vector3(-4.25f, 1.68f, 64.1f);
            altarLightObject.transform.SetParent(root, true);
            var altarLight = altarLightObject.AddComponent<Light>();
            altarLight.type = LightType.Point;
            altarLight.color = new Color(1f, 0.72f, 0.42f);
            altarLight.intensity = 2.1f;
            altarLight.range = 8f;
            altarLight.shadows = LightShadows.None;

            var entryCueObject = new GameObject("Church — single bell and altar flicker on entry");
            entryCueObject.transform.position = new Vector3(0f, 1.2f, 54.6f);
            entryCueObject.transform.SetParent(root, true);
            var entryTrigger = entryCueObject.AddComponent<BoxCollider>();
            entryTrigger.isTrigger = true;
            entryTrigger.size = new Vector3(4.5f, 2.4f, 1.1f);
            var entryTriggerBody = entryCueObject.AddComponent<Rigidbody>();
            entryTriggerBody.isKinematic = true;
            entryTriggerBody.useGravity = false;
            entryCueObject.AddComponent<ChurchEntryCue>().Configure(player, progression, altarLight);

            falseElias = new GameObject("False Elias");
            falseElias.transform.position = new Vector3(0f, 0f, 65.45f);
            falseElias.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            falseElias.transform.SetParent(root, true);
            PrimitiveChild(falseElias.transform, "False Elias — reclined torso", PrimitiveType.Capsule, new Vector3(0f, 0.38f, 0f), new Vector3(0.68f, 1.15f, 0.56f), Quaternion.Euler(0f, 0f, -78f), materials.ChurchRobe);
            PrimitiveChild(falseElias.transform, "False Elias — shadowed face", PrimitiveType.Sphere, new Vector3(0.65f, 0.62f, 0.04f), new Vector3(0.43f, 0.46f, 0.4f), Quaternion.identity, materials.Enemy);
            PrimitiveChild(falseElias.transform, "False Elias — collar", PrimitiveType.Cube, new Vector3(0.38f, 0.7f, 0.01f), new Vector3(0.5f, 0.12f, 0.48f), Quaternion.Euler(0f, 0f, -68f), materials.Church);
            PrimitiveChild(falseElias.transform, "False Elias — bloodied abdomen", PrimitiveType.Sphere, new Vector3(-0.05f, 0.43f, 0.3f), new Vector3(0.34f, 0.23f, 0.055f), Quaternion.identity, materials.Blood);
            PrimitiveChild(falseElias.transform, "False Elias — arm over wound", PrimitiveType.Capsule, new Vector3(0.02f, 0.55f, 0.38f), new Vector3(0.14f, 0.6f, 0.14f), Quaternion.Euler(0f, 0f, 82f), materials.ChurchRobe);
            PrimitiveChild(falseElias.transform, "False Elias — hand at abdomen", PrimitiveType.Sphere, new Vector3(0.31f, 0.45f, 0.39f), new Vector3(0.19f, 0.13f, 0.16f), Quaternion.identity, materials.Skin);
            PrimitiveChild(falseElias.transform, "False Elias — trailing stole", PrimitiveType.Cube, new Vector3(-0.35f, 0.2f, 0.31f), new Vector3(0.15f, 0.72f, 0.035f), Quaternion.Euler(0f, 0f, -76f), materials.Blood);
            var priestCollider = falseElias.AddComponent<BoxCollider>();
            priestCollider.center = new Vector3(0f, 0.52f, 0f);
            priestCollider.size = new Vector3(1.9f, 1.25f, 0.82f);
            falseElias.AddComponent<ChurchFalseEliasEncounter>();

            var badge = BuildSheriffBadge(falseElias.transform, new Vector3(0.38f, 0.42f, 65.08f), materials.SheriffGold);
            badge.name = "Church — Elias's sheriff badge";
            Object.DestroyImmediate(badge.GetComponent<Collider>());

            returnReveal = new GameObject("Church return — Elias and the warning");
            returnReveal.transform.SetParent(root, true);

            var confessionalBack = Box("Church return — confessional back wall", new Vector3(-4.8f, 1.28f, 66f), new Vector3(2.45f, 2.55f, 0.14f), materials.Wood);
            confessionalBack.transform.SetParent(returnReveal.transform, true);
            var confessionalWest = Box("Church return — confessional west wall", new Vector3(-6.03f, 1.28f, 65f), new Vector3(0.14f, 2.55f, 2f), materials.WoodLight);
            confessionalWest.transform.SetParent(returnReveal.transform, true);
            var confessionalEast = Box("Church return — confessional east wall", new Vector3(-3.57f, 1.28f, 65f), new Vector3(0.14f, 2.55f, 2f), materials.WoodLight);
            confessionalEast.transform.SetParent(returnReveal.transform, true);
            var confessionalRoof = Box("Church return — confessional roof", new Vector3(-4.8f, 2.62f, 65f), new Vector3(2.58f, 0.18f, 2.1f), materials.Wood);
            confessionalRoof.transform.SetParent(returnReveal.transform, true);
            var openConfessionalDoor = Box("Church return — confessional door standing open", new Vector3(-3.5f, 1.25f, 64.25f), new Vector3(0.12f, 2.4f, 1.2f), materials.Wood);
            openConfessionalDoor.transform.rotation = Quaternion.Euler(0f, 64f, 0f);
            openConfessionalDoor.transform.SetParent(returnReveal.transform, true);

            var body = new GameObject("True Elias — dead inside the confessional");
            body.transform.SetParent(returnReveal.transform, false);
            body.transform.localPosition = new Vector3(-4.8f, 0f, 64.85f);
            PrimitiveChild(body.transform, "Elias — decomposing robe", PrimitiveType.Capsule, new Vector3(0f, 0.3f, 0f), new Vector3(0.55f, 0.72f, 0.43f), Quaternion.Euler(90f, 0f, 0f), materials.ChurchRobe);
            PrimitiveChild(body.transform, "Elias — still face", PrimitiveType.Sphere, new Vector3(0f, 0.26f, 0.68f), new Vector3(0.34f, 0.3f, 0.32f), Quaternion.identity, materials.Skin);
            PrimitiveChild(body.transform, "Elias — hand under the warning", PrimitiveType.Capsule, new Vector3(0.22f, 0.36f, 0.34f), new Vector3(0.08f, 0.25f, 0.08f), Quaternion.Euler(0f, 0f, 60f), materials.Skin);

            var note = Box("Church return — final note in Elias's hand", new Vector3(-4.48f, 0.4f, 65.17f), new Vector3(0.34f, 0.025f, 0.24f), materials.Linen);
            note.transform.rotation = Quaternion.Euler(8f, -8f, 12f);
            note.transform.SetParent(returnReveal.transform, true);
            note.AddComponent<ChurchReturnRevealInteractable>();

            returnReveal.SetActive(false);
            falseElias.SetActive(false);
        }

        private static void BuildChesterAndJackTrail(MaterialSet materials)
        {
            var trail = new GameObject("Blacksmith alley — Chester and Jack's trail");
            const int bootstepCount = 15;
            for (var i = 0; i < bootstepCount; i++)
            {
                var progress = i / (float)(bootstepCount - 1);
                var center = Vector3.Lerp(new Vector3(7.15f, 0f, 44.4f), new Vector3(10.6f, 0f, 34.55f), progress);
                center.x += (i % 2 == 0 ? -0.11f : 0.11f);
                var boot = Box("Chester's muddy bootprint", new Vector3(center.x, 0.018f, center.z), new Vector3(0.15f, 0.025f, 0.34f), materials.Mud);
                boot.transform.rotation = Quaternion.Euler(0f, 39f + (i % 2 == 0 ? -8f : 8f), 0f);
                boot.transform.SetParent(trail.transform, true);
                Object.DestroyImmediate(boot.GetComponent<Collider>());

                if (i % 2 != 0)
                    continue;

                var pawCenter = center + new Vector3(0.28f, 0f, -0.18f);
                AddPawPrint(trail.transform, pawCenter, materials.Mud, i);
            }

            var thread = Box("Clue — Jack's collar thread", new Vector3(7.15f, 0.76f, 44.15f), new Vector3(0.065f, 0.34f, 0.09f), materials.Blood);
            thread.transform.rotation = Quaternion.Euler(0f, -18f, 8f);
            thread.transform.SetParent(trail.transform, true);
            var trailClue = thread.AddComponent<InteractableClue>();
            trailClue.Configure(
                ChesterJackRouteState.TrailInteractionId,
                "Examinar as pegadas de bota e de pata",
                "As botas seguem pelo atalho da ferraria. Pegadas frescas de um cão acompanham o rastro até uma grade de ferro reforçada.",
                2.8f,
                false,
                DemoObjective.FollowChesterAndJack,
                string.Empty);

            var nailPost = Box("Alley trail — weathered post", new Vector3(10.82f, 0.72f, 34.46f), new Vector3(0.11f, 1.44f, 0.11f), materials.WoodLight);
            nailPost.transform.SetParent(trail.transform, true);
            Object.DestroyImmediate(nailPost.GetComponent<Collider>());
            var collarCloth = Box("Alley trail — torn red collar", new Vector3(10.72f, 0.82f, 34.4f), new Vector3(0.055f, 0.42f, 0.1f), materials.Blood);
            collarCloth.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            collarCloth.transform.SetParent(trail.transform, true);
            var routeClue = collarCloth.AddComponent<InteractableClue>();
            routeClue.Configure(
                ChesterJackRouteState.SheriffOfficeExitInteractionId,
                "Seguir Jack até o acesso da delegacia",
                "As pegadas atravessam o beco e chegam ao acesso lateral do escritório do xerife. Jack mantém o focinho baixo, farejando o caminho.",
                2.8f,
                false,
                DemoObjective.FollowChesterAndJack,
                "BECO DO FERREIRO — SAÍDA\nJack chegou com você ao escritório do xerife.");
        }

        private static AlleySetpieceParts BuildBlacksmithStealthRoute(Transform root, MaterialSet materials, FirstPersonController player)
        {
            BuildMainStreetBlockade(root, materials);
            BuildStealthCover(root, materials);

            var watcher = GameObject.Find("Watcher — blacksmith alley");
            if (watcher == null)
                throw new System.InvalidOperationException("The alley watcher was not found in the Ash Creek scene.");
            watcher.transform.position = new Vector3(9.2f, 0f, 45f);
            watcher.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var awareness = watcher.GetComponent<EnemyAwarenessAgent>();
            var eye = watcher.transform.Find("Watcher — eye point");
            if (awareness == null || eye == null)
                throw new System.InvalidOperationException("The alley watcher is missing its awareness agent or eye point.");
            awareness.Configure(player, eye);

            var forge = Box("Blacksmith — forge hearth", new Vector3(8.15f, 0.58f, 37.65f), new Vector3(1.7f, 1.16f, 1.5f), materials.Stone);
            forge.transform.SetParent(root, true);
            MarkStatic(forge);
            var anvil = Box("Blacksmith — iron anvil", new Vector3(8.45f, 0.72f, 37.15f), new Vector3(1.0f, 0.35f, 0.55f), materials.Metal);
            anvil.transform.SetParent(root, true);
            MarkStatic(anvil);
            var coalGlow = Sphere("Blacksmith — coal ember", new Vector3(8.12f, 1.11f, 37.52f), new Vector3(0.42f, 0.2f, 0.48f), materials.WarmGlow);
            coalGlow.transform.SetParent(root, true);
            var forgeLight = new GameObject("Blacksmith — localized amber forge light").AddComponent<Light>();
            forgeLight.transform.position = new Vector3(8.12f, 1.55f, 37.52f);
            forgeLight.transform.SetParent(root, true);
            forgeLight.type = LightType.Point;
            forgeLight.color = new Color(1f, 0.48f, 0.2f);
            forgeLight.intensity = 1.15f;
            forgeLight.range = 5.5f;
            forgeLight.shadows = LightShadows.None;
            forgeLight.lightmapBakeType = LightmapBakeType.Baked;

            var gateLeftPost = Box("Blacksmith — iron gate post left", new Vector3(6.72f, 1.35f, 39.65f), new Vector3(0.24f, 2.7f, 0.24f), materials.Metal);
            gateLeftPost.transform.SetParent(root, true);
            MarkStatic(gateLeftPost);
            var gateRightPost = Box("Blacksmith — iron gate post right", new Vector3(11.0f, 1.35f, 39.65f), new Vector3(0.24f, 2.7f, 0.24f), materials.Metal);
            gateRightPost.transform.SetParent(root, true);
            MarkStatic(gateRightPost);
            var gateHeader = Box("Blacksmith — iron gate lintel", new Vector3(8.86f, 2.63f, 39.65f), new Vector3(4.45f, 0.18f, 0.24f), materials.Metal);
            gateHeader.transform.SetParent(root, true);
            MarkStatic(gateHeader);

            var gateLeaf = new GameObject("Blacksmith — reinforced iron gate leaf").transform;
            gateLeaf.SetParent(root, true);
            gateLeaf.position = new Vector3(6.84f, 0f, 39.65f);
            gateLeaf.rotation = Quaternion.identity;
            for (var bar = 0; bar < 14; bar++)
            {
                var x = 0.15f + bar * 0.29f;
                var vertical = Box("Blacksmith — gate iron bar", new Vector3(0f, 0f, 0f), new Vector3(0.075f, 2.5f, 0.075f), materials.Metal);
                vertical.transform.SetParent(gateLeaf, false);
                vertical.transform.localPosition = new Vector3(x, 1.27f, 0f);
            }
            var upper = Box("Blacksmith — gate crossbar", new Vector3(0f, 0f, 0f), new Vector3(4.1f, 0.09f, 0.1f), materials.Metal);
            upper.transform.SetParent(gateLeaf, false);
            upper.transform.localPosition = new Vector3(2.05f, 2.15f, 0f);
            var lower = Box("Blacksmith — gate crossbar", new Vector3(0f, 0f, 0f), new Vector3(4.1f, 0.09f, 0.1f), materials.Metal);
            lower.transform.SetParent(gateLeaf, false);
            lower.transform.localPosition = new Vector3(2.05f, 0.3f, 0f);

            var latch = Box("Blacksmith — gate chain and latch", new Vector3(8.88f, 1.0f, 39.45f), new Vector3(0.22f, 0.42f, 0.16f), materials.WoodLight);
            latch.transform.SetParent(gateLeaf, true);
            var gate = latch.AddComponent<BlacksmithIronGate>();

            var chesterRoot = new GameObject("Chester — behind the iron gate");
            chesterRoot.transform.position = new Vector3(8.9f, 0f, 38.05f);
            chesterRoot.transform.SetParent(root, true);
            var living = new GameObject("Chester — living silhouette");
            living.transform.SetParent(chesterRoot.transform, false);
            PrimitiveChild(living.transform, "Chester — seated work coat", PrimitiveType.Capsule, new Vector3(0f, 0.79f, 0f), new Vector3(0.65f, 0.88f, 0.5f), Quaternion.Euler(0f, 0f, -12f), materials.Leather);
            PrimitiveChild(living.transform, "Chester — seated hips", PrimitiveType.Capsule, new Vector3(0f, 0.34f, 0.12f), new Vector3(0.62f, 0.44f, 0.5f), Quaternion.Euler(90f, 0f, 0f), materials.Leather);
            PrimitiveChild(living.transform, "Chester — bent left leg", PrimitiveType.Capsule, new Vector3(-0.22f, 0.2f, 0.38f), new Vector3(0.18f, 0.52f, 0.18f), Quaternion.Euler(84f, 0f, -18f), materials.Leather);
            PrimitiveChild(living.transform, "Chester — bent right leg", PrimitiveType.Capsule, new Vector3(0.22f, 0.2f, 0.4f), new Vector3(0.18f, 0.52f, 0.18f), Quaternion.Euler(82f, 0f, 16f), materials.Leather);
            PrimitiveChild(living.transform, "Chester — left boot", PrimitiveType.Cube, new Vector3(-0.26f, 0.12f, 0.7f), new Vector3(0.22f, 0.18f, 0.38f), Quaternion.Euler(0f, -8f, 0f), materials.Wood);
            PrimitiveChild(living.transform, "Chester — right boot", PrimitiveType.Cube, new Vector3(0.26f, 0.12f, 0.7f), new Vector3(0.22f, 0.18f, 0.38f), Quaternion.Euler(0f, 8f, 0f), materials.Wood);
            PrimitiveChild(living.transform, "Chester — hat brim", PrimitiveType.Cylinder, new Vector3(0.15f, 1.47f, 0.03f), new Vector3(0.48f, 0.065f, 0.42f), Quaternion.identity, materials.Wood);
            PrimitiveChild(living.transform, "Chester — hat crown", PrimitiveType.Cylinder, new Vector3(0.15f, 1.61f, 0.03f), new Vector3(0.28f, 0.2f, 0.27f), Quaternion.identity, materials.Wood);
            PrimitiveChild(living.transform, "Chester — panicked face", PrimitiveType.Sphere, new Vector3(0.14f, 1.29f, 0.25f), new Vector3(0.33f, 0.38f, 0.28f), Quaternion.identity, materials.Skin);
            PrimitiveChild(living.transform, "Chester — left eye", PrimitiveType.Sphere, new Vector3(0.04f, 1.34f, 0.49f), new Vector3(0.055f, 0.055f, 0.035f), Quaternion.identity, materials.WarmGlow);
            PrimitiveChild(living.transform, "Chester — right eye", PrimitiveType.Sphere, new Vector3(0.24f, 1.34f, 0.49f), new Vector3(0.055f, 0.055f, 0.035f), Quaternion.identity, materials.WarmGlow);
            PrimitiveChild(living.transform, "Chester — gun arm", PrimitiveType.Capsule, new Vector3(0.38f, 0.83f, 0.22f), new Vector3(0.16f, 0.62f, 0.16f), Quaternion.Euler(0f, 0f, -35f), materials.Leather);
            var chesterGun = new GameObject("Chester — one-round revolver");
            chesterGun.transform.SetParent(living.transform, false);
            chesterGun.transform.localPosition = new Vector3(0.46f, 0.56f, 0.53f);
            chesterGun.transform.localRotation = Quaternion.Euler(8f, -6f, -18f);
            PrimitiveChild(chesterGun.transform, "Chester revolver — dark iron frame", PrimitiveType.Cube, new Vector3(0f, 0f, 0.06f), new Vector3(0.09f, 0.1f, 0.26f), Quaternion.identity, materials.Metal);
            PrimitiveChild(chesterGun.transform, "Chester revolver — walnut grip", PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.035f), new Vector3(0.075f, 0.2f, 0.09f), Quaternion.Euler(0f, 0f, -12f), materials.Wood);
            PrimitiveChild(chesterGun.transform, "Chester revolver — cylinder", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, -0.025f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0f, 0f), materials.Metal);
            PrimitiveChild(chesterGun.transform, "Chester revolver — single brass cartridge", PrimitiveType.Cylinder, new Vector3(0.042f, 0.015f, -0.025f), new Vector3(0.018f, 0.042f, 0.018f), Quaternion.Euler(90f, 0f, 0f), materials.SheriffGold);

            var workshopBeam = Box("Blacksmith — overhead beam for Chester's noose", new Vector3(8.9f, 3.75f, 37.95f), new Vector3(5.2f, 0.22f, 0.24f), materials.Wood);
            workshopBeam.transform.SetParent(root, true);
            MarkStatic(workshopBeam);
            var noose = new GameObject("Chester — hanging noose from workshop beam");
            noose.transform.SetParent(root, true);
            noose.transform.position = new Vector3(9.55f, 0f, 37.6f);
            PrimitiveChild(noose.transform, "Chester noose — hanging cord", PrimitiveType.Cylinder, new Vector3(0f, 2.72f, 0f), new Vector3(0.035f, 0.88f, 0.035f), Quaternion.identity, materials.WoodLight);
            const int nooseSegments = 12;
            const float nooseRadius = 0.16f;
            for (var segment = 0; segment < nooseSegments; segment++)
            {
                var angle = segment * Mathf.PI * 2f / nooseSegments;
                var nextAngle = (segment + 1) * Mathf.PI * 2f / nooseSegments;
                var midpoint = (angle + nextAngle) * 0.5f;
                var segmentLength = nooseRadius * 2f * Mathf.Sin(Mathf.PI / nooseSegments);
                PrimitiveChild(
                    noose.transform,
                    "Chester noose — loop strand",
                    PrimitiveType.Cylinder,
                    new Vector3(Mathf.Cos(midpoint) * nooseRadius, 1.78f + Mathf.Sin(midpoint) * nooseRadius, 0f),
                    new Vector3(0.03f, segmentLength * 0.5f, 0.03f),
                    Quaternion.Euler(0f, 0f, midpoint * Mathf.Rad2Deg + 90f),
                    materials.WoodLight);
            }
            var fallen = new GameObject("Chester — still beside the anvil");
            fallen.transform.SetParent(chesterRoot.transform, false);
            PrimitiveChild(fallen.transform, "Chester — fallen work coat", PrimitiveType.Capsule, new Vector3(0.48f, 0.27f, -0.5f), new Vector3(0.62f, 0.9f, 0.5f), Quaternion.Euler(0f, 0f, 82f), materials.Leather);
            PrimitiveChild(fallen.transform, "Chester — fallen hat", PrimitiveType.Cylinder, new Vector3(0.48f, 0.12f, -1.05f), new Vector3(0.42f, 0.055f, 0.38f), Quaternion.identity, materials.Wood);
            var blood = Cylinder("Chester — dark stain under the anvil", new Vector3(0.43f, 0.012f, -0.45f), new Vector3(0.42f, 0.01f, 0.37f), materials.Blood);
            blood.transform.SetParent(fallen.transform, false);
            fallen.SetActive(false);
            var chesterCollider = chesterRoot.AddComponent<BoxCollider>();
            chesterCollider.center = new Vector3(0f, 1.0f, 0f);
            chesterCollider.size = new Vector3(0.9f, 2.0f, 0.7f);
            var chester = chesterRoot.AddComponent<ChesterEncounterInteractable>();

            var jackRoot = new GameObject("Jack — waiting beside Chester");
            jackRoot.transform.position = new Vector3(10.05f, 0f, 37.8f);
            jackRoot.transform.SetParent(root, true);
            PrimitiveChild(jackRoot.transform, "Jack — russet body", PrimitiveType.Capsule, new Vector3(0f, 0.42f, 0f), new Vector3(0.75f, 0.45f, 0.38f), Quaternion.Euler(0f, 0f, 90f), materials.WoodLight);
            PrimitiveChild(jackRoot.transform, "Jack — alert head", PrimitiveType.Sphere, new Vector3(0f, 0.64f, 0.35f), new Vector3(0.38f, 0.34f, 0.32f), Quaternion.identity, materials.WoodLight);
            PrimitiveChild(jackRoot.transform, "Jack — left ear", PrimitiveType.Cube, new Vector3(-0.16f, 0.84f, 0.38f), new Vector3(0.12f, 0.3f, 0.1f), Quaternion.Euler(0f, 0f, -18f), materials.Leather);
            PrimitiveChild(jackRoot.transform, "Jack — right ear", PrimitiveType.Cube, new Vector3(0.16f, 0.84f, 0.38f), new Vector3(0.12f, 0.3f, 0.1f), Quaternion.Euler(0f, 0f, 18f), materials.Leather);
            PrimitiveChild(jackRoot.transform, "Jack — collar", PrimitiveType.Cylinder, new Vector3(0f, 0.58f, 0.19f), new Vector3(0.33f, 0.045f, 0.3f), Quaternion.Euler(90f, 0f, 0f), materials.Blood);
            PrimitiveChild(jackRoot.transform, "Jack — left eye", PrimitiveType.Sphere, new Vector3(-0.13f, 0.69f, 0.61f), new Vector3(0.07f, 0.07f, 0.045f), Quaternion.identity, materials.WarmGlow);
            PrimitiveChild(jackRoot.transform, "Jack — right eye", PrimitiveType.Sphere, new Vector3(0.13f, 0.69f, 0.61f), new Vector3(0.07f, 0.07f, 0.045f), Quaternion.identity, materials.WarmGlow);
            var jackCollider = jackRoot.AddComponent<BoxCollider>();
            jackCollider.center = new Vector3(0f, 0.42f, 0.2f);
            jackCollider.size = new Vector3(0.82f, 0.72f, 0.86f);
            var jackAudio = jackRoot.AddComponent<AudioSource>();
            var jack = jackRoot.AddComponent<JackRescueInteractable>();

            return new AlleySetpieceParts
            {
                Gate = gate,
                GateLeaf = gateLeaf,
                Chester = chester,
                LivingChester = living,
                FallenChester = fallen,
                Jack = jack,
                JackAudio = jackAudio
            };
        }

        private static void BuildMainStreetBlockade(Transform parent, MaterialSet materials)
        {
            var fallenBeam = Box("Rubble — fallen timber blocking main street", new Vector3(0f, 0.72f, 47.2f), new Vector3(11.8f, 0.38f, 0.58f), materials.Wood);
            fallenBeam.transform.rotation = Quaternion.Euler(0f, 3f, 4f);
            fallenBeam.transform.SetParent(parent, true);
            MarkStatic(fallenBeam);
            for (var i = 0; i < 4; i++)
            {
                var plank = Box("Rubble — splintered street plank", new Vector3(-4.2f + i * 2.8f, 0.27f, 46.55f + (i % 2) * 0.36f), new Vector3(2.2f, 0.2f, 0.42f), materials.WoodLight);
                plank.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 14f : -12f, 0f);
                plank.transform.SetParent(parent, true);
                MarkStatic(plank);
            }

            for (var i = 0; i < 2; i++)
            {
                var creature = new GameObject("Blind creature — foraging in the street");
                creature.transform.position = new Vector3(-2.1f + i * 4.2f, 0f, 45.7f + (i % 2) * 0.7f);
                creature.transform.SetParent(parent, true);
                PrimitiveChild(creature.transform, "Blind creature — hunched torso", PrimitiveType.Capsule, new Vector3(0f, 0.82f, 0.12f), new Vector3(0.52f, 0.86f, 0.48f), Quaternion.Euler(18f, 0f, 0f), materials.Enemy);
                PrimitiveChild(creature.transform, "Blind creature — lowered head", PrimitiveType.Sphere, new Vector3(0f, 0.54f, 0.5f), new Vector3(0.42f, 0.38f, 0.42f), Quaternion.identity, materials.Enemy);
                PrimitiveChild(creature.transform, "Blind creature — reaching arm", PrimitiveType.Capsule, new Vector3(0.37f, 0.38f, 0.46f), new Vector3(0.14f, 0.76f, 0.14f), Quaternion.Euler(0f, 0f, 52f), materials.Enemy);
            }
        }

        private static void BuildStealthCover(Transform parent, MaterialSet materials)
        {
            var coverPositions = new[]
            {
                new Vector3(7.55f, 0.68f, 43.1f),
                new Vector3(10.06f, 0.82f, 41.4f),
                new Vector3(7.6f, 0.66f, 38.05f)
            };
            for (var i = 0; i < coverPositions.Length; i++)
            {
                var size = i == 1 ? new Vector3(1.36f, 1.64f, 1.1f) : new Vector3(1.58f, 1.36f, 1.02f);
                var crate = Box("Stealth cover — stacked supply crate", coverPositions[i], size, i == 1 ? materials.Wood : materials.WoodLight);
                crate.transform.rotation = Quaternion.Euler(0f, i * 13f - 10f, 0f);
                crate.transform.SetParent(parent, true);
                MarkStatic(crate);
                for (var plank = -1; plank <= 1; plank += 2)
                {
                    var brace = Box("Stealth cover — crate brace", coverPositions[i] + new Vector3(plank * (size.x * 0.3f), 0f, -size.z * 0.5f - 0.02f), new Vector3(0.1f, size.y * 0.92f, 0.06f), materials.Wood);
                    brace.transform.SetParent(parent, true);
                    MarkStatic(brace);
                }
            }

            var sideSign = Box("Blacksmith alley — route sign", new Vector3(6.45f, 2.1f, 44.25f), new Vector3(0.18f, 0.68f, 1.7f), materials.Wood);
            sideSign.transform.SetParent(parent, true);
            MarkStatic(sideSign);
            var signText = AddText("FERRARIA  →", new Vector3(6.32f, 2.1f, 44.25f), 0.1f, new Color(0.8f, 0.68f, 0.5f), 90f);
            signText.transform.SetParent(parent, true);
        }

        private static void AddPawPrint(Transform parent, Vector3 center, Material material, int index)
        {
            var pad = Cylinder("Jack's muddy pawprint", new Vector3(center.x, 0.016f, center.z), new Vector3(0.13f, 0.012f, 0.13f), material);
            pad.transform.SetParent(parent, true);
            Object.DestroyImmediate(pad.GetComponent<Collider>());

            var direction = index % 4 == 0 ? 1f : -1f;
            for (var toe = 0; toe < 3; toe++)
            {
                var side = toe - 1;
                var toeMark = Sphere(
                    "Jack's pawprint — toe",
                    new Vector3(center.x + side * 0.075f, 0.02f, center.z + direction * (0.105f + (side == 0 ? 0.025f : 0f))),
                    new Vector3(0.055f, 0.025f, 0.07f),
                    material);
                toeMark.transform.SetParent(parent, true);
                Object.DestroyImmediate(toeMark.GetComponent<Collider>());
            }
        }

        private static GameObject BuildSheriffBadge(Transform parent, Vector3 position, Material material)
        {
            const int pointCount = 10;
            const float outerRadius = 0.35f;
            const float innerRadius = 0.16f;
            const float halfThickness = 0.045f;
            var vertices = new Vector3[pointCount * 2 + 2];
            for (var i = 0; i < pointCount; i++)
            {
                var angle = -Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                var radius = i % 2 == 0 ? outerRadius : innerRadius;
                var x = Mathf.Cos(angle) * radius;
                var y = Mathf.Sin(angle) * radius;
                vertices[i] = new Vector3(x, y, halfThickness);
                vertices[i + pointCount] = new Vector3(x, y, -halfThickness);
            }
            vertices[pointCount * 2] = new Vector3(0f, 0f, halfThickness);
            vertices[pointCount * 2 + 1] = new Vector3(0f, 0f, -halfThickness);

            var triangles = new int[pointCount * 12];
            var index = 0;
            for (var i = 0; i < pointCount; i++)
            {
                var next = (i + 1) % pointCount;
                triangles[index++] = pointCount * 2;
                triangles[index++] = i;
                triangles[index++] = next;
                triangles[index++] = pointCount * 2 + 1;
                triangles[index++] = next + pointCount;
                triangles[index++] = i + pointCount;
                triangles[index++] = i;
                triangles[index++] = i + pointCount;
                triangles[index++] = next + pointCount;
                triangles[index++] = i;
                triangles[index++] = next + pointCount;
                triangles[index++] = next;
            }

            var badgeMesh = new Mesh { name = "Sheriff's five-point star" };
            badgeMesh.vertices = vertices;
            badgeMesh.triangles = triangles;
            badgeMesh.RecalculateNormals();
            badgeMesh.RecalculateBounds();

            var badge = new GameObject("Church investigation — Chester's sheriff badge");
            badge.transform.position = position;
            badge.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            badge.transform.SetParent(parent, true);
            badge.AddComponent<MeshFilter>().sharedMesh = badgeMesh;
            badge.AddComponent<MeshRenderer>().sharedMaterial = material;
            badge.AddComponent<MeshCollider>().sharedMesh = badgeMesh;
            return badge;
        }

        private static void ConfigureSaloonWindows(Scene scene, Material transparentGlass)
        {
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                foreach (var renderer in rootObject.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.gameObject.name.StartsWith("Saloon — front window", System.StringComparison.Ordinal))
                        continue;
                    renderer.sharedMaterial = transparentGlass;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static void BuildSaloonInteriorAndClues(MaterialSet materials)
        {
            BuildSaloonDriedBloodStains(materials);

            var bar = Box("Saloon — bar counter", new Vector3(-15f, 0.85f, 37.1f), new Vector3(3.5f, 1.45f, 1.1f), materials.WoodLight);
            MarkStatic(bar);

            var knife = Box("Clue — knife", new Vector3(-15f, 1.62f, 37.1f), new Vector3(0.85f, 0.06f, 0.13f), materials.Metal);
            knife.transform.rotation = Quaternion.Euler(0f, 47f, 0f);
            var knifeClue = knife.AddComponent<InteractableClue>();
            knifeClue.Configure("saloon.knife", "Examinar a faca", "Uma faca está cravada sobre o balcão destruído. Ela desbloqueia o ataque corpo a corpo emergencial.", 2.8f, true, DemoObjective.ExamineSaloonKnife);

            for (var step = 1; step <= 14; step++)
            {
                var stepHeight = 0.24f * step;
                var stair = Box("Saloon — blocked staircase", new Vector3(-21.5f, stepHeight * 0.5f, 35.2f + (step - 1) * 0.34f), new Vector3(2f, stepHeight, 0.58f), materials.Wood);
                MarkStatic(stair);
            }

            var upperFloor = Box("Saloon — second-floor landing", new Vector3(-17f, 3.45f, 42.65f), new Vector3(11.2f, 0.18f, 5.1f), materials.Wood);
            MarkStatic(upperFloor);
            var noteTable = Box("Saloon — upstairs table", new Vector3(-19f, 3.86f, 42f), new Vector3(2.2f, 0.64f, 1.1f), materials.WoodLight);
            MarkStatic(noteTable);

            var note = Box("Clue — Carmen and Miss Moses note", new Vector3(-19f, 4.22f, 42f), new Vector3(0.72f, 0.045f, 0.56f), materials.WoodLight);
            note.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
            var clue = note.AddComponent<InteractableClue>();
            clue.Configure("saloon.torn-note", "Ler a anotação", CarmenNoteText, 2.8f, false, DemoObjective.InvestigateSaloonClues);

            BuildSaloonInvestigationSetpiece(materials);
        }

        private static void BuildTrees(MaterialSet materials)
        {
            var positions = new[]
            {
                new Vector3(-31f, 0f, 24f), new Vector3(31f, 0f, 27f), new Vector3(-35f, 0f, 46f),
                new Vector3(34f, 0f, 51f), new Vector3(-30f, 0f, 70f), new Vector3(32f, 0f, 78f),
                new Vector3(-22f, 0f, 98f), new Vector3(24f, 0f, 103f), new Vector3(-13f, 0f, 119f),
                new Vector3(15f, 0f, 124f), new Vector3(-37f, 0f, 129f), new Vector3(38f, 0f, 132f)
            };
            for (var i = 0; i < positions.Length; i++)
                CreateTree(positions[i], 0.82f + (i % 4) * 0.14f, materials);

            BuildEntranceForestFrame(materials);
        }

        private static void BuildEntranceForestFrame(MaterialSet materials, bool mapOriented = false)
        {
            var frame = new GameObject(EntranceForestFrameName);
            if (mapOriented)
                frame.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            var positions = new[]
            {
                new Vector3(-12.5f, 0f, -9f), new Vector3(13.5f, 0f, -5f),
                new Vector3(-14f, 0f, 2f), new Vector3(12f, 0f, 7f),
                new Vector3(-13f, 0f, 14f), new Vector3(15f, 0f, 18f),
                new Vector3(-16f, 0f, 26f), new Vector3(17f, 0f, 30f)
            };

            for (var i = 0; i < positions.Length; i++)
                CreateTree(positions[i], 0.78f + (i % 4) * 0.09f, materials, frame.transform, "Entrance forest");

            if (mapOriented)
            {
                var marker = new GameObject(MapOrientationMarkerName) { hideFlags = HideFlags.HideInHierarchy };
                marker.transform.SetParent(frame.transform, false);
            }
        }

        private static void CreateTree(Vector3 position, float scale, MaterialSet materials, Transform parent = null, string namePrefix = "Forest")
        {
            var trunk = Cylinder(namePrefix + " — trunk", position + Vector3.up * (2.1f * scale), new Vector3(0.42f, 2.1f, 0.42f) * scale, materials.Wood);
            if (parent != null)
                trunk.transform.SetParent(parent, false);
            MarkStatic(trunk);
            var crown = Sphere(namePrefix + " — canopy", position + Vector3.up * (4.7f * scale), new Vector3(4.3f, 3.5f, 4.1f) * scale, materials.Foliage);
            if (parent != null)
                crown.transform.SetParent(parent, false);
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            MarkStatic(crown);
            var crown2 = Sphere(namePrefix + " — canopy", position + Vector3.up * (5.6f * scale) + Vector3.right * (1.2f * scale), new Vector3(2.8f, 2.7f, 2.8f) * scale, materials.Foliage);
            if (parent != null)
                crown2.transform.SetParent(parent, false);
            Object.DestroyImmediate(crown2.GetComponent<Collider>());
            MarkStatic(crown2);
        }

        private static void BuildLocalMist()
        {
            CreateMist("Mist field — localized layers", new Vector3(0f, 0.2f, 80f), new Vector3(24f, 1.3f, 24f), 5f, 2f);
            CreateMist("Forest — localized layers", new Vector3(0f, 0.4f, 119f), new Vector3(34f, 1.8f, 24f), 7f, 3f);
        }

        private static void CreateMist(string name, Vector3 position, Vector3 boxSize, float particleSize, float particlesPerSecond)
        {
            var mist = new GameObject(name);
            mist.transform.position = position;
            var system = mist.AddComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.duration = 14f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 18f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.015f, 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.65f, particleSize);
            main.startColor = new Color(0.64f, 0.71f, 0.82f, 0.09f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 10;

            var emission = system.emission;
            emission.rateOverTime = particlesPerSecond;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.025f, 0.025f);
            velocity.z = new ParticleSystem.MinMaxCurve(0.025f, 0.12f);

            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.64f, 0.71f, 0.82f), 0f), new GradientColorKey(new Color(0.5f, 0.58f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.09f, 0.28f), new GradientAlphaKey(0.07f, 0.72f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = mist.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
                renderer.sharedMaterial = new Material(shader) { name = name + " material", color = new Color(0.64f, 0.71f, 0.82f, 0.15f) };
        }

        private static PlayerRig BuildPlayer()
        {
            var progression = new GameObject("Demo Progression").AddComponent<DemoProgressionComponent>();
            var playerObject = new GameObject("Player — Investigator");
            playerObject.transform.position = new Vector3(0f, 0.05f, 2.2f);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            var cameraObject = new GameObject("First person camera");
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.57f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 150f;
            camera.backgroundColor = new Color(0.11f, 0.14f, 0.2f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.renderShadows = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var firstPersonController = playerObject.AddComponent<FirstPersonController>();
            firstPersonController.ConfigureViewCamera(camera);
            var openingLine = playerObject.AddComponent<ScreenplayOpeningLine>();
            playerObject.AddComponent<SheriffBadgeInventory>();
            playerObject.AddComponent<CombatKnifeInventory>();
            playerObject.AddComponent<BarnKeyInventory>();
            playerObject.AddComponent<SavingShotInventory>();
            var journal = playerObject.AddComponent<PlayerJournalComponent>();
            journal.Configure(string.Empty);
            var handAnchor = new GameObject("Lantern hand anchor").transform;
            handAnchor.SetParent(cameraObject.transform, false);
            handAnchor.localPosition = new Vector3(0.34f, -0.34f, 0.52f);
            handAnchor.localRotation = Quaternion.Euler(22f, 0f, 0f);
            handAnchor.localScale = Vector3.one * 0.72f;
            var interactor = playerObject.AddComponent<PlayerInteractor>();
            interactor.Configure(camera, progression, journal);
            var grants = playerObject.AddComponent<ScreenplayItemGrantTracker>();
            grants.Configure(interactor, playerObject.GetComponent<CombatKnifeInventory>(), playerObject.GetComponent<BarnKeyInventory>());
            return new PlayerRig
            {
                LanternHandAnchor = handAnchor,
                PlayerRoot = playerObject.transform,
                Controller = firstPersonController,
                OpeningLine = openingLine
            };
        }

        private static void ConfigureLukeLanternLight(Light lamp, Transform luke)
        {
            if (lamp == null)
                return;

            lamp.type = LightType.Spot;
            lamp.color = new Color(1f, 0.63f, 0.34f);
            lamp.intensity = 7.5f;
            lamp.range = 16f;
            lamp.spotAngle = 62f;
            lamp.innerSpotAngle = 42f;
            lamp.shadows = LightShadows.None;
            if (luke != null)
            {
                var aimPoint = luke.position + Vector3.up * 0.38f;
                lamp.transform.rotation = Quaternion.LookRotation(aimPoint - lamp.transform.position);
            }
        }

        private static void BuildLukeAndArrivalTrail(MaterialSet materials, PlayerRig player)
        {
            var luke = new GameObject("Luke — wounded at the gate");
            luke.transform.position = new Vector3(-1.35f, 0f, 9.3f);
            PrimitiveChild(luke.transform, "Luke — fallen coat", PrimitiveType.Capsule, new Vector3(0f, 0.31f, 0f), new Vector3(0.42f, 1.05f, 0.42f), Quaternion.Euler(0f, 0f, 90f), materials.LukeCloth);
            PrimitiveChild(luke.transform, "Luke — head", PrimitiveType.Sphere, new Vector3(0.78f, 0.37f, 0.02f), new Vector3(0.34f, 0.31f, 0.32f), Quaternion.identity, materials.Skin);
            PrimitiveChild(luke.transform, "Luke — hat brim", PrimitiveType.Cylinder, new Vector3(0.8f, 0.55f, 0.02f), new Vector3(0.46f, 0.055f, 0.42f), Quaternion.identity, materials.Leather);
            PrimitiveChild(luke.transform, "Luke — hat crown", PrimitiveType.Cylinder, new Vector3(0.8f, 0.63f, 0.02f), new Vector3(0.27f, 0.15f, 0.27f), Quaternion.identity, materials.Leather);
            PrimitiveChild(luke.transform, "Luke — left boot", PrimitiveType.Cube, new Vector3(-0.65f, 0.15f, -0.27f), new Vector3(0.48f, 0.22f, 0.22f), Quaternion.Euler(0f, 7f, 0f), materials.Leather);
            PrimitiveChild(luke.transform, "Luke — right boot", PrimitiveType.Cube, new Vector3(-0.65f, 0.15f, 0.27f), new Vector3(0.48f, 0.22f, 0.22f), Quaternion.Euler(0f, -8f, 0f), materials.Leather);
            var reachingArm = new GameObject("Luke — reaching arm pivot");
            reachingArm.transform.SetParent(luke.transform, false);
            reachingArm.transform.localPosition = new Vector3(0.12f, 0.13f, 0.34f);
            reachingArm.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            PrimitiveChild(reachingArm.transform, "Luke — wounded forearm", PrimitiveType.Capsule, new Vector3(0f, 0.25f, 0f), new Vector3(0.14f, 0.42f, 0.14f), Quaternion.identity, materials.LukeCloth);
            PrimitiveChild(reachingArm.transform, "Luke — reaching hand", PrimitiveType.Sphere, new Vector3(0f, 0.51f, 0.015f), new Vector3(0.18f, 0.16f, 0.14f), Quaternion.identity, materials.Skin);
            foreach (var armCollider in reachingArm.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(armCollider);

            if (player.PlayerRoot != null && player.Controller != null && player.OpeningLine != null)
            {
                var oldHorse = player.PlayerRoot.Find("Arrival horse — Ash Creek mount");
                if (oldHorse != null)
                    Object.DestroyImmediate(oldHorse.gameObject);
                var arrivalHorse = BuildArrivalHorse(player.PlayerRoot, materials);
                player.OpeningLine.ConfigureArrival(player.Controller, arrivalHorse, luke.transform, reachingArm.transform);
            }
            var blood = Cylinder("Luke — blood on the road", new Vector3(-1.1f, MainStreetSurfaceY + 0.008f, 9.55f), new Vector3(0.58f, 0.014f, 0.74f), materials.Blood);
            blood.transform.rotation = Quaternion.Euler(0f, 21f, 0f);
            MarkStatic(blood);

            var lantern = new GameObject("Luke's oil lantern");
            lantern.transform.position = new Vector3(-0.25f, 0.48f, 10f);
            var collider = lantern.AddComponent<SphereCollider>();
            collider.radius = 0.46f;
            PrimitiveChild(lantern.transform, "Lantern — metal base", PrimitiveType.Cylinder, new Vector3(0f, -0.25f, 0f), new Vector3(0.28f, 0.055f, 0.26f), Quaternion.identity, materials.Metal);
            PrimitiveChild(lantern.transform, "Lantern — amber globe", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.34f, 0.2f), Quaternion.identity, materials.WarmGlow);
            PrimitiveChild(lantern.transform, "Lantern — top cap", PrimitiveType.Cylinder, new Vector3(0f, 0.22f, 0f), new Vector3(0.29f, 0.055f, 0.27f), Quaternion.identity, materials.Metal);
            PrimitiveChild(lantern.transform, "Lantern — handle sides", PrimitiveType.Cube, new Vector3(-0.12f, 0.38f, 0f), new Vector3(0.035f, 0.27f, 0.04f), Quaternion.identity, materials.Metal);
            PrimitiveChild(lantern.transform, "Lantern — handle sides", PrimitiveType.Cube, new Vector3(0.12f, 0.38f, 0f), new Vector3(0.035f, 0.27f, 0.04f), Quaternion.identity, materials.Metal);
            PrimitiveChild(lantern.transform, "Lantern — handle crown", PrimitiveType.Cube, new Vector3(0f, 0.51f, 0f), new Vector3(0.27f, 0.035f, 0.04f), Quaternion.identity, materials.Metal);
            var globe = lantern.transform.Find("Lantern — amber globe").GetComponent<Renderer>();
            var lamp = new GameObject("Lamp beam — warm, no realtime shadows").AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.transform.SetParent(lantern.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0.06f, 0.12f);
            ConfigureLukeLanternLight(lamp, luke.transform);
            var pickup = lantern.AddComponent<HandLanternPickup>();
            pickup.Configure(2.8f, player.LanternHandAnchor, collider, lamp, globe, materials.WarmGlow, materials.WarmGlassDim, reachingArm.transform);

            BuildGateBootprints(materials);
        }

        private static Transform BuildArrivalHorse(Transform rider, MaterialSet materials)
        {
            var horse = new GameObject("Arrival horse — Ash Creek mount");
            horse.transform.SetParent(rider, false);
            horse.transform.localPosition = new Vector3(0f, -0.6f, 0.24f);
            PrimitiveChild(horse.transform, "Arrival horse — bay body", PrimitiveType.Capsule, new Vector3(0f, 0.56f, 0.02f), new Vector3(0.62f, 0.56f, 1.22f), Quaternion.Euler(90f, 0f, 0f), materials.Leather);
            PrimitiveChild(horse.transform, "Arrival horse — shoulder", PrimitiveType.Sphere, new Vector3(0f, 0.83f, 0.55f), new Vector3(0.48f, 0.66f, 0.54f), Quaternion.identity, materials.Leather);
            PrimitiveChild(horse.transform, "Arrival horse — neck", PrimitiveType.Capsule, new Vector3(0f, 1.08f, 0.84f), new Vector3(0.32f, 0.78f, 0.34f), Quaternion.Euler(27f, 0f, 0f), materials.Leather);
            PrimitiveChild(horse.transform, "Arrival horse — head", PrimitiveType.Capsule, new Vector3(0f, 1.34f, 1.2f), new Vector3(0.31f, 0.55f, 0.3f), Quaternion.Euler(28f, 0f, 0f), materials.Wood);
            PrimitiveChild(horse.transform, "Arrival horse — muzzle", PrimitiveType.Sphere, new Vector3(0f, 1.15f, 1.47f), new Vector3(0.29f, 0.25f, 0.31f), Quaternion.identity, materials.WoodLight);
            PrimitiveChild(horse.transform, "Arrival horse — mane", PrimitiveType.Capsule, new Vector3(0f, 1.54f, 0.93f), new Vector3(0.13f, 0.54f, 0.16f), Quaternion.Euler(30f, 0f, 0f), materials.Wood);
            PrimitiveChild(horse.transform, "Arrival horse — left ear", PrimitiveType.Cube, new Vector3(-0.13f, 1.73f, 1.17f), new Vector3(0.08f, 0.26f, 0.08f), Quaternion.Euler(0f, 0f, -12f), materials.Leather);
            PrimitiveChild(horse.transform, "Arrival horse — right ear", PrimitiveType.Cube, new Vector3(0.13f, 1.73f, 1.17f), new Vector3(0.08f, 0.26f, 0.08f), Quaternion.Euler(0f, 0f, 12f), materials.Leather);
            PrimitiveChild(horse.transform, "Arrival horse — saddle", PrimitiveType.Cube, new Vector3(0f, 0.94f, -0.03f), new Vector3(0.55f, 0.16f, 0.5f), Quaternion.identity, materials.Wood);
            PrimitiveChild(horse.transform, "Arrival horse — saddle horn", PrimitiveType.Cylinder, new Vector3(0f, 1.08f, 0.09f), new Vector3(0.09f, 0.12f, 0.09f), Quaternion.identity, materials.WoodLight);
            PrimitiveChild(horse.transform, "Arrival horse — bridle", PrimitiveType.Cylinder, new Vector3(0f, 1.28f, 1.35f), new Vector3(0.36f, 0.035f, 0.36f), Quaternion.Euler(90f, 0f, 0f), materials.Metal);
            foreach (var collider in horse.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
            return horse.transform;
        }

        private static void BuildGateBootprints(MaterialSet materials)
        {
            const int printCount = 20;
            for (var i = 0; i < printCount; i++)
            {
                var z = 15.1f + i * 0.82f;
                var side = i % 2 == 0 ? -0.14f : 0.14f;
                var x = -0.9f + Mathf.Sin(i * 0.57f) * 0.42f + side;
                var footprint = Box("Clue — bootprints from gate to well", new Vector3(x, MainStreetSurfaceY + 0.008f, z), new Vector3(0.18f, 0.016f, 0.39f), materials.Mud);
                footprint.transform.rotation = Quaternion.Euler(0f, i % 2 == 0 ? -7f : 8f, 0f);
                MarkStatic(footprint);

                if (i != 0)
                    continue;

                var clue = footprint.AddComponent<InteractableClue>();
                clue.Configure("gate.bootprints", "Examinar pegadas recentes", "Pegadas enlameadas deixam o portão e seguem rua adentro, na direção do poço central. Há marcas menores misturadas às botas pesadas.", 2.8f, false, DemoObjective.FollowBootprintsToSaloon, "PEGADAS — PORTÃO\nPegadas enlameadas seguem do portão para o poço central. Há marcas menores misturadas às botas pesadas.");
            }
        }

        private static GameObject PrimitiveChild(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            var child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            child.transform.localRotation = localRotation;
            child.GetComponent<Renderer>().sharedMaterial = material;
            var collider = child.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            return child;
        }

        private static void BuildEnemy(MaterialSet materials)
        {
            var enemy = new GameObject("Watcher — blacksmith alley");
            enemy.transform.position = new Vector3(9.2f, 0f, 45f);
            enemy.transform.rotation = Quaternion.Euler(0f, 205f, 0f);
            var torso = Capsule("Watcher — silhouette", enemy.transform.position + Vector3.up * 1.15f, new Vector3(0.58f, 1.12f, 0.46f), materials.Enemy);
            torso.transform.SetParent(enemy.transform, true);
            var head = Sphere("Watcher — head", enemy.transform.position + Vector3.up * 2.25f, new Vector3(0.42f, 0.48f, 0.42f), materials.Enemy);
            head.transform.SetParent(enemy.transform, true);
            var eyePoint = new GameObject("Watcher — eye point").transform;
            eyePoint.SetParent(enemy.transform, false);
            eyePoint.localPosition = new Vector3(0f, 1.85f, 0.18f);
            var awareness = enemy.AddComponent<EnemyAwarenessAgent>();
            awareness.Configure(Object.FindFirstObjectByType<FirstPersonController>(), eyePoint);
        }

        private static void BuildVolume()
        {
            var profilePath = RenderingPath + "/AshCreekVolumeProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "Ash Creek — cold night / amber practicals";
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            else
            {
                for (var i = profile.components.Count - 1; i >= 0; i--)
                {
                    var existing = profile.components[i];
                    profile.components.RemoveAt(i);
                    if (existing != null)
                        Object.DestroyImmediate(existing, true);
                }
                EditorUtility.SetDirty(profile);
            }

            var color = VolumeProfileFactory.CreateVolumeComponent<ColorAdjustments>(profile, true, false);
            color.postExposure.Override(0.35f);
            color.contrast.Override(8f);
            color.saturation.Override(-5f);
            var tone = VolumeProfileFactory.CreateVolumeComponent<Tonemapping>(profile, true, false);
            tone.mode.Override(TonemappingMode.ACES);
            var vignette = VolumeProfileFactory.CreateVolumeComponent<Vignette>(profile, true, false);
            vignette.intensity.Override(0.06f);
            vignette.smoothness.Override(0.5f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            var volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null)
                volume = new GameObject("Global volume — restrained western night").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;
        }

        private static void CreateLamp(Vector3 basePosition, MaterialSet materials)
        {
            var post = Cylinder("Street lamp — post", basePosition + Vector3.up * 2.4f, new Vector3(0.12f, 2.4f, 0.12f), materials.Metal);
            MarkStatic(post);
            var arm = Box("Street lamp — arm", basePosition + new Vector3(0.4f, 4.6f, 0f), new Vector3(0.9f, 0.12f, 0.12f), materials.Metal);
            MarkStatic(arm);
            var glass = Sphere("Street lamp — amber glass", basePosition + new Vector3(0.78f, 4.35f, 0f), new Vector3(0.28f, 0.42f, 0.28f), materials.WarmGlow);
            MarkStatic(glass);
            var light = new GameObject("Warm practical — no realtime shadows").AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.58f, 0.31f);
            light.intensity = 0.75f;
            light.range = 8f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.transform.position = basePosition + new Vector3(0.78f, 4.3f, 0f);
        }

        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = size;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject Cylinder(string name, Vector3 position, Vector3 size, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = size;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject Sphere(string name, Vector3 position, Vector3 size, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = size;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject Capsule(string name, Vector3 position, Vector3 size, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = size;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject AddText(string text, Vector3 position, float size, Color color, float rotationY)
        {
            var gameObject = new GameObject("Sign — " + text);
            gameObject.transform.position = position;
            gameObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            var mesh = gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size * 0.62f;
            mesh.fontSize = 48;
            var marker = new GameObject("Readable text size v3 applied")
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            marker.transform.SetParent(gameObject.transform, false);
            mesh.color = color;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mesh.font != null)
                gameObject.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            return gameObject;
        }

        private static void MarkStatic(GameObject gameObject)
        {
            gameObject.isStatic = true;
            GameObjectUtility.SetStaticEditorFlags(gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI);
        }

        private sealed class MaterialSet
        {
            public Material Ground;
            public Material Road;
            public Material Wood;
            public Material WoodLight;
            public Material Roof;
            public Material Barn;
            public Material Stone;
            public Material Church;
            public Material Window;
            public Material SaloonWindow;
            public Material Metal;
            public Material Blood;
            public Material Linen;
            public Material Mud;
            public Material Foliage;
            public Material WarmGlow;
            public Material ChurchRobe;
            public Material SheriffGold;
            public Material Enemy;
            public Material LukeCloth;
            public Material Skin;
            public Material Leather;
            public Material Hay;
            public Material PaleSkin;
            public Material GideonCloth;
            public Material GideonSkin;
            public Material WarmGlassDim;
        }

        private sealed class PlayerRig
        {
            public Transform LanternHandAnchor;
            public Transform PlayerRoot;
            public FirstPersonController Controller;
            public ScreenplayOpeningLine OpeningLine;
        }

        private sealed class AlleySetpieceParts
        {
            public BlacksmithIronGate Gate;
            public Transform GateLeaf;
            public ChesterEncounterInteractable Chester;
            public GameObject LivingChester;
            public GameObject FallenChester;
            public JackRescueInteractable Jack;
            public AudioSource JackAudio;
        }
    }
}
