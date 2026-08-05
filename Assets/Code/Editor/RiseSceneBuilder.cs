using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rise.Core;
using Rise.Systems;
using Rise.UI;

namespace Rise.EditorTools
{
    public static class RiseSceneBuilder
    {
        private const string OpenWorldSceneName = "OpenWorld";
        private const string OpenWorldScenePath = "Assets/Scenes/OpenWorld/OpenWorld.unity";
        private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string MaterialsFolder = "Assets/Art/Environment/Materials";
        private const string TexturesFolder = "Assets/Art/Environment/Textures";

        private static string materialsFolder;

        [MenuItem("Rise/Setup/Build OpenWorld Scene")]
        public static void BuildOpenWorldScene()
        {
            Scene current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (current.name != OpenWorldSceneName)
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "Build OpenWorld Scene",
                    "This tool expects the 'OpenWorld' scene to be active, but you're in '" + current.name + "'.\n\n" +
                    "It will open and build the scene in: " + OpenWorldScenePath + "\n\nContinue?",
                    "Yes, open it", "Cancel");
                if (!proceed) return;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OpenWorldScenePath);
            }

            materialsFolder = MaterialsFolder;
            Directory.CreateDirectory(materialsFolder);

            BuildWorld();
            BuildPlayerRig();
            EnsureLighting();
            EnsureCameraBrain();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: OpenWorld scene built. Press Play to run.");
        }

        [MenuItem("Rise/Setup/Build Environment Details")]
        public static void BuildEnvironmentDetails()
        {
            Scene current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (current.name != OpenWorldSceneName)
            {
                if (!EditorUtility.DisplayDialog(
                    "Build Environment Details",
                    "This expects the 'OpenWorld' scene to be active.\n\nContinue?",
                    "Yes, open it", "Cancel"))
                    return;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OpenWorldScenePath);
            }

            materialsFolder = MaterialsFolder;
            Directory.CreateDirectory(materialsFolder);

            EnsureEnvironment();
            BuildTownDetails();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: Environment details built. Press Play to view.");
        }

        private static void BuildWorld()
        {
            GameObject oldWorld = GameObject.Find("World");
            if (oldWorld != null) Object.DestroyImmediate(oldWorld);

            Transform world = GetOrCreateEmpty("World");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(world);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            SetRendererMaterial(ground, CreateDetailedMaterial("M_Grass", new Color(0.5f, 0.68f, 0.35f), 0.06f, 0.1f));

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "MainRoad";
            road.transform.SetParent(world);
            road.transform.position = new Vector3(0f, 0.05f, 0f);
            road.transform.localScale = new Vector3(12f, 0.1f, 80f);
            SetRendererMaterial(road, CreateDetailedMaterial("M_Road", new Color(0.32f, 0.32f, 0.34f), 0.05f, 0.2f));

            Transform town = GetOrCreateEmpty("Town", world);

            Material houseA = CreateDetailedMaterial("M_HouseA", new Color(0.86f, 0.8f, 0.66f), 0.04f, 0.15f);
            Material houseB = CreateDetailedMaterial("M_HouseB", new Color(0.62f, 0.5f, 0.38f), 0.06f, 0.15f);
            Material shop = CreateDetailedMaterial("M_Shop", new Color(0.74f, 0.74f, 0.76f), 0.05f, 0.15f);
            Material roof = CreateDetailedMaterial("M_Roof", new Color(0.5f, 0.22f, 0.15f), 0.08f, 0.25f);

            BuildBuilding(town, "House_01", new Vector3(-16f, 0f, 12f), new Vector3(12f, 6f, 12f), houseA, roof);
            BuildBuilding(town, "House_02", new Vector3(-16f, 0f, -12f), new Vector3(11f, 5f, 11f), houseB, roof);
            BuildBuilding(town, "Shop_01", new Vector3(12f, 0f, 6f), new Vector3(9f, 4f, 10f), shop, roof);
            BuildBuilding(town, "Shop_02", new Vector3(12f, 0f, -14f), new Vector3(9f, 4f, 10f), shop, roof);
            BuildBuilding(town, "TownHall", new Vector3(0f, 0f, -30f), new Vector3(14f, 10f, 14f), houseA, roof);
            BuildBuilding(town, "Market_01", new Vector3(20f, 0f, 24f), new Vector3(7f, 3.5f, 11f), houseB, roof);
            BuildBuilding(town, "Market_02", new Vector3(-22f, 0f, 24f), new Vector3(7f, 3.5f, 11f), houseB, roof);
        }

        private static void BuildBuilding(Transform parent, string name, Vector3 basePos, Vector3 baseSize, Material wallMat, Material roofMat)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name + "_Body";
            body.transform.SetParent(parent);
            body.transform.position = basePos + Vector3.up * (baseSize.y * 0.5f);
            body.transform.localScale = baseSize;
            SetRendererMaterial(body, wallMat);

            BuildGableRoof(parent, name, basePos, baseSize, roofMat, name == "TownHall" ? 2.5f : 1.6f);

            Material windowMat = CreateMaterial("M_Window", new Color(0.16f, 0.26f, 0.42f));
            Material doorMat = CreateMaterial("M_Door", new Color(0.32f, 0.20f, 0.10f));
            AddWindowsAndDoor(parent, name, basePos, baseSize, windowMat, doorMat);
        }

        private static void BuildGableRoof(Transform parent, string name, Vector3 basePos, Vector3 baseSize, Material mat, float rise)
        {
            float topY = basePos.y + baseSize.y;
            float halfZ = baseSize.z * 0.5f + 0.3f;
            float length = Mathf.Sqrt(halfZ * halfZ + rise * rise);
            float angle = Mathf.Atan2(rise, halfZ) * Mathf.Rad2Deg;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = name + "_RoofPanel";
                panel.transform.SetParent(parent);
                panel.transform.position = new Vector3(basePos.x, topY + rise * 0.5f, basePos.z + side * halfZ * 0.5f);
                panel.transform.localScale = new Vector3(baseSize.x + 0.8f, 0.25f, length);
                panel.transform.rotation = Quaternion.Euler(side * angle, 0f, 0f);
                SetRendererMaterial(panel, mat);
            }

            GameObject ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ridge.name = name + "_RoofRidge";
            ridge.transform.SetParent(parent);
            ridge.transform.position = new Vector3(basePos.x, topY + rise, basePos.z);
            ridge.transform.localScale = new Vector3(baseSize.x + 0.8f, 0.35f, 0.6f);
            SetRendererMaterial(ridge, mat);
        }

        private static void AddWindowsAndDoor(Transform parent, string name, Vector3 basePos, Vector3 baseSize, Material windowMat, Material doorMat)
        {
            Vector3 half = baseSize * 0.5f;
            float inset = 0.05f;
            float winY = basePos.y + baseSize.y * 0.55f;
            float winH = baseSize.y * 0.26f;
            float winW = baseSize.x * 0.22f;
            float sideW = baseSize.z * 0.22f;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = basePos.z + (side > 0 ? half.z + inset : -half.z - inset);
                AddPrim(parent, name + "_Window", new Vector3(basePos.x - baseSize.x * 0.26f, winY, z), new Vector3(winW, winH, 0.15f), windowMat);
                AddPrim(parent, name + "_Window", new Vector3(basePos.x + baseSize.x * 0.26f, winY, z), new Vector3(winW, winH, 0.15f), windowMat);
                AddPrim(parent, name + "_WindowSide", new Vector3(basePos.x - half.x - inset, winY, basePos.z - baseSize.z * 0.2f), new Vector3(0.15f, winH, sideW), windowMat);
                AddPrim(parent, name + "_WindowSide", new Vector3(basePos.x + half.x + inset, winY, basePos.z + baseSize.z * 0.2f), new Vector3(0.15f, winH, sideW), windowMat);
            }

            float doorW = Mathf.Min(1.8f, baseSize.x * 0.3f);
            float doorH = Mathf.Min(3.4f, baseSize.y * 0.72f);
            AddPrim(parent, name + "_Door", new Vector3(basePos.x, basePos.y + doorH * 0.5f, basePos.z + half.z + inset), new Vector3(doorW, doorH, 0.15f), doorMat);
        }

        private static void BuildPlayerRig()
        {
            Transform world = GameObject.Find("World").transform;

            Transform rig = GetOrCreateEmpty("PlayerRig", world);
            rig.position = new Vector3(0f, 1f, 0f);

            GameObject playerGO = new GameObject("Player");
            playerGO.transform.SetParent(rig);
            playerGO.transform.localPosition = Vector3.zero;
            playerGO.transform.localRotation = Quaternion.identity;

            CharacterController controller = playerGO.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 1f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(playerGO.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            SetRendererMaterial(body, CreateMaterial("M_PlayerBody", new Color(0.25f, 0.45f, 0.85f)));

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(rig);
            pivot.transform.localPosition = new Vector3(0f, 1f, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            GameObject cmGO = new GameObject("CM Player Camera");
            cmGO.transform.SetParent(rig);
            cmGO.transform.position = pivot.transform.position;

            CinemachineCamera cmCamera = cmGO.AddComponent<CinemachineCamera>();
            cmCamera.Follow = pivot.transform;
            cmCamera.LookAt = body.transform;

            CinemachineThirdPersonFollow follow = cmGO.AddComponent<CinemachineThirdPersonFollow>();
            follow.CameraDistance = 5f;
            follow.ShoulderOffset = new Vector3(0.5f, 0.2f, -0.5f);
            follow.VerticalArmLength = 0.5f;

            PlayerController player = playerGO.AddComponent<PlayerController>();
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (inputAsset != null)
            {
                player.ConfigureForSceneBuilder(
                    InputActionReference.Create(inputAsset.FindAction("Player/Move", true)),
                    InputActionReference.Create(inputAsset.FindAction("Player/Look", true)),
                    InputActionReference.Create(inputAsset.FindAction("Player/Jump", true)),
                    InputActionReference.Create(inputAsset.FindAction("Player/Sprint", true)),
                    pivot.transform,
                    cmGO.transform);
            }
            else
            {
                Debug.LogError("Rise: Could not load input asset at " + InputAssetPath);
            }
        }

        private static void EnsureLighting()
        {
            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunGO = new GameObject("Directional Light");
                sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun = sunGO.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
        }

        private static void EnsureCameraBrain()
        {
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
            }
            if (!cam.TryGetComponent<CinemachineBrain>(out _))
            {
                cam.gameObject.AddComponent<CinemachineBrain>();
            }
        }

        private static void EnsureEnvironment()
        {
            Material sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.name = "Sky_Procedural";
            sky.SetColor("_SkyTint", new Color(0.48f, 0.6f, 0.85f));
            sky.SetColor("_GroundColor", new Color(0.42f, 0.42f, 0.36f));
            sky.SetFloat("_Exposure", 1.15f);
            sky.SetFloat("_AtmosphereThickness", 1.05f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.78f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.63f, 0.64f);
            RenderSettings.ambientGroundColor = new Color(0.5f, 0.48f, 0.44f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.82f, 0.86f, 0.92f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;

            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.shadows = LightShadows.Soft;
            }
        }

        private static void BuildTownDetails()
        {
            GameObject worldGO = GameObject.Find("World");
            Transform world = worldGO != null ? worldGO.transform : new GameObject("World").transform;

            Transform old = world.Find("WorldDetails");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Transform details = new GameObject("WorldDetails").transform;
            details.SetParent(world);

            Material trunk = CreateMaterial("M_Trunk", new Color(0.42f, 0.28f, 0.15f));
            Material leaves = CreateMaterial("M_Leaves", new Color(0.30f, 0.55f, 0.25f));
            Material bush = CreateMaterial("M_Bush", new Color(0.27f, 0.50f, 0.22f));
            Material rock = CreateMaterial("M_Rock", new Color(0.48f, 0.47f, 0.50f));
            Material lamp = CreateMaterial("M_Lamp", new Color(0.15f, 0.15f, 0.18f));
            Material lampHead = CreateMaterial("M_LampHead", new Color(1f, 0.85f, 0.5f));
            Material fence = CreateMaterial("M_Fence", new Color(0.60f, 0.46f, 0.28f));
            Material line = CreateMaterial("M_RoadLine", new Color(0.92f, 0.75f, 0.15f));
            Material flowerRed = CreateMaterial("M_FlowerRed", new Color(0.85f, 0.2f, 0.2f));
            Material flowerWhite = CreateMaterial("M_FlowerWhite", new Color(0.95f, 0.95f, 0.95f));
            Material birdMat = CreateMaterial("M_Bird", new Color(0.09f, 0.09f, 0.12f));
            Material skin = CreateMaterial("M_Skin", new Color(0.93f, 0.82f, 0.72f));

            BuildRoadLines(details, line);
            BuildTrees(details, trunk, leaves);
            BuildBushes(details, bush);
            BuildRocks(details, rock);
            BuildFlowers(details, flowerRed, flowerWhite);
            BuildLamps(details, lamp, lampHead);
            BuildFences(details, fence);
            BuildBirds(details, birdMat);
            BuildTownspeople(details, skin);
        }

        private static void BuildTownspeople(Transform parent, Material skin)
        {
            Transform npcs = new GameObject("Townspeople").transform;
            npcs.SetParent(parent);

            Material body = CreateMaterial("M_CitizenBody", new Color(0.6f, 0.6f, 0.6f));

            Color[] shirts =
            {
                new Color(0.85f, 0.40f, 0.45f),
                new Color(0.35f, 0.55f, 0.85f),
                new Color(0.40f, 0.70f, 0.45f),
                new Color(0.85f, 0.75f, 0.30f),
                new Color(0.60f, 0.45f, 0.75f),
                new Color(0.20f, 0.60f, 0.65f),
                new Color(0.75f, 0.30f, 0.30f)
            };

            Vector3[] roadRoute =
            {
                new Vector3(0f, 0f, -16f), new Vector3(0f, 0f, -8f), new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 8f), new Vector3(0f, 0f, 16f), new Vector3(0f, 0f, 24f),
                new Vector3(0f, 0f, 32f)
            };

            Vector3[] shopRoute =
            {
                new Vector3(-4f, 0f, 26f), new Vector3(4f, 0f, 26f),
                new Vector3(0f, 0f, 30f), new Vector3(0f, 0f, 22f)
            };

            Vector3[] marketEast =
            {
                new Vector3(24f, 0f, 20f), new Vector3(16f, 0f, 28f),
                new Vector3(16f, 0f, 20f), new Vector3(24f, 0f, 28f)
            };

            Vector3[] marketWest =
            {
                new Vector3(-24f, 0f, 20f), new Vector3(-16f, 0f, 28f),
                new Vector3(-16f, 0f, 20f), new Vector3(-24f, 0f, 28f)
            };

            BuildCitizen(npcs, "Citizen_1", new Vector3(0f, 0f, -16f), roadRoute, body, skin, shirts[0]);
            BuildCitizen(npcs, "Citizen_2", new Vector3(0f, 0f, 24f), roadRoute, body, skin, shirts[1]);
            BuildCitizen(npcs, "Citizen_3", new Vector3(-4f, 0f, 26f), shopRoute, body, skin, shirts[2]);
            BuildCitizen(npcs, "Citizen_4", new Vector3(4f, 0f, 26f), shopRoute, body, skin, shirts[3]);
            BuildCitizen(npcs, "Citizen_5", new Vector3(16f, 0f, 20f), marketEast, body, skin, shirts[4]);
            BuildCitizen(npcs, "Citizen_6", new Vector3(-16f, 0f, 20f), marketWest, body, skin, shirts[5]);
            BuildCitizen(npcs, "Citizen_7", new Vector3(0f, 0f, 32f), roadRoute, body, skin, shirts[6]);
        }

        private static void BuildCitizen(Transform parent, string name, Vector3 start, Vector3[] route,
            Material bodyMat, Material skinMat, Color tint)
        {
            GameObject npc = new GameObject(name);
            npc.transform.SetParent(parent);
            npc.transform.position = start;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(npc.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npc.transform);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            SetRendererMaterial(head, skinMat);

            TownNPC town = npc.AddComponent<TownNPC>();
            town.bodyMaterial = bodyMat;
            town.bodyTint = tint;
            town.skinMaterial = skinMat;
            town.SetRoute(route);
            town.walkSpeed = UnityEngine.Random.Range(1f, 1.6f);
        }

        private static void BuildRoadLines(Transform parent, Material mat)
        {
            for (int z = -20; z <= 36; z += 6)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "RoadDash";
                dash.transform.SetParent(parent);
                dash.transform.position = new Vector3(0f, 0.11f, z);
                dash.transform.localScale = new Vector3(0.15f, 0.04f, 2.2f);
                SetRendererMaterial(dash, mat);
            }
        }

        private static void BuildTrees(Transform parent, Material trunk, Material leaves)
        {
            Vector3[] spots =
            {
                new Vector3(-8f, 0f, -20f), new Vector3(8f, 0f, -20f),
                new Vector3(-8f, 0f, -5f), new Vector3(8f, 0f, -5f),
                new Vector3(-8f, 0f, 10f), new Vector3(8f, 0f, 10f),
                new Vector3(-8f, 0f, 25f), new Vector3(8f, 0f, 25f),
                new Vector3(-32f, 0f, 34f), new Vector3(32f, 0f, 34f),
                new Vector3(-32f, 0f, -34f), new Vector3(32f, 0f, -34f),
                new Vector3(-32f, 0f, 0f), new Vector3(32f, 0f, 0f),
                new Vector3(-18f, 0f, 38f), new Vector3(18f, 0f, 38f),
                new Vector3(-18f, 0f, -38f), new Vector3(18f, 0f, -38f),
                new Vector3(-28f, 0f, 12f), new Vector3(28f, 0f, 12f),
                new Vector3(-28f, 0f, -12f), new Vector3(28f, 0f, -12f)
            };
            foreach (Vector3 spot in spots)
            {
                if (IsNearBuilding(spot, 3f)) continue;
                BuildTree(parent, spot, trunk, leaves);
            }
        }

        private static bool IsNearBuilding(Vector3 pos, float margin)
        {
            Rect[] buildings =
            {
                new Rect(-16f - 6f - margin, 12f - 6f - margin, 12f + margin * 2f, 12f + margin * 2f),
                new Rect(-16f - 5.5f - margin, -12f - 5.5f - margin, 11f + margin * 2f, 11f + margin * 2f),
                new Rect(12f - 4.5f - margin, 6f - 5f - margin, 9f + margin * 2f, 10f + margin * 2f),
                new Rect(12f - 4.5f - margin, -14f - 5f - margin, 9f + margin * 2f, 10f + margin * 2f),
                new Rect(0f - 7f - margin, -30f - 7f - margin, 14f + margin * 2f, 14f + margin * 2f),
                new Rect(20f - 3.5f - margin, 24f - 5.5f - margin, 7f + margin * 2f, 11f + margin * 2f),
                new Rect(-22f - 3.5f - margin, 24f - 5.5f - margin, 7f + margin * 2f, 11f + margin * 2f)
            };
            foreach (Rect rect in buildings)
            {
                if (rect.Contains(pos)) return true;
            }
            return false;
        }

        private static void BuildTree(Transform parent, Vector3 pos, Material trunk, Material leaves)
        {
            float h = UnityEngine.Random.Range(3f, 4f);
            GameObject trunkGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkGO.name = "Tree_Trunk";
            trunkGO.transform.SetParent(parent);
            trunkGO.transform.position = new Vector3(pos.x, h * 0.5f, pos.z);
            trunkGO.transform.localScale = new Vector3(0.55f, h * 0.5f, 0.55f);
            SetRendererMaterial(trunkGO, trunk);

            float baseS = UnityEngine.Random.Range(2.6f, 3.2f);
            float topY = h + baseS * 0.35f;

            GameObject main = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            main.name = "Tree_Canopy";
            main.transform.SetParent(parent);
            main.transform.position = new Vector3(pos.x, topY, pos.z);
            main.transform.localScale = Vector3.one * baseS;
            SetRendererMaterial(main, leaves);

            GameObject side1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            side1.name = "Tree_Canopy";
            side1.transform.SetParent(parent);
            side1.transform.position = new Vector3(pos.x - baseS * 0.45f, topY - baseS * 0.15f, pos.z + baseS * 0.15f);
            side1.transform.localScale = Vector3.one * baseS * 0.75f;
            SetRendererMaterial(side1, leaves);

            GameObject side2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            side2.name = "Tree_Canopy";
            side2.transform.SetParent(parent);
            side2.transform.position = new Vector3(pos.x + baseS * 0.5f, topY - baseS * 0.1f, pos.z - baseS * 0.1f);
            side2.transform.localScale = Vector3.one * baseS * 0.8f;
            SetRendererMaterial(side2, leaves);
        }

        private static void BuildBirds(Transform parent, Material birdMat)
        {
            GameObject birdsGO = new GameObject("Birds");
            birdsGO.transform.SetParent(parent);
            birdsGO.transform.position = new Vector3(0f, 18f, 5f);
            Birds birds = birdsGO.AddComponent<Birds>();
            birds.material = birdMat;
        }

        private static void BuildBushes(Transform parent, Material mat)
        {
            Vector3[] spots =
            {
                new Vector3(-5f, 0f, -2f), new Vector3(5f, 0f, -2f),
                new Vector3(-5f, 0f, 16f), new Vector3(5f, 0f, 16f),
                new Vector3(-18f, 0f, 10f), new Vector3(18f, 0f, 10f),
                new Vector3(-25f, 0f, 30f), new Vector3(25f, 0f, 30f),
                new Vector3(0f, 0f, -40f), new Vector3(0f, 0f, 40f)
            };
            foreach (Vector3 spot in spots)
            {
                GameObject bushGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bushGO.name = "Bush";
                bushGO.transform.SetParent(parent);
                float s = UnityEngine.Random.Range(1.1f, 1.6f);
                bushGO.transform.position = new Vector3(spot.x, s * 0.45f, spot.z);
                bushGO.transform.localScale = new Vector3(s, s * 0.8f, s);
                SetRendererMaterial(bushGO, mat);
            }
        }

        private static void BuildRocks(Transform parent, Material mat)
        {
            Vector3[] spots =
            {
                new Vector3(-12f, 0f, 18f), new Vector3(14f, 0f, -18f),
                new Vector3(-28f, 0f, -25f), new Vector3(30f, 0f, -28f),
                new Vector3(2f, 0f, -8f), new Vector3(-2f, 0f, 20f)
            };
            foreach (Vector3 spot in spots)
            {
                GameObject rockGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rockGO.name = "Rock";
                rockGO.transform.SetParent(parent);
                float s = UnityEngine.Random.Range(0.5f, 0.9f);
                rockGO.transform.position = new Vector3(spot.x, s * 0.4f, spot.z);
                rockGO.transform.localScale = new Vector3(s, s * 0.7f, s);
                SetRendererMaterial(rockGO, mat);
            }
        }

        private static void BuildFlowers(Transform parent, Material red, Material white)
        {
            Material[] mats = { red, white };
            Vector3[] spots =
            {
                new Vector3(-7f, 0f, 12f), new Vector3(-6f, 0f, 13f), new Vector3(-8f, 0f, 14f),
                new Vector3(6f, 0f, 13f), new Vector3(7f, 0f, 14f), new Vector3(8f, 0f, 12f),
                new Vector3(22f, 0f, 22f), new Vector3(21f, 0f, 23f), new Vector3(23f, 0f, 25f),
                new Vector3(-24f, 0f, 21f), new Vector3(-23f, 0f, 22f), new Vector3(-25f, 0f, 23f)
            };
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject flowerGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flowerGO.name = "Flower";
                flowerGO.transform.SetParent(parent);
                flowerGO.transform.position = spots[i] + Vector3.up * 0.12f;
                flowerGO.transform.localScale = Vector3.one * 0.25f;
                SetRendererMaterial(flowerGO, mats[i % mats.Length]);
            }
        }

        private static void BuildLamps(Transform parent, Material pole, Material head)
        {
            int[] zs = { -20, -5, 10, 25 };
            foreach (int z in zs)
            {
                BuildLamp(parent, new Vector3(-6.5f, 0f, z), pole, head);
                BuildLamp(parent, new Vector3(6.5f, 0f, z), pole, head);
            }
        }

        private static void BuildLamp(Transform parent, Vector3 pos, Material pole, Material head)
        {
            GameObject postGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postGO.name = "Lamp_Post";
            postGO.transform.SetParent(parent);
            postGO.transform.position = new Vector3(pos.x, 2.6f, pos.z);
            postGO.transform.localScale = new Vector3(0.18f, 2.6f, 0.18f);
            SetRendererMaterial(postGO, pole);

            GameObject lightGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightGO.name = "Lamp_Head";
            lightGO.transform.SetParent(parent);
            lightGO.transform.position = new Vector3(pos.x, 5.4f, pos.z);
            lightGO.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);
            SetRendererMaterial(lightGO, head);

            GameObject lampLightGO = new GameObject("Lamp_Light");
            lampLightGO.transform.SetParent(parent);
            lampLightGO.transform.position = new Vector3(pos.x, 5.2f, pos.z);
            Light lampLight = lampLightGO.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.color = new Color(1f, 0.85f, 0.6f);
            lampLight.range = 9f;
            lampLight.intensity = 1.5f;
            lampLight.shadows = LightShadows.None;
        }

        private static void BuildFences(Transform parent, Material mat)
        {
            BuildFence(parent, new Vector3(16f, 0f, 20f), new Vector3(16f, 0f, 28f), mat);
            BuildFence(parent, new Vector3(-16f, 0f, 20f), new Vector3(-16f, 0f, 28f), mat);
        }

        private static void BuildFence(Transform parent, Vector3 start, Vector3 end, Material mat)
        {
            Vector3 dir = end - start;
            float length = dir.magnitude;
            Vector3 dirN = dir.normalized;

            int posts = Mathf.Max(2, Mathf.RoundToInt(length) + 1);
            for (int i = 0; i < posts; i++)
            {
                Vector3 p = start + dirN * (length * i / (posts - 1));
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Fence_Post";
                post.transform.SetParent(parent);
                post.transform.position = new Vector3(p.x, 0.65f, p.z);
                post.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
                SetRendererMaterial(post, mat);
            }

            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Fence_Rail";
            rail.transform.SetParent(parent);
            rail.transform.position = new Vector3((start.x + end.x) * 0.5f, 1.05f, (start.z + end.z) * 0.5f);
            rail.transform.localScale = new Vector3(0.1f, 0.12f, length);
            rail.transform.rotation = Quaternion.LookRotation(dirN);
            SetRendererMaterial(rail, mat);
        }

        private static Transform GetOrCreateEmpty(string name, Transform parent = null)
        {
            Transform existing = parent != null
                ? parent.Find(name)
                : GameObject.Find(name)?.transform;
            if (existing != null) return existing;

            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            return go.transform;
        }

        private static void SetRendererMaterial(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = Path.Combine(materialsFolder, name + ".mat");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D CreateNoiseTexture(string name, float noise)
        {
            string path = TexturesFolder + "/" + name + ".png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) return tex;

            Directory.CreateDirectory(TexturesFolder);
            tex = new Texture2D(256, 256, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            System.Random rng = new System.Random(777 + name.Length * 13);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    float v = 1f - noise + (float)rng.NextDouble() * 2f * noise;
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            return tex;
        }

        private static Material CreateDetailedMaterial(string name, Color color, float noise, float smoothness)
        {
            Material mat = CreateMaterial(name, color);
            mat.SetTexture("_BaseMap", CreateNoiseTexture(name + "_Noise", noise));
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void AddPrim(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            SetRendererMaterial(go, mat);
        }

        [MenuItem("Rise/Setup/Build Gameplay Systems")]
        public static void BuildGameplaySystems()
        {
            Scene current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (current.name != OpenWorldSceneName)
            {
                if (!EditorUtility.DisplayDialog(
                    "Build Gameplay Systems",
                    "This expects the 'OpenWorld' scene to be active.\n\nContinue?",
                    "Yes, open it", "Cancel"))
                    return;
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(OpenWorldScenePath);
            }

            materialsFolder = MaterialsFolder;
            Directory.CreateDirectory(materialsFolder);

            GameManager gameManager = EnsureGameManager();
            JobDefinition general = EnsureJobAsset("Job_GeneralWorker", "General Worker", 30, 0);
            JobDefinition cashier = EnsureJobAsset("Job_Cashier", "Cashier", 60, 200);
            JobDefinition manager = EnsureJobAsset("Job_Manager", "Manager", 120, 1000);
            EnsureWorkStations(general, cashier, manager);
            EnsurePlayerNeeds();
            EnsureShop();
            EnsurePartner();
            EnsureHUD(gameManager);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: Gameplay systems built. Press Play, walk to the yellow work spot, and press E.");
        }

        private static GameManager EnsureGameManager()
        {
            GameManager existing = Object.FindFirstObjectByType<GameManager>();
            if (existing != null) return existing;

            GameObject gmGO = new GameObject("GameManager");
            return gmGO.AddComponent<GameManager>();
        }

        private static JobDefinition EnsureJobAsset(string fileName, string jobName, int pay, int unlock)
        {
            const string jobsFolder = "Assets/Data/Jobs";
            string path = jobsFolder + "/" + fileName + ".asset";
            Directory.CreateDirectory(jobsFolder);
            JobDefinition job = AssetDatabase.LoadAssetAtPath<JobDefinition>(path);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<JobDefinition>();
                job.name = fileName;
                AssetDatabase.CreateAsset(job, path);
            }
            job.Configure(jobName, pay, unlock, "");
            EditorUtility.SetDirty(job);
            return job;
        }

        private static void EnsureWorkStations(JobDefinition general, JobDefinition cashier, JobDefinition manager)
        {
            GameObject old = GameObject.Find("WorkSpot_Shop");
            if (old != null) Object.DestroyImmediate(old);

            EnsureStation(general, new Vector3(12f, 0.25f, 14f), "WorkSpot_General");
            EnsureStation(cashier, new Vector3(13f, 0.25f, -6f), "WorkSpot_Cashier");
            EnsureStation(manager, new Vector3(0f, 0.25f, -22f), "WorkSpot_Manager");
        }

        private static void EnsureStation(JobDefinition job, Vector3 spot, string name)
        {
            GameObject marker = GameObject.Find(name);
            if (marker != null)
            {
                marker.transform.position = spot;
                marker.GetComponent<WorkStation>().SetJob(job);
                return;
            }

            marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = name;
            marker.transform.position = spot;
            marker.transform.localScale = new Vector3(2.2f, 0.5f, 2.2f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            SetRendererMaterial(marker, CreateMaterial("M_WorkSpot", new Color(1f, 0.8f, 0.1f)));

            WorkStation station = marker.AddComponent<WorkStation>();
            station.SetJob(job);
        }

        private static void EnsureShop()
        {
            ShopStand shop = Object.FindFirstObjectByType<ShopStand>();
            if (shop == null)
            {
                GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stand.name = "ShopStand";
                stand.transform.position = new Vector3(0f, 0.6f, 26f);
                stand.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);
                Object.DestroyImmediate(stand.GetComponent<Collider>());
                SetRendererMaterial(stand, CreateMaterial("M_ShopStand", new Color(0.6f, 0.25f, 0.8f)));
                shop = stand.AddComponent<ShopStand>();
            }

            shop.SetItems(new List<ShopItemData>
            {
                new ShopItemData { itemName = "Bread", price = 5, itemType = ShopItemType.Food },
                new ShopItemData { itemName = "Flowers", price = 30, itemType = ShopItemType.GiftFlower },
                new ShopItemData { itemName = "Chocolate", price = 20, itemType = ShopItemType.GiftChocolate },
                new ShopItemData { itemName = "Ring", price = 500, itemType = ShopItemType.GiftRing },
                new ShopItemData { itemName = "Shirt", price = 25 },
                new ShopItemData { itemName = "Shoes", price = 40 },
                new ShopItemData { itemName = "Watch", price = 120 }
            });
        }

        private static void EnsurePartner()
        {
            Partner existing = Object.FindFirstObjectByType<Partner>();
            if (existing != null) return;

            GameObject maya = new GameObject("Maya");
            maya.transform.position = new Vector3(3.5f, 0f, 26f);

            Material skin = CreateMaterial("M_Skin", new Color(0.93f, 0.82f, 0.72f));
            Material clothes = CreateMaterial("M_Clothes", new Color(0.85f, 0.4f, 0.45f));
            Material hair = CreateMaterial("M_Hair", new Color(0.2f, 0.12f, 0.08f));

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(maya.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
            SetRendererMaterial(body, clothes);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(maya.transform);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = Vector3.one * 0.42f;
            SetRendererMaterial(head, skin);

            GameObject hairGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hairGO.name = "Hair";
            hairGO.transform.SetParent(maya.transform);
            hairGO.transform.localPosition = new Vector3(0f, 2.05f, -0.05f);
            hairGO.transform.localScale = new Vector3(0.46f, 0.22f, 0.46f);
            SetRendererMaterial(hairGO, hair);

            Partner partner = maya.AddComponent<Partner>();
            partner.skinMaterial = skin;
        }

        private static void EnsurePlayerNeeds()
        {
            GameObject playerGO = GameObject.Find("Player");
            if (playerGO == null) return;
            if (playerGO.GetComponent<PlayerNeeds>() == null)
            {
                playerGO.AddComponent<PlayerNeeds>();
            }
        }

        private static void EnsureHUD(GameManager gameManager)
        {
            // Remove any old HUD to avoid duplicates.
            GameObject oldHud = GameObject.Find("GameHUD CanvaWindow");
            if (oldHud != null) Object.DestroyImmediate(oldHud);

            GameObject canvasGO = new GameObject("GameHUD CanvaWindow");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Text money = CreateHudText("Money", canvasGO.transform, new Vector2(0.02f, 0.97f), new Vector2(0f, 1f), 60, TextAnchor.UpperLeft);
            Text day = CreateHudText("Day", canvasGO.transform, new Vector2(0.02f, 0.87f), new Vector2(0f, 1f), 44, TextAnchor.UpperLeft);
            Text time = CreateHudText("Time", canvasGO.transform, new Vector2(0.02f, 0.79f), new Vector2(0f, 1f), 44, TextAnchor.UpperLeft);
            Text work = CreateHudText("Work", canvasGO.transform, new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0f), 34, TextAnchor.LowerCenter);
            Text needs = CreateHudText("Needs", canvasGO.transform, new Vector2(0.02f, 0.71f), new Vector2(0f, 1f), 40, TextAnchor.UpperLeft);
            Text shop = CreateHudText("ShopMenu", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 38, TextAnchor.MiddleCenter);

            GameHUD hud = canvasGO.AddComponent<GameHUD>();
            hud.Configure(gameManager, money, time, day, work, needs, shop);
        }

        private static Text CreateHudText(string name, Transform parent, Vector2 anchor, Vector2 pivot, int size, TextAnchor align)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(700f, 60f);

            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
