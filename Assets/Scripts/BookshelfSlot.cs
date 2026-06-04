using UnityEngine;

public class BookshelfSlot : MonoBehaviour
{
    public bool isOccupied = false;
    public BookItem placedBook = null;
    public Vector3 bookOffset = Vector3.zero;
    public Vector3 bookRotation = new Vector3(0, 0, 0);

    [Header("Gizmo")]
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    public Vector3 gizmoSize = new Vector3(0.35f, 0.45f, 0.08f);

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position + bookOffset, gizmoSize);
    }

    public void SetHighlight(bool active)
    {
        Transform indicator = transform.Find("SlotIndicator");
        if (indicator != null)
            indicator.gameObject.SetActive(active);
    }
}
