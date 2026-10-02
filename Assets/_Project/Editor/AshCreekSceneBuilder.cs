using System.IO;
using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Enemies;
using ForgottenTrail.Gameplay.Journal;
using ForgottenTrail.Gameplay.Lantern;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
using ForgottenTrail.Gameplay.Saloon;
using ForgottenTrail.Gameplay.World;
using UnityEditor;
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
            ConfigureSaloonWindows(scene, materials.SaloonWindow);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("The saloon investigation, apparition, and exit beat were updated in " + ScenePath);
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

            var journal = playerObject.GetComponent<PlayerJournalComponent>();
            if (journal == null)
                journal = playerObject.AddComponent<PlayerJournalComponent>();
            journal.Configure("LAYLA — A BUSCA\nVocê chegou a Ash Creek seguindo o rastro de Layla. O portão está aberto, mas não há sinal de vida na rua.");

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

            BuildLukeAndArrivalTrail(materials, new PlayerRig { LanternHandAnchor = handAnchor });

            var wellTrailClue = FindClueById(scene, "saloon.boot-trail");
            if (wellTrailClue == null)
                throw new System.InvalidOperationException("The existing well-to-saloon boot trail was not found.");
            wellTrailClue.Configure("saloon.boot-trail", "Examinar as marcas de botas", "Marcas de botas de garimpeiros foram arrastadas do poço pela rua principal, na direção do saloon.", 2.8f, true, DemoObjective.FollowBootprintsToSaloon, "RASTRO — DO POÇO AO SALOON\nAs marcas de botas deixam o poço e seguem pela rua principal, em direção ao saloon.");
            EditorUtility.SetDirty(wellTrailClue);

            var saloonNoteClue = FindClueById(scene, "saloon.torn-note");
            if (saloonNoteClue == null)
                throw new System.InvalidOperationException("The existing saloon note clue was not found.");
            saloonNoteClue.Configure("saloon.torn-note", "Ler a anotação", "Carmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.", 2.8f, false, DemoObjective.InvestigateSaloonClues, "ANOTAÇÃO RASGADA\nCarmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.");
            EditorUtility.SetDirty(saloonNoteClue);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Luke, the portable lantern, the arrival trail, and journal progression were added to " + ScenePath);
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

        private static void ConfigureEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.17f, 0.23f);
            RenderSettings.ambientIntensity = 0.8f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.17f, 0.20f, 0.27f);
            RenderSettings.fogDensity = 0.009f;

            var moon = new GameObject("Moonlight — cool key").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.53f, 0.63f, 0.82f);
            moon.intensity = 0.42f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.68f;
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
                Mud = MakeMaterial("Clue — boot marks in mud", new Color(0.095f, 0.085f, 0.075f), 0f, 0.08f),
                Foliage = MakeMaterial("Foliage — night pine", new Color(0.09f, 0.14f, 0.13f), 0f, 0.36f),
                WarmGlow = MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f),
                Enemy = MakeMaterial("Figure — near-black cloth", new Color(0.08f, 0.085f, 0.10f), 0f, 0.25f),
                LukeCloth = MakeMaterial("Luke — dusty work coat", new Color(0.25f, 0.20f, 0.15f), 0f, 0.32f),
                Skin = MakeMaterial("Luke — pale skin", new Color(0.39f, 0.29f, 0.23f), 0f, 0.36f),
                Leather = MakeMaterial("Luke — worn leather", new Color(0.16f, 0.12f, 0.09f), 0f, 0.28f),
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
            var ground = Box("Ash Creek ground", new Vector3(0f, -0.3f, 55f), new Vector3(100f, 0.6f, 150f), materials.Ground);
            MarkStatic(ground);

            var street = Box("Main street", new Vector3(0f, MainStreetSurfaceY - MainStreetThickness * 0.5f, 48f), new Vector3(MainStreetWidth, MainStreetThickness, 92f), materials.Road);
            MarkStatic(street);

            for (var i = 0; i < 9; i++)
            {
                var z = 14f + i * 9.4f;
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
            var back = Box(name + " — rear wall", new Vector3(origin.x, wallCenterY, origin.z + depth * 0.5f - wallThickness * 0.5f), new Vector3(width, height, wallThickness), wall);
            var left = Box(name + " — left wall", new Vector3(origin.x - width * 0.5f + wallThickness * 0.5f, wallCenterY, origin.z), new Vector3(wallThickness, height, depth), wall);
            var right = Box(name + " — right wall", new Vector3(origin.x + width * 0.5f - wallThickness * 0.5f, wallCenterY, origin.z), new Vector3(wallThickness, height, depth), wall);
            var frontZ = origin.z - depth * 0.5f + wallThickness * 0.5f;
            var doorWidth = name == "Church" ? 2.2f : 2.0f;
            var frontWidth = (width - doorWidth) * 0.5f;
            var frontLeft = Box(name + " — front wall left", new Vector3(origin.x - (doorWidth + frontWidth) * 0.5f, wallCenterY, frontZ), new Vector3(frontWidth, height, wallThickness), wall);
            var frontRight = Box(name + " — front wall right", new Vector3(origin.x + (doorWidth + frontWidth) * 0.5f, wallCenterY, frontZ), new Vector3(frontWidth, height, wallThickness), wall);
            foreach (var part in new[] { back, left, right, frontLeft, frontRight }) MarkStatic(part);

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
                    var slat = Box("Barn — front timber", new Vector3(origin.x + i * 2.4f, 3.4f, frontZ - 0.15f), new Vector3(0.22f, 6.8f, 0.25f), materials.WoodLight);
                    MarkStatic(slat);
                }
                var arenaDoor = Box("Barn — open arena", new Vector3(origin.x, 2.8f, frontZ - 0.23f), new Vector3(6.1f, 5.6f, 0.1f), materials.Enemy);
                MarkStatic(arenaDoor);
            }
        }

        private static void BuildBootTrailByWell(MaterialSet materials)
        {
            const int footprintCount = 15;
            for (var i = 0; i < footprintCount; i++)
            {
                var progress = i / (float)(footprintCount - 1);
                var x = Mathf.Lerp(-2.2f, -11.55f, progress);
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
                var position = new Vector3(x, supportHeight + size.y * 0.5f, Mathf.Lerp(33.45f, 34.05f, progress) + side);
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
                if (i == 0)
                {
                    var clue = stain.AddComponent<InteractableClue>();
                    clue.Configure("saloon.blood-trail", "Examinar o sangue seco", "O sangue foi arrastado para longe do balcão, em direção à porta dos fundos. A trilha termina antes de alcançar a rua.", 2.8f, false, DemoObjective.InvestigateSaloonClues, "SALÃO — SANGUE SECO\nUma trilha de sangue segue do balcão até a porta dos fundos e termina ali.");
                }
            }
        }

        private static void ConfigureSaloonClues(InteractableClue note, InteractableClue knife)
        {
            note.Configure(
                SaloonApparitionState.NoteInteractionId,
                "Ler a anotação",
                "Carmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.",
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                "ANOTAÇÃO RASGADA\nCarmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.");
            knife.Configure(
                "saloon.knife",
                "Examinar a faca",
                "A faca está presa no balcão destruído. É uma pista, não uma arma pronta para combate.",
                2.8f,
                true,
                DemoObjective.ExamineSaloonKnife,
                "SALÃO — A FACA\nUma faca está presa no balcão destruído. A empunhadura está gasta; você a registra como evidência.");
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
                        "Pegadas enlameadas cruzam o salão e sobem pela escada parcialmente bloqueada.",
                        2.8f,
                        false,
                        DemoObjective.InvestigateSaloonClues,
                        "SALÃO — PEGADAS\nPegadas enlameadas cruzam o assoalho e sobem para o andar de cima.");
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
                "Uma mesa foi virada com força; lascas e vidro se espalham pelo chão. As marcas não parecem antigas.",
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                "SALÃO — MÓVEIS DESTRUÍDOS\nUma mesa virada e vidro quebrado indicam que houve uma luta recente.");

            PrimitiveChild(table.transform, "Broken furniture — snapped leg", PrimitiveType.Cube, new Vector3(0.56f, -0.3f, 0.38f), new Vector3(0.16f, 0.88f, 0.16f), Quaternion.Euler(0f, 0f, -37f), materials.Wood);
            PrimitiveChild(table.transform, "Broken furniture — loose plank", PrimitiveType.Cube, new Vector3(-0.45f, -0.2f, -0.47f), new Vector3(1.15f, 0.12f, 0.14f), Quaternion.Euler(0f, 24f, 31f), materials.Wood);
            MarkStatic(table);
        }

        private static void BuildSaloonWallWarning(MaterialSet materials)
        {
            var warning = new GameObject("Clue — warning on wall");
            warning.transform.position = new Vector3(-10.97f, 2.25f, 38.7f);
            var collider = warning.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.22f, 0.82f, 0.08f);
            var clue = warning.AddComponent<InteractableClue>();
            clue.Configure(
                "saloon.warning",
                "Ler o aviso na parede",
                "A tinta seca diz: “Eles ouvem tudo. Levaram os sobreviventes para o celeiro.”",
                2.8f,
                false,
                DemoObjective.InvestigateSaloonClues,
                "AVISO NA PAREDE\n“Eles ouvem tudo. Levaram os sobreviventes para o celeiro.” A igreja é o próximo lugar indicado pelas pistas.");

            var writingObject = new GameObject("Warning — dried red writing");
            writingObject.transform.SetParent(warning.transform, false);
            writingObject.transform.localPosition = new Vector3(-0.13f, 0f, 0f);
            writingObject.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
            var writing = writingObject.AddComponent<TextMesh>();
            writing.text = "ELES OUVEM TUDO.\nLEVARAM OS SOBREVIVENTES\nPARA O CELEIRO.";
            writing.anchor = TextAnchor.MiddleCenter;
            writing.alignment = TextAlignment.Center;
            writing.characterSize = 0.105f;
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
            knifeClue.Configure("saloon.knife", "Examinar a faca", "A faca está presa no balcão destruído. É uma pista, não uma arma pronta para combate.", 2.8f, true, DemoObjective.ExamineSaloonKnife, "SALÃO — A FACA\nUma faca está presa no balcão destruído. A empunhadura está gasta; você a registra como evidência.");

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
            clue.Configure("saloon.torn-note", "Ler a anotação", "Carmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.", 2.8f, false, DemoObjective.InvestigateSaloonClues, "ANOTAÇÃO RASGADA\nCarmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.");

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
        }

        private static void CreateTree(Vector3 position, float scale, MaterialSet materials)
        {
            var trunk = Cylinder("Forest — trunk", position + Vector3.up * (2.1f * scale), new Vector3(0.42f, 2.1f, 0.42f) * scale, materials.Wood);
            MarkStatic(trunk);
            var crown = Sphere("Forest — canopy", position + Vector3.up * (4.7f * scale), new Vector3(4.3f, 3.5f, 4.1f) * scale, materials.Foliage);
            MarkStatic(crown);
            var crown2 = Sphere("Forest — canopy", position + Vector3.up * (5.6f * scale) + Vector3.right * (1.2f * scale), new Vector3(2.8f, 2.7f, 2.8f) * scale, materials.Foliage);
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

            playerObject.AddComponent<FirstPersonController>();
            var journal = playerObject.AddComponent<PlayerJournalComponent>();
            journal.Configure("LAYLA — A BUSCA\nVocê chegou a Ash Creek seguindo o rastro de Layla. O portão está aberto, mas não há sinal de vida na rua.");
            var handAnchor = new GameObject("Lantern hand anchor").transform;
            handAnchor.SetParent(cameraObject.transform, false);
            handAnchor.localPosition = new Vector3(0.34f, -0.34f, 0.52f);
            handAnchor.localRotation = Quaternion.Euler(22f, 0f, 0f);
            handAnchor.localScale = Vector3.one * 0.72f;
            var interactor = playerObject.AddComponent<PlayerInteractor>();
            interactor.Configure(camera, progression, journal);
            return new PlayerRig
            {
                LanternHandAnchor = handAnchor
            };
        }

        private static void BuildLukeAndArrivalTrail(MaterialSet materials, PlayerRig player)
        {
            var luke = new GameObject("Luke — wounded at the gate");
            luke.transform.position = new Vector3(-2.15f, 0f, 14.2f);
            PrimitiveChild(luke.transform, "Luke — fallen coat", PrimitiveType.Capsule, new Vector3(0f, 0.31f, 0f), new Vector3(0.42f, 1.05f, 0.42f), Quaternion.Euler(0f, 0f, 90f), materials.LukeCloth);
            PrimitiveChild(luke.transform, "Luke — head", PrimitiveType.Sphere, new Vector3(0.78f, 0.37f, 0.02f), new Vector3(0.34f, 0.31f, 0.32f), Quaternion.identity, materials.Skin);
            PrimitiveChild(luke.transform, "Luke — hat brim", PrimitiveType.Cylinder, new Vector3(0.8f, 0.55f, 0.02f), new Vector3(0.46f, 0.055f, 0.42f), Quaternion.identity, materials.Leather);
            PrimitiveChild(luke.transform, "Luke — hat crown", PrimitiveType.Cylinder, new Vector3(0.8f, 0.63f, 0.02f), new Vector3(0.27f, 0.15f, 0.27f), Quaternion.identity, materials.Leather);
            PrimitiveChild(luke.transform, "Luke — left boot", PrimitiveType.Cube, new Vector3(-0.65f, 0.15f, -0.27f), new Vector3(0.48f, 0.22f, 0.22f), Quaternion.Euler(0f, 7f, 0f), materials.Leather);
            PrimitiveChild(luke.transform, "Luke — right boot", PrimitiveType.Cube, new Vector3(-0.65f, 0.15f, 0.27f), new Vector3(0.48f, 0.22f, 0.22f), Quaternion.Euler(0f, -8f, 0f), materials.Leather);
            var blood = Cylinder("Luke — blood on the road", new Vector3(-1.9f, MainStreetSurfaceY + 0.008f, 14.1f), new Vector3(0.58f, 0.014f, 0.74f), materials.Blood);
            blood.transform.rotation = Quaternion.Euler(0f, 21f, 0f);
            MarkStatic(blood);

            var lantern = new GameObject("Luke's oil lantern");
            lantern.transform.position = new Vector3(-0.3f, 0.48f, 14.45f);
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
            lamp.color = new Color(1f, 0.63f, 0.34f);
            lamp.intensity = 2.8f;
            lamp.range = 10f;
            lamp.spotAngle = 48f;
            lamp.innerSpotAngle = 28f;
            lamp.shadows = LightShadows.None;
            lamp.transform.SetParent(lantern.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 0.06f, 0.12f);
            var pickup = lantern.AddComponent<HandLanternPickup>();
            pickup.Configure(2.8f, player.LanternHandAnchor, collider, lamp, globe, materials.WarmGlow, materials.WarmGlassDim);

            BuildGateBootprints(materials);
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
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Ash Creek — cold night / amber practicals";
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(-0.15f);
            color.contrast.Override(12f);
            color.saturation.Override(-8f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.42f);

            var profilePath = RenderingPath + "/AshCreekVolumeProfile.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath) != null)
                AssetDatabase.DeleteAsset(profilePath);
            AssetDatabase.CreateAsset(profile, profilePath);
            var volume = new GameObject("Global volume — restrained western night").AddComponent<Volume>();
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
            light.intensity = 1.2f;
            light.range = 10f;
            light.shadows = LightShadows.None;
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

        private static void AddText(string text, Vector3 position, float size, Color color, float rotationY)
        {
            var gameObject = new GameObject("Sign — " + text);
            gameObject.transform.position = position;
            gameObject.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            var mesh = gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.color = color;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mesh.font != null)
                gameObject.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
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
            public Material Mud;
            public Material Foliage;
            public Material WarmGlow;
            public Material Enemy;
            public Material LukeCloth;
            public Material Skin;
            public Material Leather;
            public Material WarmGlassDim;
        }

        private sealed class PlayerRig
        {
            public Transform LanternHandAnchor;
        }
    }
}
