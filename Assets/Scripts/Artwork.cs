using UnityEngine;

public class Artwork : MonoBehaviour
{
    [Header("Data")]
    public string artworkTitle = "Untitled";
    public string artistName = "Unknown";
    public string wikiUrl = "";
    public bool isHeld = false;

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    public Vector3 originalScale = new Vector3(1.5f, 2f, 0.05f);

    [Header("Wall Mount")]
    public Vector3 wallPosition;
    public Quaternion wallRotation;
    public Transform wallParent;

    void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SaveWallState()
    {
        wallPosition = transform.position;
        wallRotation = transform.rotation;
        wallParent = transform.parent;
    }

    public void RestoreToWall()
    {
        transform.SetParent(wallParent);
        transform.position = wallPosition;
        transform.rotation = wallRotation;
        transform.localScale = originalScale;
        isHeld = false;
    }

    public void SetColor(Color color)
    {
        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }

    public bool IsNearOriginalPosition(Vector3 pos, float threshold = 2.5f)
    {
        return Vector3.Distance(pos, wallPosition) < threshold;
    }
}
