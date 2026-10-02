using System.IO;
using ForgottenTrail.Gameplay;
using ForgottenTrail.Gameplay.Enemies;
using ForgottenTrail.Gameplay.Player;
using ForgottenTrail.Gameplay.Progression;
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
            BuildTown(materials);
            BuildTrees(materials);
            BuildLocalMist();
            BuildPlayer();
            BuildEnemy(materials);
            BuildVolume();

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("Ash Creek graybox scene created at " + ScenePath);
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
                Metal = MakeMaterial("Metal — oxidized iron", new Color(0.20f, 0.22f, 0.23f), 0.6f, 0.42f),
                Blood = MakeMaterial("Clue — dried blood", new Color(0.26f, 0.045f, 0.035f), 0f, 0.24f),
                Foliage = MakeMaterial("Foliage — night pine", new Color(0.09f, 0.14f, 0.13f), 0f, 0.36f),
                WarmGlow = MakeEmissive("Lamp glass — amber", new Color(1f, 0.42f, 0.12f), 1.8f),
                Enemy = MakeMaterial("Figure — near-black cloth", new Color(0.08f, 0.085f, 0.10f), 0f, 0.25f)
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

        private static string MakeSafeName(string name)
        {
            return name.Replace(' ', '_').Replace('—', '-');
        }

        private static void BuildGround(MaterialSet materials)
        {
            var ground = Box("Ash Creek ground", new Vector3(0f, -0.3f, 55f), new Vector3(100f, 0.6f, 150f), materials.Ground);
            MarkStatic(ground);

            var street = Box("Main street", new Vector3(0f, 0.015f, 48f), new Vector3(12f, 0.08f, 92f), materials.Road);
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
                var window = Box(name + " — front window", new Vector3(origin.x + side * width * 0.34f, height * 0.58f, frontZ - 0.08f), new Vector3(1.15f, 1.35f, 0.08f), materials.Window);
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
                var bloodTrail = Cylinder("Clue — blood trail by well", new Vector3(-1.5f, 0.09f, 33.8f), new Vector3(0.28f, 0.025f, 0.74f), materials.Blood);
                bloodTrail.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
                var clue = bloodTrail.AddComponent<InteractableClue>();
                clue.Configure("saloon.blood-trail", "Examinar os rastros", "Marcas de botas arrastadas partem do poço e seguem pela rua principal até o saloon.", 2.8f, false, DemoObjective.InvestigateSaloonClues);
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

        private static void BuildSaloonInteriorAndClues(MaterialSet materials)
        {
            var bar = Box("Saloon — bar counter", new Vector3(-15f, 0.85f, 37.1f), new Vector3(3.5f, 1.45f, 1.1f), materials.WoodLight);
            MarkStatic(bar);

            var knife = Box("Clue — knife", new Vector3(-15f, 1.62f, 37.1f), new Vector3(0.85f, 0.06f, 0.13f), materials.Metal);
            knife.transform.rotation = Quaternion.Euler(0f, 47f, 0f);
            var knifeClue = knife.AddComponent<InteractableClue>();
            knifeClue.Configure("saloon.knife", "Examinar a faca", "A faca ficou sobre o balcão destruído depois do estrondo. A lâmina ainda pode servir para se defender.", 2.8f, true, DemoObjective.InvestigateSaloonClues);

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
            clue.Configure("saloon.torn-note", "Ler a anotação", "Carmen viu algo descer da estrada da mina. Os sussurros começaram nas janelas; os doentes ficaram para trás e os sobreviventes fugiram para o celeiro.", 2.8f, true, DemoObjective.InvestigateSaloonClues);
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

        private static void BuildPlayer()
        {
            var progression = new GameObject("Demo Progression").AddComponent<DemoProgressionComponent>();
            var playerObject = new GameObject("Player — Luke");
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

            var player = playerObject.AddComponent<FirstPersonController>();
            var interactor = playerObject.AddComponent<PlayerInteractor>();
            interactor.Configure(camera, progression);
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
            public Material Metal;
            public Material Blood;
            public Material Foliage;
            public Material WarmGlow;
            public Material Enemy;
        }
    }
}
