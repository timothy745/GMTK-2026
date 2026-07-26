using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TriggerZoneDialog : MonoBehaviour
{
    [Header("Dialog")]
    public string speakerName = "System";
    public string[] dialogLines = new string[]
    {
        "Selamat datang di area ini!"
    };

    [Header("Pengaturan")]
    public bool triggerOnce = false;
    public bool autoStartDialog = true;
    public bool disableSideScrollerPlayer = true;
    public float typeSpeed = 0.03f;

    [Header("UI (Drag dari Hierarchy)")]
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Button nextButton;
    public Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioClip[] dialogAudioClips;

    private bool hasTriggered = false;
    private bool playerInside = false;
    private bool inDialog = false;
    private bool isTyping = false;
    private bool dialogCompleted = false;
    private int currentLine = 0;
    private GameObject player;
    private bool movementDisabled = false;
    private Coroutine typeCoroutine;
    private bool useCustomUI;
    private AudioSource audioSource;

    private static GameObject lastPlayer;

    public static void ForceReEnableMovement()
    {
        if (lastPlayer == null) return;
        SideScrollerPlayer pm = lastPlayer.GetComponent<SideScrollerPlayer>();
        if (pm != null) { pm.SetMovementEnabled(true); return; }
        PlayerMovementIsometric iso = lastPlayer.GetComponent<PlayerMovementIsometric>();
        if (iso != null) iso.SetMovementEnabled(true);
    }

    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        useCustomUI = (dialogPanel != null && nameText != null && dialogText != null);

        if (useCustomUI)
        {
            dialogPanel.SetActive(false);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(EndDialog);
        }
    }

    void Update()
    {
        if (!inDialog)
        {
            if (movementDisabled)
                EnablePlayerMovement();
        }

        if (playerInside && player != null && !inDialog)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartDialog();
            }
        }
        else if (inDialog && Input.GetKeyDown(KeyCode.E))
        {
            OnNextClicked();
        }
    }

    void StartDialog()
    {
        inDialog = true;
        currentLine = 0;
        dialogCompleted = false;

        if (useCustomUI)
        {
            dialogPanel.SetActive(true);
            nameText.text = speakerName;
        }

        ShowLine();

        if (triggerOnce)
            hasTriggered = true;
    }

    void ShowLine()
    {
        if (currentLine < dialogLines.Length)
        {
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            typeCoroutine = StartCoroutine(TypeText(dialogLines[currentLine]));

            if (dialogAudioClips != null && currentLine < dialogAudioClips.Length && dialogAudioClips[currentLine] != null)
            {
                audioSource.PlayOneShot(dialogAudioClips[currentLine]);
            }

            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
        }
        else
        {
            dialogCompleted = true;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            if (closeButton != null)
                closeButton.gameObject.SetActive(true);
            else
                EndDialog();
        }
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        if (dialogText != null) dialogText.text = "";

        foreach (char c in fullText)
        {
            if (dialogText != null) dialogText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        audioSource.Stop();
    }

    void OnNextClicked()
    {
        if (!inDialog) return;

        if (isTyping)
        {
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            if (dialogText != null) dialogText.text = dialogLines[currentLine];
            isTyping = false;
            return;
        }

        if (dialogCompleted)
        {
            EndDialog();
            return;
        }

        currentLine++;
        ShowLine();
    }

    void EndDialog()
    {
        bool wasInDialog = inDialog;
        inDialog = false;
        dialogCompleted = false;

        if (typeCoroutine != null) StopCoroutine(typeCoroutine);

        if (useCustomUI && dialogPanel != null)
            dialogPanel.SetActive(false);

        EnablePlayerMovement();

        if (wasInDialog && triggerOnce)
            Destroy(gameObject);
    }

    void DisablePlayerMovement()
    {
        if (player == null) { Debug.Log("[TZD] DisablePlayerMovement: player is null!"); return; }
        lastPlayer = player;
        SideScrollerPlayer pm = player.GetComponent<SideScrollerPlayer>();
        if (pm != null) { pm.SetMovementEnabled(false); movementDisabled = true; Debug.Log("[TZD] Disabled SideScrollerPlayer movement"); return; }
        PlayerMovementIsometric iso = player.GetComponent<PlayerMovementIsometric>();
        if (iso != null) { iso.SetMovementEnabled(false); movementDisabled = true; Debug.Log("[TZD] Disabled IsometricPlayer movement"); return; }
        Debug.LogWarning("[TZD] No movement script found on player! Player name: " + player.name);
    }

    void EnablePlayerMovement()
    {
        movementDisabled = false;
        if (player != null) lastPlayer = player;
        GameObject target = player != null ? player : lastPlayer;
        if (target == null) { Debug.LogWarning("[TZD] EnablePlayerMovement: no target!"); return; }

        SideScrollerPlayer pm = target.GetComponent<SideScrollerPlayer>();
        if (pm != null) { pm.SetMovementEnabled(true); Debug.Log("[TZD] Enabled SideScrollerPlayer movement"); return; }
        PlayerMovementIsometric iso = target.GetComponent<PlayerMovementIsometric>();
        if (iso != null) { iso.SetMovementEnabled(true); Debug.Log("[TZD] Enabled IsometricPlayer movement"); return; }
        Debug.LogWarning("[TZD] No movement script found on target! Target name: " + target.name);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            lastPlayer = other.gameObject;
            playerInside = true;
            player = other.gameObject;

            if (disableSideScrollerPlayer)
                DisablePlayerMovement();

            if (autoStartDialog && !hasTriggered && !inDialog)
            {
                StartDialog();
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (disableSideScrollerPlayer)
                EnablePlayerMovement();

            playerInside = false;
            player = null;

            if (inDialog) EndDialog();
        }
    }

    void OnDisable()
    {
        if (movementDisabled)
            EnablePlayerMovement();
    }

    void OnDestroy()
    {
        if (movementDisabled)
            ForceReEnableMovement();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(
                (Vector2)transform.position + box.offset,
                box.size
            );
        }
        else
        {
            Gizmos.DrawSphere(transform.position, 1f);
        }
    }
}
