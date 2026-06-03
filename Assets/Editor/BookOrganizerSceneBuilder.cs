using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class BookOrganizerSceneBuilder : MonoBehaviour
{
    private const string ScenePath = "Assets/Scenes/BookOrganizer.unity";

    [MenuItem("BookOrganizer/Create Complete Scene")]
    public static void CreateCompleteScene()
    {
        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BookOrganizer";

        // Setup render settings
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.3f, 0.3f, 0.35f);

        // Create root
        GameObject root = new GameObject("=== Book Organizer Scene ===");

        // Floor
        GameObject floor = CreatePrimitive("Floor", PrimitiveType.Plane, root.transform,
            new Vector3(0, 0, 0), new Vector3(10, 1, 10), new Color(0.75f, 0.68f, 0.58f));
        floor.GetComponent<Collider>().material = null;

        // Walls
        CreateWall(root.transform, "Wall_Back", new Vector3(0, 2.5f, -5), new Vector3(10, 5, 0.2f), new Color(0.85f, 0.82f, 0.78f));
        CreateWall(root.transform, "Wall_Left", new Vector3(-5, 2.5f, 0), new Vector3(0.2f, 5, 10), new Color(0.85f, 0.82f, 0.78f));
        CreateWall(root.transform, "Wall_Right", new Vector3(5, 2.5f, 0), new Vector3(0.2f, 5, 10), new Color(0.85f, 0.82f, 0.78f));
        CreateWall(root.transform, "Wall_Front", new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.2f), new Color(0.85f, 0.82f, 0.78f));

        // Bookshelf
        GameObject bookshelf = CreateBookshelf(root.transform);

        // Player
        GameObject player = CreatePlayer(root.transform);

        // Camera
        GameObject cameraObj = CreateCamera(player.transform);

        // Lights
        CreateLights(root.transform);

        // Books (scattered on floor)
        CreateScatteredBooks(root.transform, bookshelf.transform);

        // UI
        CreateUI(root.transform);

        // Save scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BookOrganizer] Scene created and saved to {ScenePath}");
        Debug.Log($"[BookOrganizer] Press Play to test. Controls: WASD=move, E=pickup/place, F=read PDF");
    }

    static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = color;
        }
        return obj;
    }

    static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject wall = CreatePrimitive(name, PrimitiveType.Cube, parent, pos, scale, color);
        Rigidbody rb = wall.GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);
    }

    static GameObject CreateBookshelf(Transform parent)
    {
        GameObject shelf = new GameObject("Bookshelf");
        shelf.transform.SetParent(parent, false);
        shelf.transform.position = new Vector3(0, 0, -3.5f);

        Color woodColor = new Color(0.55f, 0.35f, 0.2f);

        // Main frame - back
        CreatePrimitive("BackPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(0, 1.5f, -0.15f), new Vector3(3f, 3f, 0.05f), woodColor);

        // Side panels
        CreatePrimitive("LeftPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(-1.5f, 1.5f, 0.1f), new Vector3(0.1f, 3f, 0.4f), woodColor);
        CreatePrimitive("RightPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(1.5f, 1.5f, 0.1f), new Vector3(0.1f, 3f, 0.4f), woodColor);

        // Shelves (4 levels)
        int rows = 4;
        int cols = 5;
        float shelfWidth = 3f;
        float shelfHeight = 3f;
        float startY = 0.3f;

        for (int r = 0; r < rows; r++)
        {
            float y = startY + (shelfHeight / rows) * r;
            GameObject board = CreatePrimitive($"Shelf_{r}", PrimitiveType.Cube, shelf.transform,
                new Vector3(0, y, 0.1f), new Vector3(shelfWidth, 0.05f, 0.4f), woodColor);
            Rigidbody rb = board.GetComponent<Rigidbody>();
            if (rb != null) DestroyImmediate(rb);

            // Create slots on this shelf
            for (int c = 0; c < cols; c++)
            {
                float x = -shelfWidth / 2 + (shelfWidth / cols) * c + (shelfWidth / cols) * 0.5f;
                GameObject slotObj = new GameObject($"Slot_{r}_{c}");
                slotObj.transform.SetParent(shelf.transform, false);
                slotObj.transform.position = new Vector3(x, y + 0.25f, 0.1f);

                BookshelfSlot slot = slotObj.AddComponent<BookshelfSlot>();
                slot.bookOffset = Vector3.zero;
                slot.bookRotation = new Vector3(0, 0, 0);
                slot.gizmoColor = new Color(0, 1, 0, 0.3f);
            }
        }

        // Top panel
        CreatePrimitive("TopPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(0, 3.05f, 0.1f), new Vector3(3.1f, 0.05f, 0.4f), woodColor);

        return shelf;
    }

    static GameObject CreatePlayer(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0, 1, 2);

        Renderer r = player.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0.2f, 0.5f, 0.9f);
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.radius = 0.5f;
        cc.height = 2f;
        cc.center = new Vector3(0, 0, 0);

        player.AddComponent<PlayerController>();

        // Add a small visual for "eyes" direction
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        face.name = "Face";
        face.transform.SetParent(player.transform, false);
        face.transform.localPosition = new Vector3(0, 0.3f, 0.35f);
        face.transform.localScale = new Vector3(0.3f, 0.25f, 0.2f);
        Renderer fr = face.GetComponent<Renderer>();
        if (fr != null)
        {
            fr.material = new Material(Shader.Find("Standard"));
            fr.material.color = new Color(0.9f, 0.8f, 0.7f);
        }
        Collider fc = face.GetComponent<Collider>();
        if (fc != null) DestroyImmediate(fc);

        return player;
    }

    static GameObject CreateCamera(Transform player)
    {
        GameObject camObj = new GameObject("MainCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0, 8, 6);
        cam.transform.rotation = Quaternion.Euler(60, 0, 0);
        cam.orthographic = false;
        cam.fieldOfView = 50;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;
        cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);

        // Simple follow script
        camObj.AddComponent<CameraFollow>().target = player;

        return camObj;
    }

    static void CreateLights(Transform parent)
    {
        // Main directional light
        GameObject lightObj = new GameObject("DirectionalLight");
        lightObj.transform.SetParent(parent, false);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        light.color = new Color(1f, 0.98f, 0.95f);
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        light.shadows = LightShadows.Soft;

        // Warm point light near bookshelf
        GameObject pointLight = new GameObject("BookshelfLight");
        pointLight.transform.SetParent(parent, false);
        pointLight.transform.position = new Vector3(0, 3.5f, -2f);
        Light pl = pointLight.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.intensity = 0.8f;
        pl.range = 8f;
        pl.color = new Color(1f, 0.9f, 0.7f);
    }

    static void CreateScatteredBooks(Transform parent, Transform shelfTransform)
    {
        int bookCount = 8;
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-2, 0.2f, 0),
            new Vector3(1.5f, 0.2f, -1),
            new Vector3(-1, 0.2f, 1.5f),
            new Vector3(2.5f, 0.2f, 1),
            new Vector3(-3, 0.2f, -2),
            new Vector3(0.5f, 0.2f, 2.5f),
            new Vector3(-2.5f, 0.2f, 2),
            new Vector3(3, 0.2f, -0.5f),
        };

        // Scan for real PDFs in StreamingAssets/PDFs
        string pdfDir = System.IO.Path.Combine(Application.dataPath, "StreamingAssets", "PDFs");
        string[] pdfFiles = new string[0];
        if (System.IO.Directory.Exists(pdfDir))
        {
            pdfFiles = System.IO.Directory.GetFiles(pdfDir, "*.pdf");
            for (int i = 0; i < pdfFiles.Length; i++)
            {
                pdfFiles[i] = System.IO.Path.GetFileName(pdfFiles[i]);
            }
        }
        Debug.Log($"[BookOrganizer] Found {pdfFiles.Length} PDF(s) in {pdfDir}");

        for (int i = 0; i < bookCount; i++)
        {
            GameObject bookObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bookObj.name = $"Book_{i}";
            bookObj.transform.SetParent(parent, false);
            bookObj.transform.position = positions[i % positions.Length];
            bookObj.transform.localScale = new Vector3(0.3f, 0.4f, 0.05f);
            bookObj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            Renderer r = bookObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = TitleGenerator.GenerateColor();
            }

            Rigidbody rb = bookObj.GetComponent<Rigidbody>();
            if (rb != null) DestroyImmediate(rb);

            Collider col = bookObj.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            BookItem book = bookObj.AddComponent<BookItem>();
            // Assign real PDF if available, use PDF filename as book title
            if (i < pdfFiles.Length)
            {
                book.pdfFileName = pdfFiles[i];
                book.bookTitle = System.IO.Path.GetFileNameWithoutExtension(pdfFiles[i]);
            }
            else
            {
                book.pdfFileName = "";
                book.bookTitle = TitleGenerator.GenerateTitle();
            }
            book.originalScale = new Vector3(0.3f, 0.4f, 0.05f);
            if (r != null) book.SetColor(r.material.color);
        }
    }

    static void CreateUI(Transform parent)
    {
        GameObject uiObj = new GameObject("BookUI");
        uiObj.transform.SetParent(parent, false);
        uiObj.AddComponent<BookUI>();
    }
}
