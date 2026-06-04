using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class BookOrganizerSceneBuilder : MonoBehaviour
{
    private const string ScenePath = "Assets/Scenes/BookOrganizer.unity";

    [MenuItem("BookOrganizer/Create Complete Scene")]
    public static void CreateCompleteScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BookOrganizer";

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.25f);

        GameObject root = new GameObject("=== Gallery & Library ===");

        // Define rooms
        RoomDef[] rooms = new RoomDef[]
        {
            new RoomDef { name = "MainHall", center = new Vector3(0, 0, 0),   width = 24, depth = 16, height = 7.5f },
            new RoomDef { name = "EastHall", center = new Vector3(0, 0, 20),  width = 22, depth = 14, height = 6.5f },
            new RoomDef { name = "WestHall", center = new Vector3(0, 0, -20), width = 22, depth = 14, height = 6.5f },
        };

        // Build rooms
        foreach (var room in rooms)
        {
            BuildRoom(root.transform, room);
        }

        // Build connecting corridors with walls
        // Corridors: width=4 (matching door width), depth=3 (gap between rooms)
        BuildCorridor(root.transform, new Vector3(0, 0, 10.5f), 4f, 3f);
        BuildCorridor(root.transform, new Vector3(0, 0, -10.5f), 4f, 3f);

        // Place bookshelves in rooms - library style layout
        PlaceLibraryBookshelves(root.transform, rooms[0]); // MainHall
        PlaceLibraryBookshelves(root.transform, rooms[1]); // EastHall
        PlaceLibraryBookshelves(root.transform, rooms[2]); // WestHall

        // Place artworks on walls
        var artworks = GetArtworkData();
        int artIndex = 0;

        // MainHall: 3 artworks
        PlaceArtworkOnWall(root.transform, rooms[0], Wall.Back,  artworks[artIndex++]);
        PlaceArtworkOnWall(root.transform, rooms[0], Wall.Left, artworks[artIndex++]);
        PlaceArtworkOnWall(root.transform, rooms[0], Wall.Right,artworks[artIndex++]);

        // EastHall: 3 artworks
        PlaceArtworkOnWall(root.transform, rooms[1], Wall.Back,  artworks[artIndex++]);
        PlaceArtworkOnWall(root.transform, rooms[1], Wall.Left, artworks[artIndex++]);
        PlaceArtworkOnWall(root.transform, rooms[1], Wall.Right,artworks[artIndex++]);

        // WestHall: 2 artworks
        PlaceArtworkOnWall(root.transform, rooms[2], Wall.Back,  artworks[artIndex++]);
        PlaceArtworkOnWall(root.transform, rooms[2], Wall.Left, artworks[artIndex++]);

        // Player
        GameObject player = CreatePlayer(root.transform);

        // Camera
        GameObject cameraObj = CreateCamera(player.transform);

        // Lights
        CreateGalleryLights(root.transform, rooms);

        // Books
        CreateScatteredBooks(root.transform);

        // UI
        CreateUI(root.transform);

        // Save
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BookOrganizer] Multi-room gallery scene created and saved to {ScenePath}");
        Debug.Log($"[BookOrganizer] Press Play to test. Controls: WASD=move, E=pickup/place/inspect, F=open PDF/wiki");
    }

    // ===================== ROOMS =====================

    struct RoomDef
    {
        public string name;
        public Vector3 center;
        public float width;
        public float depth;
        public float height;
    }

    enum Wall { Back, Left, Right, Front }

    static void BuildRoom(Transform parent, RoomDef room)
    {
        float hw = room.width * 0.5f;
        float hd = room.depth * 0.5f;
        float hh = room.height * 0.5f;
        Color wallColor = new Color(0.93f, 0.91f, 0.89f);

        // Floor (Plane default size is 10x10, so scale = targetSize / 10)
        GameObject floor = CreatePrimitive($"{room.name}_Floor", PrimitiveType.Plane, parent,
            new Vector3(room.center.x, 0, room.center.z),
            new Vector3(room.width / 10f, 1, room.depth / 10f),
            new Color(0.86f, 0.84f, 0.82f));
        floor.GetComponent<Collider>().material = null;

        // Walls
        float doorWidth = 4f;
        float segWidth = (room.width - doorWidth) / 2f;

        // Back wall (with door)
        BuildWallSegment(parent, $"{room.name}_WallBack_L",
            new Vector3(room.center.x - hw + segWidth / 2f, hh, room.center.z - hd),
            new Vector3(segWidth, room.height, 0.15f), wallColor);
        BuildWallSegment(parent, $"{room.name}_WallBack_R",
            new Vector3(room.center.x + hw - segWidth / 2f, hh, room.center.z - hd),
            new Vector3(segWidth, room.height, 0.15f), wallColor);

        // Front wall (with door)
        BuildWallSegment(parent, $"{room.name}_WallFront_L",
            new Vector3(room.center.x - hw + segWidth / 2f, hh, room.center.z + hd),
            new Vector3(segWidth, room.height, 0.15f), wallColor);
        BuildWallSegment(parent, $"{room.name}_WallFront_R",
            new Vector3(room.center.x + hw - segWidth / 2f, hh, room.center.z + hd),
            new Vector3(segWidth, room.height, 0.15f), wallColor);

        // Left wall
        BuildWallSegment(parent, $"{room.name}_WallLeft",
            new Vector3(room.center.x - hw, hh, room.center.z),
            new Vector3(0.15f, room.height, room.depth), wallColor);

        // Right wall
        BuildWallSegment(parent, $"{room.name}_WallRight",
            new Vector3(room.center.x + hw, hh, room.center.z),
            new Vector3(0.15f, room.height, room.depth), wallColor);
    }

    static void BuildCorridor(Transform parent, Vector3 center, float width, float depth)
    {
        // Floor (Plane default size is 10x10, so scale = targetSize / 10)
        GameObject floor = CreatePrimitive("Corridor_Floor", PrimitiveType.Plane, parent,
            center, new Vector3(width / 10f, 1, depth / 10f), new Color(0.84f, 0.82f, 0.80f));
        floor.GetComponent<Collider>().material = null;

        // Side walls
        float hw = width * 0.5f;
        Color wallColor = new Color(0.93f, 0.91f, 0.89f);

        BuildWallSegment(parent, "CorridorWall_L",
            new Vector3(center.x - hw, 3.5f, center.z),
            new Vector3(0.15f, 7f, depth), wallColor);
        BuildWallSegment(parent, "CorridorWall_R",
            new Vector3(center.x + hw, 3.5f, center.z),
            new Vector3(0.15f, 7f, depth), wallColor);
    }

    static void BuildWallSegment(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject wall = CreatePrimitive(name, PrimitiveType.Cube, parent, pos, scale, color);
        Rigidbody rb = wall.GetComponent<Rigidbody>();
        if (rb != null) DestroyImmediate(rb);
    }

    // ===================== BOOKSHELVES =====================

    static void PlaceBookshelfInRoom(Transform parent, RoomDef room, Vector3 localPos)
    {
        Vector3 worldPos = room.center + localPos;
        CreateBookshelf(parent, worldPos, $"Shelf_{room.name}_{localPos.x}");
    }

    static void PlaceLibraryBookshelves(Transform parent, RoomDef room)
    {
        float hw = room.width * 0.5f;
        float hd = room.depth * 0.5f;
        float shelfWidth = 3f;
        float doorHalf = 2.5f; // Leave a gap in the center aligned with the door

        // Back wall row - split into left and right sections with a central aisle
        float backZ = -hd + 0.5f;
        // Left section
        for (float x = -hw + shelfWidth * 0.5f; x < -doorHalf; x += shelfWidth)
        {
            PlaceBookshelfInRoom(parent, room, new Vector3(x, 0, backZ));
        }
        // Right section
        for (float x = doorHalf + shelfWidth * 0.5f; x <= hw - shelfWidth * 0.5f + 0.001f; x += shelfWidth)
        {
            PlaceBookshelfInRoom(parent, room, new Vector3(x, 0, backZ));
        }
    }

    static GameObject CreateBookshelf(Transform parent, Vector3 position, string name)
    {
        GameObject shelf = new GameObject(name);
        shelf.transform.SetParent(parent, false);
        // Lift slightly to prevent Z-fighting with floor
        shelf.transform.position = position + new Vector3(0, 0.01f, 0);

        Color woodColor = new Color(0.48f, 0.28f, 0.16f);

        CreatePrimitive("BackPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(0, 1.5f, -0.2f), new Vector3(3f, 3f, 0.05f), woodColor);
        CreatePrimitive("LeftPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(-1.5f, 1.5f, 0.1f), new Vector3(0.1f, 3f, 0.4f), woodColor);
        CreatePrimitive("RightPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(1.5f, 1.5f, 0.1f), new Vector3(0.1f, 3f, 0.4f), woodColor);

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

            for (int c = 0; c < cols; c++)
            {
                float x = -shelfWidth / 2 + (shelfWidth / cols) * c + (shelfWidth / cols) * 0.5f;
                GameObject slotObj = new GameObject($"Slot_{r}_{c}");
                slotObj.transform.SetParent(shelf.transform, false);
                slotObj.transform.localPosition = new Vector3(x, y + 0.25f, 0.1f);
                BoxCollider slotCol = slotObj.AddComponent<BoxCollider>();
                slotCol.isTrigger = true;
                slotCol.size = new Vector3(0.35f, 0.45f, 0.08f);
                BookshelfSlot slot = slotObj.AddComponent<BookshelfSlot>();
                slot.bookOffset = Vector3.zero;
                slot.bookRotation = new Vector3(0, 0, 0);
                slot.gizmoColor = new Color(0, 1, 0, 0.3f);

                // Green highlight indicator (hidden by default)
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                DestroyImmediate(indicator.GetComponent<Collider>());
                indicator.name = "SlotIndicator";
                indicator.transform.SetParent(slotObj.transform, false);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(0.32f, 0.42f, 0.06f);
                Renderer indR = indicator.GetComponent<Renderer>();
                Material indMat = new Material(Shader.Find("Standard"));
                indMat.color = new Color(0.2f, 1f, 0.2f, 0.6f);
                indMat.SetFloat("_Mode", 3f);
                indMat.EnableKeyword("_ALPHABLEND_ON");
                indMat.renderQueue = 3000;
                indR.material = indMat;
                indicator.SetActive(false);
            }
        }

        CreatePrimitive("TopPanel", PrimitiveType.Cube, shelf.transform,
            new Vector3(0, 3.1f, 0.1f), new Vector3(3.1f, 0.05f, 0.4f), woodColor);

        return shelf;
    }

    // ===================== ARTWORKS =====================

    struct ArtworkInfo
    {
        public string title;
        public string artist;
        public string wikiUrl;
        public Color color;
    }

    static List<ArtworkInfo> GetArtworkData()
    {
        return new List<ArtworkInfo>
        {
            new ArtworkInfo { title = "Mona Lisa", artist = "Leonardo da Vinci", wikiUrl = "https://en.wikipedia.org/wiki/Mona_Lisa", color = new Color(0.35f, 0.55f, 0.35f) },
            new ArtworkInfo { title = "The Starry Night", artist = "Vincent van Gogh", wikiUrl = "https://en.wikipedia.org/wiki/The_Starry_Night", color = new Color(0.15f, 0.25f, 0.65f) },
            new ArtworkInfo { title = "The Great Wave", artist = "Hokusai", wikiUrl = "https://en.wikipedia.org/wiki/The_Great_Wave_off_Kanagawa", color = new Color(0.25f, 0.50f, 0.70f) },
            new ArtworkInfo { title = "The Scream", artist = "Edvard Munch", wikiUrl = "https://en.wikipedia.org/wiki/The_Scream", color = new Color(0.90f, 0.55f, 0.15f) },
            new ArtworkInfo { title = "Girl with a Pearl Earring", artist = "Johannes Vermeer", wikiUrl = "https://en.wikipedia.org/wiki/Girl_with_a_Pearl_Earring", color = new Color(0.15f, 0.55f, 0.70f) },
            new ArtworkInfo { title = "The Birth of Venus", artist = "Sandro Botticelli", wikiUrl = "https://en.wikipedia.org/wiki/The_Birth_of_Venus", color = new Color(0.90f, 0.75f, 0.55f) },
            new ArtworkInfo { title = "The Persistence of Memory", artist = "Salvador Dalí", wikiUrl = "https://en.wikipedia.org/wiki/The_Persistence_of_Memory", color = new Color(0.70f, 0.60f, 0.30f) },
            new ArtworkInfo { title = "The Night Watch", artist = "Rembrandt", wikiUrl = "https://en.wikipedia.org/wiki/The_Night_Watch", color = new Color(0.35f, 0.25f, 0.15f) },
        };
    }

    static Texture2D GenerateArtTexture(string title, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];
        System.Random rng = new System.Random(title.GetHashCode());
        float seed = rng.Next(1000);

        Color baseC = Color.gray;
        Color accentC = Color.white;
        int style = 0;

        if (title.Contains("Mona"))       { baseC = new Color(0.25f, 0.35f, 0.20f); accentC = new Color(0.65f, 0.55f, 0.35f); style = 1; }
        else if (title.Contains("Starry")){ baseC = new Color(0.05f, 0.08f, 0.25f); accentC = new Color(0.90f, 0.85f, 0.20f); style = 2; }
        else if (title.Contains("Wave"))  { baseC = new Color(0.10f, 0.30f, 0.45f); accentC = new Color(0.85f, 0.95f, 1.00f); style = 3; }
        else if (title.Contains("Scream")){ baseC = new Color(0.85f, 0.45f, 0.10f); accentC = new Color(0.30f, 0.10f, 0.10f); style = 4; }
        else if (title.Contains("Pearl")) { baseC = new Color(0.10f, 0.25f, 0.45f); accentC = new Color(0.95f, 0.90f, 0.55f); style = 5; }
        else if (title.Contains("Venus")) { baseC = new Color(0.85f, 0.65f, 0.55f); accentC = new Color(0.55f, 0.75f, 0.90f); style = 6; }
        else if (title.Contains("Memory")){ baseC = new Color(0.60f, 0.50f, 0.30f); accentC = new Color(0.25f, 0.45f, 0.55f); style = 7; }
        else if (title.Contains("Night")) { baseC = new Color(0.15f, 0.12f, 0.08f); accentC = new Color(0.75f, 0.65f, 0.25f); style = 8; }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w;
                float ny = y / (float)h;
                float n = 0f;
                Color c = baseC;

                switch (style)
                {
                    case 1: // Mona Lisa: soft portrait gradient
                        n = Mathf.PerlinNoise(x * 0.02f + seed, y * 0.02f + seed);
                        float face = 1f - Mathf.Clamp01(Mathf.Abs(nx - 0.5f) * 3f + Mathf.Abs(ny - 0.55f) * 4f);
                        c = Color.Lerp(baseC, accentC, n * 0.7f + face * 0.3f);
                        break;
                    case 2: // Starry Night: swirling stars
                        float angle = Mathf.Atan2(ny - 0.5f, nx - 0.5f);
                        float dist = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                        float swirl = Mathf.Sin(angle * 5f + dist * 15f + seed);
                        n = Mathf.PerlinNoise(x * 0.04f + seed, y * 0.04f + seed);
                        c = Color.Lerp(baseC, accentC, n * 0.5f + Mathf.Max(0, swirl) * 0.5f);
                        // Stars
                        if (n > 0.78f) c = Color.Lerp(c, Color.white, (n - 0.78f) * 5f);
                        break;
                    case 3: // Great Wave: horizontal wave bands
                        float wave = Mathf.Sin(nx * 8f + ny * 4f + seed) * 0.5f + 0.5f;
                        n = Mathf.PerlinNoise(x * 0.03f + seed, y * 0.03f + seed);
                        c = Color.Lerp(baseC, accentC, wave * 0.6f + n * 0.4f);
                        break;
                    case 4: // Scream: wavy distortion
                        float distort = Mathf.Sin(ny * 20f + seed) * 0.03f;
                        n = Mathf.PerlinNoise((nx + distort) * 5f + seed, ny * 5f + seed);
                        c = Color.Lerp(baseC, accentC, n);
                        break;
                    case 5: // Pearl Earring: dark background + bright focal
                        float focal = 1f - Mathf.Clamp01(Mathf.Abs(nx - 0.5f) * 2.5f + Mathf.Abs(ny - 0.5f) * 3f);
                        n = Mathf.PerlinNoise(x * 0.025f + seed, y * 0.025f + seed);
                        c = Color.Lerp(baseC, accentC, focal * 0.8f + n * 0.2f);
                        break;
                    case 6: // Birth of Venus: soft shells & sky
                        float shell = Mathf.Sin(nx * 6f) * Mathf.Cos(ny * 4f) * 0.5f + 0.5f;
                        n = Mathf.PerlinNoise(x * 0.02f + seed, y * 0.02f + seed);
                        c = Color.Lerp(baseC, accentC, shell * 0.5f + n * 0.5f);
                        break;
                    case 7: // Persistence of Memory: melting blobs
                        float blob = Mathf.PerlinNoise(x * 0.015f + seed * 2f, y * 0.015f + seed * 2f);
                        float blob2 = Mathf.PerlinNoise(x * 0.03f + seed, y * 0.03f + seed);
                        c = Color.Lerp(baseC, accentC, blob * 0.6f + blob2 * 0.4f);
                        break;
                    case 8: // Night Watch: dramatic chiaroscuro
                        float lightSpot = 1f - Mathf.Clamp01(Mathf.Abs(nx - 0.4f) * 2f + Mathf.Abs(ny - 0.45f) * 2.5f);
                        n = Mathf.PerlinNoise(x * 0.015f + seed, y * 0.015f + seed);
                        c = Color.Lerp(baseC * 0.5f, Color.Lerp(baseC, accentC, n), lightSpot * 0.7f + 0.3f);
                        break;
                }

                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static void PlaceArtworkOnWall(Transform parent, RoomDef room, Wall wall, ArtworkInfo info)
    {
        float hw = room.width * 0.5f - 1.5f;
        float hd = room.depth * 0.5f - 1.5f;
        Vector3 pos = Vector3.zero;
        Vector3 normal = Vector3.zero;
        float offset = 0.1f;

        switch (wall)
        {
            case Wall.Back:
                pos = new Vector3(room.center.x + Random.Range(-hw * 0.5f, hw * 0.5f), 2.2f, room.center.z - hd + offset);
                normal = Vector3.forward;
                break;
            case Wall.Left:
                pos = new Vector3(room.center.x - hw + offset, 2.2f, room.center.z + Random.Range(-hd * 0.5f, hd * 0.5f));
                normal = Vector3.right;
                break;
            case Wall.Right:
                pos = new Vector3(room.center.x + hw - offset, 2.2f, room.center.z + Random.Range(-hd * 0.5f, hd * 0.5f));
                normal = Vector3.left;
                break;
            case Wall.Front:
                pos = new Vector3(room.center.x + Random.Range(-hw * 0.5f, hw * 0.5f), 2.2f, room.center.z + hd - offset);
                normal = Vector3.back;
                break;
        }

        // Frame
        GameObject frame = new GameObject($"Art_{info.title.Replace(' ', '_')}");
        frame.transform.SetParent(parent, false);
        frame.transform.position = pos;
        frame.transform.rotation = Quaternion.LookRotation(normal);

        // Canvas (the painting itself)
        GameObject canvas = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canvas.name = "Canvas";
        canvas.transform.SetParent(frame.transform, false);
        canvas.transform.localScale = new Vector3(1.4f, 1.9f, 0.04f);
        Renderer r = canvas.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            Texture2D artTex = GenerateArtTexture(info.title, 256, 256);
            r.material.mainTexture = artTex;
            r.material.color = Color.white;
        }
        Collider col = canvas.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Frame border (simple outer box)
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = "Frame";
        border.transform.SetParent(frame.transform, false);
        border.transform.localScale = new Vector3(1.6f, 2.1f, 0.03f);
        border.transform.localPosition = new Vector3(0, 0, -0.02f);
        Renderer br = border.GetComponent<Renderer>();
        if (br != null)
        {
            br.material = new Material(Shader.Find("Standard"));
            br.material.color = new Color(0.25f, 0.15f, 0.08f);
        }
        Collider bc = border.GetComponent<Collider>();
        if (bc != null) DestroyImmediate(bc);

        Artwork art = canvas.AddComponent<Artwork>();
        art.artworkTitle = info.title;
        art.artistName = info.artist;
        art.wikiUrl = info.wikiUrl;
        art.originalScale = new Vector3(1.4f, 1.9f, 0.04f);
        art.SetColor(info.color);
        art.SaveWallState();
    }

    // ===================== PLAYER & CAMERA =====================

    static GameObject CreatePlayer(Transform parent)
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.SetParent(parent, false);
        player.transform.position = new Vector3(0, 1, 6);

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
        cam.transform.position = new Vector3(0, 14, 10);
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
        cam.orthographic = false;
        cam.fieldOfView = 65;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 80f;
        cam.backgroundColor = new Color(0.10f, 0.10f, 0.13f);

        var follow = camObj.AddComponent<CameraFollow>();
        follow.target = player;
        follow.offset = new Vector3(0, 14, 10);

        return camObj;
    }

    // ===================== LIGHTS =====================

    static void CreateGalleryLights(Transform parent, RoomDef[] rooms)
    {
        // Soft directional
        GameObject dirLight = new GameObject("DirectionalLight");
        dirLight.transform.SetParent(parent, false);
        Light dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 0.5f;
        dl.color = new Color(1f, 0.98f, 0.95f);
        dl.transform.rotation = Quaternion.Euler(55, -15, 0);
        dl.shadows = LightShadows.Soft;

        // Per-room accent lights
        foreach (var room in rooms)
        {
            GameObject point = new GameObject($"RoomLight_{room.name}");
            point.transform.SetParent(parent, false);
            point.transform.position = new Vector3(room.center.x, room.height - 0.5f, room.center.z);
            Light pl = point.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.intensity = 0.6f;
            pl.range = Mathf.Max(room.width, room.depth) * 1.2f;
            pl.color = new Color(1f, 0.95f, 0.85f);
        }

        // Artwork spotlights (one per room, aimed at center)
        foreach (var room in rooms)
        {
            GameObject spot = new GameObject($"ArtSpot_{room.name}");
            spot.transform.SetParent(parent, false);
            spot.transform.position = new Vector3(room.center.x, room.height - 1f, room.center.z);
            Light sl = spot.AddComponent<Light>();
            sl.type = LightType.Spot;
            sl.intensity = 0.8f;
            sl.range = room.height * 1.5f;
            sl.spotAngle = 70f;
            sl.color = new Color(1f, 0.95f, 0.9f);
            sl.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    // ===================== BOOKS =====================

    static void CreateScatteredBooks(Transform parent)
    {
        int bookCount = 36;

        // Generate positions across all rooms
        Vector3[] positions = new Vector3[bookCount];
        for (int i = 0; i < bookCount; i++)
        {
            float x = Random.Range(-10f, 10f);
            float z = Random.Range(-32f, 32f);
            positions[i] = new Vector3(x, 0.2f, z);
        }

        // Scan PDFs
        string pdfDir = System.IO.Path.Combine(Application.dataPath, "StreamingAssets", "PDFs");
        string[] pdfFiles = new string[0];
        if (System.IO.Directory.Exists(pdfDir))
        {
            pdfFiles = System.IO.Directory.GetFiles(pdfDir, "*.pdf");
            for (int i = 0; i < pdfFiles.Length; i++)
                pdfFiles[i] = System.IO.Path.GetFileName(pdfFiles[i]);
        }
        Debug.Log($"[BookOrganizer] Found {pdfFiles.Length} PDF(s)");

        for (int i = 0; i < bookCount; i++)
        {
            GameObject bookObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bookObj.name = $"Book_{i}";
            bookObj.transform.SetParent(parent, false);
            bookObj.transform.position = positions[i];
            bookObj.transform.localScale = new Vector3(0.3f, 0.4f, 0.05f);
            bookObj.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            Renderer r = bookObj.GetComponent<Renderer>();
            if (r != null)
                r.material = new Material(Shader.Find("Standard"));

            Rigidbody rb = bookObj.GetComponent<Rigidbody>();
            if (rb == null) rb = bookObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = true;
            rb.mass = 0.5f;

            Collider col = bookObj.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;

            BookItem book = bookObj.AddComponent<BookItem>();
            book.originalScale = new Vector3(0.3f, 0.4f, 0.05f);

            if (i < pdfFiles.Length)
            {
                book.pdfFileName = pdfFiles[i];
                book.bookTitle = System.IO.Path.GetFileNameWithoutExtension(pdfFiles[i]);
                if (r != null)
                {
                    r.material.color = TitleGenerator.GenerateColor();
                    book.SetColor(r.material.color);
                }
            }
            else
            {
                book.pdfFileName = "";
                book.bookTitle = TitleGenerator.GenerateTitle();
                if (r != null)
                {
                    r.material.color = Color.white;
                    book.SetColor(Color.white);
                }
            }
        }
    }

    // ===================== UI =====================

    static void CreateUI(Transform parent)
    {
        GameObject uiObj = new GameObject("BookUI");
        uiObj.transform.SetParent(parent, false);
        uiObj.AddComponent<BookUI>();
    }

    // ===================== UTILS =====================

    static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = color;
        }
        return obj;
    }
}

