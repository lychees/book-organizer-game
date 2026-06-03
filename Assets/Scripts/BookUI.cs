using UnityEngine;

public class BookUI : MonoBehaviour
{
    public static BookUI Instance { get; private set; }

    [Header("UI Style")]
    public GUIStyle promptStyle;
    public GUIStyle heldBookStyle;
    public GUIStyle progressStyle;
    public GUIStyle pdfMockStyle;
    public GUIStyle panelStyle;

    private PlayerController player;
    private string currentPrompt = "";
    private string heldBookTitle = "";
    private string progressText = "Books placed: 0 / 0";
    private string pdfMockMessage = "";
    private float pdfMockTimer = 0f;
    private bool showHeldBook = false;

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
        promptStyle.fontStyle = FontStyle.Normal;

        heldBookStyle = new GUIStyle();
        heldBookStyle.fontSize = 20;
        heldBookStyle.normal.textColor = Color.white;
        heldBookStyle.alignment = TextAnchor.MiddleCenter;
        heldBookStyle.fontStyle = FontStyle.Bold;

        progressStyle = new GUIStyle();
        progressStyle.fontSize = 26;
        progressStyle.normal.textColor = new Color(1f, 0.9f, 0.5f);
        progressStyle.alignment = TextAnchor.MiddleCenter;
        progressStyle.fontStyle = FontStyle.Bold;

        pdfMockStyle = new GUIStyle();
        pdfMockStyle.fontSize = 18;
        pdfMockStyle.normal.textColor = Color.white;
        pdfMockStyle.alignment = TextAnchor.MiddleCenter;
        pdfMockStyle.wordWrap = true;

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

        if (pdfMockTimer > 0)
        {
            pdfMockTimer -= Time.deltaTime;
            if (pdfMockTimer <= 0)
            {
                pdfMockMessage = "";
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
            currentPrompt += "     [F] Read / Open PDF";
        }
        else if (player.GetHoveredBook() != null)
        {
            currentPrompt = "[E] Pick up book";
        }
        else
        {
            currentPrompt = "WASD to move     Find books and place them on the shelf";
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

        // Held book info (right side)
        if (showHeldBook && !string.IsNullOrEmpty(heldBookTitle))
        {
            Rect panelRect = new Rect(Screen.width - 370, Screen.height / 2 - 40, 350, 80);
            GUI.Box(panelRect, "", panelStyle);
            GUI.Label(panelRect, $"Holding:\n{heldBookTitle}", heldBookStyle);
        }

        // PDF Mock message (center)
        if (!string.IsNullOrEmpty(pdfMockMessage))
        {
            Rect panelRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 150, 600, 300);
            GUI.Box(panelRect, "", panelStyle);
            GUI.Label(new Rect(panelRect.x + 20, panelRect.y + 20, panelRect.width - 40, panelRect.height - 40), pdfMockMessage, pdfMockStyle);
        }
    }

    public void SetHeldBook(BookItem book)
    {
        if (book != null)
        {
            heldBookTitle = book.bookTitle;
            showHeldBook = true;
        }
        else
        {
            heldBookTitle = "";
            showHeldBook = false;
        }
    }

    public void UpdateProgressManual()
    {
        UpdateProgress();
    }

    public void ShowPdfMockMessage(string bookTitle, string expectedPath)
    {
        pdfMockMessage = $"PDF not found for:\n<b>{bookTitle}</b>\n\nExpected path:\n{expectedPath}\n\nPlease place a real PDF file there to replace this mock.";
        pdfMockTimer = 5f;
    }
}
