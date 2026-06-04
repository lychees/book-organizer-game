using UnityEngine;

public class BookItem : MonoBehaviour
{
    [Header("Data")]
    public string bookTitle = "Untitled Book";
    public string pdfFileName = "mock.pdf";
    public bool isHeld = false;
    public bool isPlaced = false;

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    public Vector3 originalScale = new Vector3(0.3f, 0.4f, 0.05f);

    [Header("Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.05f;
    private Vector3 startPos;
    private bool wasHeld = false;

    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        startPos = transform.position;
    }

    void Update()
    {
        // Detect state transitions FIRST (before animation overwrites position)
        if (isHeld && !wasHeld)
        {
            // Just picked up
            startPos = transform.position;
        }
        else if (wasHeld && !isHeld && !isPlaced)
        {
            // Just dropped (not placed in shelf) - update startPos to drop location
            startPos = transform.position;
        }
        
        wasHeld = isHeld;

        // Only apply floating animation when physics is not active
        Rigidbody rb = GetComponent<Rigidbody>();
        bool physicsActive = (rb != null && !rb.isKinematic);

        if (!isHeld && !isPlaced && !physicsActive)
        {
            // Gentle floating animation when on ground
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(startPos.x, startPos.y + yOffset, startPos.z);
            transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
        }
    }

    public void SetColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }

    public string GetPdfPath()
    {
        if (string.IsNullOrEmpty(pdfFileName))
            return "";
        return System.IO.Path.Combine(Application.streamingAssetsPath, "PDFs", pdfFileName);
    }

    public bool PdfExists()
    {
        string path = GetPdfPath();
        return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
    }
}
