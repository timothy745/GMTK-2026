using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private float matchThreshold = 0.85f;

    [Header("References")]
    [SerializeField] private Canvas colorCanvas;
    [SerializeField] private RectTransform colorContainer;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resetHintText;
    [SerializeField] private GameObject paperObject;
    [SerializeField] private GameObject playerDot;
    [SerializeField] private MazeParticles mazeParticles;

    [Header("Sprites")]
    [SerializeField] private Sprite referenceSprite;
    [SerializeField] private Sprite tableSprite;

    [Header("Colors")]
    [SerializeField] private Color[] paletteColors = new Color[]
    {
        Color.white,
        Color.black,
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 0.5f),
        new Color(0.5f, 0.8f, 1f),
        new Color(0.6f, 0.3f, 0.1f)
    };

    [Header("Paper")]
    [SerializeField] private Sprite paperSprite;
    [SerializeField] private float paperYPosition = -1.08f;

    [Header("Tips")]
    [SerializeField] private Sprite tipsSprite;

    [Header("Maw")]
    [SerializeField] private Sprite mawSprite;
    [SerializeField] private Sprite maw2Sprite;
    [SerializeField] private Sprite maw3Sprite;

    [Header("Paper Shards")]
    [SerializeField] private Sprite shardSpriteA;
    [SerializeField] private Sprite shardSpriteB;

    [Header("Reward")]
    [SerializeField] private Sprite rewardSprite;
    [SerializeField] private string rewardName = "Photo Piece";

    [Header("Audio")]
    [SerializeField] private AudioClip clockTickClip;
    [SerializeField] private AudioClip screamClip;

    [Header("Dialog")]
    [SerializeField] private MinigameDialog minigameDialog;

    private ColorRenderer colorRenderer;
    private ColorPlayerController playerController;
    private ColorInteractable interactable;
    private MazeMaw maw;
    private SpriteRenderer paperRenderer;
    private GameObject tipsOverlay;
    private Image tipsImage;
    private bool tipsShowing;
    private bool shattering;

    private float timeRemaining;
    private bool colorActive;
    private int lastDisplayedSecond;
    private AudioSource sfxSource;

    public RectTransform SubmitButtonRect { get; private set; }

    public static bool IsAnyColorActive { get; private set; }
    public static bool IsTipsShowing { get; private set; }

    void Awake()
    {
        colorRenderer = GetComponent<ColorRenderer>();
        if (colorRenderer == null)
            colorRenderer = gameObject.AddComponent<ColorRenderer>();

        interactable = GetComponent<ColorInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<ColorInteractable>();

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        EnsureCanvasSetup();
    }

    void Update()
    {
        if (tipsShowing)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseTips();
                return;
            }
            UpdateTipsPulse();
            return;
        }

        if (!colorActive) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();
        UpdatePaperEffects();
        UpdateMawVisibility();
        UpdateCameraShake();

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetColoring();
        }

        if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            OnSubmitClicked();
        }


        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            FailColoring();
        }
    }

    private void EnsureCanvasSetup()
    {
        if (colorCanvas == null)
        {
            GameObject canvasObj = new GameObject("ColorOverlay");
            colorCanvas = canvasObj.AddComponent<Canvas>();
            colorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            colorCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ensure EventSystem exists with InputSystemUIInputModule
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem");
            eventSystem = es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        else if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        colorCanvas.gameObject.SetActive(false);

        Transform bgTransform = colorCanvas.transform.Find("ColorBackground");
        if (bgTransform == null)
        {
            GameObject bg = new GameObject("ColorBackground");
            bg.transform.SetParent(colorCanvas.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        }

        if (colorContainer == null)
        {
            Transform containerTransform = colorCanvas.transform.Find("ColorContainer");
            if (containerTransform == null)
            {
                GameObject container = new GameObject("ColorContainer");
                container.transform.SetParent(colorCanvas.transform, false);
                colorContainer = container.AddComponent<RectTransform>();
            }
            else
            {
                colorContainer = containerTransform as RectTransform;
            }
        }

        if (timerText == null)
        {
            Transform timerTransform = colorCanvas.transform.Find("TimerText");
            if (timerTransform == null)
            {
                GameObject timerObj = new GameObject("TimerText");
                timerObj.transform.SetParent(colorCanvas.transform, false);

                RectTransform timerRect = timerObj.AddComponent<RectTransform>();
                timerRect.anchorMin = new Vector2(0.3f, 0.92f);
                timerRect.anchorMax = new Vector2(0.7f, 0.99f);
                timerRect.offsetMin = Vector2.zero;
                timerRect.offsetMax = Vector2.zero;

                timerText = timerObj.AddComponent<TextMeshProUGUI>();
                timerText.alignment = TextAlignmentOptions.Center;
                timerText.fontSize = 48;
                timerText.color = Color.white;
                timerText.text = "60s";
                timerText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            }
            else
            {
                timerText = timerTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (resetHintText == null)
        {
            Transform hintTransform = colorCanvas.transform.Find("ResetHintText");
            if (hintTransform == null)
            {
                GameObject hintObj = new GameObject("ResetHintText");
                hintObj.transform.SetParent(colorCanvas.transform, false);

                RectTransform hintRect = hintObj.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.3f, 0.01f);
                hintRect.anchorMax = new Vector2(0.7f, 0.06f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;

                resetHintText = hintObj.AddComponent<TextMeshProUGUI>();
                resetHintText.alignment = TextAlignmentOptions.Center;
                resetHintText.fontSize = 24;
                resetHintText.color = new Color(1f, 1f, 1f, 0.6f);
                resetHintText.text = "[R] Reset Coloring";
                resetHintText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            }
            else
            {
                resetHintText = hintTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (mazeParticles == null)
        {
            mazeParticles = gameObject.GetComponent<MazeParticles>();
            if (mazeParticles == null)
                mazeParticles = gameObject.AddComponent<MazeParticles>();
        }

        if (maw == null)
        {
            maw = gameObject.GetComponent<MazeMaw>();
            if (maw == null)
                maw = gameObject.AddComponent<MazeMaw>();

            if (mawSprite != null)
                maw.Setup(colorCanvas, mawSprite);
        }

        if (tipsOverlay == null)
        {
            Transform tipsTransform = colorCanvas.transform.Find("TipsOverlay");
            if (tipsTransform != null)
            {
                tipsOverlay = tipsTransform.gameObject;
                tipsImage = tipsOverlay.GetComponent<Image>();
            }
            else if (tipsSprite != null)
            {
                GameObject tipsObj = new GameObject("TipsOverlay");
                tipsObj.transform.SetParent(colorCanvas.transform, false);

                RectTransform tipsRect = tipsObj.AddComponent<RectTransform>();
                tipsRect.anchorMin = Vector2.zero;
                tipsRect.anchorMax = Vector2.one;
                tipsRect.offsetMin = Vector2.zero;
                tipsRect.offsetMax = Vector2.zero;

                tipsImage = tipsObj.AddComponent<Image>();
                tipsImage.sprite = tipsSprite;
                tipsImage.type = Image.Type.Simple;
                tipsImage.preserveAspect = true;
                tipsImage.raycastTarget = true;

                Button tipsBtn = tipsObj.AddComponent<Button>();
                tipsBtn.targetGraphic = tipsImage;
                tipsBtn.transition = Selectable.Transition.None;
                tipsBtn.onClick.AddListener(OnTipsClicked);

                tipsOverlay = tipsObj;

                GameObject escObj = new GameObject("EscHint");
                escObj.transform.SetParent(tipsObj.transform, false);

                RectTransform escRect = escObj.AddComponent<RectTransform>();
                escRect.anchorMin = new Vector2(1f, 1f);
                escRect.anchorMax = new Vector2(1f, 1f);
                escRect.pivot = new Vector2(1f, 1f);
                escRect.anchoredPosition = new Vector2(-20f, -20f);
                escRect.sizeDelta = new Vector2(200f, 40f);

                TextMeshProUGUI escText = escObj.AddComponent<TextMeshProUGUI>();
                escText.alignment = TextAlignmentOptions.TopRight;
                escText.fontSize = 20;
                escText.color = new Color(1f, 1f, 1f, 0.7f);
                escText.text = "ESC to return";
                escText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
                escText.raycastTarget = false;
            }
        }

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        if (playerDot == null)
        {
            Transform dotTransform = colorCanvas.transform.Find("PlayerDot");
            if (dotTransform == null)
            {
                GameObject dot = new GameObject("PlayerDot");
                dot.transform.SetParent(colorCanvas.transform, false);

                RectTransform dotRect = dot.AddComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(28f, 28f);
                dotRect.anchorMin = new Vector2(0.5f, 0.5f);
                dotRect.anchorMax = new Vector2(0.5f, 0.5f);
                dotRect.pivot = new Vector2(0.5f, 0.5f);

                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = Color.white;
                dotImg.raycastTarget = false;

                playerDot = dot;
            }
            else
            {
                playerDot = dotTransform.gameObject;
            }
        }

        playerDot.SetActive(false);
    }

    public void OpenColoring()
    {
        if (colorActive) return;
        if (interactable != null && interactable.IsLocked) return;

        colorCanvas.gameObject.SetActive(true);

        if (tipsOverlay != null && tipsSprite != null)
        {
            tipsShowing = true;
            IsTipsShowing = true;
            tipsOverlay.SetActive(true);
            tipsOverlay.transform.SetAsLastSibling();

            if (timerText != null) timerText.gameObject.SetActive(false);
            if (resetHintText != null) resetHintText.gameObject.SetActive(false);
        }
        else
        {
            StartColoring();
        }
    }

    private void OnTipsClicked()
    {
        if (!tipsShowing) return;
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        StartColoring();
    }

    private void CloseTips()
    {
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        colorCanvas.gameObject.SetActive(false);
    }

    private void StartColoring()
    {
        if (colorActive) return;

        if (timerText != null) timerText.gameObject.SetActive(true);
        if (resetHintText != null) resetHintText.gameObject.SetActive(true);

        timeRemaining = timeLimit;
        colorActive = true;
        IsAnyColorActive = true;
        lastDisplayedSecond = Mathf.CeilToInt(timeLimit);
        BGMManager.PauseBGM();

        colorRenderer.BuildGrid(referenceSprite, paletteColors, colorContainer, tableSprite);
        colorContainer.SetAsLastSibling();

        if (paperObject == null)
        {
            SpriteRenderer existing = GetComponentInChildren<SpriteRenderer>();
            if (existing != null && existing != colorRenderer.GetComponent<SpriteRenderer>())
                paperObject = existing.gameObject;
        }

        if (paperObject == null)
        {
            paperObject = new GameObject("PaperObject");
            paperObject.transform.position = transform.position;
            paperRenderer = paperObject.AddComponent<SpriteRenderer>();
            paperRenderer.sortingOrder = 50;

            if (paperSprite != null)
            {
                float spriteW = paperSprite.texture.width / paperSprite.pixelsPerUnit;
                float spriteH = paperSprite.texture.height / paperSprite.pixelsPerUnit;
                float targetH = 1.5f;
                float scale = targetH / spriteH;
                paperObject.transform.localScale = Vector3.one * scale;
            }
            else
            {
                paperObject.transform.localScale = Vector3.one * 0.2f;
            }
        }

        if (paperObject != null)
        {
            paperObject.SetActive(true);
            paperRenderer = paperObject.GetComponent<SpriteRenderer>();
            if (paperRenderer != null && paperSprite != null)
                paperRenderer.sprite = paperSprite;

            Vector3 pos = paperRenderer.transform.localPosition;
            pos.y = paperYPosition;
            paperRenderer.transform.localPosition = pos;
        }

        if (playerDot != null)
            Destroy(playerDot);

        GameObject dot = new GameObject("PlayerDot");
        dot.transform.SetParent(colorContainer, false);

        RectTransform dotRect = dot.AddComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(28f, 28f);
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);

        Image dotImg = dot.AddComponent<Image>();
        dotImg.color = Color.white;
        dotImg.raycastTarget = false;

        playerDot = dot;
        playerDot.SetActive(false);

        playerController = gameObject.AddComponent<ColorPlayerController>();
        playerController.Initialize(colorRenderer, this);

        if (mazeParticles != null)
            mazeParticles.SetActive(true, colorCanvas);

        colorContainer.SetAsLastSibling();

        CreateSubmitButton();
    }

    private void CreateSubmitButton()
{
    GameObject btnObj = new GameObject("SubmitButton");
    btnObj.transform.SetParent(colorContainer, false);

    RectTransform btnRect = btnObj.AddComponent<RectTransform>();
    btnRect.anchorMin = new Vector2(0.5f, 0.5f);
    btnRect.anchorMax = new Vector2(0.5f, 0.5f);
    btnRect.pivot = new Vector2(0.5f, 0.5f);
    btnRect.anchoredPosition = new Vector2(0f, -340f);
    btnRect.sizeDelta = new Vector2(180f, 50f);

    SubmitButtonRect = btnRect;

    Image btnImg = btnObj.AddComponent<Image>();
    btnImg.color = new Color(0.2f, 0.7f, 0.3f);
    btnImg.raycastTarget = true; // Ensure raycasting is on

    // --- ADD standard Unity Button component ---
    Button btn = btnObj.AddComponent<Button>();
    btn.targetGraphic = btnImg;
    btn.onClick.AddListener(OnSubmitClicked);

    GameObject textObj = new GameObject("Text");
    textObj.transform.SetParent(btnObj.transform, false);

    RectTransform textRect = textObj.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.offsetMin = Vector2.zero;
    textRect.offsetMax = Vector2.zero;

    TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
    text.alignment = TextAlignmentOptions.Center;
    text.fontSize = 24;
    text.fontStyle = FontStyles.Bold;
    text.color = Color.white;
    text.text = "SUBMIT";
    text.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
    text.raycastTarget = false; // Disable on text to prevent blocking
}

    public void OnSubmitClicked()
    {
        if (!colorActive) return;
        CheckWinCondition();
    }

    public void CloseColoring(bool fromFail = false)
    {
        colorActive = false;
        tipsShowing = false;
        IsTipsShowing = false;
        IsAnyColorActive = false;
        colorCanvas.gameObject.SetActive(false);
        BGMManager.ResumeBGM();

        if (sfxSource != null) sfxSource.Stop();

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        if (playerController != null)
        {
            playerController.Deactivate();
            Destroy(playerController);
            playerController = null;
        }

        if (playerDot != null)
        {
            Destroy(playerDot);
            playerDot = null;
        }

        if (mazeParticles != null)
            mazeParticles.SetActive(false);

        if (!fromFail)
        {
            ResetPaperVisuals();
            if (colorContainer != null) colorContainer.anchoredPosition = Vector2.zero;

            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr != null && sr.gameObject != paperObject)
                    sr.gameObject.SetActive(false);
            }

            paperObject = null;
            paperRenderer = null;
        }
        else if (paperRenderer != null)
        {
            paperRenderer.color = Color.white;
            paperRenderer.transform.localRotation = Quaternion.identity;
        }

        if (maw != null)
            maw.ResetVisibility();
    }

    public void ResetColoring()
    {
        if (!colorActive) return;

        if (playerController != null)
        {
            playerController.Deactivate();
            Destroy(playerController);
            playerController = null;
        }

        colorRenderer.BuildGrid(referenceSprite, paletteColors, colorContainer, tableSprite);
        colorContainer.SetAsLastSibling();

        playerController = gameObject.AddComponent<ColorPlayerController>();
        playerController.Initialize(colorRenderer, this);
    }

    private bool isChecking;

    public void CheckWinCondition()
    {
        if (!colorActive) return;
        if (isChecking) return;

        float match = colorRenderer.GetMatchPercentage();

        if (match >= matchThreshold)
        {
            ColoringCompleted();
        }
        else
        {
            StartCoroutine(FailedSubmit());
        }
    }

    private System.Collections.IEnumerator FailedSubmit()
    {
        isChecking = true;

        timeRemaining -= 4f;
        if (timeRemaining < 0f) timeRemaining = 0f;

        List<Vector2Int> wrongPixels = colorRenderer.GetWrongPixels();
        yield return colorRenderer.FlashWrongPixels(wrongPixels);

        isChecking = false;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            FailColoring();
        }
    }

    public void ColoringCompleted()
    {
        if (!colorActive) return;
        colorActive = false;

        if (interactable != null)
            interactable.IsLocked = true;

        InventoryUI inventory = FindObjectOfType<InventoryUI>();
        if (inventory == null)
        {
            GameObject invObj = new GameObject("InventoryUI");
            inventory = invObj.AddComponent<InventoryUI>();
        }
        if (inventory != null && rewardSprite != null)
            inventory.AddItem(rewardSprite, rewardName);

        if (colorContainer != null)
            colorContainer.gameObject.SetActive(false);

        BGMManager.ResumeBGM();

        if (minigameDialog != null)
            minigameDialog.ShowWinDialog();

        PlayCountdown(1, 0);
        StartCoroutine(ShatterPaper());

        Debug.Log("Coloring completed! " + rewardName + " collected.");
    }

    public void FailColoring()
    {
        if (!colorActive) return;
        colorActive = false;

        if (paperRenderer != null)
        {
            paperRenderer.color = Color.white;
            paperRenderer.transform.localRotation = Quaternion.identity;
        }

        BGMManager.ResumeBGM();

        if (maw != null && maw2Sprite != null && maw3Sprite != null)
        {
            colorContainer.gameObject.SetActive(false);
            maw.PlayJumpscare(maw2Sprite, maw3Sprite, () =>
            {
                CloseColoring(true);
                if (minigameDialog != null)
                    minigameDialog.ShowFailDialog();
            }, screamClip);
        }
        else
        {
            CloseColoring(true);
            if (minigameDialog != null)
                minigameDialog.ShowFailDialog();
        }
    }

    public bool IsColorActive()
    {
        return colorActive;
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds + "s";

        if (seconds != lastDisplayedSecond)
        {
            lastDisplayedSecond = seconds;
            if (clockTickClip != null)
            {
                sfxSource.Stop();
                sfxSource.PlayOneShot(clockTickClip);
            }
        }

        if (timeRemaining <= 10f)
        {
            float flash = Mathf.PingPong(Time.time * 4f, 1f);
            timerText.color = Color.Lerp(Color.red, Color.white, flash);
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    private void UpdatePaperEffects()
    {
        if (paperRenderer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);

        paperRenderer.color = Color.Lerp(Color.white, Color.red, urgency);

        float shakeAmount = urgency * 2f;
        float shakeSpeed = urgency * 25f;
        float rotZ = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;

        paperRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    private void ResetPaperVisuals()
    {
        if (paperRenderer == null) return;
        paperRenderer.color = Color.white;
        paperRenderer.transform.localRotation = Quaternion.identity;
        paperRenderer.transform.localPosition = Vector3.zero;
    }

    private void UpdateMawVisibility()
    {
        if (maw == null) return;
        float urgency = 1f - (timeRemaining / timeLimit);
        maw.SetVisibility(urgency);
    }

    private void UpdateTipsPulse()
    {
        if (tipsImage == null) return;
        float alpha = Mathf.Lerp(0.4f, 1f, (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f);
        Color c = tipsImage.color;
        c.a = alpha;
        tipsImage.color = c;
    }

    private void UpdateCameraShake()
    {
        if (colorContainer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);

        float shakeIntensity = Mathf.Clamp01((urgency - 0.5f) * 2f) * 6f;
        float shakeSpeed = 20f;

        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
        float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity * 0.7f;

        colorContainer.anchoredPosition = new Vector2(offsetX, offsetY);
    }

    private IEnumerator ShatterPaper()
    {
        if (shattering) yield break;
        shattering = true;
        Vector3 paperPos = transform.position + Vector3.up * 1.5f;

        if (paperRenderer != null)
        {
            paperPos = paperRenderer.transform.position;
            paperRenderer.gameObject.SetActive(false);
        }

        List<GameObject> pieces = new List<GameObject>();

        for (int i = 0; i < 8; i++)
        {
            GameObject piece = CreateShardPiece(paperPos, i);

            Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 force = new Vector2(Random.Range(-5f, 5f), Random.Range(2f, 7f));
                rb.AddForce(force, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-300f, 300f));
            }

            pieces.Add(piece);
        }

        yield return new WaitForSeconds(0.2f);

        float fadeTime = 0.3f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);

            foreach (GameObject piece in pieces)
            {
                if (piece == null) continue;
                SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = a;
                    sr.color = c;
                }
            }

            yield return null;
        }

        foreach (GameObject piece in pieces)
        {
            if (piece != null)
                Destroy(piece);
        }

        if (paperRenderer != null)
        {
            paperRenderer.gameObject.SetActive(false);
            paperRenderer = null;
        }

        CloseColoring();
    }

    private GameObject CreateShardPiece(Vector3 position, int index)
    {
        GameObject piece = new GameObject("Shard");
        float angle = (index / 8f) * 360f;
        float dist = Random.Range(0.3f, 0.8f);
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0f) * dist;
        Vector3 pos = position + offset;
        pos.z = 0f;
        piece.transform.position = pos;

        if (shardSpriteA != null || shardSpriteB != null)
        {
            float scale = Random.Range(0.1f, 0.2f);
            piece.transform.localScale = new Vector3(scale, scale, 1f);
            piece.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = (index % 2 == 0) ? shardSpriteA : shardSpriteB;
            sr.sortingOrder = 100;
        }
        else
        {
            piece.transform.localScale = new Vector3(0.05f, 0.05f, 1f);

            SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWhiteSquareSprite();
            sr.color = new Color(0.9f, 0.85f, 0.8f);
            sr.sortingOrder = 100;
        }

        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.5f;

        return piece;
    }

    private Sprite CreateWhiteSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }

    private void PlayCountdown(int from, int to)
    {
        GameObject canvasObj = new GameObject("ColorCountdownCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 201;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject tmpObj = new GameObject("CountdownText");
        tmpObj.transform.SetParent(canvasObj.transform, false);

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

        CountdownMotionBlur blur = tmpObj.AddComponent<CountdownMotionBlur>();
        blur.StartCountdown(from, to);

        Destroy(canvasObj, 8f);
    }
}
