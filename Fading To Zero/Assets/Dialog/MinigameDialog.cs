using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MinigameDialog : MonoBehaviour
{
    [Header("Win Dialog")]
    public string winSpeakerName = "System";
    public string[] winDialogLines = new string[] { "Well done!" };
    [SerializeField] private AudioClip[] winDialogAudioClips;

    [Header("Fail Dialog")]
    public string failSpeakerName = "System";
    public string[] failDialogLines = new string[] { "Better luck next time..." };
    [SerializeField] private AudioClip[] failDialogAudioClips;

    [Header("Settings")]
    public float typeSpeed = 0.03f;

    [Header("UI (Drag dari Hierarchy)")]
    public GameObject dialogPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Button nextButton;
    public Button closeButton;

    private bool inDialog;
    private bool isTyping;
    private int currentLine;
    private string[] activeLines;
    private AudioClip[] activeAudioClips;
    private Coroutine typeCoroutine;
    private System.Action onDialogDone;
    private GameObject player;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (dialogPanel != null) dialogPanel.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(EndDialog);
    }

    public void ShowWinDialog()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        DisableMovement();

        activeAudioClips = winDialogAudioClips;
        ShowDialog(winSpeakerName, winDialogLines);
    }

    public void ShowFailDialog()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        DisableMovement();

        activeAudioClips = failDialogAudioClips;
        ShowDialog(failSpeakerName, failDialogLines);
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

    public void ShowDialog(string speaker, string[] lines, System.Action done = null)
    {
        if (lines == null || lines.Length == 0)
        {
            done?.Invoke();
            return;
        }

        onDialogDone = done;
        inDialog = true;
        currentLine = 0;
        activeLines = lines;

        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (nameText != null) nameText.text = speaker;
        if (dialogText != null) dialogText.text = "";
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        TypeLine();
    }

    void Update()
    {
        if (inDialog && Input.GetKeyDown(KeyCode.E))
        {
            OnNextClicked();
        }
    }

    void TypeLine()
    {
        if (currentLine < activeLines.Length)
        {
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);
            typeCoroutine = StartCoroutine(TypeText(activeLines[currentLine]));

            if (activeAudioClips != null && currentLine < activeAudioClips.Length && activeAudioClips[currentLine] != null)
            {
                audioSource.PlayOneShot(activeAudioClips[currentLine]);
            }
        }
        else
        {
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(true);
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
            if (dialogText != null) dialogText.text = activeLines[currentLine];
            isTyping = false;
            return;
        }

        currentLine++;
        TypeLine();
    }

    void EndDialog()
    {
        inDialog = false;

        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        audioSource.Stop();

        if (dialogPanel != null) dialogPanel.SetActive(false);

        EnableMovement();

        onDialogDone?.Invoke();
    }
}
