using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SortInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Prompt UI")]
    [SerializeField] private Sprite interactSprite;
    [SerializeField] private Vector2 promptSize = new Vector2(200f, 80f);

    public bool IsPlayerInRange { get; private set; }
    public bool IsLocked { get; set; }

    private CircleCollider2D triggerCollider;
    private GameObject promptRoot;
    private Image promptBg;
    private TextMeshProUGUI promptText;

    void Awake()
    {
        SetupTriggerCollider();
        CreatePromptUI();
    }

    void OnValidate()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = interactionRadius;
            col.isTrigger = true;
        }

        if (promptBg != null && interactSprite != null)
            promptBg.sprite = interactSprite;

        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    void Update()
    {
        if (!IsPlayerInRange) return;
        if (IsLocked) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            SortManager manager = GetComponent<SortManager>();
            if (manager != null && !manager.IsSortActive())
            {
                manager.OpenSort();
            }
        }

        if (SortManager.IsAnySortActive || SortManager.IsTipsShowing)
        {
            promptRoot.SetActive(false);
            return;
        }

        UpdatePromptPosition();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            IsPlayerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
            if (promptRoot != null)
                promptRoot.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    void LateUpdate()
    {
        if (IsPlayerInRange && !IsLocked && !SortManager.IsAnySortActive && !SortManager.IsTipsShowing)
        {
            promptRoot.SetActive(true);
            UpdatePromptPosition();
        }
        else
        {
            promptRoot.SetActive(false);
        }
    }

    private void CreatePromptUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InteractCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        promptRoot = new GameObject("InteractPrompt");
        promptRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = promptRoot.AddComponent<RectTransform>();
        rootRect.sizeDelta = promptSize;

        if (interactSprite != null)
        {
            promptBg = promptRoot.AddComponent<Image>();
            promptBg.sprite = interactSprite;
            promptBg.type = Image.Type.Simple;
            promptBg.preserveAspect = true;
            promptBg.color = Color.white;
        }
        else
        {
            promptBg = promptRoot.AddComponent<Image>();
            promptBg.color = new Color(0f, 0f, 0f, 0.8f);
        }

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptRoot.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.text = "[E] Open Sort";
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 20;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = Color.white;
        promptText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        promptRoot.SetActive(false);
    }

    private void UpdatePromptPosition()
    {
        if (promptRoot == null) return;

        Vector3 worldPos = transform.position + Vector3.up * 1.8f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform rootRect = promptRoot.GetComponent<RectTransform>();
        rootRect.position = new Vector3(screenPos.x, Screen.height - screenPos.y, 0f);
    }

    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;
    }
}
