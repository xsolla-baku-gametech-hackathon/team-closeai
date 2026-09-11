using System;
using System.IO;
using InertialNetcodeDemo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds and validates the Sun-Earth A/B netcode demonstration.</summary>
public static class AutoBuildDemoScene
{
    private const string Root = "Assets/InertialNetcodeDemo";
    private const string SceneDirectory = Root + "/Scenes";
    private const string ScenePath = SceneDirectory + "/InertialNetcodeDemo.unity";
    private const string MaterialDirectory = Root + "/Materials";
    private const string ClassicLayer = "DemoClassic";
    private const string InertialLayer = "DemoInertial";

    [MenuItem("Tools/Auto Build Complete Scene")]
    public static void BuildScene()
    {
        EnsureFolder(Root);
        EnsureFolder(SceneDirectory);
        EnsureFolder(MaterialDirectory);

        int classicLayer = EnsureLayer(ClassicLayer);
        int inertialLayer = EnsureLayer(InertialLayer);
        Texture2D sunTexture = RequireTexture("Assets/Textures/sun.jpg");
        Texture2D earthTexture = RequireTexture("Assets/Textures/earth.jpg");
        Material sunMaterial = CreateSunMaterial(sunTexture);
        Material earthMaterial = CreateEarthMaterial(earthTexture);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TargetMovement target = new GameObject("MasterTarget").AddComponent<TargetMovement>();
        SetFloat(target, "radiusX", 6f);
        SetFloat(target, "radiusZ", 6f);
        NetworkPacketLossSimulator network = new GameObject("Network").AddComponent<NetworkPacketLossSimulator>();
        SetReference(network, "target", target);

        GameObject classicWorld = new GameObject("ClassicWorld");
        classicWorld.transform.position = new Vector3(-20f, 0f, 0f);
        CreateSun("Sun_Classic", classicWorld.transform, classicLayer, sunMaterial);
        GameObject classicAvatar = CreatePrimitive(PrimitiveType.Sphere, "ClassicAvatar", classicWorld.transform,
            Vector3.zero, Vector3.one * 0.8f, classicLayer, earthMaterial);
        ClassicNetcodeDummy classicClient = classicAvatar.AddComponent<ClassicNetcodeDummy>();
        classicAvatar.AddComponent<RotateEarth>();
        SetReference(classicClient, "network", network);

        GameObject inertialWorld = new GameObject("InertialWorld");
        inertialWorld.transform.position = new Vector3(20f, 0f, 0f);
        CreateSun("Sun_Inertial", inertialWorld.transform, inertialLayer, sunMaterial);
        GameObject inertialAvatar = CreatePrimitive(PrimitiveType.Sphere, "InertialAvatar", inertialWorld.transform,
            Vector3.zero, Vector3.one * 0.8f, inertialLayer, earthMaterial);
        InertialNetcodeDummy inertialClient = inertialAvatar.AddComponent<InertialNetcodeDummy>();
        inertialAvatar.AddComponent<RotateEarth>();
        SetReference(inertialClient, "network", network);

        CreateStarfield();

        Camera classicCamera = CreateCamera("ClassicCamera", new Vector3(-20f, 20f, 0f),
            classicLayer, new Rect(0f, 0f, 0.5f, 1f));
        Camera inertialCamera = CreateCamera("InertialCamera", new Vector3(20f, 20f, 0f),
            inertialLayer, new Rect(0.5f, 0f, 0.5f, 1f));

        Canvas canvas = CreateCanvas(out Text status, out Text telemetry, out Text classicLabel,
            out Text inertialLabel, out Button toggleButton, out Text toggleButtonText);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        DemoUIController demoUI = new GameObject("DemoUI").AddComponent<DemoUIController>();
        SetReference(demoUI, "network", network);
        SetReference(demoUI, "classicClient", classicClient);
        SetReference(demoUI, "inertialClient", inertialClient);
        SetReference(demoUI, "classicCamera", classicCamera);
        SetReference(demoUI, "inertialCamera", inertialCamera);
        SetReference(demoUI, "overlayCanvas", canvas);
        SetReference(demoUI, "packetLossText", status);
        SetReference(demoUI, "telemetryText", telemetry);
        SetReference(demoUI, "classicLabel", classicLabel);
        SetReference(demoUI, "inertialLabel", inertialLabel);
        SetReference(demoUI, "toggleLossButton", toggleButton);
        SetReference(demoUI, "toggleButtonText", toggleButtonText);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Unable to save " + ScenePath);

        AssetDatabase.SaveAssets();
        Validate(scene);
        Debug.Log("Inertial Netcode Astronomical Scene Built Successfully!");
    }

