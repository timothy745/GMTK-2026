using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SimpleNPCInteract : MonoBehaviour
{
    public string npcName = "NPC";
    public string[] dialogLines = new string[] { "Halo!" };
    public float interactRange = 3f;
    public float typeSpeed = 0.03f;

    [Header("UI - Drag dari Hierarchy")]
    public GameObject promptE;
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Button nextButton;
    public Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioClip[] dialogAudioClips;

    private GameObject player;
    private bool inDialog;
    private int currentLine;
    private bool isTyping;
    private Coroutine typeCoroutine;
    private AudioSource audioSource;

    void Start()
    {
        if (promptE != null) promptE.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(EndDialog);
    }

    void OnNextClicked()
    {
        if (!inDialog) return;

        // Kalau lagi ketik, langsung tampil semua
        if (isTyping)
        {
            StopCoroutine(typeCoroutine);
            dialogText.text = dialogLines[currentLine];
            isTyping = false;
            return;
        }

        // Kalau sudah selesai ketik, lanjut baris berikutnya
        NextLine();
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        bool near = dist <= interactRange;

        if (promptE != null)
            promptE.SetActive(near && !inDialog);

        if (near && Input.GetKeyDown(KeyCode.E) && !inDialog)
        {
            StartDialog();
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

        if (promptE != null) promptE.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (nameText != null) nameText.text = npcName;

        TypeLine();

        DisableMovement();
    }

    void DisableMovement()
    {
        if (player == null) return;
        SideScrollerPlayer pm = player.GetComponent<SideScrollerPlayer>();
        if (pm != null) { pm.SetMovementEnabled(false); return; }
        PlayerMovementIsometric iso = player.GetComponent<PlayerMovementIsometric>();
        if (iso != null) iso.SetMovementEnabled(false);
    }

    void EnableMovement()
    {
        if (player == null) return;
        SideScrollerPlayer pm = player.GetComponent<SideScrollerPlayer>();
        if (pm != null) { pm.SetMovementEnabled(true); return; }
        PlayerMovementIsometric iso = player.GetComponent<PlayerMovementIsometric>();
        if (iso != null) iso.SetMovementEnabled(true);
    }

    void TypeLine()
    {
        if (currentLine < dialogLines.Length)
        {
            typeCoroutine = StartCoroutine(TypeText(dialogLines[currentLine]));

            if (dialogAudioClips != null && currentLine < dialogAudioClips.Length && dialogAudioClips[currentLine] != null)
            {
                audioSource.PlayOneShot(dialogAudioClips[currentLine]);
            }
        }
        else
        {
            EndDialog();
        }
    }

    IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char c in fullText)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        audioSource.Stop();
    }

    void NextLine()
    {
        currentLine++;
        TypeLine();
    }

    void EndDialog()
    {
        inDialog = false;

        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        if (dialogPanel != null) dialogPanel.SetActive(false);

        EnableMovement();
    }
}
