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
        private struct BuildingInfo
        {
            public string name;
            public Vector3 pos;
            public float frontZ;
            public DoorInteractable.InteriorType interiorType;
        }

        private const string OpenWorldSceneName = "OpenWorld";
        private const string OpenWorldScenePath = "Assets/Scenes/OpenWorld/OpenWorld.unity";
        private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string MaterialsFolder = "Assets/Art/Environment/Materials";
        private const string TexturesFolder = "Assets/Art/Environment/Textures";

        private static string materialsFolder;

        private static bool BlockIfInPlayMode(string toolName)
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(toolName,
                    "You are in Play Mode. Press the Stop button (or Esc) to exit Play Mode first, then run this tool again.",
                    "OK");
                return true;
            }
            return false;
        }

        [MenuItem("Rise/Setup/Build OpenWorld Scene")]
        public static void BuildOpenWorldScene()
        {
            if (BlockIfInPlayMode("Build OpenWorld Scene")) return;

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

        [MenuItem("Rise/Setup/Rebuild Player")]
        public static void RebuildPlayer()
        {
            if (BlockIfInPlayMode("Rebuild Player")) return;

            Scene current = EditorSceneManager.GetActiveScene();
            if (current.name != OpenWorldSceneName)
            {
                if (!EditorUtility.DisplayDialog("Rebuild Player", "This expects the 'OpenWorld' scene to be active.\n\nContinue?", "Yes, open it", "Cancel"))
                    return;
                EditorSceneManager.OpenScene(OpenWorldScenePath);
            }

            materialsFolder = MaterialsFolder;
            Directory.CreateDirectory(materialsFolder);

            BuildPlayerRig();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: Player rig rebuilt as humanoid. Run Build Gameplay Systems to wire up.");
        }

        [MenuItem("Rise/Setup/Build Environment Details")]
        public static void BuildEnvironmentDetails()
        {
            if (BlockIfInPlayMode("Build Environment Details")) return;

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
            BuildGasStation();

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
            ground.transform.localScale = new Vector3(160f, 1f, 160f);
            SetRendererMaterial(ground, CreateDetailedMaterial("M_Grass", new Color(0.5f, 0.68f, 0.35f), 0.06f, 0.1f));

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "MainRoad";
            road.transform.SetParent(world);
            road.transform.position = new Vector3(0f, 0.05f, 0f);
            road.transform.localScale = new Vector3(12f, 0.1f, 120f);
            SetRendererMaterial(road, CreateDetailedMaterial("M_Road", new Color(0.32f, 0.32f, 0.34f), 0.05f, 0.2f));

            Transform town = GetOrCreateEmpty("Town", world);

            Material houseA = CreateDetailedMaterial("M_HouseA", new Color(0.86f, 0.8f, 0.66f), 0.04f, 0.15f);
            Material houseB = CreateDetailedMaterial("M_HouseB", new Color(0.62f, 0.5f, 0.38f), 0.06f, 0.15f);
            Material houseC = CreateDetailedMaterial("M_HouseC", new Color(0.78f, 0.72f, 0.58f), 0.04f, 0.15f);
            Material shop = CreateDetailedMaterial("M_Shop", new Color(0.74f, 0.74f, 0.76f), 0.05f, 0.15f);
            Material publicBldg = CreateDetailedMaterial("M_Public", new Color(0.82f, 0.80f, 0.78f), 0.03f, 0.2f);
            Material churchMat = CreateDetailedMaterial("M_Church", new Color(0.88f, 0.85f, 0.82f), 0.02f, 0.25f);
            Material roof = CreateDetailedMaterial("M_Roof", new Color(0.5f, 0.22f, 0.15f), 0.08f, 0.25f);
            Material roofDark = CreateDetailedMaterial("M_RoofDark", new Color(0.35f, 0.18f, 0.12f), 0.06f, 0.25f);

            BuildBuilding(town, "House_01", new Vector3(-40f, 0f, 18f), new Vector3(10f, 5f, 10f), houseA, roof);
            BuildBuilding(town, "House_02", new Vector3(-20f, 0f, 18f), new Vector3(10f, 5f, 10f), houseB, roof);
            BuildBuilding(town, "Shop_01", new Vector3(12f, 0f, 6f), new Vector3(9f, 4f, 10f), shop, roof);
            BuildBuilding(town, "Shop_02", new Vector3(12f, 0f, -14f), new Vector3(9f, 4f, 10f), shop, roof);
            BuildBuilding(town, "TownHall", new Vector3(0f, 0f, -30f), new Vector3(14f, 10f, 14f), houseA, roof);
            BuildBuilding(town, "Market_01", new Vector3(20f, 0f, 24f), new Vector3(7f, 3.5f, 11f), houseB, roof);
            BuildBuilding(town, "Market_02", new Vector3(-22f, 0f, 24f), new Vector3(7f, 3.5f, 11f), houseB, roof);

            GameObject crossA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossA.name = "CrossRoad_A";
            crossA.transform.SetParent(world);
            crossA.transform.position = new Vector3(0f, 0.04f, 24f);
            crossA.transform.localScale = new Vector3(120f, 0.1f, 10f);
            SetRendererMaterial(crossA, road.GetComponent<Renderer>().sharedMaterial);

            GameObject crossB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossB.name = "CrossRoad_B";
            crossB.transform.SetParent(world);
            crossB.transform.position = new Vector3(0f, 0.04f, -24f);
            crossB.transform.localScale = new Vector3(120f, 0.1f, 10f);
            SetRendererMaterial(crossB, road.GetComponent<Renderer>().sharedMaterial);

            BuildBuilding(town, "Church", new Vector3(0f, 0f, 42f), new Vector3(12f, 14f, 12f), churchMat, roofDark);
            Material steepleMat = CreateDetailedMaterial("M_Steeple", new Color(0.75f, 0.72f, 0.68f), 0.02f, 0.3f);
            GameObject steeple = GameObject.CreatePrimitive(PrimitiveType.Cube);
            steeple.name = "Church_Steeple";
            steeple.transform.SetParent(town);
            steeple.transform.position = new Vector3(0f, 20f, 38f);
            steeple.transform.localScale = new Vector3(3f, 6f, 3f);
            SetRendererMaterial(steeple, steepleMat);
            Object.DestroyImmediate(steeple.GetComponent<Collider>());
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Church_Spire";
            spire.transform.SetParent(town);
            spire.transform.position = new Vector3(0f, 25f, 38f);
            spire.transform.localScale = new Vector3(1.2f, 3f, 1.2f);
            SetRendererMaterial(spire, roofDark);
            Object.DestroyImmediate(spire.GetComponent<Collider>());

            BuildBuilding(town, "School", new Vector3(-36f, 0f, 32f), new Vector3(14f, 7f, 12f), publicBldg, roof);
            BuildBuilding(town, "Bakery", new Vector3(-22f, 0f, 36f), new Vector3(8f, 4f, 9f), shop, roof);
            BuildBuilding(town, "Bank", new Vector3(22f, 0f, 36f), new Vector3(10f, 5f, 10f), publicBldg, roof);
            BuildBuilding(town, "House_03", new Vector3(20f, 0f, 18f), new Vector3(10f, 5f, 10f), houseC, roof);
            BuildBuilding(town, "House_04", new Vector3(40f, 0f, 18f), new Vector3(10f, 5f, 10f), houseA, roofDark);
            BuildBuilding(town, "House_05", new Vector3(56f, 0f, 18f), new Vector3(10f, 5f, 10f), houseB, roof);
            BuildBuilding(town, "Restaurant", new Vector3(22f, 0f, -36f), new Vector3(9f, 5f, 10f), shop, roofDark);
            BuildBuilding(town, "PostOffice", new Vector3(-22f, 0f, -36f), new Vector3(10f, 5f, 10f), publicBldg, roof);
            BuildBuilding(town, "House_06", new Vector3(-40f, 0f, 52f), new Vector3(10f, 5f, 10f), houseC, roof);
            BuildBuilding(town, "House_07", new Vector3(-20f, 0f, 52f), new Vector3(10f, 5f, 10f), houseA, roofDark);
            BuildBuilding(town, "House_08", new Vector3(20f, 0f, 52f), new Vector3(10f, 5f, 10f), houseB, roof);
            BuildBuilding(town, "House_09", new Vector3(40f, 0f, 52f), new Vector3(10f, 5f, 10f), houseC, roofDark);
            BuildBuilding(town, "House_10", new Vector3(56f, 0f, 52f), new Vector3(10f, 5f, 10f), houseA, roof);
            BuildBuilding(town, "House_11", new Vector3(-40f, 0f, -55f), new Vector3(10f, 5f, 10f), houseA, roof);
            BuildBuilding(town, "House_12", new Vector3(-20f, 0f, -55f), new Vector3(10f, 5f, 10f), houseC, roofDark);
            BuildBuilding(town, "House_13", new Vector3(20f, 0f, -55f), new Vector3(10f, 5f, 10f), houseB, roof);
            BuildBuilding(town, "House_14", new Vector3(40f, 0f, -55f), new Vector3(10f, 5f, 10f), houseA, roofDark);
            BuildBuilding(town, "House_15", new Vector3(56f, 0f, -55f), new Vector3(10f, 5f, 10f), houseC, roof);

            Material parkGrass = CreateDetailedMaterial("M_ParkGrass", new Color(0.40f, 0.65f, 0.30f), 0.08f, 0.05f);
            GameObject park = GameObject.CreatePrimitive(PrimitiveType.Cube);
            park.name = "Park_Ground";
            park.transform.SetParent(town);
            park.transform.position = new Vector3(0f, 0.03f, 58f);
            park.transform.localScale = new Vector3(24f, 0.06f, 16f);
            SetRendererMaterial(park, parkGrass);
            Object.DestroyImmediate(park.GetComponent<Collider>());
        }

        private static void BuildBuilding(Transform parent, string name, Vector3 basePos, Vector3 baseSize, Material wallMat, Material roofMat)
        {
            Material foundationMat = CreateDetailedMaterial("M_Foundation", new Color(0.35f, 0.33f, 0.30f), 0.04f, 0.6f);
            Material trimMat = CreateDetailedMaterial("M_Trim_" + name, Color.Lerp(wallMat.color, Color.white, 0.15f), 0.02f, 0.3f);
            Material frameMat = CreateDetailedMaterial("M_Frame_" + name, new Color(0.90f, 0.87f, 0.82f), 0.01f, 0.35f);
            Material glassMat = CreateMaterial("M_Glass_" + name, new Color(0.16f, 0.26f, 0.42f, 0.85f));
            Material doorMat = CreateDetailedMaterial("M_Door_" + name, new Color(0.32f, 0.20f, 0.10f), 0.03f, 0.25f);
            Material stepMat = CreateDetailedMaterial("M_Step_" + name, new Color(0.55f, 0.53f, 0.50f), 0.03f, 0.5f);
            Material chimneyMat = CreateDetailedMaterial("M_Chimney_" + name, new Color(0.45f, 0.22f, 0.12f), 0.05f, 0.2f);
            Material awningMat = CreateMaterial("M_Awning_" + name, new Color(0.75f, 0.20f, 0.15f));
            Material windowFrameMat = CreateDetailedMaterial("M_WinFrame_" + name, new Color(0.88f, 0.85f, 0.80f), 0.01f, 0.3f);

            float foundationH = 0.6f;
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name = name + "_Foundation";
            foundation.transform.SetParent(parent);
            foundation.transform.position = basePos + Vector3.up * (foundationH * 0.5f);
            foundation.transform.localScale = new Vector3(baseSize.x + 0.6f, foundationH, baseSize.z + 0.6f);
            SetRendererMaterial(foundation, foundationMat);
            Object.DestroyImmediate(foundation.GetComponent<Collider>());

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name + "_Body";
            body.transform.SetParent(parent);
            body.transform.position = basePos + Vector3.up * (baseSize.y * 0.5f);
            body.transform.localScale = baseSize;
            SetRendererMaterial(body, wallMat);
            body.isStatic = true;

            float corniceH = 0.25f;
            float corniceY = basePos.y + baseSize.y - corniceH * 0.5f;
            float fx = baseSize.x * 0.5f + 0.15f;
            float fz = baseSize.z * 0.5f + 0.15f;

            AddPrim(parent, name + "_CorniceFront", new Vector3(basePos.x, corniceY, basePos.z + fz), new Vector3(baseSize.x + 0.4f, corniceH, 0.2f), trimMat);
            AddPrim(parent, name + "_CorniceBack", new Vector3(basePos.x, corniceY, basePos.z - fz), new Vector3(baseSize.x + 0.4f, corniceH, 0.2f), trimMat);
            AddPrim(parent, name + "_CorniceLeft", new Vector3(basePos.x - fx, corniceY, basePos.z), new Vector3(0.2f, corniceH, baseSize.z + 0.4f), trimMat);
            AddPrim(parent, name + "_CorniceRight", new Vector3(basePos.x + fx, corniceY, basePos.z), new Vector3(0.2f, corniceH, baseSize.z + 0.4f), trimMat);

            BuildGableRoof(parent, name, basePos, baseSize, roofMat, name == "TownHall" ? 2.5f : 1.6f);

            bool hasChimney = name.Contains("House") || name == "TownHall";
            if (hasChimney)
            {
                GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chimney.name = name + "_Chimney";
                chimney.transform.SetParent(parent);
                chimney.transform.position = new Vector3(basePos.x + baseSize.x * 0.25f, basePos.y + baseSize.y + 1.0f, basePos.z - baseSize.z * 0.2f);
                chimney.transform.localScale = new Vector3(0.8f, 1.8f, 0.8f);
                SetRendererMaterial(chimney, chimneyMat);
                Object.DestroyImmediate(chimney.GetComponent<Collider>());
            }

            AddWindowsAndDoorEnhanced(parent, name, basePos, baseSize, windowFrameMat, glassMat, doorMat, stepMat);

            bool isStorefront = name.Contains("Shop") || name.Contains("Market");
            if (isStorefront)
            {
                float halfZ = baseSize.z * 0.5f;
                float awningY = basePos.y + baseSize.y * 0.85f;
                float awningW = baseSize.x * 0.7f;
                float awningD = 1.5f;
                float awningAngle = -12f;

                GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                awning.name = name + "_Awning";
                awning.transform.SetParent(parent);
                awning.transform.position = new Vector3(basePos.x, awningY, basePos.z + halfZ + awningD * 0.45f);
                awning.transform.localScale = new Vector3(awningW, 0.12f, awningD);
                awning.transform.rotation = Quaternion.Euler(awningAngle, 0f, 0f);
                SetRendererMaterial(awning, awningMat);
                Object.DestroyImmediate(awning.GetComponent<Collider>());

                Material awningStripeMat = CreateMaterial("M_AwningStripe_" + name, Color.white);
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = name + "_AwningStripe";
                stripe.transform.SetParent(awning.transform);
                stripe.transform.localPosition = new Vector3(0f, 0.02f, 0.3f);
                stripe.transform.localScale = new Vector3(1f, 0.6f, 0.3f);
                stripe.transform.localRotation = Quaternion.identity;
                SetRendererMaterial(stripe, awningStripeMat);
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
            }
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

        private static void AddWindowsAndDoorEnhanced(Transform parent, string name, Vector3 basePos, Vector3 baseSize,
            Material frameMat, Material glassMat, Material doorMat, Material stepMat)
        {
            Vector3 half = baseSize * 0.5f;
            float inset = 0.05f;
            float winY = basePos.y + baseSize.y * 0.55f;
            float winH = baseSize.y * 0.28f;
            float winW = baseSize.x * 0.22f;
            float sideW = baseSize.z * 0.22f;
            float frameP = 0.12f;
            float winDepth = 0.15f;

            for (int side = -1; side <= 1; side += 2)
            {
                float z = basePos.z + (side > 0 ? half.z + inset : -half.z - inset);
                Vector3[] frontWins =
                {
                    new Vector3(basePos.x - baseSize.x * 0.26f, winY, z),
                    new Vector3(basePos.x + baseSize.x * 0.26f, winY, z)
                };
                foreach (Vector3 wpos in frontWins)
                {
                    AddPrim(parent, name + "_WinFrame", wpos, new Vector3(winW + frameP, winH + frameP, winDepth + 0.04f), frameMat);
                    AddPrim(parent, name + "_WinGlass", wpos, new Vector3(winW, winH, winDepth), glassMat);
                }

                Vector3[] sideWins =
                {
                    new Vector3(basePos.x - half.x - inset, winY, basePos.z - baseSize.z * 0.2f),
                    new Vector3(basePos.x + half.x + inset, winY, basePos.z + baseSize.z * 0.2f)
                };
                foreach (Vector3 wpos in sideWins)
                {
                    AddPrim(parent, name + "_WinFrameSide", wpos, new Vector3(winDepth + 0.04f, winH + frameP, sideW + frameP), frameMat);
                    AddPrim(parent, name + "_WinGlassSide", wpos, new Vector3(winDepth, winH, sideW), glassMat);
                }
            }

            float doorW = Mathf.Min(1.8f, baseSize.x * 0.3f);
            float doorH = Mathf.Min(3.4f, baseSize.y * 0.72f);
            float doorZ = basePos.z + half.z + inset;
            AddPrim(parent, name + "_DoorFrame", new Vector3(basePos.x, basePos.y + doorH * 0.5f, doorZ), new Vector3(doorW + 0.25f, doorH + 0.2f, 0.12f), frameMat);
            AddPrim(parent, name + "_DoorPanel", new Vector3(basePos.x, basePos.y + doorH * 0.5f, doorZ + 0.02f), new Vector3(doorW, doorH, 0.15f), doorMat);
            AddPrim(parent, name + "_DoorStep", new Vector3(basePos.x, basePos.y + 0.12f, doorZ + 0.7f), new Vector3(doorW + 0.6f, 0.24f, 0.8f), stepMat);

            AddPrim(parent, name + "_Knob", new Vector3(basePos.x + doorW * 0.35f, basePos.y + doorH * 0.45f, doorZ + 0.1f), new Vector3(0.08f, 0.08f, 0.06f), frameMat);
        }

        private static void BuildPlayerRig()
        {
            Transform world = GameObject.Find("World").transform;
            Transform oldRig = world.Find("PlayerRig");
            if (oldRig != null) Object.DestroyImmediate(oldRig.gameObject);

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

            Transform humanoid = BuildHumanoid(playerGO.transform, "Humanoid", Vector3.zero,
                new Color(0.88f, 0.76f, 0.64f),
                new Color(0.25f, 0.45f, 0.85f),
                new Color(0.20f, 0.20f, 0.25f),
                new Color(0.25f, 0.15f, 0.08f),
                1f);
            humanoid.localPosition = Vector3.zero;

            humanoid.gameObject.AddComponent<WalkAnimation>();
            playerGO.AddComponent<PlayerAppearance>();

            GameObject pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(rig);
            pivot.transform.localPosition = new Vector3(0f, 1f, 0f);
            pivot.transform.localRotation = Quaternion.identity;

            GameObject cmGO = new GameObject("CM Player Camera");
            cmGO.transform.SetParent(rig);
            cmGO.transform.position = pivot.transform.position;

            CinemachineCamera cmCamera = cmGO.AddComponent<CinemachineCamera>();
            cmCamera.Follow = pivot.transform;

            Transform headLook = humanoid.Find("Head");
            if (headLook == null) headLook = playerGO.transform;
            cmCamera.LookAt = headLook;

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
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun == null)
            {
                GameObject sunGO = new GameObject("Directional Light");
                sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun = sunGO.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;

            Light[] allLights = Object.FindObjectsByType<Light>();
            bool moonExists = false;
            foreach (Light l in allLights)
            {
                if (l.gameObject.name == "Moon Light") { moonExists = true; break; }
            }
            if (!moonExists)
            {
                GameObject moonGO = new GameObject("Moon Light");
                moonGO.transform.rotation = Quaternion.Euler(200f, 30f, 0f);
                Light moon = moonGO.AddComponent<Light>();
                moon.type = LightType.Directional;
                moon.color = new Color(0.5f, 0.55f, 0.7f);
                moon.intensity = 0.4f;
                moon.shadows = LightShadows.None;
            }
        }

        private static void EnsureCameraBrain()
        {
            Camera cam = Object.FindAnyObjectByType<Camera>();
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

            Light sun = Object.FindAnyObjectByType<Light>();
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
            BuildBulletinBoard(details);
        }

        private static void BuildBulletinBoard(Transform parent)
        {
            string name = "BulletinBoard";
            GameObject old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);

            Material boardMat = CreateMaterial("M_BulletinBoard", new Color(0.55f, 0.38f, 0.22f));
            Material postMat = CreateMaterial("M_BulletinPost", new Color(0.45f, 0.30f, 0.18f));

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = name;
            board.transform.SetParent(parent);
            board.transform.position = new Vector3(3f, 1.5f, -28f);
            board.transform.localScale = new Vector3(2f, 1.5f, 0.15f);
            Object.DestroyImmediate(board.GetComponent<Renderer>());
            MeshRenderer mr = board.AddComponent<MeshRenderer>();
            mr.material = boardMat;

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name + "_Post";
            post.transform.SetParent(board.transform);
            post.transform.localPosition = new Vector3(0f, -1f, 0f);
            post.transform.localScale = new Vector3(0.12f, 1f, 0.12f);
            Object.DestroyImmediate(post.GetComponent<Renderer>());
            MeshRenderer pmr = post.AddComponent<MeshRenderer>();
            pmr.material = postMat;

            board.AddComponent<BulletinBoard>();
        }

        private static void BuildGasStation()
        {
            GameObject worldGO = GameObject.Find("World");
            Transform world = worldGO != null ? worldGO.transform : new GameObject("World").transform;

            Transform old = world.Find("GasStation");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Transform station = new GameObject("GasStation").transform;
            station.SetParent(world);
            station.position = new Vector3(40f, 0f, 0f);

            Material pumpMat = CreateMaterial("M_GasPump", new Color(0.8f, 0.2f, 0.15f));
            Material canopyMat = CreateMaterial("M_GasCanopy", new Color(0.85f, 0.85f, 0.88f));
            Material poleMat = CreateMaterial("M_GasPole", new Color(0.5f, 0.5f, 0.52f));
            Material signMat = CreateMaterial("M_GasSign", new Color(0.1f, 0.5f, 0.8f));

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "Canopy";
            canopy.transform.SetParent(station);
            canopy.transform.position = new Vector3(0f, 3.5f, 0f);
            canopy.transform.localScale = new Vector3(8f, 0.2f, 5f);
            SetRendererMaterial(canopy, canopyMat);

            for (int i = 0; i < 4; i++)
            {
                float x = i < 2 ? -3f : 3f;
                float z = i % 2 == 0 ? -1.5f : 1.5f;
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Pole_" + i;
                pole.transform.SetParent(station);
                pole.transform.position = new Vector3(x, 1.75f, z);
                pole.transform.localScale = new Vector3(0.15f, 1.75f, 0.15f);
                SetRendererMaterial(pole, poleMat);
            }

            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -1.5f : 1.5f;
                GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pump.name = "Pump_" + i;
                pump.transform.SetParent(station);
                pump.transform.position = new Vector3(0f, 0.75f, z);
                pump.transform.localScale = new Vector3(0.6f, 1.5f, 0.4f);
                SetRendererMaterial(pump, pumpMat);

                GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = "Nozzle_" + i;
                nozzle.transform.SetParent(pump.transform);
                nozzle.transform.localPosition = new Vector3(0.4f, 0.3f, 0f);
                nozzle.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);
                nozzle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                SetRendererMaterial(nozzle, poleMat);
            }

            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(station);
            sign.transform.position = new Vector3(0f, 4.5f, 0f);
            sign.transform.localScale = new Vector3(3f, 0.8f, 0.1f);
            SetRendererMaterial(sign, signMat);

            GameObject signLabel = new GameObject("SignLabel");
            signLabel.transform.SetParent(sign.transform);
            signLabel.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            Canvas signCanvas = signLabel.AddComponent<Canvas>();
            signCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform signRect = signLabel.GetComponent<RectTransform>();
            signRect.sizeDelta = new Vector2(3f, 0.8f);
            TextMesh signText = signLabel.AddComponent<TextMesh>();
            signText.text = "GAS";
            signText.fontSize = 60;
            signText.characterSize = 0.15f;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.alignment = TextAlignment.Center;
            signText.color = Color.white;

            GasStation gas = station.gameObject.AddComponent<GasStation>();

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "GasPad";
            pad.transform.SetParent(station);
            pad.transform.position = new Vector3(0f, 0.02f, 0f);
            pad.transform.localScale = new Vector3(10f, 0.04f, 7f);
            Material asphalt = CreateDetailedMaterial("M_GasAsphalt", new Color(0.22f, 0.22f, 0.24f), 0.02f, 0.8f);
            SetRendererMaterial(pad, asphalt);
        }

        private static void BuildTownspeople(Transform parent, Material skin)
        {
            Transform npcs = new GameObject("Townspeople").transform;
            npcs.SetParent(parent);

            Color[] skinTones =
            {
                new Color(0.88f, 0.76f, 0.64f), new Color(0.80f, 0.68f, 0.55f),
                new Color(0.92f, 0.80f, 0.68f), new Color(0.75f, 0.60f, 0.48f),
                new Color(0.85f, 0.72f, 0.58f), new Color(0.90f, 0.78f, 0.66f),
                new Color(0.82f, 0.70f, 0.56f),
                new Color(0.86f, 0.74f, 0.62f), new Color(0.78f, 0.64f, 0.52f),
                new Color(0.84f, 0.72f, 0.60f), new Color(0.91f, 0.80f, 0.70f),
                new Color(0.76f, 0.62f, 0.50f), new Color(0.83f, 0.71f, 0.59f)
            };

            Color[] shirts =
            {
                new Color(0.85f, 0.40f, 0.45f), new Color(0.35f, 0.55f, 0.85f),
                new Color(0.40f, 0.70f, 0.45f), new Color(0.85f, 0.75f, 0.30f),
                new Color(0.60f, 0.45f, 0.75f), new Color(0.20f, 0.60f, 0.65f),
                new Color(0.75f, 0.30f, 0.30f),
                new Color(0.30f, 0.50f, 0.35f), new Color(0.70f, 0.25f, 0.45f),
                new Color(0.45f, 0.55f, 0.70f), new Color(0.80f, 0.50f, 0.30f),
                new Color(0.25f, 0.40f, 0.60f), new Color(0.55f, 0.65f, 0.40f)
            };

            Color[] pants =
            {
                new Color(0.25f, 0.22f, 0.18f), new Color(0.20f, 0.20f, 0.30f),
                new Color(0.35f, 0.30f, 0.25f), new Color(0.15f, 0.15f, 0.18f),
                new Color(0.30f, 0.25f, 0.20f), new Color(0.18f, 0.22f, 0.35f),
                new Color(0.22f, 0.18f, 0.15f),
                new Color(0.28f, 0.24f, 0.20f), new Color(0.18f, 0.18f, 0.22f),
                new Color(0.32f, 0.28f, 0.22f), new Color(0.20f, 0.20f, 0.25f),
                new Color(0.25f, 0.20f, 0.18f), new Color(0.15f, 0.15f, 0.20f)
            };

            Color[] hairs =
            {
                new Color(0.40f, 0.30f, 0.18f), new Color(0.60f, 0.40f, 0.20f),
                new Color(0.15f, 0.10f, 0.06f), new Color(0.70f, 0.50f, 0.25f),
                new Color(0.30f, 0.20f, 0.10f), new Color(0.50f, 0.35f, 0.18f),
                new Color(0.20f, 0.15f, 0.08f),
                new Color(0.45f, 0.32f, 0.16f), new Color(0.55f, 0.38f, 0.20f),
                new Color(0.10f, 0.08f, 0.05f), new Color(0.65f, 0.45f, 0.22f),
                new Color(0.35f, 0.25f, 0.12f), new Color(0.25f, 0.18f, 0.10f)
            };

            float[] heights =
            {
                0.95f, 1.0f, 1.05f, 0.90f, 1.10f, 0.95f, 1.0f,
                0.92f, 1.08f, 0.98f, 1.02f, 0.96f, 1.05f
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

            Vector3[] crossNorth =
            {
                new Vector3(-36f, 0f, 24f), new Vector3(-10f, 0f, 24f),
                new Vector3(10f, 0f, 24f), new Vector3(36f, 0f, 24f)
            };

            Vector3[] crossSouth =
            {
                new Vector3(-36f, 0f, -24f), new Vector3(-10f, 0f, -24f),
                new Vector3(10f, 0f, -24f), new Vector3(36f, 0f, -24f)
            };

            Vector3[] northLoop =
            {
                new Vector3(-20f, 0f, 38f), new Vector3(0f, 0f, 38f),
                new Vector3(20f, 0f, 38f), new Vector3(20f, 0f, 48f),
                new Vector3(0f, 0f, 48f), new Vector3(-20f, 0f, 48f)
            };

            Vector3[] southLoop =
            {
                new Vector3(-20f, 0f, -38f), new Vector3(0f, 0f, -38f),
                new Vector3(20f, 0f, -38f), new Vector3(20f, 0f, -48f),
                new Vector3(0f, 0f, -48f), new Vector3(-20f, 0f, -48f)
            };

            BuildCitizen(npcs, "Citizen_1", new Vector3(0f, 0f, -16f), roadRoute,
                skinTones[0], shirts[0], pants[0], hairs[0], heights[0], 1.2f,
                "Old Thomas", new[] { "I've lived in this town for forty years.", "The market used to be twice this size.", "Come back when you're older, kid." }, "Marriage keeps you humble.", NPCBehavior.Guard, "House_01");

            BuildCitizen(npcs, "Citizen_2", new Vector3(0f, 0f, 24f), roadRoute,
                skinTones[1], shirts[1], pants[1], hairs[1], heights[1], 1.1f,
                "Bella", new[] { "Welcome to town! Everything's better with a smile.", "Try the shop near the square, good prices.", "I hope you find what you're looking for." }, "Love makes every day brighter.", NPCBehavior.Wander, "House_02");

            BuildCitizen(npcs, "Citizen_3", new Vector3(-4f, 0f, 26f), shopRoute,
                skinTones[2], shirts[2], pants[2], hairs[2], heights[2], 1.15f,
                "Grocer Mark", new[] { "Fresh bread every morning, don't miss it.", "Business has been slow lately.", "Stop by and say hello sometime." }, "My wife runs the best bakery.", NPCBehavior.Stand, "Shop_01");

            BuildCitizen(npcs, "Citizen_4", new Vector3(4f, 0f, 26f), shopRoute,
                skinTones[3], shirts[3], pants[3], hairs[3], heights[3], 0.95f,
                "Lucy", new[] { "The flowers here are beautiful, aren't they?", "I work at the flower stand.", "A little kindness goes a long way." }, "My husband helps at the market.", NPCBehavior.Wander, "Shop_02");

            BuildCitizen(npcs, "Citizen_5", new Vector3(16f, 0f, 20f), marketEast,
                skinTones[4], shirts[4], pants[4], hairs[4], heights[4], 1.2f,
                "Farmer Joe", new[] { "I grow the best vegetables in the county.", "The soil here is rich and good.", "Work hard, eat well, sleep tight." }, "My wife brings me lunch every day.", NPCBehavior.Stand, "Market_01");

            BuildCitizen(npcs, "Citizen_6", new Vector3(-16f, 0f, 20f), marketWest,
                skinTones[5], shirts[5], pants[5], hairs[5], heights[5], 1.0f,
                "Millie", new[] { "I teach the children at the schoolhouse.", "Education opens every door.", "Keep your chin up, things will improve." }, "My sweetheart brings me flowers.", NPCBehavior.Wander, "School");

            BuildCitizen(npcs, "Citizen_7", new Vector3(0f, 0f, 32f), roadRoute,
                skinTones[6], shirts[6], pants[6], hairs[6], heights[6], 1.1f,
                "Sam", new[] { "The town hall is where you get your papers.", "I'm in charge of keeping the roads clean.", "It's a living, not much else to say.", "The mayor's a good man, listen to him.", "Stay out of trouble and you'll be fine." }, "My partner keeps me in line.", NPCBehavior.Route, "TownHall");

            BuildCitizen(npcs, "Citizen_8", new Vector3(0f, 0f, 42f), northLoop,
                skinTones[7], shirts[7], pants[7], hairs[7], heights[7], 0.9f,
                "Pastor John", new[] { "The church welcomes all.", "Prayer and community keep us strong.", "Visit the school when you have time." }, "My wife plays the organ beautifully.", NPCBehavior.Stand, "Church");

            BuildCitizen(npcs, "Citizen_9", new Vector3(-36f, 0f, 32f), crossNorth,
                skinTones[8], shirts[8], pants[8], hairs[8], heights[8], 1.15f,
                "Miss Elena", new[] { "I teach at the new school.", "Children are the future of this town.", "Education is the greatest gift.", "The library is open after class." }, "My partner supports all my dreams.", NPCBehavior.Stand, "House_03");

            BuildCitizen(npcs, "Citizen_10", new Vector3(-22f, 0f, 36f), crossNorth,
                skinTones[9], shirts[9], pants[9], hairs[9], heights[9], 1.0f,
                "Baker Rosa", new[] { "Fresh pastries every morning at dawn.", "The secret is in the flour.", "Come try the cinnamon rolls!", "Business is better than ever." }, "My sweetheart helps knead the dough.", NPCBehavior.Stand, "Bakery");

            BuildCitizen(npcs, "Citizen_11", new Vector3(22f, 0f, 36f), crossSouth,
                skinTones[10], shirts[10], pants[10], hairs[10], heights[10], 1.05f,
                "Mr. Carter", new[] { "The bank is open from nine to five.", "Save your money wisely.", "Good credit opens every door.", "We have plans for every budget." }, "My wife handles all our finances.", NPCBehavior.Wander, "Bank");

            BuildCitizen(npcs, "Citizen_12", new Vector3(22f, 0f, -36f), southLoop,
                skinTones[11], shirts[11], pants[11], hairs[11], heights[11], 1.0f,
                "Chef Marco", new[] { "The best food in town, guaranteed.", "I learned to cook in the city.", "Try the special today.", "Every dish tells a story." }, "My partner taste-tests everything.", NPCBehavior.Stand, "Restaurant");

            BuildCitizen(npcs, "Citizen_13", new Vector3(-22f, 0f, -36f), southLoop,
                skinTones[12], shirts[12], pants[12], hairs[12], heights[12], 1.1f,
                "Officer Dan", new[] { "The post office is always here for you.", "Letters connect people far and wide.", "I walk this route rain or shine.", "Every delivery matters.", "The town keeps growing, keeps me busy." }, "My partner waits for me at home.", NPCBehavior.Route, "PostOffice");
        }

        private static void BuildCitizen(Transform parent, string name, Vector3 start, Vector3[] route,
            Color skinColor, Color shirtColor, Color pantsColor, Color hairColor, float height, float walkSpeed,
            string npcName, string[] lines, string marriedLine, NPCBehavior behavior = NPCBehavior.Route,
            string homeBuilding = "", int homeEnter = 18, int homeLeave = 6)
        {
            BuildHumanoid(parent, name, start, skinColor, shirtColor, pantsColor, hairColor, height);

            GameObject npcGO = parent.Find(name).gameObject;

            TownNPC town = npcGO.AddComponent<TownNPC>();
            town.npcName = npcName;
            town.lines = lines;
            town.marriedLine = marriedLine;
            town.bodyTint = shirtColor;
            town.walkSpeed = walkSpeed;
            town.behavior = behavior;
            town.homeBuilding = homeBuilding;
            town.homeHourEnter = homeEnter;
            town.homeHourLeave = homeLeave;
            if (behavior == NPCBehavior.Route)
                town.SetRoute(route);

            CharacterController cc = npcGO.AddComponent<CharacterController>();
            cc.height = 2f * height;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1f * height, 0f);

            npcGO.AddComponent<WalkAnimation>();
        }

        private static void BuildRoadLines(Transform parent, Material mat)
        {
            for (int z = -56; z <= 56; z += 6)
            {
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = "RoadDash";
                dash.transform.SetParent(parent);
                dash.transform.position = new Vector3(0f, 0.11f, z);
                dash.transform.localScale = new Vector3(0.15f, 0.04f, 2.2f);
                SetRendererMaterial(dash, mat);
            }
            for (int x = -56; x <= 56; x += 6)
            {
                GameObject dashA = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dashA.name = "CrossDash_A";
                dashA.transform.SetParent(parent);
                dashA.transform.position = new Vector3(x, 0.10f, 24f);
                dashA.transform.localScale = new Vector3(2.2f, 0.04f, 0.15f);
                SetRendererMaterial(dashA, mat);

                GameObject dashB = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dashB.name = "CrossDash_B";
                dashB.transform.SetParent(parent);
                dashB.transform.position = new Vector3(x, 0.10f, -24f);
                dashB.transform.localScale = new Vector3(2.2f, 0.04f, 0.15f);
                SetRendererMaterial(dashB, mat);
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
                new Vector3(-28f, 0f, -12f), new Vector3(28f, 0f, -12f),
                new Vector3(-8f, 0f, -52f), new Vector3(8f, 0f, -52f),
                new Vector3(-8f, 0f, 52f), new Vector3(8f, 0f, 52f),
                new Vector3(-40f, 0f, 42f), new Vector3(40f, 0f, 42f),
                new Vector3(-40f, 0f, -42f), new Vector3(40f, 0f, -42f),
                new Vector3(-50f, 0f, 20f), new Vector3(50f, 0f, 20f),
                new Vector3(-50f, 0f, -20f), new Vector3(50f, 0f, -20f),
                new Vector3(-50f, 0f, 50f), new Vector3(50f, 0f, 50f),
                new Vector3(-50f, 0f, -50f), new Vector3(50f, 0f, -50f),
                new Vector3(-10f, 0f, 54f), new Vector3(10f, 0f, 54f),
                new Vector3(-10f, 0f, 62f), new Vector3(10f, 0f, 62f),
                new Vector3(-30f, 0f, 54f), new Vector3(30f, 0f, 54f),
                new Vector3(-30f, 0f, -54f), new Vector3(30f, 0f, -54f)
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
                new Rect(-22f - 3.5f - margin, 24f - 5.5f - margin, 7f + margin * 2f, 11f + margin * 2f),
                new Rect(0f - 6f - margin, 42f - 6f - margin, 12f + margin * 2f, 12f + margin * 2f),
                new Rect(-36f - 7f - margin, 32f - 6f - margin, 14f + margin * 2f, 12f + margin * 2f),
                new Rect(-22f - 4f - margin, 36f - 4.5f - margin, 8f + margin * 2f, 9f + margin * 2f),
                new Rect(22f - 5f - margin, 36f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(36f - 5f - margin, 32f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(16f - 5f - margin, 46f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(-16f - 5f - margin, 46f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(22f - 4.5f - margin, -36f - 5f - margin, 9f + margin * 2f, 10f + margin * 2f),
                new Rect(-22f - 5f - margin, -36f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(16f - 5f - margin, -46f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f),
                new Rect(-16f - 5f - margin, -46f - 5f - margin, 10f + margin * 2f, 10f + margin * 2f)
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
                new Vector3(0f, 0f, -40f), new Vector3(0f, 0f, 40f),
                new Vector3(-30f, 0f, 28f), new Vector3(30f, 0f, 28f),
                new Vector3(-30f, 0f, -28f), new Vector3(30f, 0f, -28f),
                new Vector3(-45f, 0f, 40f), new Vector3(45f, 0f, 40f),
                new Vector3(-45f, 0f, -40f), new Vector3(45f, 0f, -40f),
                new Vector3(-10f, 0f, 50f), new Vector3(10f, 0f, 50f),
                new Vector3(-10f, 0f, -50f), new Vector3(10f, 0f, -50f)
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
                new Vector3(2f, 0f, -8f), new Vector3(-2f, 0f, 20f),
                new Vector3(-48f, 0f, 50f), new Vector3(48f, 0f, 50f),
                new Vector3(-48f, 0f, -50f), new Vector3(48f, 0f, -50f),
                new Vector3(-40f, 0f, -10f), new Vector3(40f, 0f, -10f)
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
                new Vector3(-24f, 0f, 21f), new Vector3(-23f, 0f, 22f), new Vector3(-25f, 0f, 23f),
                new Vector3(-8f, 0f, 50f), new Vector3(8f, 0f, 50f), new Vector3(0f, 0f, 52f),
                new Vector3(-40f, 0f, 28f), new Vector3(40f, 0f, 28f),
                new Vector3(-40f, 0f, -28f), new Vector3(40f, 0f, -28f)
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
            int[] zs = { -52, -36, -20, -5, 10, 25, 38, 52 };
            foreach (int z in zs)
            {
                BuildLamp(parent, new Vector3(-6.5f, 0f, z), pole, head);
                BuildLamp(parent, new Vector3(6.5f, 0f, z), pole, head);
            }
            int[] crossXs = { -40, -20, 20, 40 };
            foreach (int x in crossXs)
            {
                BuildLamp(parent, new Vector3(x, 0f, 19f), pole, head);
                BuildLamp(parent, new Vector3(x, 0f, 29f), pole, head);
                BuildLamp(parent, new Vector3(x, 0f, -19f), pole, head);
                BuildLamp(parent, new Vector3(x, 0f, -29f), pole, head);
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
            BuildFence(parent, new Vector3(42f, 0f, 28f), new Vector3(42f, 0f, 36f), mat);
            BuildFence(parent, new Vector3(-42f, 0f, 28f), new Vector3(-42f, 0f, 36f), mat);
            BuildFence(parent, new Vector3(12f, 0f, -42f), new Vector3(12f, 0f, -50f), mat);
            BuildFence(parent, new Vector3(-12f, 0f, -42f), new Vector3(-12f, 0f, -50f), mat);
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

        private static Transform BuildHumanoid(Transform parent, string name, Vector3 position,
            Color skinColor, Color shirtColor, Color pantsColor, Color hairColor, float scale = 1f)
        {
            Material skin = CreateMaterial("M_Skin_" + name, skinColor);
            Material shirt = CreateMaterial("M_Shirt_" + name, shirtColor);
            Material pants = CreateMaterial("M_Pants_" + name, pantsColor);
            Material hair = CreateMaterial("M_Hair_" + name, hairColor);

            GameObject root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = position;

            float s = scale;

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Body_Torso";
            torso.transform.SetParent(root.transform);
            torso.transform.localPosition = new Vector3(0f, 1.05f * s, 0f);
            torso.transform.localScale = new Vector3(0.50f * s, 0.50f * s, 0.30f * s);
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            SetRendererMaterial(torso, shirt);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(0f, 1.70f * s, 0f);
            head.transform.localScale = Vector3.one * 0.38f * s;
            Object.DestroyImmediate(head.GetComponent<Collider>());
            SetRendererMaterial(head, skin);

            GameObject hairGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hairGO.name = "Hair";
            hairGO.transform.SetParent(root.transform);
            hairGO.transform.localPosition = new Vector3(0f, 1.82f * s, -0.02f * s);
            hairGO.transform.localScale = new Vector3(0.42f * s, 0.20f * s, 0.42f * s);
            Object.DestroyImmediate(hairGO.GetComponent<Collider>());
            SetRendererMaterial(hairGO, hair);

            GameObject legPivotL = new GameObject("LegPivot_L");
            legPivotL.transform.SetParent(root.transform);
            legPivotL.transform.localPosition = new Vector3(-0.14f * s, 0.72f * s, 0f);
            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            legL.name = "Leg_L";
            legL.transform.SetParent(legPivotL.transform);
            legL.transform.localPosition = new Vector3(0f, -0.37f * s, 0f);
            legL.transform.localScale = new Vector3(0.18f * s, 0.40f * s, 0.18f * s);
            Object.DestroyImmediate(legL.GetComponent<Collider>());
            SetRendererMaterial(legL, pants);

            GameObject legPivotR = new GameObject("LegPivot_R");
            legPivotR.transform.SetParent(root.transform);
            legPivotR.transform.localPosition = new Vector3(0.14f * s, 0.72f * s, 0f);
            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            legR.name = "Leg_R";
            legR.transform.SetParent(legPivotR.transform);
            legR.transform.localPosition = new Vector3(0f, -0.37f * s, 0f);
            legR.transform.localScale = new Vector3(0.18f * s, 0.40f * s, 0.18f * s);
            Object.DestroyImmediate(legR.GetComponent<Collider>());
            SetRendererMaterial(legR, pants);

            GameObject armPivotL = new GameObject("ArmPivot_L");
            armPivotL.transform.SetParent(root.transform);
            armPivotL.transform.localPosition = new Vector3(-0.27f * s, 1.28f * s, 0f);
            GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            armL.name = "Arm_L";
            armL.transform.SetParent(armPivotL.transform);
            armL.transform.localPosition = new Vector3(0f, -0.22f * s, 0f);
            armL.transform.localScale = new Vector3(0.15f * s, 0.38f * s, 0.15f * s);
            Object.DestroyImmediate(armL.GetComponent<Collider>());
            SetRendererMaterial(armL, shirt);

            GameObject handL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handL.name = "Hand_L";
            handL.transform.SetParent(armPivotL.transform);
            handL.transform.localPosition = new Vector3(0f, -0.60f * s, 0f);
            handL.transform.localScale = Vector3.one * 0.14f * s;
            Object.DestroyImmediate(handL.GetComponent<Collider>());
            SetRendererMaterial(handL, skin);

            GameObject armPivotR = new GameObject("ArmPivot_R");
            armPivotR.transform.SetParent(root.transform);
            armPivotR.transform.localPosition = new Vector3(0.27f * s, 1.28f * s, 0f);
            GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            armR.name = "Arm_R";
            armR.transform.SetParent(armPivotR.transform);
            armR.transform.localPosition = new Vector3(0f, -0.22f * s, 0f);
            armR.transform.localScale = new Vector3(0.15f * s, 0.38f * s, 0.15f * s);
            Object.DestroyImmediate(armR.GetComponent<Collider>());
            SetRendererMaterial(armR, shirt);

            GameObject handR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handR.name = "Hand_R";
            handR.transform.SetParent(armPivotR.transform);
            handR.transform.localPosition = new Vector3(0f, -0.60f * s, 0f);
            handR.transform.localScale = Vector3.one * 0.14f * s;
            Object.DestroyImmediate(handR.GetComponent<Collider>());
            SetRendererMaterial(handR, skin);

            return root.transform;
        }

        [MenuItem("Rise/Setup/Build Gameplay Systems")]
        public static void BuildGameplaySystems()
        {
            if (BlockIfInPlayMode("Build Gameplay Systems")) return;

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
            JobDefinition delivery = EnsureJobAsset("Job_DeliveryDriver", "Delivery Driver", 45, 100);
            JobDefinition farmer = EnsureJobAsset("Job_Farmer", "Farmer", 40, 50);
            JobDefinition baker = EnsureJobAsset("Job_Baker", "Baker", 55, 300);
            JobDefinition chef = EnsureJobAsset("Job_Chef", "Chef", 75, 500);
            JobDefinition bankTeller = EnsureJobAsset("Job_BankTeller", "Bank Teller", 90, 750);
            EnsureWorkStations(general, cashier, manager, delivery, farmer, baker, chef, bankTeller);
            EnsurePlayerNeeds();
            EnsureShop();
            EnsureClothingShop();
            EnsurePartner();
            EnsureRival();
            EnsureHUD(gameManager);
            EnsureDoorTriggers();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: Gameplay systems built. Press Play, walk to the yellow work spot, and press E.");
        }

        [MenuItem("Rise/Setup/Build Cars")]
        public static void BuildCars()
        {
            if (BlockIfInPlayMode("Build Cars")) return;

            Scene current = EditorSceneManager.GetActiveScene();
            if (current.name != OpenWorldSceneName)
            {
                if (!EditorUtility.DisplayDialog("Build Cars", "This expects the 'OpenWorld' scene to be active.\n\nContinue?", "Yes, open it", "Cancel"))
                    return;
                EditorSceneManager.OpenScene(OpenWorldScenePath);
            }

            materialsFolder = MaterialsFolder;
            Directory.CreateDirectory(materialsFolder);

            EnsureParkingLot();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), OpenWorldScenePath);

            Debug.Log("Rise: Cars and parking lot built.");
        }

        private static GameManager EnsureGameManager()
        {
            GameManager existing = Object.FindAnyObjectByType<GameManager>();
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

        private static void EnsureWorkStations(JobDefinition general, JobDefinition cashier, JobDefinition manager,
            JobDefinition delivery, JobDefinition farmer, JobDefinition baker, JobDefinition chef, JobDefinition bankTeller)
        {
            GameObject old = GameObject.Find("WorkSpot_Shop");
            if (old != null) Object.DestroyImmediate(old);

            EnsureStation(general, new Vector3(12f, 0.25f, 14f), "WorkSpot_General");
            EnsureStation(cashier, new Vector3(13f, 0.25f, -6f), "WorkSpot_Cashier");
            EnsureStation(manager, new Vector3(0f, 0.25f, -22f), "WorkSpot_Manager");
            EnsureStation(delivery, new Vector3(-22f, 0.25f, -30f), "WorkSpot_Delivery");
            EnsureStation(farmer, new Vector3(20f, 0.25f, 20f), "WorkSpot_Farmer");
            EnsureStation(baker, new Vector3(-22f, 0.25f, 32f), "WorkSpot_Baker");
            EnsureStation(chef, new Vector3(22f, 0.25f, -30f), "WorkSpot_Chef");
            EnsureStation(bankTeller, new Vector3(22f, 0.25f, 32f), "WorkSpot_BankTeller");
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
            ShopStand oldShop = Object.FindAnyObjectByType<ShopStand>();
            if (oldShop != null)
            {
                Transform oldParent = oldShop.transform.parent;
                if (oldParent != null && oldParent.name == "ShopBuilding") Object.DestroyImmediate(oldParent.gameObject);
                else Object.DestroyImmediate(oldShop.gameObject);
            }

            Transform world = GameObject.Find("World").transform;
            Transform shopRoot = new GameObject("ShopBuilding").transform;
            shopRoot.SetParent(world);
            shopRoot.position = new Vector3(0f, 0f, 26f);

            Material wallMat = CreateDetailedMaterial("M_ShopWall", new Color(0.92f, 0.88f, 0.82f), 0.04f, 0.15f);
            Material roofMat = CreateMaterial("M_ShopRoof", new Color(0.55f, 0.25f, 0.15f));
            Material floorMat = CreateDetailedMaterial("M_ShopFloor", new Color(0.45f, 0.35f, 0.25f), 0.06f, 0.3f);
            Material counterMat = CreateMaterial("M_ShopCounter", new Color(0.4f, 0.25f, 0.12f));
            Material signMat = CreateMaterial("M_ShopSign", new Color(0.2f, 0.6f, 0.3f));
            Material windowMat = CreateMaterial("M_ShopWindow", new Color(0.6f, 0.8f, 0.9f, 0.5f));
            Material doorMat = CreateMaterial("M_ShopDoor", new Color(0.35f, 0.2f, 0.1f));
            Material awningMat = CreateMaterial("M_ShopAwning", new Color(0.85f, 0.3f, 0.2f));

            float bw = 10f, bd = 8f, bh = 4f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ShopFloor";
            floor.transform.SetParent(shopRoot);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(bw, 0.1f, bd);
            SetRendererMaterial(floor, floorMat);

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "ShopBackWall";
            backWall.transform.SetParent(shopRoot);
            backWall.transform.localPosition = new Vector3(0f, bh * 0.5f, -bd * 0.5f);
            backWall.transform.localScale = new Vector3(bw, bh, 0.3f);
            SetRendererMaterial(backWall, wallMat);

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "ShopLeftWall";
            leftWall.transform.SetParent(shopRoot);
            leftWall.transform.localPosition = new Vector3(-bw * 0.5f, bh * 0.5f, 0f);
            leftWall.transform.localScale = new Vector3(0.3f, bh, bd);
            SetRendererMaterial(leftWall, wallMat);

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "ShopRightWall";
            rightWall.transform.SetParent(shopRoot);
            rightWall.transform.localPosition = new Vector3(bw * 0.5f, bh * 0.5f, 0f);
            rightWall.transform.localScale = new Vector3(0.3f, bh, bd);
            SetRendererMaterial(rightWall, wallMat);

            GameObject frontWallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWallL.name = "ShopFrontWallL";
            frontWallL.transform.SetParent(shopRoot);
            frontWallL.transform.localPosition = new Vector3(-bw * 0.3f, bh * 0.5f, bd * 0.5f);
            frontWallL.transform.localScale = new Vector3(bw * 0.35f, bh, 0.3f);
            SetRendererMaterial(frontWallL, wallMat);

            GameObject frontWallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWallR.name = "ShopFrontWallR";
            frontWallR.transform.SetParent(shopRoot);
            frontWallR.transform.localPosition = new Vector3(bw * 0.3f, bh * 0.5f, bd * 0.5f);
            frontWallR.transform.localScale = new Vector3(bw * 0.35f, bh, 0.3f);
            SetRendererMaterial(frontWallR, wallMat);

            GameObject winL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winL.name = "ShopWindowL";
            winL.transform.SetParent(shopRoot);
            winL.transform.localPosition = new Vector3(-bw * 0.3f, bh * 0.6f, bd * 0.5f);
            winL.transform.localScale = new Vector3(1.5f, 1.8f, 0.1f);
            SetRendererMaterial(winL, windowMat);

            GameObject winR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winR.name = "ShopWindowR";
            winR.transform.SetParent(shopRoot);
            winR.transform.localPosition = new Vector3(bw * 0.3f, bh * 0.6f, bd * 0.5f);
            winR.transform.localScale = new Vector3(1.5f, 1.8f, 0.1f);
            SetRendererMaterial(winR, windowMat);

            float doorW = 2.2f, doorH = 3.4f;
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "ShopDoor";
            door.transform.SetParent(shopRoot);
            door.transform.localPosition = new Vector3(0f, doorH * 0.5f, bd * 0.5f);
            door.transform.localScale = new Vector3(doorW, doorH, 0.15f);
            SetRendererMaterial(door, doorMat);
            Object.DestroyImmediate(door.GetComponent<Collider>());

            float roofRise = 2f;
            GameObject roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofL.name = "ShopRoofL";
            roofL.transform.SetParent(shopRoot);
            roofL.transform.localPosition = new Vector3(0f, bh + roofRise * 0.5f, -bd * 0.25f);
            float roofLen = Mathf.Sqrt(bd * 0.5f * bd * 0.5f + roofRise * roofRise);
            float roofAngle = Mathf.Atan2(roofRise, bd * 0.5f) * Mathf.Rad2Deg;
            roofL.transform.localScale = new Vector3(bw + 0.6f, 0.25f, roofLen);
            roofL.transform.rotation = Quaternion.Euler(roofAngle, 0f, 0f);
            SetRendererMaterial(roofL, roofMat);

            GameObject roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofR.name = "ShopRoofR";
            roofR.transform.SetParent(shopRoot);
            roofR.transform.localPosition = new Vector3(0f, bh + roofRise * 0.5f, bd * 0.25f);
            roofR.transform.localScale = new Vector3(bw + 0.6f, 0.25f, roofLen);
            roofR.transform.rotation = Quaternion.Euler(-roofAngle, 0f, 0f);
            SetRendererMaterial(roofR, roofMat);

            GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awning.name = "ShopAwning";
            awning.transform.SetParent(shopRoot);
            awning.transform.localPosition = new Vector3(0f, bh - 0.3f, bd * 0.5f + 1f);
            awning.transform.localScale = new Vector3(bw * 0.6f, 0.15f, 2f);
            awning.transform.rotation = Quaternion.Euler(-15f, 0f, 0f);
            SetRendererMaterial(awning, awningMat);

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "ShopCounter";
            counter.transform.SetParent(shopRoot);
            counter.transform.localPosition = new Vector3(0f, 0.9f, -bd * 0.25f);
            counter.transform.localScale = new Vector3(6f, 1f, 1.2f);
            SetRendererMaterial(counter, counterMat);

            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "ShopSign";
            sign.transform.SetParent(shopRoot);
            sign.transform.localPosition = new Vector3(0f, bh + 0.8f, bd * 0.5f + 0.2f);
            sign.transform.localScale = new Vector3(4f, 1.2f, 0.2f);
            SetRendererMaterial(sign, signMat);

            GameObject signText = new GameObject("SignLabel");
            signText.transform.SetParent(shopRoot);
            signText.transform.localPosition = new Vector3(0f, bh + 0.8f, bd * 0.5f + 0.35f);
            Canvas signCanvas = signText.AddComponent<Canvas>();
            signCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform signRT = signText.GetComponent<RectTransform>();
            signRT.sizeDelta = new Vector2(4f, 1f);
            signRT.localScale = Vector3.one * 0.08f;

            GameObject signTextGO = new GameObject("Text");
            signTextGO.transform.SetParent(signText.transform, false);
            RectTransform stRT = signTextGO.AddComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero;
            stRT.anchorMax = Vector2.one;
            stRT.sizeDelta = Vector2.zero;
            Text stLabel = signTextGO.AddComponent<Text>();
            stLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stLabel.fontSize = 32;
            stLabel.alignment = TextAnchor.MiddleCenter;
            stLabel.color = Color.white;
            stLabel.text = "General Store";

            ShopStand shop = counter.AddComponent<ShopStand>();
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

        private static void EnsureClothingShop()
        {
            ClothingStand oldClothing = Object.FindAnyObjectByType<ClothingStand>();
            if (oldClothing != null) Object.DestroyImmediate(oldClothing.gameObject);

            Transform world = GameObject.Find("World").transform;
            Transform boutiqueRoot = new GameObject("BoutiqueBuilding").transform;
            boutiqueRoot.SetParent(world);
            boutiqueRoot.position = new Vector3(10f, 0f, -6f);

            Material wallMat = CreateDetailedMaterial("M_BoutiqueWall", new Color(0.15f, 0.12f, 0.18f), 0.03f, 0.4f);
            Material roofMat = CreateMaterial("M_BoutiqueRoof", new Color(0.1f, 0.1f, 0.12f));
            Material floorMat = CreateDetailedMaterial("M_BoutiqueFloor", new Color(0.7f, 0.15f, 0.2f), 0.02f, 0.6f);
            Material counterMat = CreateMaterial("M_BoutiqueCounter", new Color(0.85f, 0.82f, 0.78f));
            Material signMat = CreateMaterial("M_BoutiqueSign", new Color(0.85f, 0.72f, 0.3f));
            Material windowMat = CreateMaterial("M_BoutiqueWindow", new Color(0.6f, 0.7f, 0.9f, 0.4f));
            Material doorMat = CreateMaterial("M_BoutiqueDoor", new Color(0.1f, 0.1f, 0.12f));
            Material trimMat = CreateMaterial("M_BoutiqueTrim", new Color(0.85f, 0.72f, 0.3f));
            Material displayMat = CreateMaterial("M_BoutiqueDisplay", new Color(0.9f, 0.15f, 0.2f));

            float bw = 8f, bd = 7f, bh = 4.5f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "BoutiqueFloor";
            floor.transform.SetParent(boutiqueRoot);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(bw, 0.1f, bd);
            SetRendererMaterial(floor, floorMat);

            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BoutiqueBackWall";
            backWall.transform.SetParent(boutiqueRoot);
            backWall.transform.localPosition = new Vector3(0f, bh * 0.5f, -bd * 0.5f);
            backWall.transform.localScale = new Vector3(bw, bh, 0.25f);
            SetRendererMaterial(backWall, wallMat);

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "BoutiqueLeftWall";
            leftWall.transform.SetParent(boutiqueRoot);
            leftWall.transform.localPosition = new Vector3(-bw * 0.5f, bh * 0.5f, 0f);
            leftWall.transform.localScale = new Vector3(0.25f, bh, bd);
            SetRendererMaterial(leftWall, wallMat);

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "BoutiqueRightWall";
            rightWall.transform.SetParent(boutiqueRoot);
            rightWall.transform.localPosition = new Vector3(bw * 0.5f, bh * 0.5f, 0f);
            rightWall.transform.localScale = new Vector3(0.25f, bh, bd);
            SetRendererMaterial(rightWall, wallMat);

            GameObject frontWallL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWallL.name = "BoutiqueFrontWallL";
            frontWallL.transform.SetParent(boutiqueRoot);
            frontWallL.transform.localPosition = new Vector3(-bw * 0.32f, bh * 0.5f, bd * 0.5f);
            frontWallL.transform.localScale = new Vector3(bw * 0.3f, bh, 0.25f);
            SetRendererMaterial(frontWallL, wallMat);

            GameObject frontWallR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWallR.name = "BoutiqueFrontWallR";
            frontWallR.transform.SetParent(boutiqueRoot);
            frontWallR.transform.localPosition = new Vector3(bw * 0.32f, bh * 0.5f, bd * 0.5f);
            frontWallR.transform.localScale = new Vector3(bw * 0.3f, bh, 0.25f);
            SetRendererMaterial(frontWallR, wallMat);

            GameObject winL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winL.name = "BoutiqueWindowL";
            winL.transform.SetParent(boutiqueRoot);
            winL.transform.localPosition = new Vector3(-bw * 0.32f, bh * 0.55f, bd * 0.5f);
            winL.transform.localScale = new Vector3(1.6f, 2.2f, 0.08f);
            SetRendererMaterial(winL, windowMat);

            GameObject winR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            winR.name = "BoutiqueWindowR";
            winR.transform.SetParent(boutiqueRoot);
            winR.transform.localPosition = new Vector3(bw * 0.32f, bh * 0.55f, bd * 0.5f);
            winR.transform.localScale = new Vector3(1.6f, 2.2f, 0.08f);
            SetRendererMaterial(winR, windowMat);

            float doorW = 2f, doorH = 3.6f;
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "BoutiqueDoor";
            door.transform.SetParent(boutiqueRoot);
            door.transform.localPosition = new Vector3(0f, doorH * 0.5f, bd * 0.5f);
            door.transform.localScale = new Vector3(doorW, doorH, 0.15f);
            SetRendererMaterial(door, doorMat);
            Object.DestroyImmediate(door.GetComponent<Collider>());

            float roofRise = 2.5f;
            GameObject roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofL.name = "BoutiqueRoofL";
            roofL.transform.SetParent(boutiqueRoot);
            roofL.transform.localPosition = new Vector3(0f, bh + roofRise * 0.5f, -bd * 0.25f);
            float roofLen = Mathf.Sqrt(bd * 0.5f * bd * 0.5f + roofRise * roofRise);
            float roofAngle = Mathf.Atan2(roofRise, bd * 0.5f) * Mathf.Rad2Deg;
            roofL.transform.localScale = new Vector3(bw + 0.6f, 0.2f, roofLen);
            roofL.transform.rotation = Quaternion.Euler(roofAngle, 0f, 0f);
            SetRendererMaterial(roofL, roofMat);

            GameObject roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roofR.name = "BoutiqueRoofR";
            roofR.transform.SetParent(boutiqueRoot);
            roofR.transform.localPosition = new Vector3(0f, bh + roofRise * 0.5f, bd * 0.25f);
            roofR.transform.localScale = new Vector3(bw + 0.6f, 0.2f, roofLen);
            roofR.transform.rotation = Quaternion.Euler(-roofAngle, 0f, 0f);
            SetRendererMaterial(roofR, roofMat);

            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "BoutiqueTrim";
            trim.transform.SetParent(boutiqueRoot);
            trim.transform.localPosition = new Vector3(0f, bh + 0.1f, bd * 0.5f + 0.15f);
            trim.transform.localScale = new Vector3(bw * 0.7f, 0.3f, 0.2f);
            SetRendererMaterial(trim, trimMat);

            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "BoutiqueCounter";
            counter.transform.SetParent(boutiqueRoot);
            counter.transform.localPosition = new Vector3(0f, 0.85f, -bd * 0.2f);
            counter.transform.localScale = new Vector3(5f, 0.9f, 1f);
            SetRendererMaterial(counter, counterMat);

            for (int i = -1; i <= 1; i++)
            {
                GameObject display = GameObject.CreatePrimitive(PrimitiveType.Cube);
                display.name = "BoutiqueDisplay_" + (i + 1);
                display.transform.SetParent(boutiqueRoot);
                display.transform.localPosition = new Vector3(i * 1.8f, 1.5f, -bd * 0.2f);
                display.transform.localScale = new Vector3(1f, 1.8f, 0.6f);
                SetRendererMaterial(display, displayMat);
            }

            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "BoutiqueSign";
            sign.transform.SetParent(boutiqueRoot);
            sign.transform.localPosition = new Vector3(0f, bh + 0.8f, bd * 0.5f + 0.2f);
            sign.transform.localScale = new Vector3(4.5f, 1.4f, 0.2f);
            SetRendererMaterial(sign, signMat);

            GameObject signText = new GameObject("SignLabel");
            signText.transform.SetParent(boutiqueRoot);
            signText.transform.localPosition = new Vector3(0f, bh + 0.8f, bd * 0.5f + 0.35f);
            Canvas signCanvas = signText.AddComponent<Canvas>();
            signCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform signRT = signText.GetComponent<RectTransform>();
            signRT.sizeDelta = new Vector2(4.5f, 1.2f);
            signRT.localScale = Vector3.one * 0.07f;

            GameObject signTextGO = new GameObject("Text");
            signTextGO.transform.SetParent(signText.transform, false);
            RectTransform stRT = signTextGO.AddComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero;
            stRT.anchorMax = Vector2.one;
            stRT.sizeDelta = Vector2.zero;
            Text stLabel = signTextGO.AddComponent<Text>();
            stLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stLabel.fontSize = 30;
            stLabel.alignment = TextAnchor.MiddleCenter;
            stLabel.color = new Color(0.1f, 0.1f, 0.12f);
            stLabel.text = "Merci Couture";

            ClothingStand clothing = counter.AddComponent<ClothingStand>();
            clothing.SetItems(new ClothingItemData[]
            {
                new ClothingItemData { itemName = "Streetwear", price = 0, outfitIndex = 0, minReputation = 0 },
                new ClothingItemData { itemName = "Classic Tee", price = 25, outfitIndex = 1, minReputation = 0 },
                new ClothingItemData { itemName = "Casual Denim", price = 40, outfitIndex = 2, minReputation = 0 },
                new ClothingItemData { itemName = "Fresh Green", price = 50, outfitIndex = 3, minReputation = 0 },
                new ClothingItemData { itemName = "Sunset Orange", price = 60, outfitIndex = 4, minReputation = 0 },
                new ClothingItemData { itemName = "Urban Black", price = 75, outfitIndex = 5, minReputation = 0 },
                new ClothingItemData { itemName = "Royal Purple", price = 80, outfitIndex = 6, minReputation = 0 },
                new ClothingItemData { itemName = "Designer Suit", price = 200, outfitIndex = 7, minReputation = 50 },
                new ClothingItemData { itemName = "Merci Couture", price = 350, outfitIndex = 8, minReputation = 80 },
                new ClothingItemData { itemName = "Elite Gold", price = 500, outfitIndex = 9, minReputation = 100 }
            });
        }

        private static void EnsurePartner()
        {
            Partner existing = Object.FindAnyObjectByType<Partner>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Transform mayaRoot = BuildHumanoid(null, "Maya", new Vector3(3.5f, 0f, 26f),
                new Color(0.93f, 0.82f, 0.72f),
                new Color(0.85f, 0.40f, 0.45f),
                new Color(0.30f, 0.20f, 0.18f),
                new Color(0.20f, 0.12f, 0.08f),
                0.95f);

            CharacterController cc = mayaRoot.gameObject.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.95f, 0f);

            Partner partner = mayaRoot.gameObject.AddComponent<Partner>();
            partner.skinMaterial = CreateMaterial("M_Skin_Maya", new Color(0.93f, 0.82f, 0.72f));
        }

        private static void EnsureRival()
        {
            Rival existing = Object.FindAnyObjectByType<Rival>();
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            Transform rivalRoot = BuildHumanoid(null, "Marcus_Blackwood", new Vector3(-10f, 0f, -20f),
                new Color(0.85f, 0.72f, 0.62f),
                new Color(0.15f, 0.15f, 0.2f),
                new Color(0.25f, 0.25f, 0.3f),
                new Color(0.1f, 0.08f, 0.06f),
                1.1f);

            CharacterController cc = rivalRoot.gameObject.AddComponent<CharacterController>();
            cc.height = 2.1f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 1.05f, 0f);

            rivalRoot.gameObject.AddComponent<Rival>();
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

        private static void EnsureDoorTriggers()
        {
            Transform world = GameObject.Find("World").transform;
            Transform town = world.Find("Town");
            if (town == null) return;

            Transform doorRoot = world.Find("DoorTriggers");
            if (doorRoot != null) Object.DestroyImmediate(doorRoot.gameObject);
            doorRoot = new GameObject("DoorTriggers").transform;
            doorRoot.SetParent(world);

            BuildingInfo[] buildings = new BuildingInfo[]
            {
                new BuildingInfo { name = "House_01", pos = new Vector3(-40f, 0f, 18f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_02", pos = new Vector3(-20f, 0f, 18f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_03", pos = new Vector3(20f, 0f, 18f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_04", pos = new Vector3(40f, 0f, 18f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_05", pos = new Vector3(56f, 0f, 18f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_06", pos = new Vector3(-40f, 0f, 52f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_07", pos = new Vector3(-20f, 0f, 52f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_08", pos = new Vector3(20f, 0f, 52f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_09", pos = new Vector3(40f, 0f, 52f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_10", pos = new Vector3(56f, 0f, 52f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_11", pos = new Vector3(-40f, 0f, -55f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_12", pos = new Vector3(-20f, 0f, -55f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_13", pos = new Vector3(20f, 0f, -55f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_14", pos = new Vector3(40f, 0f, -55f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "House_15", pos = new Vector3(56f, 0f, -55f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.House },
                new BuildingInfo { name = "Shop_01", pos = new Vector3(12f, 0f, 6f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "Shop_02", pos = new Vector3(12f, 0f, -14f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "TownHall", pos = new Vector3(0f, 0f, -30f), frontZ = 7f, interiorType = DoorInteractable.InteriorType.Public },
                new BuildingInfo { name = "Market_01", pos = new Vector3(20f, 0f, 24f), frontZ = 6f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "Market_02", pos = new Vector3(-22f, 0f, 24f), frontZ = 6f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "Church", pos = new Vector3(0f, 0f, 42f), frontZ = 6f, interiorType = DoorInteractable.InteriorType.Church },
                new BuildingInfo { name = "School", pos = new Vector3(-36f, 0f, 32f), frontZ = 6f, interiorType = DoorInteractable.InteriorType.Public },
                new BuildingInfo { name = "Bakery", pos = new Vector3(-22f, 0f, 36f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "Bank", pos = new Vector3(22f, 0f, 36f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Public },
                new BuildingInfo { name = "Restaurant", pos = new Vector3(22f, 0f, -36f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Shop },
                new BuildingInfo { name = "PostOffice", pos = new Vector3(-22f, 0f, -36f), frontZ = 5f, interiorType = DoorInteractable.InteriorType.Public },
            };

            GameObject playerGO = GameObject.Find("Player");
            Transform player = playerGO != null ? playerGO.transform : null;
            CanvasGroup fadeOverlay = Object.FindAnyObjectByType<Canvas>()?.GetComponentInChildren<CanvasGroup>();

            foreach (BuildingInfo b in buildings)
            {
                GameObject doorGO = new GameObject("Door_" + b.name);
                doorGO.transform.SetParent(doorRoot);
                doorGO.transform.position = new Vector3(b.pos.x, 1f, b.pos.z + b.frontZ + 1f);

                BoxCollider col = doorGO.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(3f, 3f, 3f);

                DoorInteractable door = doorGO.AddComponent<DoorInteractable>();
                door.buildingName = b.name;
                door.interiorType = b.interiorType;
                door.interiorOffset = new Vector3(b.pos.x, -200f, b.pos.z);
                door.Configure(player, Object.FindAnyObjectByType<Core.GameManager>(), fadeOverlay);

                GameObject label = new GameObject("Label");
                label.transform.SetParent(doorGO.transform);
                label.transform.localPosition = new Vector3(0f, 2f, 0f);
                label.transform.localScale = Vector3.one * 0.05f;
                Canvas canvas = label.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform rt = label.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(4f, 0.6f);

                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(label.transform, false);
                RectTransform textRT = textGO.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;
                Text text = textGO.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 28;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(1f, 0.9f, 0.6f);
                text.text = "[E] Enter " + b.name;
            }

            Debug.Log("Rise: Door triggers created for " + buildings.Length + " buildings.");
        }

        private static void EnsureHUD(GameManager gameManager)
        {
            // Remove any old HUD to avoid duplicates.
            GameObject oldHud = GameObject.Find("GameHUD CanvaWindow");
            if (oldHud != null) Object.DestroyImmediate(oldHud);

            GameObject canvasGO = new GameObject("GameHUD CanvaWindow");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasGroup>();

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Text money = CreateHudText("Money", canvasGO.transform, new Vector2(0.02f, 0.97f), new Vector2(0f, 1f), 60, TextAnchor.UpperLeft);
            Text day = CreateHudText("Day", canvasGO.transform, new Vector2(0.02f, 0.87f), new Vector2(0f, 1f), 44, TextAnchor.UpperLeft);
            Text time = CreateHudText("Time", canvasGO.transform, new Vector2(0.02f, 0.79f), new Vector2(0f, 1f), 44, TextAnchor.UpperLeft);
            Text work = CreateHudText("Work", canvasGO.transform, new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0f), 34, TextAnchor.LowerCenter);
            Text needs = CreateHudText("Needs", canvasGO.transform, new Vector2(0.02f, 0.71f), new Vector2(0f, 1f), 40, TextAnchor.UpperLeft);
            Text shop = CreateHudText("ShopMenu", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 38, TextAnchor.MiddleCenter);
            Text phone = CreateHudText("Phone", canvasGO.transform, new Vector2(0.98f, 0.97f), new Vector2(1f, 1f), 32, TextAnchor.UpperRight);

            GameObject toggleBtnGO = new GameObject("HudToggleBtn");
            toggleBtnGO.transform.SetParent(canvasGO.transform, false);
            RectTransform toggleRt = toggleBtnGO.AddComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(1f, 0.5f);
            toggleRt.anchorMax = new Vector2(1f, 0.5f);
            toggleRt.pivot = new Vector2(1f, 0.5f);
            toggleRt.anchoredPosition = new Vector2(-10f, 0f);
            toggleRt.sizeDelta = new Vector2(100f, 30f);
            Image toggleBg = toggleBtnGO.AddComponent<Image>();
            toggleBg.color = new Color(0.15f, 0.15f, 0.2f, 0.7f);
            Button toggleBtn = toggleBtnGO.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleBg;

            GameObject toggleLabelGO = new GameObject("ToggleLabel");
            toggleLabelGO.transform.SetParent(toggleBtnGO.transform, false);
            RectTransform toggleLabelRt = toggleLabelGO.AddComponent<RectTransform>();
            toggleLabelRt.anchorMin = Vector2.zero;
            toggleLabelRt.anchorMax = Vector2.one;
            toggleLabelRt.offsetMin = Vector2.zero;
            toggleLabelRt.offsetMax = Vector2.zero;
            Text toggleLabel = toggleLabelGO.AddComponent<Text>();
            toggleLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toggleLabel.fontSize = 14;
            toggleLabel.alignment = TextAnchor.MiddleCenter;
            toggleLabel.color = Color.white;
            toggleLabel.text = "HUD: ON";
            toggleLabel.raycastTarget = false;

            Text showHint = CreateHudText("ShowHint", canvasGO.transform, new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0f), 20, TextAnchor.LowerCenter);
            showHint.text = "Press TAB to show HUD";
            showHint.color = new Color(1f, 1f, 1f, 0.6f);
            showHint.enabled = false;

            GameHUD hud = canvasGO.AddComponent<GameHUD>();
            hud.Configure(gameManager, money, time, day, work, needs, shop, phone);
            hud.SetToggleElements(toggleBtn, toggleLabel, showHint);

            MinimapUI minimap = canvasGO.AddComponent<MinimapUI>();
            minimap.Configure(gameManager.Player);
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

        private static void EnsureParkingLot()
        {
            Transform world = GameObject.Find("World").transform;
            Transform old = world.Find("ParkingLot");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            Transform lot = new GameObject("ParkingLot").transform;
            lot.SetParent(world);

            Material asphalt = CreateDetailedMaterial("M_Asphalt", new Color(0.2f, 0.2f, 0.22f), 0.02f, 0.8f);
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "ParkingPad";
            pad.transform.SetParent(lot);
            pad.transform.position = new Vector3(32f, 0.02f, -10f);
            pad.transform.localScale = new Vector3(36f, 0.04f, 10f);
            SetRendererMaterial(pad, asphalt);

            Material lineMat = CreateMaterial("M_ParkingLine", new Color(0.9f, 0.9f, 0.9f));
            for (int i = 0; i <= 9; i++)
            {
                float x = 15.25f + i * 3.5f;
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "ParkingLine_" + i;
                line.transform.SetParent(lot);
                line.transform.position = new Vector3(x, 0.045f, -10f);
                line.transform.localScale = new Vector3(0.08f, 0.02f, 9f);
                SetRendererMaterial(line, lineMat);
                Object.DestroyImmediate(line.GetComponent<Collider>());
            }

            string[] brands = { "Mercedes-Benz", "BMW", "Toyota", "Honda", "Ford", "Range Rover", "Tesla", "Chevrolet", "Lexus" };
            Color[] colors =
            {
                new Color(0.75f, 0.75f, 0.75f), new Color(0.11f, 0.41f, 0.83f), new Color(0.8f, 0f, 0f),
                Color.white, new Color(0f, 0.2f, 0.47f), new Color(0.1f, 0.1f, 0.1f),
                new Color(0.91f, 0.13f, 0.15f), new Color(1f, 0.84f, 0f), new Color(0.5f, 0.5f, 0.5f)
            };
            int[] repReqs = { 0, 30, 0, 0, 0, 30, 60, 0, 60 };

            for (int i = 0; i < 9; i++)
            {
                float x = 17f + i * 3.5f;
                EnsureCar(lot, brands[i], colors[i], repReqs[i], new Vector3(x, 0f, -10f));
            }
        }

        private static void EnsureCar(Transform parent, string brand, Color color, int minRep, Vector3 position)
        {
            Material bodyMat = CreateMaterial("M_Car_" + brand, color);
            Material glassMat = CreateMaterial("M_CarGlass_" + brand, new Color(0.3f, 0.4f, 0.55f, 0.6f));
            Material wheelMat = CreateMaterial("M_CarWheel", new Color(0.12f, 0.12f, 0.12f));
            Material headlightMat = CreateMaterial("M_Headlight", new Color(1f, 0.95f, 0.7f));
            Material taillightMat = CreateMaterial("M_Taillight", new Color(0.8f, 0.1f, 0.1f));
            Material chromeMat = CreateMaterial("M_CarChrome", new Color(0.8f, 0.82f, 0.85f));
            Material darkMat = CreateMaterial("M_CarDark_" + brand, new Color(0.08f, 0.08f, 0.1f));
            Material bumperMat = CreateMaterial("M_CarBumper_" + brand, Color.Lerp(color, Color.black, 0.15f));
            Material tireMat = CreateMaterial("M_CarTire", new Color(0.06f, 0.06f, 0.06f));
            Material rimMat = CreateMaterial("M_CarRim", new Color(0.75f, 0.77f, 0.8f));

            bool isSUV = brand == "Range Rover";
            bool isCoupe = brand == "Tesla" || brand == "Lexus";
            string modelType = isSUV ? "car_suv" : (isCoupe ? "car_coupe" : "car_sedan");
            string fbxPath = "Assets/Models/Cars/" + modelType + ".fbx";
            GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

            if (fbxModel != null)
            {
                BuildFBXCar(parent, brand, color, minRep, position, fbxModel, isSUV, isCoupe, bodyMat, glassMat, wheelMat, headlightMat, taillightMat, tireMat, rimMat);
                return;
            }

            float carW = isSUV ? 2f : 1.85f;
            float carH = isSUV ? 0.9f : 0.65f;
            float carL = isSUV ? 4.6f : (isCoupe ? 4.2f : 4.4f);
            float cabinH = isSUV ? 0.75f : 0.55f;
            float cabinL = isSUV ? 2.6f : (isCoupe ? 2.0f : 2.3f);
            float rideH = isSUV ? 0.45f : 0.3f;

            GameObject car = new GameObject(brand);
            car.transform.SetParent(parent);
            car.transform.position = position;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(car.transform);
            body.transform.localPosition = new Vector3(0f, rideH + carH * 0.5f, 0f);
            body.transform.localScale = new Vector3(carW, carH, carL);
            SetRendererMaterial(body, bodyMat);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(car.transform);
            cabin.transform.localPosition = new Vector3(0f, rideH + carH + cabinH * 0.45f, -0.15f);
            cabin.transform.localScale = new Vector3(carW - 0.15f, cabinH, cabinL);
            SetRendererMaterial(cabin, glassMat);
            Object.DestroyImmediate(cabin.GetComponent<Collider>());

            float hoodFront = carL * 0.5f;
            float trunkBack = -carL * 0.5f;
            float bumperH = 0.2f;

            GameObject frontBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontBumper.name = "FrontBumper";
            frontBumper.transform.SetParent(car.transform);
            frontBumper.transform.localPosition = new Vector3(0f, rideH + bumperH * 0.5f + 0.05f, hoodFront + 0.08f);
            frontBumper.transform.localScale = new Vector3(carW + 0.1f, bumperH + 0.1f, 0.15f);
            SetRendererMaterial(frontBumper, bumperMat);
            Object.DestroyImmediate(frontBumper.GetComponent<Collider>());

            GameObject rearBumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearBumper.name = "RearBumper";
            rearBumper.transform.SetParent(car.transform);
            rearBumper.transform.localPosition = new Vector3(0f, rideH + bumperH * 0.5f + 0.05f, trunkBack - 0.08f);
            rearBumper.transform.localScale = new Vector3(carW + 0.1f, bumperH + 0.1f, 0.15f);
            SetRendererMaterial(rearBumper, bumperMat);
            Object.DestroyImmediate(rearBumper.GetComponent<Collider>());

            float grillW = carW * 0.65f;
            GameObject grill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grill.name = "Grill";
            grill.transform.SetParent(car.transform);
            grill.transform.localPosition = new Vector3(0f, rideH + 0.15f, hoodFront + 0.12f);
            grill.transform.localScale = new Vector3(grillW, 0.25f, 0.05f);
            SetRendererMaterial(grill, darkMat);
            Object.DestroyImmediate(grill.GetComponent<Collider>());

            float windshieldAngle = isSUV ? 25f : 30f;
            GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.name = "Windshield";
            windshield.transform.SetParent(car.transform);
            windshield.transform.localPosition = new Vector3(0f, rideH + carH + cabinH * 0.5f + 0.1f, cabinL * 0.4f + 0.1f);
            windshield.transform.localScale = new Vector3(carW - 0.2f, cabinH + 0.1f, 0.08f);
            windshield.transform.localRotation = Quaternion.Euler(windshieldAngle, 0f, 0f);
            SetRendererMaterial(windshield, glassMat);
            Object.DestroyImmediate(windshield.GetComponent<Collider>());

            GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWindow.name = "RearWindow";
            rearWindow.transform.SetParent(car.transform);
            rearWindow.transform.localPosition = new Vector3(0f, rideH + carH + cabinH * 0.5f + 0.1f, -cabinL * 0.4f - 0.1f);
            rearWindow.transform.localScale = new Vector3(carW - 0.2f, cabinH + 0.1f, 0.08f);
            rearWindow.transform.localRotation = Quaternion.Euler(-windshieldAngle, 0f, 0f);
            SetRendererMaterial(rearWindow, glassMat);
            Object.DestroyImmediate(rearWindow.GetComponent<Collider>());

            float mirrorH = rideH + carH * 0.7f;
            float mirrorOut = carW * 0.5f + 0.12f;
            Vector3[] mirrorPos = { new Vector3(-mirrorOut, mirrorH, cabinL * 0.25f), new Vector3(mirrorOut, mirrorH, cabinL * 0.25f) };
            foreach (Vector3 mp in mirrorPos)
            {
                GameObject mirror = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mirror.name = "Mirror";
                mirror.transform.SetParent(car.transform);
                mirror.transform.localPosition = mp;
                mirror.transform.localScale = new Vector3(0.15f, 0.12f, 0.1f);
                SetRendererMaterial(mirror, darkMat);
                Object.DestroyImmediate(mirror.GetComponent<Collider>());
            }

            Vector3[] hlPos = { new Vector3(-carW * 0.35f, rideH + 0.18f, hoodFront + 0.13f), new Vector3(carW * 0.35f, rideH + 0.18f, hoodFront + 0.13f) };
            foreach (Vector3 hp in hlPos)
            {
                GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hl.name = "Headlight";
                hl.transform.SetParent(car.transform);
                hl.transform.localPosition = hp;
                hl.transform.localScale = new Vector3(0.35f, 0.18f, 0.06f);
                SetRendererMaterial(hl, headlightMat);
                Object.DestroyImmediate(hl.GetComponent<Collider>());
            }

            Vector3[] tlPos = { new Vector3(-carW * 0.35f, rideH + 0.18f, trunkBack - 0.13f), new Vector3(carW * 0.35f, rideH + 0.18f, trunkBack - 0.13f) };
            foreach (Vector3 tp in tlPos)
            {
                GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tl.name = "Taillight";
                tl.transform.SetParent(car.transform);
                tl.transform.localPosition = tp;
                tl.transform.localScale = new Vector3(0.28f, 0.14f, 0.06f);
                SetRendererMaterial(tl, taillightMat);
                Object.DestroyImmediate(tl.GetComponent<Collider>());
            }

            float wheelW = isSUV ? 0.38f : 0.32f;
            float wheelH = isSUV ? 0.18f : 0.14f;
            float wheelR = isSUV ? 0.3f : 0.25f;
            float fZ = carL * 0.32f;
            float rZ = -carL * 0.32f;
            float wheelY = rideH * 0.5f + wheelH;
            Vector3[] wheelPos =
            {
                new Vector3(-carW * 0.5f - 0.02f, wheelY, fZ),
                new Vector3(carW * 0.5f + 0.02f, wheelY, fZ),
                new Vector3(-carW * 0.5f - 0.02f, wheelY, rZ),
                new Vector3(carW * 0.5f + 0.02f, wheelY, rZ)
            };
            foreach (Vector3 wp in wheelPos)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(car.transform);
                wheel.transform.localPosition = wp;
                wheel.transform.localScale = new Vector3(wheelW, wheelH, wheelW);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                SetRendererMaterial(wheel, tireMat);
                Object.DestroyImmediate(wheel.GetComponent<Collider>());

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wheel.transform);
                rim.transform.localPosition = Vector3.zero;
                rim.transform.localScale = new Vector3(0.55f, 0.52f, 0.55f);
                rim.transform.localRotation = Quaternion.identity;
                SetRendererMaterial(rim, rimMat);
                Object.DestroyImmediate(rim.GetComponent<Collider>());
            }

            float roofR = isSUV ? 0.08f : 0.05f;
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(car.transform);
            roof.transform.localPosition = new Vector3(0f, rideH + carH + cabinH + roofR * 0.5f, -0.15f);
            roof.transform.localScale = new Vector3(carW - 0.2f, roofR, cabinL - 0.2f);
            SetRendererMaterial(roof, bodyMat);
            Object.DestroyImmediate(roof.GetComponent<Collider>());

            if (!isSUV)
            {
                float trunkH = 0.15f;
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trunk.name = "Trunk";
                trunk.transform.SetParent(car.transform);
                trunk.transform.localPosition = new Vector3(0f, rideH + carH + trunkH * 0.3f, -cabinL * 0.5f - 0.3f);
                trunk.transform.localScale = new Vector3(carW - 0.1f, trunkH, 0.4f);
                SetRendererMaterial(trunk, bodyMat);
                Object.DestroyImmediate(trunk.GetComponent<Collider>());
            }

            float plateW = 0.6f, plateH = 0.15f;
            GameObject frontPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPlate.name = "FrontPlate";
            frontPlate.transform.SetParent(car.transform);
            frontPlate.transform.localPosition = new Vector3(0f, rideH + 0.15f, hoodFront + 0.14f);
            frontPlate.transform.localScale = new Vector3(plateW, plateH, 0.02f);
            SetRendererMaterial(frontPlate, CreateMaterial("M_Plate_" + brand, Color.white));
            Object.DestroyImmediate(frontPlate.GetComponent<Collider>());

            GameObject rearPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearPlate.name = "RearPlate";
            rearPlate.transform.SetParent(car.transform);
            rearPlate.transform.localPosition = new Vector3(0f, rideH + 0.15f, trunkBack - 0.14f);
            rearPlate.transform.localScale = new Vector3(plateW, plateH, 0.02f);
            SetRendererMaterial(rearPlate, CreateMaterial("M_RearPlate_" + brand, Color.white));
            Object.DestroyImmediate(rearPlate.GetComponent<Collider>());

            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(carW + 0.2f, 1.2f, carL + 0.3f);
            col.center = new Vector3(0f, rideH + carH * 0.5f, 0f);

            GameObject labelGO = new GameObject("BrandLabel");
            labelGO.transform.SetParent(car.transform);
            labelGO.transform.localPosition = new Vector3(0f, rideH + carH + cabinH + 0.3f, 0f);
            labelGO.transform.localRotation = Quaternion.identity;

            Canvas labelCanvas = labelGO.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRT = labelGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(4f, 0.6f);
            canvasRT.localScale = Vector3.one * 0.05f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(labelGO.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            Text label = textGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = brand;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            CarController cc = car.AddComponent<CarController>();
            cc.brandName = brand;
            cc.brandColor = color;
            cc.minRep = minRep;
        }

        private static void BuildFBXCar(Transform parent, string brand, Color color, int minRep, Vector3 position,
            GameObject fbxModel, bool isSUV, bool isCoupe, Material bodyMat, Material glassMat, Material wheelMat,
            Material headlightMat, Material taillightMat, Material tireMat, Material rimMat)
        {
            float carW = isSUV ? 2f : 1.85f;
            float carL = isSUV ? 4.6f : (isCoupe ? 4.2f : 4.4f);
            float rideH = isSUV ? 0.45f : 0.3f;

            GameObject car = new GameObject(brand);
            car.transform.SetParent(parent);
            car.transform.position = position;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxModel, car.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds totalBounds = new Bounds(model.transform.position, Vector3.zero);
            foreach (Renderer r in renderers)
            {
                totalBounds.Encapsulate(r.bounds);
            }

            float modelLength = totalBounds.size.z;
            float modelWidth = totalBounds.size.x;
            float modelHeight = totalBounds.size.y;
            float scaleX = carW / Mathf.Max(modelWidth, 0.1f);
            float scaleZ = carL / Mathf.Max(modelLength, 0.1f);
            float uniformScale = Mathf.Min(scaleX, scaleZ);
            model.transform.localScale = Vector3.one * uniformScale;

            float groundOffset = -totalBounds.min.y * uniformScale + rideH;
            model.transform.localPosition = new Vector3(0f, groundOffset, 0f);

            foreach (Renderer r in renderers)
            {
                Object.DestroyImmediate(r.GetComponent<Collider>());
                string rName = r.gameObject.name.ToLower();
                if (rName.Contains("wheel") || rName.Contains("tire"))
                    r.sharedMaterial = tireMat;
                else if (rName.Contains("glass") || rName.Contains("window") || rName.Contains("windshield"))
                    r.sharedMaterial = glassMat;
                else if (rName.Contains("light"))
                    r.sharedMaterial = headlightMat;
                else
                    r.sharedMaterial = bodyMat;
            }

            float wheelW = isSUV ? 0.38f : 0.32f;
            float wheelH = isSUV ? 0.18f : 0.14f;
            float fZ = carL * 0.32f;
            float rZ = -carL * 0.32f;
            float wheelY = rideH * 0.5f + wheelH;
            Vector3[] wheelPos =
            {
                new Vector3(-carW * 0.5f - 0.02f, wheelY, fZ),
                new Vector3(carW * 0.5f + 0.02f, wheelY, fZ),
                new Vector3(-carW * 0.5f - 0.02f, wheelY, rZ),
                new Vector3(carW * 0.5f + 0.02f, wheelY, rZ)
            };
            foreach (Vector3 wp in wheelPos)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(car.transform);
                wheel.transform.localPosition = wp;
                wheel.transform.localScale = new Vector3(wheelW, wheelH, wheelW);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                SetRendererMaterial(wheel, tireMat);
                Object.DestroyImmediate(wheel.GetComponent<Collider>());

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wheel.transform);
                rim.transform.localPosition = Vector3.zero;
                rim.transform.localScale = new Vector3(0.55f, 0.52f, 0.55f);
                rim.transform.localRotation = Quaternion.identity;
                SetRendererMaterial(rim, rimMat);
                Object.DestroyImmediate(rim.GetComponent<Collider>());
            }

            float hoodFront = carL * 0.5f;
            float trunkBack = -carL * 0.5f;
            Vector3[] hlPos = { new Vector3(-carW * 0.35f, rideH + 0.18f, hoodFront + 0.13f), new Vector3(carW * 0.35f, rideH + 0.18f, hoodFront + 0.13f) };
            foreach (Vector3 hp in hlPos)
            {
                GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hl.name = "Headlight";
                hl.transform.SetParent(car.transform);
                hl.transform.localPosition = hp;
                hl.transform.localScale = new Vector3(0.35f, 0.18f, 0.06f);
                SetRendererMaterial(hl, headlightMat);
                Object.DestroyImmediate(hl.GetComponent<Collider>());
            }

            Vector3[] tlPos = { new Vector3(-carW * 0.35f, rideH + 0.18f, trunkBack - 0.13f), new Vector3(carW * 0.35f, rideH + 0.18f, trunkBack - 0.13f) };
            foreach (Vector3 tp in tlPos)
            {
                GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tl.name = "Taillight";
                tl.transform.SetParent(car.transform);
                tl.transform.localPosition = tp;
                tl.transform.localScale = new Vector3(0.28f, 0.14f, 0.06f);
                SetRendererMaterial(tl, taillightMat);
                Object.DestroyImmediate(tl.GetComponent<Collider>());
            }

            float plateW = 0.6f, plateH = 0.15f;
            GameObject frontPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPlate.name = "FrontPlate";
            frontPlate.transform.SetParent(car.transform);
            frontPlate.transform.localPosition = new Vector3(0f, rideH + 0.15f, hoodFront + 0.14f);
            frontPlate.transform.localScale = new Vector3(plateW, plateH, 0.02f);
            SetRendererMaterial(frontPlate, CreateMaterial("M_Plate_" + brand, Color.white));
            Object.DestroyImmediate(frontPlate.GetComponent<Collider>());

            GameObject rearPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearPlate.name = "RearPlate";
            rearPlate.transform.SetParent(car.transform);
            rearPlate.transform.localPosition = new Vector3(0f, rideH + 0.15f, trunkBack - 0.14f);
            rearPlate.transform.localScale = new Vector3(plateW, plateH, 0.02f);
            SetRendererMaterial(rearPlate, CreateMaterial("M_RearPlate_" + brand, Color.white));
            Object.DestroyImmediate(rearPlate.GetComponent<Collider>());

            BoxCollider col = car.AddComponent<BoxCollider>();
            col.size = new Vector3(carW + 0.2f, 1.2f, carL + 0.3f);
            col.center = new Vector3(0f, rideH + 0.45f, 0f);

            GameObject labelGO = new GameObject("BrandLabel");
            labelGO.transform.SetParent(car.transform);
            labelGO.transform.localPosition = new Vector3(0f, rideH + 1.2f, 0f);
            labelGO.transform.localRotation = Quaternion.identity;

            Canvas labelCanvas = labelGO.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRT = labelGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(4f, 0.6f);
            canvasRT.localScale = Vector3.one * 0.05f;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(labelGO.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            Text label = textGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.text = brand;

            CarController cc = car.AddComponent<CarController>();
            cc.brandName = brand;
            cc.brandColor = color;
            cc.minRep = minRep;
        }
    }
}
