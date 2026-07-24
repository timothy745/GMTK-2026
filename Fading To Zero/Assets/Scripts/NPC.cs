using UnityEngine;
using UnityEngine.Events;

public class NPC : MonoBehaviour
{
    [Header("NPC Data")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField][TextArea(2, 4)] private string[] dialogueLines = new string[] { "Hello there!" };

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Events")]
    public UnityEvent OnInteractionStart;
    public UnityEvent OnInteractionEnd;

    // Public state properties
    public bool IsPlayerInRange { get; private set; }
    public bool IsInteracting { get; private set; }

    private CircleCollider2D triggerCollider;
    private int dialogueIndex = 0;

    void Awake()
    {
        SetupTriggerCollider();
    }

    void OnValidate()
    {
        // Sync collider values when changed in Inspector
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = interactionRadius;
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (!IsPlayerInRange) return;

        if (!IsInteracting && Input.GetKeyDown(interactKey))
        {
            StartInteraction();
        }
        else if (IsInteracting && Input.GetKeyDown(interactKey))
        {
            AdvanceDialogue();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
            if (IsInteracting) EndInteraction();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize interaction radius in Scene view
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    void OnGUI()
    {
        // Prompt above NPC when player is nearby
        if (IsPlayerInRange && !IsInteracting)
        {
            Vector3 worldPos = transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            GUIStyle promptStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 15, 120, 25),
                $"[{interactKey}] Talk", promptStyle);
        }

        // Dialogue box during interaction
        if (IsInteracting && dialogueIndex < dialogueLines.Length)
        {
            float boxW = Mathf.Min(500, Screen.width - 40);
            float boxH = 140;
            float x = (Screen.width - boxW) / 2;
            float y = Screen.height - boxH - 40;

            // Background
            GUI.Box(new Rect(x, y, boxW, boxH), "");

            // NPC name
            GUIStyle nameStyle = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold };
            nameStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(x + 12, y + 10, boxW - 24, 24), npcName, nameStyle);

            // Dialogue text
            GUIStyle textStyle = new GUIStyle { fontSize = 14, wordWrap = true };
            textStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 12, y + 40, boxW - 24, 65), dialogueLines[dialogueIndex], textStyle);

            // Next prompt
            string footer = dialogueIndex < dialogueLines.Length - 1 ? $"[{interactKey}] Next" : $"[{interactKey}] Done";
            GUIStyle footerStyle = new GUIStyle { alignment = TextAnchor.MiddleRight, fontSize = 12 };
            footerStyle.normal.textColor = Color.gray;
            GUI.Label(new Rect(x + boxW - 120, y + boxH - 28, 110, 20), footer, footerStyle);
        }
    }

    // --- Internal ---

    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;
    }

    private void StartInteraction()
    {
        IsInteracting = true;
        dialogueIndex = 0;
        OnInteractionStart?.Invoke();
    }

    private void AdvanceDialogue()
    {
        dialogueIndex++;
        if (dialogueIndex >= dialogueLines.Length)
        {
            EndInteraction();
        }
    }

    private void EndInteraction()
    {
        IsInteracting = false;
        dialogueIndex = 0;
        OnInteractionEnd?.Invoke();
    }
}
