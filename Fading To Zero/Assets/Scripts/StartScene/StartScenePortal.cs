using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartScenePortal : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private float interactionOffsetX = 0f;
    [SerializeField] private float interactionOffsetY = 0f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Prompt")]
    [SerializeField] private Sprite interactSprite;
    [SerializeField] private Vector2 promptSize = new Vector2(200f, 80f);
    [SerializeField] private float promptOffsetX = 0f;
    [SerializeField] private float promptOffsetY = 1.8f;

    [Header("Flash")]
    [SerializeField] private float flashHoldDuration = 1.0f;
    [SerializeField] private float flashOutDuration = 1.5f;

    [Header("Teleport")]
    [SerializeField] private float teleportOffsetX = 22f;

    [Header("Portal Pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 1f;

    public bool IsPlayerInRange { get; private set; }
    public string GetInteractText() => "[E] Enter Portal";

    private CircleCollider2D triggerCollider;
    private SpriteRenderer portalSprite;
    private GameObject promptRoot;
    private Image promptBg;
    private TextMeshProUGUI promptTMP;
    private Canvas flashCanvas;
    private Image flashImage;
    private Canvas countdownCanvas;
    private CountdownMotionBlur countdownEffect;
    private bool isActivated;

    void Awake()
    {
        portalSprite = GetComponent<SpriteRenderer>();
        SetupTriggerCollider();
        CreatePromptUI();
    }

    void Start()
    {
        CreateFlashCanvas();
        CreateCountdownCanvas();
        flashCanvas.gameObject.SetActive(false);
        countdownCanvas.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isActivated || !IsPlayerInRange) return;

        if (Input.GetKeyDown(interactKey))
            Activate();

        UpdatePromptPosition();
    }

    void LateUpdate()
    {
        if (isActivated) return;

        bool show = IsPlayerInRange;
        promptRoot.SetActive(show);
        if (show) UpdatePromptPosition();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) IsPlayerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
            promptRoot.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.position + new Vector3(interactionOffsetX, interactionOffsetY, 0f);
        Gizmos.DrawWireSphere(center, interactionRadius);
    }

    public void Interact()
    {
        if (!isActivated) Activate();
    }

    private void Activate()
    {
        if (isActivated) return;
        isActivated = true;

        portalSprite.enabled = false;
        promptRoot.SetActive(false);

        StartCoroutine(FlashSequence());
    }

    private IEnumerator FlashSequence()
    {
        flashCanvas.gameObject.SetActive(true);
        flashImage.color = Color.white;

        TeleportPlayer();

        yield return new WaitForSeconds(flashHoldDuration);

        float elapsed = 0f;
        while (elapsed < flashOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashOutDuration);
            flashImage.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        flashImage.color = Color.clear;
        flashCanvas.gameObject.SetActive(false);

        countdownCanvas.gameObject.SetActive(true);
        countdownEffect.StartCountdown(0, 3);
    }

    private void TeleportPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 pos = player.transform.position;
        pos.x += teleportOffsetX;
        player.transform.position = pos;
    }

    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;
        triggerCollider.offset = new Vector2(interactionOffsetX, interactionOffsetY);
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

        promptBg = promptRoot.AddComponent<Image>();
        if (interactSprite != null)
        {
            promptBg.sprite = interactSprite;
            promptBg.type = Image.Type.Simple;
            promptBg.preserveAspect = true;
            promptBg.color = Color.white;
        }
        else
        {
            promptBg.color = new Color(0f, 0f, 0f, 0.8f);
        }

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(promptRoot.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptTMP = textObj.AddComponent<TextMeshProUGUI>();
        promptTMP.text = "[E] Enter Portal";
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.fontSize = 20;
        promptTMP.fontStyle = FontStyles.Bold;
        promptTMP.color = Color.white;
        promptTMP.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        promptRoot.SetActive(false);
    }

    private void UpdatePromptPosition()
    {
        Vector3 worldPos = transform.position + new Vector3(promptOffsetX, promptOffsetY, 0f);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        promptRoot.GetComponent<RectTransform>().position = new Vector3(screenPos.x, Screen.height - screenPos.y, 0f);
    }

    private void CreateFlashCanvas()
    {
        flashCanvas = CreateOverlayCanvas("PortalFlashCanvas", 200);
        flashImage = CreateOverlayImage(flashCanvas.transform, Color.clear);
    }

    private void CreateCountdownCanvas()
    {
        countdownCanvas = CreateOverlayCanvas("PortalCountdownCanvas", 201);

        GameObject tmpObj = new GameObject("CountdownText");
        tmpObj.transform.SetParent(countdownCanvas.transform, false);

        RectTransform rt = tmpObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 200f);

        TextMeshProUGUI tmp = tmpObj.AddComponent<TextMeshProUGUI>();
        tmp.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        tmp.fontSize = 150f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        countdownEffect = tmpObj.AddComponent<CountdownMotionBlur>();
    }

    private Canvas CreateOverlayCanvas(string name, int sortOrder)
    {
        GameObject obj = new GameObject(name);
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        obj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Image CreateOverlayImage(Transform parent, Color color)
    {
        GameObject obj = new GameObject("Image");
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return img;
    }
}
