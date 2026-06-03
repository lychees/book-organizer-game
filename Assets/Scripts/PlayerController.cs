using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Interaction")]
    public float interactionRadius = 2f;
    public Transform holdPoint;
    public LayerMask bookLayer;
    public LayerMask shelfLayer;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;
    private BookItem heldBook = null;
    private BookItem hoveredBook = null;
    private BookshelfSlot hoveredSlot = null;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(transform);
            hp.transform.localPosition = new Vector3(0, 0.8f, 0.6f);
            holdPoint = hp.transform;
        }
    }

    void Update()
    {
        Move();
        DetectHoveredObjects();
        HandleInput();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0, v);
        if (input.magnitude > 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            controller.Move(moveDir * moveSpeed * Time.deltaTime);

            if (moveDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void DetectHoveredObjects()
    {
        hoveredBook = null;
        hoveredSlot = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius);
        float nearestBookDist = float.MaxValue;
        float nearestSlotDist = float.MaxValue;

        foreach (var hit in hits)
        {
            BookItem book = hit.GetComponent<BookItem>();
            if (book != null && !book.isHeld && !book.isPlaced)
            {
                float d = Vector3.Distance(transform.position, book.transform.position);
                if (d < nearestBookDist)
                {
                    nearestBookDist = d;
                    hoveredBook = book;
                }
            }

            BookshelfSlot slot = hit.GetComponent<BookshelfSlot>();
            if (slot != null && !slot.isOccupied)
            {
                float d = Vector3.Distance(transform.position, slot.transform.position);
                if (d < nearestSlotDist)
                {
                    nearestSlotDist = d;
                    hoveredSlot = slot;
                }
            }
        }
    }

    void HandleInput()
    {
        // E: Pick up / Place
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldBook == null)
            {
                TryPickup();
            }
            else
            {
                TryPlaceOrDrop();
            }
        }

        // F: Read / Open PDF
        if (Input.GetKeyDown(KeyCode.F) && heldBook != null)
        {
            TryOpenPdf();
        }
    }

    void TryPickup()
    {
        if (hoveredBook != null)
        {
            Pickup(hoveredBook);
        }
    }

    void Pickup(BookItem book)
    {
        heldBook = book;
        book.isHeld = true;
        book.isPlaced = false;
        book.transform.SetParent(holdPoint);
        book.transform.localPosition = Vector3.zero;
        book.transform.localRotation = Quaternion.identity;
        book.transform.localScale = book.originalScale;

        Rigidbody rb = book.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = book.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        BookUI.Instance?.SetHeldBook(book);
    }

    void TryPlaceOrDrop()
    {
        if (hoveredSlot != null)
        {
            PlaceInSlot(hoveredSlot);
        }
        else
        {
            Drop();
        }
    }

    void PlaceInSlot(BookshelfSlot slot)
    {
        if (heldBook == null) return;

        heldBook.isHeld = false;
        heldBook.isPlaced = true;
        heldBook.transform.SetParent(slot.transform);
        heldBook.transform.localPosition = slot.bookOffset;
        heldBook.transform.localEulerAngles = slot.bookRotation;
        heldBook.transform.localScale = heldBook.originalScale;

        slot.isOccupied = true;
        slot.placedBook = heldBook;

        Rigidbody rb = heldBook.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = heldBook.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        BookUI.Instance?.SetHeldBook(null);
        BookUI.Instance?.UpdateProgress();
        heldBook = null;
    }

    void Drop()
    {
        if (heldBook == null) return;

        heldBook.isHeld = false;
        heldBook.transform.SetParent(null);
        heldBook.transform.position = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
        heldBook.transform.localScale = heldBook.originalScale;

        Rigidbody rb = heldBook.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Collider col = heldBook.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        BookUI.Instance?.SetHeldBook(null);
        heldBook = null;
    }

    void TryOpenPdf()
    {
        if (heldBook == null) return;
        string path = heldBook.GetPdfPath();
        if (heldBook.PdfExists())
        {
            Application.OpenURL("file:///" + path.Replace('\\', '/'));
        }
        else if (string.IsNullOrEmpty(path))
        {
            BookUI.Instance?.ShowPdfMockMessage(heldBook.bookTitle, "No PDF assigned. Place a .pdf file in Assets/StreamingAssets/PDFs/ and rebuild the scene.");
        }
        else
        {
            BookUI.Instance?.ShowPdfMockMessage(heldBook.bookTitle, path);
        }
    }

    public BookItem GetHeldBook() => heldBook;
    public BookItem GetHoveredBook() => hoveredBook;
    public BookshelfSlot GetHoveredSlot() => hoveredSlot;
}
