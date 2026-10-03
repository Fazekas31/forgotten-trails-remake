using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenTrail.Tests.World
{
    public sealed class AshCreekTownCompositionSceneTests
    {
        [Test]
        public void AshCreekLandmarksFrameAnOpenWellPlazaAndKeepChestersAlleyReachable()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                AssertPosition(scene, "Central well — stone rim", new Vector3(33f, 0.45f, 0f));
                AssertPosition(scene, "Saloon — floor", new Vector3(49f, 0.12f, 20f));
                AssertPosition(scene, "Church — floor", new Vector3(77f, 0.12f, 14f));
                AssertPosition(scene, "Sheriff office — floor", new Vector3(99f, 0.12f, -23f));
                AssertPosition(scene, "Barn — floor", new Vector3(128f, 0.12f, 25f));
                AssertPosition(scene, "Forest — localized layers", new Vector3(150f, 0.4f, 25f));
                Assert.That(FindTransform(scene, "Sheriff office — floor").position.x,
                    Is.GreaterThan(FindTransform(scene, "Church — floor").position.x + 15f));
                Assert.That(Vector2.Distance(
                    new Vector2(FindTransform(scene, "Barn — floor").position.x, FindTransform(scene, "Barn — floor").position.z),
                    new Vector2(FindTransform(scene, "Central well — stone rim").position.x, FindTransform(scene, "Central well — stone rim").position.z)),
                    Is.GreaterThan(70f));

                var forge = FindTransform(scene, "Blacksmith — forge hearth");
                var chester = FindTransform(scene, "Chester — behind the iron gate");
                var jack = FindTransform(scene, "Jack — waiting beside Chester");
                Assert.That(forge, Is.Not.Null);
                Assert.That(chester, Is.Not.Null);
                Assert.That(jack, Is.Not.Null);
                AssertPosition(scene, "Blacksmith — forge hearth", new Vector3(79.65f, 0.58f, -14.15f));
                AssertPosition(scene, "Chester — behind the iron gate", new Vector3(80.05f, 0f, -14.9f));
                AssertPosition(scene, "Jack — waiting beside Chester", new Vector3(79.8f, 0f, -16.05f));
                Assert.That(Vector3.Distance(forge.position, chester.position), Is.LessThan(2f));
                Assert.That(Vector3.Distance(chester.position, jack.position), Is.LessThan(4f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [TestCase("Godot prototype town composition applied", 12f)]
        [TestCase("Godot prototype town composition v2 applied", 0f)]
        public void LegacyBarnLayoutMigratesToV3AndSecondApplyIsIdempotent(string legacyMarkerName, float legacyZ)
        {
            var originalActiveScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/AshCreekApproach.unity", OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var barn = new GameObject("Barn — migration test");
                SceneManager.MoveGameObjectToScene(barn, scene);
                barn.transform.position = new Vector3(118f, 0.12f, legacyZ);
                AddMarker(barn.transform, "Map layout orientation applied");
                AddMarker(barn.transform, legacyMarkerName);

                var applyComposition = FindApplyCompositionMethod();
                var firstMovedCount = (int)applyComposition.Invoke(null, new object[] { scene });
                Assert.That(firstMovedCount, Is.EqualTo(1));
                Assert.That(Vector3.Distance(barn.transform.position, new Vector3(128f, 0.12f, 25f)), Is.LessThan(0.001f));
                Assert.That(barn.transform.Find(legacyMarkerName), Is.Null);

                var currentMarker = barn.transform.Find("Ash Creek map composition v3 applied");
                Assert.That(currentMarker, Is.Not.Null);
                Assert.That(currentMarker.gameObject.hideFlags & HideFlags.DontSaveInBuild, Is.Not.Zero);

                var positionAfterFirstApply = barn.transform.position;
                var secondMovedCount = (int)applyComposition.Invoke(null, new object[] { scene });
                Assert.That(secondMovedCount, Is.Zero);
                Assert.That(Vector3.Distance(barn.transform.position, positionAfterFirstApply), Is.LessThan(0.001f));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalActiveScene.IsValid() && originalActiveScene.isLoaded)
                    SceneManager.SetActiveScene(originalActiveScene);
            }
        }

        private static void AssertPosition(Scene scene, string objectName, Vector3 expected)
        {
            var transform = FindTransform(scene, objectName);
            Assert.That(transform, Is.Not.Null, objectName + " should exist in Ash Creek.");
            Assert.That(Vector3.Distance(transform.position, expected), Is.LessThan(0.05f), objectName + " should match the town composition.");
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == objectName)
                        return transform;
                }
            }

            return null;
        }

        private static void AddMarker(Transform parent, string markerName)
        {
            var marker = new GameObject(markerName);
            marker.transform.SetParent(parent, false);
        }

        private static MethodInfo FindApplyCompositionMethod()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var builderType = assembly.GetType("ForgottenTrail.Editor.AshCreekSceneBuilder");
                var method = builderType?.GetMethod("ApplyPrototypeTownComposition", BindingFlags.NonPublic | BindingFlags.Static);
                if (method != null)
                    return method;
            }

            Assert.Fail("The Ash Creek editor composition migration should be loaded in EditMode.");
            return null;
        }
    }
}
