using UnityEngine;

public class BookUI : MonoBehaviour
{
    public static BookUI Instance { get; private set; }

    [Header("UI Style")]
    public GUIStyle promptStyle;
    public GUIStyle heldItemStyle;
    public GUIStyle progressStyle;
    public GUIStyle mockMessageStyle;
    public GUIStyle panelStyle;

    private PlayerController player;
    private string currentPrompt = "";
    private string heldItemTitle = "";
    private string heldItemType = "";
    private string progressText = "Books placed: 0 / 0";
    private string mockMessage = "";
    private float mockTimer = 0f;
    private bool showHeldItem = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        InitializeStyles();
    }

    void InitializeStyles()
    {
        promptStyle = new GUIStyle();
        promptStyle.fontSize = 22;
        promptStyle.normal.textColor = Color.white;
        promptStyle.alignment = TextAnchor.MiddleCenter;

        heldItemStyle = new GUIStyle();
        heldItemStyle.fontSize = 18;
        heldItemStyle.normal.textColor = Color.white;
        heldItemStyle.alignment = TextAnchor.MiddleCenter;
        heldItemStyle.wordWrap = true;

        progressStyle = new GUIStyle();
        progressStyle.fontSize = 26;
        progressStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
        progressStyle.alignment = TextAnchor.MiddleCenter;
        progressStyle.fontStyle = FontStyle.Bold;

        mockMessageStyle = new GUIStyle();
        mockMessageStyle.fontSize = 18;
        mockMessageStyle.normal.textColor = Color.white;
        mockMessageStyle.alignment = TextAnchor.MiddleCenter;
        mockMessageStyle.wordWrap = true;

        panelStyle = new GUIStyle();
        panelStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.75f));
    }

    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void Update()
    {
        UpdatePrompt();
        UpdateProgress();

        if (mockTimer > 0)
        {
            mockTimer -= Time.deltaTime;
            if (mockTimer <= 0)
            {
                mockMessage = "";
            }
        }
    }

    void UpdatePrompt()
    {
        if (player == null) return;

        if (player.GetHeldBook() != null)
        {
            currentPrompt = "[E] Place book";
            if (player.GetHoveredSlot() != null)
                currentPrompt += " (on shelf)";
            else
                currentPrompt += " (drop)";
            currentPrompt += "     [F] Open PDF";
        }
        else if (player.GetHeldArtwork() != null)
        {
            currentPrompt = "[E] Hang back / Drop artwork";
            currentPrompt += "     [F] Open Wiki";
        }
        else if (player.GetHoveredBook() != null)
        {
            currentPrompt = "[E] Pick up book";
        }
        else if (player.GetHoveredArtwork() != null)
        {
            currentPrompt = "[E] Inspect artwork";
        }
        else
        {
            currentPrompt = "WASD = move | Space = jump | Find books and artworks to interact";
        }
    }

    public void UpdateProgress()
    {
        var books = FindObjectsOfType<BookItem>();
        int total = books.Length;
        int placed = 0;
        foreach (var b in books)
            if (b.isPlaced) placed++;
        progressText = $"Books placed: {placed} / {total}";
    }

    void OnGUI()
    {
        // Progress (top center)
        GUI.Label(new Rect(Screen.width / 2 - 200, 20, 400, 40), progressText, progressStyle);

        // Prompt (bottom center)
        GUI.Label(new Rect(Screen.width / 2 - 400, Screen.height - 60, 800, 40), currentPrompt, promptStyle);

        // Held item info (right side)
        if (showHeldItem && !string.IsNullOrEmpty(heldItemTitle))
        {
            Rect panelRect = new Rect(Screen.width - 380, Screen.height / 2 - 50, 360, 100);
            GUI.Box(panelRect, "", panelStyle);
            string label = heldItemType == "artwork" ? "Inspecting:" : "Holding:";
            GUI.Label(panelRect, $"{label}\n{heldItemTitle}", heldItemStyle);
        }

        // Mock message (center)
        if (!string.IsNullOrEmpty(mockMessage))
        {
            Rect panelRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 150, 600, 300);
            GUI.Box(panelRect, "", panelStyle);
            GUI.Label(new Rect(panelRect.x + 20, panelRect.y + 20, panelRect.width - 40, panelRect.height - 40), mockMessage, mockMessageStyle);
        }
    }

    public void SetHeldItem(string title, string type)
    {
        heldItemTitle = title;
        heldItemType = type;
        showHeldItem = true;
    }

    public void ClearHeldItem()
    {
        heldItemTitle = "";
        heldItemType = "";
        showHeldItem = false;
    }

    public void UpdateProgressManual()
    {
        UpdateProgress();
    }

    public void ShowMockMessage(string title, string detail)
    {
        if (string.IsNullOrEmpty(detail))
            mockMessage = title;
        else
            mockMessage = $"{title}\n\n{detail}";
        mockTimer = 5f;
    }
}