    [MenuItem("Tools/Validate Inertial Netcode Astronomical Scene")]
    public static void VerifyScene()
    {
        if (!File.Exists(ScenePath))
            throw new FileNotFoundException("Build the demo scene first.", ScenePath);
        Validate(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
        Debug.Log("Inertial Netcode Astronomical Scene Built Successfully!");
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Invalid asset path: " + path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;

        UnityEngine.Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets.Length == 0) throw new InvalidOperationException("TagManager.asset is unavailable.");
        SerializedProperty layers = new SerializedObject(tagAssets[0]).FindProperty("layers");
        for (int index = 8; index < layers.arraySize; index++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;
            layer.stringValue = name;
            layer.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return index;
        }
        throw new InvalidOperationException("No user layer is available for " + name);
    }

    private static Texture2D RequireTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) throw new FileNotFoundException("Required texture is missing.", path);
        return texture;
    }

    private static Material CreateSunMaterial(Texture2D texture)
    {
        Material material = CreateUrpLitMaterial("Sun_Mat");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.EnableKeyword("_EMISSION");
        material.SetTexture("_EmissionMap", texture);
        material.SetColor("_EmissionColor", new Color(1f, 0.33f, 0.04f) * 3f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateEarthMaterial(Texture2D texture)
    {
        Material material = CreateUrpLitMaterial("Earth_Mat");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0.02f);
        material.SetFloat("_Smoothness", 0.62f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateUrpLitMaterial(string name)
    {
        string path = MaterialDirectory + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP/Lit shader was not found.");
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        return material;
    }
    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
        Vector3 scale, int layer, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        gameObject.layer = layer;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        return gameObject;
    }

    private static Camera CreateCamera(string name, Vector3 position, int layer, Rect viewport)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
        Camera camera = gameObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 9f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        camera.rect = viewport;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = (1 << layer) | (1 << 0);
        return camera;
    }

    private static ParticleSystem CreateStarfield()
    {
        GameObject starfieldObject = new GameObject("ProceduralStarfield");
        starfieldObject.transform.position = Vector3.zero;
        ParticleSystem starfield = starfieldObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = starfield.main;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.maxParticles = 3000;
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        ParticleSystem.EmissionModule emission = starfield.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3000) });

        ParticleSystem.ShapeModule shape = starfield.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 100f;
        shape.radiusThickness = 0f;
        return starfield;
    }

    private static void CreateSun(string name, Transform world, int layer, Material material)
    {
        GameObject sun = CreatePrimitive(PrimitiveType.Sphere, name, world, new Vector3(0f, 0.5f, 0f),
            Vector3.one * 2f, layer, material);
        GameObject lightObject = new GameObject("Point Light");
        lightObject.transform.SetParent(sun.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 50f;
        light.intensity = 4f;
        light.color = new Color(1f, 0.82f, 0.52f);
        light.shadows = LightShadows.Soft;
    }
    private static Canvas CreateCanvas(out Text status, out Text telemetry, out Text classicLabel,
        out Text inertialLabel, out Button toggleButton, out Text toggleButtonText)
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        status = TextObject("Status", canvas.transform, 28, Color.white);
        SetTop(status.rectTransform, new Vector2(0f, -25f), new Vector2(1100f, 45f));
        telemetry = TextObject("Telemetry", canvas.transform, 20, Color.white);
        SetTop(telemetry.rectTransform, new Vector2(0f, -80f), new Vector2(1780f, 150f));
        classicLabel = TextObject("ClassicLabel", canvas.transform, 22, Color.white);
        SetFloating(classicLabel.rectTransform);
        inertialLabel = TextObject("InertialLabel", canvas.transform, 22, Color.white);
        SetFloating(inertialLabel.rectTransform);

        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divider.transform.SetParent(canvas.transform, false);
        RectTransform dividerRect = divider.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0f);
        dividerRect.anchorMax = new Vector2(0.5f, 1f);
        dividerRect.sizeDelta = new Vector2(2f, 0f);
        Image dividerImage = divider.GetComponent<Image>();
        dividerImage.color = new Color(1f, 1f, 1f, 0.18f);
        dividerImage.raycastTarget = false;

        GameObject buttonObject = new GameObject("ToggleLossButton", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 25f);
        buttonRect.sizeDelta = new Vector2(400f, 60f);
        toggleButton = buttonObject.GetComponent<Button>();
        toggleButton.targetGraphic = buttonObject.GetComponent<Image>();
        toggleButtonText = TextObject("Text", buttonObject.transform, 20, Color.black);
        RectTransform textRect = toggleButtonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        return canvas;
    }

    private static Text TextObject(string name, Transform parent, int fontSize, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        Text text = gameObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (text.font == null) throw new InvalidOperationException("Unity's legacy UI font is unavailable.");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTop(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetFloating(RectTransform rect)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(520f, 70f);
    }

    private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(fieldName);
        if (property == null) throw new InvalidOperationException("Missing serialized field " + fieldName);
        property.objectReferenceValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string fieldName, float value)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(fieldName);
        if (property == null) throw new InvalidOperationException("Missing serialized field " + fieldName);
        property.floatValue = value;
        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
    private static UnityEngine.Object GetReference(UnityEngine.Object target, string fieldName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(fieldName);
        if (property == null) throw new InvalidOperationException("Missing serialized field " + fieldName);
        return property.objectReferenceValue;
    }

    private static void Validate(Scene scene)
    {
        Require(scene.IsValid() && scene.isLoaded, "Scene did not load.");
        GameObject targetObject = RootObject(scene, "MasterTarget");
        GameObject networkObject = RootObject(scene, "Network");
        GameObject classicWorld = RootObject(scene, "ClassicWorld");
        GameObject inertialWorld = RootObject(scene, "InertialWorld");
        GameObject canvasObject = RootObject(scene, "Canvas");
        Camera classicCamera = RootObject(scene, "ClassicCamera").GetComponent<Camera>();
        Camera inertialCamera = RootObject(scene, "InertialCamera").GetComponent<Camera>();
        ParticleSystem starfield = RootObject(scene, "ProceduralStarfield").GetComponent<ParticleSystem>();
        RootObject(scene, "EventSystem");
        GameObject demoUIObject = RootObject(scene, "DemoUI");

        TargetMovement target = targetObject.GetComponent<TargetMovement>();
        NetworkPacketLossSimulator network = networkObject.GetComponent<NetworkPacketLossSimulator>();
        GameObject classicSun = Child(classicWorld, "Sun_Classic");
        GameObject inertialSun = Child(inertialWorld, "Sun_Inertial");
        GameObject classicAvatar = Child(classicWorld, "ClassicAvatar");
        GameObject inertialAvatar = Child(inertialWorld, "InertialAvatar");
        ClassicNetcodeDummy classic = classicAvatar.GetComponent<ClassicNetcodeDummy>();
        InertialNetcodeDummy inertial = inertialAvatar.GetComponent<InertialNetcodeDummy>();
        DemoUIController demoUI = demoUIObject.GetComponent<DemoUIController>();
        Require(target != null && network != null && classic != null && inertial != null && demoUI != null,
            "A required demo component is missing.");
        Require(classicWorld.transform.position == new Vector3(-20f, 0f, 0f), "Classic world position is incorrect.");
        Require(inertialWorld.transform.position == new Vector3(20f, 0f, 0f), "Inertial world position is incorrect.");
        Require(Child(classicSun, "Point Light").GetComponent<Light>() != null, "Classic sun light is missing.");
        Require(Child(inertialSun, "Point Light").GetComponent<Light>() != null, "Inertial sun light is missing.");
        Require(classicAvatar.GetComponent<RotateEarth>() != null && inertialAvatar.GetComponent<RotateEarth>() != null,
            "Earth rotation components are missing.");
        Require(classicSun.GetComponent<Renderer>().sharedMaterial.name == "Sun_Mat", "Classic sun material is incorrect.");
        Require(inertialSun.GetComponent<Renderer>().sharedMaterial.name == "Sun_Mat", "Inertial sun material is incorrect.");
        Require(classicAvatar.GetComponent<Renderer>().sharedMaterial.name == "Earth_Mat", "Classic Earth material is incorrect.");
        Require(inertialAvatar.GetComponent<Renderer>().sharedMaterial.name == "Earth_Mat", "Inertial Earth material is incorrect.");
        Require(classicCamera != null && inertialCamera != null && classicCamera.orthographic && inertialCamera.orthographic,
            "Split-screen cameras are incorrect.");
        Require(classicCamera.clearFlags == CameraClearFlags.SolidColor && inertialCamera.clearFlags == CameraClearFlags.SolidColor
            && classicCamera.backgroundColor == Color.black && inertialCamera.backgroundColor == Color.black,
            "Deep-space camera backgrounds are incorrect.");
        Require(starfield != null, "Procedural starfield is missing.");
        ParticleSystem.MainModule starMain = starfield.main;
        ParticleSystem.EmissionModule starEmission = starfield.emission;
        ParticleSystem.ShapeModule starShape = starfield.shape;
        Require(float.IsPositiveInfinity(starMain.startLifetime.constant) && Mathf.Approximately(starMain.startSpeed.constant, 0f)
            && Mathf.Approximately(starMain.startSize.constantMin, 0.05f) && Mathf.Approximately(starMain.startSize.constantMax, 0.25f)
            && starMain.maxParticles == 3000 && starEmission.rateOverTime.constant == 0f && starEmission.burstCount == 1
            && starShape.shapeType == ParticleSystemShapeType.Sphere && Mathf.Approximately(starShape.radius, 100f)
            && Mathf.Approximately(starShape.radiusThickness, 0f), "Procedural starfield settings are incorrect.");
        Require(canvasObject.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Canvas mode is incorrect.");
        Require(Mathf.Approximately(GetFloat(target, "radiusX"), 6f) && Mathf.Approximately(GetFloat(target, "radiusZ"), 6f),
            "Master target orbit is not circular.");
        Require(GetReference(network, "target") == target, "Network target reference is incorrect.");
        Require(GetReference(classic, "network") == network, "Classic network reference is incorrect.");
        Require(GetReference(inertial, "network") == network, "Inertial network reference is incorrect.");
        Require(GetReference(demoUI, "network") == network, "Demo UI network reference is incorrect.");
        Require(GetReference(demoUI, "classicClient") == classic && GetReference(demoUI, "inertialClient") == inertial,
            "Demo UI client references are incorrect.");
        Require(GetReference(demoUI, "classicCamera") == classicCamera && GetReference(demoUI, "inertialCamera") == inertialCamera,
            "Demo UI camera references are incorrect.");
        Require(GetReference(demoUI, "overlayCanvas") == canvasObject.GetComponent<Canvas>(), "Demo UI canvas reference is incorrect.");
        Require(GetReference(demoUI, "packetLossText") != null && GetReference(demoUI, "telemetryText") != null
            && GetReference(demoUI, "classicLabel") != null && GetReference(demoUI, "inertialLabel") != null
            && GetReference(demoUI, "toggleLossButton") != null && GetReference(demoUI, "toggleButtonText") != null,
            "Demo UI text or button references are missing.");
    }

    private static float GetFloat(UnityEngine.Object target, string fieldName)
    {
        SerializedProperty property = new SerializedObject(target).FindProperty(fieldName);
        if (property == null) throw new InvalidOperationException("Missing serialized field " + fieldName);
        return property.floatValue;
    }
    private static GameObject RootObject(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        throw new InvalidOperationException("Missing root object " + name);
    }

    private static GameObject Child(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;
        throw new InvalidOperationException("Missing child " + name + " under " + parent.name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}



