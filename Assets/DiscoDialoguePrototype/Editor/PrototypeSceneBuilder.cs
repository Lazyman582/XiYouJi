using System.Collections.Generic;
using DiscoDialoguePrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiscoDialoguePrototype.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string RootFolder = "Assets/DiscoDialoguePrototype";
        private const string DataFolder = RootFolder + "/Data";
        private const string SceneFolder = RootFolder + "/Scenes";

        [MenuItem("Tools/Disco Prototype/Build Walk & Talk Scene")]
        public static void BuildScene()
        {
            EnsureFolder("Assets", "DiscoDialoguePrototype");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder, "Scenes");

            DialogueGraph dialogue = CreateDialogueGraph();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildEnvironment();
            GameObject player = BuildPlayer();
            BuildCamera(player.transform);
            BuildNpc("MARTIN", "The Man Under the Awning", new Vector3(2.8f, 1f, 2.2f), new Color(0.73f, 0.36f, 0.25f), dialogue);
            BuildNpc("KIM?", "A Patient Witness", new Vector3(-4.5f, 1f, -2.7f), new Color(0.25f, 0.46f, 0.58f), dialogue, "You can ask about the chalk marks.\nThe city keeps receipts.");
            BuildStreetSign();
            BuildRuntimeSystems();

            string scenePath = SceneFolder + "/WalkAndTalkPrototype.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            Selection.activeGameObject = player;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log("Disco Dialogue Prototype scene built at " + scenePath);
        }

        [MenuItem("Tools/Disco Prototype/Create Dialogue Graph Only")]
        public static void CreateDialogueGraphOnly()
        {
            EnsureFolder("Assets", "DiscoDialoguePrototype");
            EnsureFolder(RootFolder, "Data");
            CreateDialogueGraph();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static DialogueGraph CreateDialogueGraph()
        {
            const string assetPath = DataFolder + "/MartinConversation.asset";
            DialogueGraph graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(assetPath);
            if (graph == null)
            {
                graph = ScriptableObject.CreateInstance<DialogueGraph>();
                AssetDatabase.CreateAsset(graph, assetPath);
            }

            graph.graphId = "martin.awning.conversation";
            graph.startNodeId = "start";
            graph.nodes = new List<DialogueNode>
            {
                new DialogueNode("start", "MARTIN", "The awning leaks in three places. I have named them. The names are not helping.", "M", "ANXIETY")
                {
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice("Ask what he is guarding.", "guarding", "logic") { setFlag = "asked_guard" },
                        new DialogueChoice("Tell him the rain has a point.", "rain", "empathy"),
                        new DialogueChoice("Say nothing. Let the silence do paperwork.", "silence", "inland_empire")
                    }
                },
                new DialogueNode("guarding", "MARTIN", "A door. Behind it: a room. Inside the room: an idea that has not paid rent.", "M", "LOGIC")
                {
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice("Offer to inspect the door.", "inspect", "authority"),
                        new DialogueChoice("Back away from the idea.", "end", "volition")
                    }
                },
                new DialogueNode("rain", "MARTIN", "That is a dangerous way to look at weather. But... yes. It does seem deliberate tonight.", "M", "EMPATHY")
                {
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice("Ask about the chalk marks nearby.", "chalk", "perception"),
                        new DialogueChoice("Leave him with the rain.", "end", "volition")
                    }
                },
                new DialogueNode("silence", "MARTIN", "Good. You understand the first rule of this district: everything speaks when you stop helping it.", "M", "INLAND EMPIRE")
                {
                    nextNodeId = "end"
                },
                new DialogueNode("inspect", "MARTIN", "No. Actually, don't. The door is doing fine without witnesses.", "M", "AUTHORITY")
                {
                    nextNodeId = "end"
                },
                new DialogueNode("chalk", "MARTIN", "Those are not marks. They are a map of where the city forgets to look.", "M", "PERCEPTION")
                {
                    nextNodeId = "end"
                },
                new DialogueNode("end", "MARTIN", "The awning drips. The district waits. You can come back when you have a better question.", "M", "NEUTRAL")
            };

            EditorUtility.SetDirty(graph);
            return graph;
        }

        private static void BuildEnvironment()
        {
            Material asphalt = CreateMaterial("Asphalt", new Color(0.09f, 0.12f, 0.15f));
            Material concrete = CreateMaterial("Concrete", new Color(0.24f, 0.29f, 0.29f));
            Material curb = CreateMaterial("Curb", new Color(0.48f, 0.44f, 0.34f));
            Material wall = CreateMaterial("Brick Wall", new Color(0.26f, 0.15f, 0.13f));
            Material water = CreateMaterial("Rainwater", new Color(0.06f, 0.20f, 0.25f));
            Material neon = CreateMaterial("Neon Amber", new Color(1f, 0.35f, 0.08f), true);

            CreatePrimitive("Boardwalk", PrimitiveType.Cube, Vector3.zero, new Vector3(18f, 0.2f, 13f), asphalt);
            CreatePrimitive("Sidewalk_Left", PrimitiveType.Cube, new Vector3(-6.6f, 0.12f, 0f), new Vector3(4.8f, 0.25f, 12.5f), concrete);
            CreatePrimitive("Sidewalk_Right", PrimitiveType.Cube, new Vector3(6.6f, 0.12f, 0f), new Vector3(4.8f, 0.25f, 12.5f), concrete);
            CreatePrimitive("RainChannel", PrimitiveType.Cube, new Vector3(0f, 0.13f, -5.8f), new Vector3(17f, 0.16f, 0.6f), water);
            CreatePrimitive("Curb_Left", PrimitiveType.Cube, new Vector3(-4.25f, 0.34f, 0f), new Vector3(0.25f, 0.4f, 12.5f), curb);
            CreatePrimitive("Curb_Right", PrimitiveType.Cube, new Vector3(4.25f, 0.34f, 0f), new Vector3(0.25f, 0.4f, 12.5f), curb);
            CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 2.1f, 6.3f), new Vector3(18f, 4.1f, 0.3f), wall);
            CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-8.9f, 2.1f, 0f), new Vector3(0.3f, 4.1f, 13f), wall);
            CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3(8.9f, 2.1f, 0f), new Vector3(0.3f, 4.1f, 13f), wall);

            CreatePrimitive("NeonPool", PrimitiveType.Cube, new Vector3(0f, 0.27f, 5.65f), new Vector3(6.5f, 0.08f, 0.1f), neon);
            CreateLamp(new Vector3(-3.6f, 0f, 3.6f), neon);
            CreateLamp(new Vector3(3.7f, 0f, -0.8f), neon);
            CreateLamp(new Vector3(-6.1f, 0f, -4.0f), neon);

            GameObject title = CreateWorldText("WALK / TALK / REMEMBER", new Vector3(0f, 3.8f, 5.95f), 0.28f, new Color(1f, 0.57f, 0.23f));
            title.AddComponent<BillboardLabel>();
        }

        private static GameObject BuildPlayer()
        {
            Material coat = CreateMaterial("Player Coat", new Color(0.72f, 0.68f, 0.48f));
            Material shirt = CreateMaterial("Player Shirt", new Color(0.12f, 0.17f, 0.18f));
            GameObject player = new GameObject("Player_Protagonist");
            player.transform.position = new Vector3(0f, 1.15f, -3.7f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.42f;
            controller.center = new Vector3(0f, -0.1f, 0f);
            player.AddComponent<TopDownPlayerController>();

            GameObject body = CreatePrimitive("Body", PrimitiveType.Capsule, player.transform.position, new Vector3(0.82f, 1.05f, 0.82f), coat);
            body.transform.SetParent(player.transform);
            body.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            GameObject shirtVisual = CreatePrimitive("Shirt", PrimitiveType.Cube, player.transform.position, new Vector3(0.62f, 0.55f, 0.62f), shirt);
            shirtVisual.transform.SetParent(player.transform);
            shirtVisual.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            UnityEngine.Object.DestroyImmediate(shirtVisual.GetComponent<Collider>());
            GameObject label = CreateWorldText("YOU", player.transform.position + Vector3.up * 1.45f, 0.18f, new Color(0.93f, 0.9f, 0.62f));
            label.transform.SetParent(player.transform);
            label.transform.localPosition = Vector3.up * 1.45f;
            label.AddComponent<BillboardLabel>();
            return player;
        }

        private static GameObject BuildNpc(string glyph, string displayName, Vector3 position, Color color, DialogueGraph dialogue, string extraLine = "")
        {
            Material bodyMaterial = CreateMaterial(displayName + " Body", color);
            Material skin = CreateMaterial(displayName + " Skin", new Color(0.75f, 0.51f, 0.36f));
            GameObject npc = new GameObject("NPC_" + glyph.Replace("?", ""));
            npc.transform.position = position;
            CapsuleCollider collider = npc.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            collider.center = Vector3.zero;
            PrototypeInteractable interactable = npc.AddComponent<PrototypeInteractable>();
            interactable.displayName = displayName;
            interactable.interactionHint = "Talk";
            interactable.dialogue = dialogue;

            GameObject body = CreatePrimitive("Body", PrimitiveType.Capsule, position, new Vector3(0.95f, 1.05f, 0.95f), bodyMaterial);
            body.transform.SetParent(npc.transform);
            body.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            GameObject head = CreatePrimitive("Head", PrimitiveType.Sphere, position + Vector3.up * 0.95f, new Vector3(0.67f, 0.67f, 0.67f), skin);
            head.transform.SetParent(npc.transform);
            head.transform.localPosition = Vector3.up * 0.95f;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            GameObject halo = CreatePrimitive("FocusHalo", PrimitiveType.Cylinder, position + Vector3.up * 1.75f, new Vector3(1.25f, 0.04f, 1.25f), CreateMaterial(displayName + " Halo", new Color(1f, 0.62f, 0.18f), true));
            halo.transform.SetParent(npc.transform);
            halo.transform.localPosition = Vector3.up * 1.75f;
            UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());
            GameObject label = CreateWorldText(glyph + "\n" + displayName, position + Vector3.up * 2.2f, 0.16f, new Color(0.91f, 0.92f, 0.78f));
            label.transform.SetParent(npc.transform);
            label.transform.localPosition = Vector3.up * 2.2f;
            label.AddComponent<BillboardLabel>();

            if (!string.IsNullOrEmpty(extraLine))
            {
                GameObject note = CreateWorldText(extraLine, position + Vector3.left * 1.35f + Vector3.up * 0.3f, 0.1f, new Color(0.45f, 0.75f, 0.71f));
                note.transform.SetParent(npc.transform);
                note.transform.localPosition = Vector3.left * 1.35f + Vector3.up * 0.3f;
                note.AddComponent<BillboardLabel>();
            }

            return npc;
        }

        private static void BuildCamera(Transform player)
        {
            GameObject cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 43f;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.065f);
            cameraObject.AddComponent<AudioListener>();
            IsometricCameraFollow follow = cameraObject.AddComponent<IsometricCameraFollow>();
            follow.target = player;
            follow.offset = new Vector3(0f, 12.5f, -10.5f);
            cameraObject.transform.position = player.position + follow.offset;
            cameraObject.transform.LookAt(player.position + Vector3.up * 0.5f);
            TopDownPlayerController controller = player.GetComponent<TopDownPlayerController>();
            controller.gameplayCamera = camera;
        }

        private static void BuildRuntimeSystems()
        {
            GameObject systems = new GameObject("PrototypeRuntime");
            systems.AddComponent<DialogueRunner>();
            systems.AddComponent<DialogueUI>();

            GameObject light = new GameObject("MoonLight");
            Light directional = light.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.7f;
            directional.color = new Color(0.56f, 0.68f, 0.88f);
            light.transform.rotation = Quaternion.Euler(46f, -25f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.11f, 0.15f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.04f, 0.07f, 0.09f);
            RenderSettings.fogDensity = 0.018f;
        }

        private static void BuildStreetSign()
        {
            Material signMaterial = CreateMaterial("Sign", new Color(0.06f, 0.08f, 0.08f));
            GameObject sign = CreatePrimitive("DistrictSign", PrimitiveType.Cube, new Vector3(-6.9f, 2.2f, 4.9f), new Vector3(2.4f, 0.8f, 0.14f), signMaterial);
            GameObject text = CreateWorldText("MARTINAISE\nNIGHT SHIFT", new Vector3(-6.9f, 2.2f, 4.78f), 0.14f, new Color(0.95f, 0.48f, 0.18f));
            text.AddComponent<BillboardLabel>();
            sign.transform.rotation = Quaternion.Euler(0f, 0f, 2f);
        }

        private static void CreateLamp(Vector3 position, Material glow)
        {
            Material pole = CreateMaterial("Lamp Pole", new Color(0.09f, 0.1f, 0.1f));
            CreatePrimitive("LampPole", PrimitiveType.Cylinder, position + Vector3.up * 1.3f, new Vector3(0.12f, 1.3f, 0.12f), pole);
            GameObject lamp = CreatePrimitive("LampGlow", PrimitiveType.Sphere, position + Vector3.up * 2.6f, new Vector3(0.34f, 0.34f, 0.34f), glow);
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.44f, 0.14f);
            light.intensity = 3f;
            light.range = 5f;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return gameObject;
        }

        private static GameObject CreateWorldText(string text, Vector3 position, float characterSize, Color color)
        {
            GameObject gameObject = new GameObject("Text_" + text.Replace("\n", "_").Replace(" ", "_"));
            gameObject.transform.position = position;
            TextMesh mesh = gameObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return gameObject;
        }

        private static Material CreateMaterial(string name, Color color, bool emission = false)
        {
            string assetPath = DataFolder + "/" + name.Replace(" ", "") + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
