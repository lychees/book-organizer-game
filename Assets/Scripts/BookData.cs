using UnityEngine;

[CreateAssetMenu(fileName = "BookData", menuName = "BookOrganizer/BookData")]
public class BookData : ScriptableObject
{
    public string bookTitle;
    public string pdfFileName;
    public Color bookColor;
}
