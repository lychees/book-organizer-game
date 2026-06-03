using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public float gravity = -20f;
    private float verticalVelocity = 0f;
    private bool isGrounded = true;

    [Header("Interaction")]
    public float interactionRadius = 2.5f;
    public Transform holdPoint;
    public LayerMask bookLayer;
    public LayerMask shelfLayer;

    [Header("Camera")]
    public Transform cameraTransform;

    private CharacterController controller;

    // Held items
    private BookItem heldBook = null;
    private Artwork heldArtwork = null;

    // Hovered items
    private BookItem hoveredBook = null;
    private BookshelfSlot hoveredSlot = null;
    private Artwork hoveredArtwork = null;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(transform);
            hp.transform.localPosition = new Vector3(0, 0.8f, 0.8f);
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
        Vector3 moveDir = Vector3.zero;

        if (input.magnitude > 0.1f)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            if (moveDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        // Jump & gravity
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0)
            verticalVelocity = -0.5f;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            verticalVelocity = jumpForce;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = moveDir * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    void DetectHoveredObjects()
    {
        hoveredBook = null;
        hoveredSlot = null;
        hoveredArtwork = null;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius);
        float nearestBookDist = float.MaxValue;
        float nearestSlotDist = float.MaxValue;
        float nearestArtDist = float.MaxValue;

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

            Artwork art = hit.GetComponent<Artwork>();
            if (art != null && !art.isHeld)
            {
                float d = Vector3.Distance(transform.position, art.transform.position);
                if (d < nearestArtDist)
                {
                    nearestArtDist = d;
                    hoveredArtwork = art;
                }
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldBook != null)
            {
                TryPlaceOrDropBook();
            }
            else if (heldArtwork != null)
            {
                TryPlaceOrDropArtwork();
            }
            else if (hoveredBook != null)
            {
                PickupBook(hoveredBook);
            }
            else if (hoveredArtwork != null)
            {
                PickupArtwork(hoveredArtwork);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (heldBook != null)
            {
                TryOpenPdf();
            }
            else if (heldArtwork != null)
            {
                TryOpenWiki();
            }
        }
    }

    // ===================== BOOKS =====================

    void PickupBook(BookItem book)
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

        BookUI.Instance?.SetHeldItem(book.bookTitle, "book");
    }

    void TryPlaceOrDropBook()
    {
        if (hoveredSlot != null)
        {
            PlaceBookInSlot(hoveredSlot);
        }
        else
        {
            DropBook();
        }
    }

    void PlaceBookInSlot(BookshelfSlot slot)
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

        BookUI.Instance?.ClearHeldItem();
        BookUI.Instance?.UpdateProgress();
        heldBook = null;
    }

    void DropBook()
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

        BookUI.Instance?.ClearHeldItem();
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
            BookUI.Instance?.ShowMockMessage($"No PDF assigned for:\n{heldBook.bookTitle}", "Place a .pdf in Assets/StreamingAssets/PDFs/");
        }
        else
        {
            BookUI.Instance?.ShowMockMessage(heldBook.bookTitle, path);
        }
    }

    // ===================== ARTWORKS =====================

    void PickupArtwork(Artwork art)
    {
        heldArtwork = art;
        art.isHeld = true;
        art.transform.SetParent(holdPoint);
        art.transform.localPosition = Vector3.zero;
        art.transform.localRotation = Quaternion.identity;
        art.transform.localScale = art.originalScale * 0.6f; // Slightly smaller when holding

        Collider col = art.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        BookUI.Instance?.SetHeldItem($"{art.artworkTitle} by {art.artistName}", "artwork");
    }

    void TryPlaceOrDropArtwork()
    {
        if (heldArtwork == null) return;

        if (heldArtwork.IsNearOriginalPosition(transform.position))
        {
            // Hang back on wall
            heldArtwork.RestoreToWall();
            Collider col = heldArtwork.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
        else
        {
            // Drop on floor
            heldArtwork.isHeld = false;
            heldArtwork.transform.SetParent(null);
            heldArtwork.transform.position = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
            heldArtwork.transform.localScale = heldArtwork.originalScale;
            heldArtwork.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            Collider col = heldArtwork.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        BookUI.Instance?.ClearHeldItem();
        heldArtwork = null;
    }

    void TryOpenWiki()
    {
        if (heldArtwork == null) return;
        if (!string.IsNullOrEmpty(heldArtwork.wikiUrl))
        {
            Application.OpenURL(heldArtwork.wikiUrl);
        }
        else
        {
            BookUI.Instance?.ShowMockMessage($"No wiki URL for:\n{heldArtwork.artworkTitle}", "");
        }
    }

    // ===================== GETTERS =====================

    public BookItem GetHeldBook() => heldBook;
    public BookItem GetHoveredBook() => hoveredBook;
    public BookshelfSlot GetHoveredSlot() => hoveredSlot;
    public Artwork GetHeldArtwork() => heldArtwork;
    public Artwork GetHoveredArtwork() => hoveredArtwork;
}
