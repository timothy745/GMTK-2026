using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    [Header("Maze Settings")]
    [SerializeField] private int mazeWidth = 64;
    [SerializeField] private int mazeHeight = 64;
    [SerializeField] private float timeLimit = 60f;

    [Header("References")]
    [SerializeField] private Canvas mazeCanvas;
    [SerializeField] private RectTransform mazeContainer;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resetHintText;
    [SerializeField] private GameObject paperObject;
    [SerializeField] private GameObject playerDot;
    [SerializeField] private MazeParticles mazeParticles;

    [Header("Paper")]
    [SerializeField] private Sprite paperSprite;

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

    [Header("Locked")]
    [SerializeField] private Sprite lockedSprite;

    private MazeGenerator generator;
    private MazeRenderer mazeRenderer;
    private MazePlayerController playerController;
    private MazeInteractable interactable;
    private MazeMaw maw;
    private SpriteRenderer paperRenderer;
    private GameObject tipsOverlay;
    private Image tipsImage;
    private bool tipsShowing;
    private bool shattering;

    private float timeRemaining;
    private bool mazeActive;
    private Vector2Int exitPosition;

    public static bool IsAnyMazeActive { get; private set; }
    public static bool IsTipsShowing { get; private set; }

    void Awake()
    {
        generator = GetComponent<MazeGenerator>();
        if (generator == null)
            generator = gameObject.AddComponent<MazeGenerator>();

        mazeRenderer = GetComponent<MazeRenderer>();
        if (mazeRenderer == null)
            mazeRenderer = gameObject.AddComponent<MazeRenderer>();

        interactable = GetComponent<MazeInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<MazeInteractable>();

        EnsureCanvasSetup();
    }

    void Update()
    {
        if (tipsShowing)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseTips();
                return;
            }
            UpdateTipsPulse();
            return;
        }

        if (!mazeActive) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();
        UpdatePaperEffects();
        UpdateTileColors();
        UpdateMawVisibility();
        UpdateCameraShake();

        // R to reset maze
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetMaze();
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            FailMaze();
        }
    }

    private void EnsureCanvasSetup()
    {
        if (mazeCanvas == null)
        {
            GameObject canvasObj = new GameObject("MazeOverlay");
            mazeCanvas = canvasObj.AddComponent<Canvas>();
            mazeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mazeCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        mazeCanvas.gameObject.SetActive(false);

        // Background
        Transform bgTransform = mazeCanvas.transform.Find("MazeBackground");
        if (bgTransform == null)
        {
            GameObject bg = new GameObject("MazeBackground");
            bg.transform.SetParent(mazeCanvas.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        }

        // Maze container
        if (mazeContainer == null)
        {
            Transform containerTransform = mazeCanvas.transform.Find("MazeContainer");
            if (containerTransform == null)
            {
                GameObject container = new GameObject("MazeContainer");
                container.transform.SetParent(mazeCanvas.transform, false);
                mazeContainer = container.AddComponent<RectTransform>();
            }
            else
            {
                mazeContainer = containerTransform as RectTransform;
            }
        }

        // Timer text
        if (timerText == null)
        {
            Transform timerTransform = mazeCanvas.transform.Find("TimerText");
            if (timerTransform == null)
            {
                GameObject timerObj = new GameObject("TimerText");
                timerObj.transform.SetParent(mazeCanvas.transform, false);

                RectTransform timerRect = timerObj.AddComponent<RectTransform>();
                timerRect.anchorMin = new Vector2(0.3f, 0.92f);
                timerRect.anchorMax = new Vector2(0.7f, 0.99f);
                timerRect.offsetMin = Vector2.zero;
                timerRect.offsetMax = Vector2.zero;

                timerText = timerObj.AddComponent<TextMeshProUGUI>();
                timerText.alignment = TextAlignmentOptions.Center;
                timerText.fontSize = 48;
                timerText.color = Color.white;
                timerText.text = "30s";
                timerText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            }
            else
            {
                timerText = timerTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Reset hint text (R to reset)
        if (resetHintText == null)
        {
            Transform hintTransform = mazeCanvas.transform.Find("ResetHintText");
            if (hintTransform == null)
            {
                GameObject hintObj = new GameObject("ResetHintText");
                hintObj.transform.SetParent(mazeCanvas.transform, false);

                RectTransform hintRect = hintObj.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.3f, 0.01f);
                hintRect.anchorMax = new Vector2(0.7f, 0.06f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;

                resetHintText = hintObj.AddComponent<TextMeshProUGUI>();
                resetHintText.alignment = TextAlignmentOptions.Center;
                resetHintText.fontSize = 24;
                resetHintText.color = new Color(1f, 1f, 1f, 0.6f);
                resetHintText.text = "[R] Reset Maze";
                resetHintText.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            }
            else
            {
                resetHintText = hintTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        // Maze particles
        if (mazeParticles == null)
        {
            mazeParticles = gameObject.GetComponent<MazeParticles>();
            if (mazeParticles == null)
                mazeParticles = gameObject.AddComponent<MazeParticles>();
        }

        // Maw overlay
        if (maw == null)
        {
            maw = gameObject.GetComponent<MazeMaw>();
            if (maw == null)
                maw = gameObject.AddComponent<MazeMaw>();

            if (mawSprite != null)
                maw.Setup(mazeCanvas, mawSprite);
        }

        // Tips overlay
        if (tipsOverlay == null)
        {
            Transform tipsTransform = mazeCanvas.transform.Find("TipsOverlay");
            if (tipsTransform != null)
            {
                tipsOverlay = tipsTransform.gameObject;
                tipsImage = tipsOverlay.GetComponent<Image>();
            }
            else if (tipsSprite != null)
            {
                GameObject tipsObj = new GameObject("TipsOverlay");
                tipsObj.transform.SetParent(mazeCanvas.transform, false);

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

                // ESC hint
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

        // Ensure tips overlay has a clickable button even if it was pre-created
        if (tipsOverlay != null)
        {
            if (tipsImage == null)
                tipsImage = tipsOverlay.GetComponent<Image>();
            if (tipsImage != null)
                tipsImage.raycastTarget = true;

            if (tipsOverlay.GetComponent<Button>() == null)
            {
                Button tipsBtn = tipsOverlay.AddComponent<Button>();
                tipsBtn.targetGraphic = tipsImage;
                tipsBtn.transition = Selectable.Transition.None;
                tipsBtn.onClick.AddListener(OnTipsClicked);
            }
        }

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        // Player dot
        if (playerDot == null)
        {
            Transform dotTransform = mazeCanvas.transform.Find("PlayerDot");
            if (dotTransform == null)
            {
                GameObject dot = new GameObject("PlayerDot");
                dot.transform.SetParent(mazeCanvas.transform, false);

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

    public void OpenMaze()
    {
        if (mazeActive) return;
        if (interactable != null && interactable.IsLocked) return;

        mazeCanvas.gameObject.SetActive(true);

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
            StartMaze();
        }
    }

    private void OnTipsClicked()
    {
        if (!tipsShowing) return;
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        StartMaze();
    }

    private void CloseTips()
    {
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        mazeCanvas.gameObject.SetActive(false);
    }

    private void StartMaze()
    {
        if (mazeActive) return;

        if (timerText != null) timerText.gameObject.SetActive(true);
        if (resetHintText != null) resetHintText.gameObject.SetActive(true);

        timeRemaining = timeLimit;
        mazeActive = true;
        IsAnyMazeActive = true;

        MazeGenerator.Cell[,] grid = generator.GenerateMaze(mazeWidth, mazeHeight);
        mazeRenderer.BuildMaze(grid, mazeContainer, mazeWidth, mazeHeight);
        mazeContainer.SetAsLastSibling();
        exitPosition = mazeRenderer.ExitPosition;

        // Cache paper renderer and apply sprite if set
        if (paperObject != null)
        {
            paperObject.SetActive(true);
            paperRenderer = paperObject.GetComponent<SpriteRenderer>();
            if (paperRenderer != null && paperSprite != null)
                paperRenderer.sprite = paperSprite;
        }

        // Destroy old player dot if it exists
        if (playerDot != null)
            Destroy(playerDot);

        // Create fresh player dot (parented to mazeContainer so it shakes together)
        GameObject dot = new GameObject("PlayerDot");
        dot.transform.SetParent(mazeContainer, false);

        RectTransform dotRect = dot.AddComponent<RectTransform>();
        dotRect.sizeDelta = new Vector2(28f, 28f);
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);

        Image dotImg = dot.AddComponent<Image>();
        dotImg.color = Color.white;
        dotImg.raycastTarget = false;

        playerDot = dot;
        playerDot.SetActive(true);

        playerController = playerDot.AddComponent<MazePlayerController>();
        playerController.Initialize(generator, this, Vector2Int.zero, mazeWidth, mazeHeight);

        if (mazeParticles != null)
            mazeParticles.SetActive(true, mazeCanvas);

        mazeContainer.SetAsLastSibling();
        if (playerDot != null)
            playerDot.transform.SetAsLastSibling();
        if (playerController != null && playerController.TrailContainer != null)
            playerController.TrailContainer.SetAsLastSibling();
    }

    public void CloseMaze()
    {
        mazeActive = false;
        tipsShowing = false;
        IsTipsShowing = false;
        IsAnyMazeActive = false;
        mazeCanvas.gameObject.SetActive(false);

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        if (playerController != null)
            playerController.Deactivate();

        if (playerDot != null)
        {
            Destroy(playerDot);
            playerDot = null;
        }

        if (mazeParticles != null)
            mazeParticles.SetActive(false);

        ResetPaperVisuals();

        if (paperRenderer != null && lockedSprite != null)
        {
            paperRenderer.sprite = lockedSprite;
            paperRenderer.color = Color.white;
        }
        else if (paperObject != null)
        {
            paperObject.SetActive(false);
        }

        paperRenderer = null;

        if (maw != null)
            maw.ResetVisibility();
    }

    public void ResetMaze()
    {
        if (!mazeActive) return;

        // Reset player position and clear trail only
        if (playerController != null)
        {
            playerController.Initialize(generator, this, Vector2Int.zero, mazeWidth, mazeHeight);
        }
    }

    public void MazeCompleted()
    {
        if (!mazeActive) return;
        mazeActive = false;

        if (interactable != null)
            interactable.IsLocked = true;

        InventoryUI inventory = FindObjectOfType<InventoryUI>();
        if (inventory != null && rewardSprite != null)
            inventory.AddItem(rewardSprite, rewardName);

        if (mazeContainer != null)
            mazeContainer.gameObject.SetActive(false);

        StartCoroutine(ShatterPaper());

        Debug.Log("Maze completed! " + rewardName + " collected.");
    }

    public void FailMaze()
    {
        if (!mazeActive) return;
        mazeActive = false;

        if (interactable != null)
            interactable.IsLocked = true;

        if (maw != null && maw2Sprite != null && maw3Sprite != null)
        {
            mazeContainer.gameObject.SetActive(false);
            maw.PlayJumpscare(maw2Sprite, maw3Sprite, () =>
            {
                StartCoroutine(ShatterPaper());
            });
        }
        else
        {
            StartCoroutine(ShatterPaper());
        }
    }

    public bool IsMazeActive()
    {
        return mazeActive;
    }

    public Vector2Int GetExitPosition()
    {
        return exitPosition;
    }

    // --- Timer & Visuals ---

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = seconds + "s";

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
        float posX = Mathf.Sin(Time.time * shakeSpeed * 1.3f) * shakeAmount * 0.01f;
        float posY = Mathf.Cos(Time.time * shakeSpeed * 0.7f) * shakeAmount * 0.005f;

        paperRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
        paperRenderer.transform.localPosition += new Vector3(posX, posY, 0f);
    }

    private void UpdateTileColors()
    {
        if (mazeRenderer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);
        Color tileColor = Color.Lerp(Color.white, Color.red, urgency);

        foreach (var img in mazeRenderer.WhiteTiles)
        {
            if (img != null)
                img.color = tileColor;
        }

        if (mazeParticles != null)
            mazeParticles.SetTintColor(tileColor);
    }

    private void ResetPaperVisuals()
    {
        if (paperRenderer == null) return;
        paperRenderer.color = Color.white;
        paperRenderer.transform.localRotation = Quaternion.identity;
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
        if (mazeContainer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);

        float shakeIntensity = Mathf.Clamp01((urgency - 0.5f) * 2f) * 6f;
        float shakeSpeed = 20f;

        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
        float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity * 0.7f;

        mazeContainer.anchoredPosition = new Vector2(offsetX, offsetY);
    }

    // --- Paper Shatter ---

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

        Debug.Log("[MazeManager] ShatterPaper at " + paperPos + " shardA=" + (shardSpriteA != null ? shardSpriteA.name : "NULL") + " shardB=" + (shardSpriteB != null ? shardSpriteB.name : "NULL"));

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
            if (lockedSprite != null)
            {
                paperRenderer.gameObject.SetActive(true);
                paperRenderer.sprite = lockedSprite;
                paperRenderer.color = Color.white;
            }
            else
            {
                paperRenderer.gameObject.SetActive(false);
                paperRenderer = null;
            }
        }

        CloseMaze();
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
}
