using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
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
            Transform world = GetOrCreateEmpty("World");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(world);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            SetRendererMaterial(ground, CreateMaterial("M_Grass", new Color(0.45f, 0.62f, 0.34f)));

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "MainRoad";
            road.transform.SetParent(world);
            road.transform.position = new Vector3(0f, 0.05f, 0f);
            road.transform.localScale = new Vector3(12f, 0.1f, 80f);
            SetRendererMaterial(road, CreateMaterial("M_Road", new Color(0.30f, 0.30f, 0.32f)));

            Transform town = GetOrCreateEmpty("Town", world);

            Material houseA = CreateMaterial("M_HouseA", new Color(0.85f, 0.78f, 0.62f));
            Material houseB = CreateMaterial("M_HouseB", new Color(0.62f, 0.50f, 0.38f));
            Material shop = CreateMaterial("M_Shop", new Color(0.72f, 0.72f, 0.74f));
            Material roof = CreateMaterial("M_Roof", new Color(0.45f, 0.20f, 0.14f));

            BuildBuilding(town, "House_01", new Vector3(-16f, 0f, 12f), new Vector3(8f, 5f, 8f), houseA, roof);
            BuildBuilding(town, "House_02", new Vector3(-16f, 0f, -12f), new Vector3(8f, 4f, 8f), houseB, roof);
            BuildBuilding(town, "Shop_01", new Vector3(12f, 0f, 6f), new Vector3(7f, 3.5f, 9f), shop, roof);
            BuildBuilding(town, "Shop_02", new Vector3(12f, 0f, -14f), new Vector3(7f, 3.5f, 9f), shop, roof);
            BuildBuilding(town, "TownHall", new Vector3(0f, 0f, -30f), new Vector3(12f, 8f, 12f), houseA, roof);
            BuildBuilding(town, "Market_01", new Vector3(20f, 0f, 24f), new Vector3(6f, 3f, 10f), houseB, roof);
            BuildBuilding(town, "Market_02", new Vector3(-22f, 0f, 24f), new Vector3(6f, 3f, 10f), houseB, roof);
        }

        private static void BuildBuilding(Transform parent, string name, Vector3 basePos, Vector3 baseSize, Material wallMat, Material roofMat)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name + "_Body";
            body.transform.SetParent(parent);
            body.transform.position = basePos + Vector3.up * (baseSize.y * 0.5f);
            body.transform.localScale = baseSize;
            SetRendererMaterial(body, wallMat);

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = name + "_Roof";
            roof.transform.SetParent(parent);
            roof.transform.position = basePos + Vector3.up * baseSize.y + Vector3.up * 0.6f;
            roof.transform.localScale = new Vector3(baseSize.x + 0.8f, 1.2f, baseSize.z + 0.8f);
            SetRendererMaterial(roof, roofMat);
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
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.82f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.82f, 0.86f, 0.92f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;

            Light sun = Object.FindFirstObjectByType<Light>();
            if (sun != null) sun.color = new Color(1f, 0.95f, 0.86f);
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

            BuildRoadLines(details, line);
            BuildTrees(details, trunk, leaves);
            BuildBushes(details, bush);
            BuildRocks(details, rock);
            BuildFlowers(details, flowerRed, flowerWhite);
            BuildLamps(details, lamp, lampHead);
            BuildFences(details, fence);
        }

        private static void BuildRoadLines(Transform parent, Material mat)
        {
            for (int z = -38; z <= 38; z += 6)
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
                new Vector3(-32f, 0f, -34f), new Vector3(32f, 0f, -34f)
            };
            foreach (Vector3 spot in spots)
            {
                BuildTree(parent, spot, trunk, leaves);
            }
        }

        private static void BuildTree(Transform parent, Vector3 pos, Material trunk, Material leaves)
        {
            float h = UnityEngine.Random.Range(2.6f, 3.4f);
            GameObject trunkGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkGO.name = "Tree_Trunk";
            trunkGO.transform.SetParent(parent);
            trunkGO.transform.position = new Vector3(pos.x, h * 0.5f, pos.z);
            trunkGO.transform.localScale = new Vector3(0.45f, h * 0.5f, 0.45f);
            SetRendererMaterial(trunkGO, trunk);

            float leafScale = UnityEngine.Random.Range(2.6f, 3.4f);
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Tree_Leaves";
            foliage.transform.SetParent(parent);
            foliage.transform.position = new Vector3(pos.x, h + leafScale * 0.45f, pos.z);
            foliage.transform.localScale = Vector3.one * leafScale;
            SetRendererMaterial(foliage, leaves);
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
            EditorUtility.SetDirty(mat);
            return mat;
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
            JobDefinition job = EnsureJobDefinition();
            EnsureWorkStation(job);
            EnsurePlayerNeeds();
            EnsureShop();
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

        private static JobDefinition EnsureJobDefinition()
        {
            const string path = "Assets/Data/Jobs/Job_GeneralWorker.asset";
            JobDefinition job = AssetDatabase.LoadAssetAtPath<JobDefinition>(path);
            if (job != null) return job;

            Directory.CreateDirectory("Assets/Data/Jobs");
            job = ScriptableObject.CreateInstance<JobDefinition>();
            job.name = "Job_GeneralWorker";
            AssetDatabase.CreateAsset(job, path);
            return job;
        }

        private static void EnsureWorkStation(JobDefinition job)
        {
            WorkStation existing = Object.FindFirstObjectByType<WorkStation>();
            if (existing != null) return;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "WorkSpot_Shop";
            marker.transform.position = new Vector3(12f, 0.25f, 11.5f);
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
                new ShopItemData { itemName = "Bread", price = 5, isFood = true },
                new ShopItemData { itemName = "Shirt", price = 25 },
                new ShopItemData { itemName = "Shoes", price = 40 },
                new ShopItemData { itemName = "Watch", price = 120 }
            });
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
