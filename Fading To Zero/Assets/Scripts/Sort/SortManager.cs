using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SortManager : MonoBehaviour
{
    [Header("Sort Settings")]
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private float knockOutInterval = 8f;

    [Header("References")]
    [SerializeField] private Canvas sortCanvas;
    [SerializeField] private RectTransform sortContainer;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI resetHintText;
    [SerializeField] private GameObject paperObject;
    [SerializeField] private GameObject playerDot;
    [SerializeField] private MazeParticles mazeParticles;

    [Header("Sprites")]
    [SerializeField] private Sprite[] holeSprites = new Sprite[4];
    [SerializeField] private Sprite[] shapeSprites = new Sprite[4];
    [SerializeField] private Sprite tableSprite;

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

    [Header("Locked")]
    [SerializeField] private Sprite lockedSprite;

    [Header("Tentacle")]
    [SerializeField] private Sprite tentacleSprite;
    [SerializeField] private float tentacleCreepDuration = 5f;

    private SortRenderer sortRenderer;
    private SortPlayerController playerController;
    private SortInteractable interactable;
    private MazeMaw maw;
    private SpriteRenderer paperRenderer;
    private GameObject tipsOverlay;
    private Image tipsImage;
    private bool tipsShowing;
    private bool shattering;

    private float timeRemaining;
    private bool sortActive;
    private float knockOutTimer;

    public static bool IsAnySortActive { get; private set; }
    public static bool IsTipsShowing { get; private set; }

    void Awake()
    {
        sortRenderer = GetComponent<SortRenderer>();
        if (sortRenderer == null)
            sortRenderer = gameObject.AddComponent<SortRenderer>();

        interactable = GetComponent<SortInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<SortInteractable>();

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

        if (!sortActive) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();
        UpdatePaperEffects();
        UpdateTileColors();
        UpdateMawVisibility();
        UpdateCameraShake();

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetSort();
        }

        knockOutTimer -= Time.deltaTime;
        if (knockOutTimer <= 0f)
        {
            knockOutTimer = knockOutInterval;
            if (!sortRenderer.IsAnyKnockingOut)
                StartCoroutine(KnockOutSequence());
        }

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            FailSort();
        }
    }

    private void EnsureCanvasSetup()
    {
        if (sortCanvas == null)
        {
            GameObject canvasObj = new GameObject("SortOverlay");
            sortCanvas = canvasObj.AddComponent<Canvas>();
            sortCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            sortCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        sortCanvas.gameObject.SetActive(false);

        Transform bgTransform = sortCanvas.transform.Find("SortBackground");
        if (bgTransform == null)
        {
            GameObject bg = new GameObject("SortBackground");
            bg.transform.SetParent(sortCanvas.transform, false);

            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.85f);
        }

        if (sortContainer == null)
        {
            Transform containerTransform = sortCanvas.transform.Find("SortContainer");
            if (containerTransform == null)
            {
                GameObject container = new GameObject("SortContainer");
                container.transform.SetParent(sortCanvas.transform, false);
                sortContainer = container.AddComponent<RectTransform>();
            }
            else
            {
                sortContainer = containerTransform as RectTransform;
            }
        }

        if (timerText == null)
        {
            Transform timerTransform = sortCanvas.transform.Find("TimerText");
            if (timerTransform == null)
            {
                GameObject timerObj = new GameObject("TimerText");
                timerObj.transform.SetParent(sortCanvas.transform, false);

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

        if (resetHintText == null)
        {
            Transform hintTransform = sortCanvas.transform.Find("ResetHintText");
            if (hintTransform == null)
            {
                GameObject hintObj = new GameObject("ResetHintText");
                hintObj.transform.SetParent(sortCanvas.transform, false);

                RectTransform hintRect = hintObj.AddComponent<RectTransform>();
                hintRect.anchorMin = new Vector2(0.3f, 0.01f);
                hintRect.anchorMax = new Vector2(0.7f, 0.06f);
                hintRect.offsetMin = Vector2.zero;
                hintRect.offsetMax = Vector2.zero;

                resetHintText = hintObj.AddComponent<TextMeshProUGUI>();
                resetHintText.alignment = TextAlignmentOptions.Center;
                resetHintText.fontSize = 24;
                resetHintText.color = new Color(1f, 1f, 1f, 0.6f);
                resetHintText.text = "[R] Reset Sort";
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
                maw.Setup(sortCanvas, mawSprite);
        }

        if (tipsOverlay == null)
        {
            Transform tipsTransform = sortCanvas.transform.Find("TipsOverlay");
            if (tipsTransform != null)
            {
                tipsOverlay = tipsTransform.gameObject;
                tipsImage = tipsOverlay.GetComponent<Image>();
            }
            else if (tipsSprite != null)
            {
                GameObject tipsObj = new GameObject("TipsOverlay");
                tipsObj.transform.SetParent(sortCanvas.transform, false);

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
            Transform dotTransform = sortCanvas.transform.Find("PlayerDot");
            if (dotTransform == null)
            {
                GameObject dot = new GameObject("PlayerDot");
                dot.transform.SetParent(sortCanvas.transform, false);

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

    public void OpenSort()
    {
        if (sortActive) return;
        if (interactable != null && interactable.IsLocked) return;

        sortCanvas.gameObject.SetActive(true);

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
            StartSort();
        }
    }

    private void OnTipsClicked()
    {
        if (!tipsShowing) return;
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        StartSort();
    }

    private void CloseTips()
    {
        tipsShowing = false;
        IsTipsShowing = false;

        if (tipsOverlay != null)
            tipsOverlay.SetActive(false);

        sortCanvas.gameObject.SetActive(false);
    }

    private void StartSort()
    {
        if (sortActive) return;

        if (timerText != null) timerText.gameObject.SetActive(true);
        if (resetHintText != null) resetHintText.gameObject.SetActive(true);

        timeRemaining = timeLimit;
        sortActive = true;
        IsAnySortActive = true;
        knockOutTimer = knockOutInterval;

        sortRenderer.BuildBoard(holeSprites, shapeSprites, sortContainer, tableSprite);
        sortContainer.SetAsLastSibling();

        if (paperObject == null)
        {
            SpriteRenderer existing = GetComponentInChildren<SpriteRenderer>();
            if (existing != null && existing != sortRenderer.GetComponent<SpriteRenderer>())
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
        dot.transform.SetParent(sortContainer, false);

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

        playerController = gameObject.AddComponent<SortPlayerController>();
        playerController.Initialize(sortRenderer, this);

        if (mazeParticles != null)
            mazeParticles.SetActive(true, sortCanvas);

        sortContainer.SetAsLastSibling();
    }

    public void CloseSort(bool fromFail = false)
    {
        sortActive = false;
        tipsShowing = false;
        IsTipsShowing = false;
        IsAnySortActive = false;
        sortCanvas.gameObject.SetActive(false);

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

        if (!fromFail)
        {
            ResetPaperVisuals();

            if (paperObject != null)
            {
                if (paperRenderer != null && lockedSprite != null)
                {
                    paperRenderer.sprite = lockedSprite;
                    paperRenderer.color = Color.white;
                }
                else
                {
                    paperObject.SetActive(false);
                }
            }

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

    public void ResetSort()
    {
        if (!sortActive) return;

        if (playerController != null)
        {
            playerController.Deactivate();
            Destroy(playerController);
            playerController = null;
        }

        sortRenderer.BuildBoard(holeSprites, shapeSprites, sortContainer, tableSprite);
        sortContainer.SetAsLastSibling();

        playerController = gameObject.AddComponent<SortPlayerController>();
        playerController.Initialize(sortRenderer, this);
    }

    public void SortCompleted()
    {
        if (!sortActive) return;
        sortActive = false;

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

        if (sortContainer != null)
            sortContainer.gameObject.SetActive(false);

        PlayCountdown(2, 1);
        StartCoroutine(ShatterPaper());

        Debug.Log("Sort completed! " + rewardName + " collected.");
    }

    public void FailSort()
    {
        if (!sortActive) return;
        sortActive = false;

        if (paperRenderer != null)
        {
            paperRenderer.color = Color.white;
            paperRenderer.transform.localRotation = Quaternion.identity;
        }

        if (maw != null && maw2Sprite != null && maw3Sprite != null)
        {
            sortContainer.gameObject.SetActive(false);
            maw.PlayJumpscare(maw2Sprite, maw3Sprite, () =>
            {
                CloseSort(true);
            });
        }
        else
        {
            CloseSort(true);
        }
    }

    public bool IsSortActive()
    {
        return sortActive;
    }

    private IEnumerator KnockOutSequence()
    {
        sortRenderer.IsAnyKnockingOut = true;

        int shapeIdx = sortRenderer.GetRandomPlacedShape();
        if (shapeIdx < 0) { sortRenderer.IsAnyKnockingOut = false; yield break; }

        int holeIdx = sortRenderer.ShapeToHole[shapeIdx];

        float warningDuration = 1.2f;
        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            if (!sortActive) { sortRenderer.IsAnyKnockingOut = false; yield break; }

            float flash = Mathf.PingPong(elapsed * 6f, 1f);
            Color warnColor = Color.Lerp(Color.black, Color.white, flash);
            sortRenderer.SetHoleWarningColor(holeIdx, warnColor);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sortRenderer.SetHoleWarningColor(holeIdx, Color.white);

        Vector2[] holePositions = sortRenderer.HolePositions;
        Vector2 holePos = holePositions[holeIdx];

        GameObject tableMask = new GameObject("TentacleMask");
        tableMask.transform.SetParent(sortContainer, false);
        RectTransform maskRect = tableMask.AddComponent<RectTransform>();
        maskRect.anchorMin = new Vector2(0.5f, 0.5f);
        maskRect.anchorMax = new Vector2(0.5f, 0.5f);
        maskRect.pivot = new Vector2(0.5f, 0.5f);
        maskRect.sizeDelta = new Vector2(1400f, 900f);
        maskRect.anchoredPosition = Vector2.zero;
        Image maskImg = tableMask.AddComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.01f);
        Mask tableMaskComp = tableMask.AddComponent<Mask>();
        tableMaskComp.showMaskGraphic = false;

        GameObject tentacleObj = new GameObject("Tentacle");
        tentacleObj.transform.SetParent(tableMask.transform, false);

        RectTransform tentacleClip = tentacleObj.AddComponent<RectTransform>();
        tentacleClip.anchorMin = new Vector2(0.5f, 0.5f);
        tentacleClip.anchorMax = new Vector2(0.5f, 0.5f);
        tentacleClip.pivot = new Vector2(0.5f, 0.5f);

        Image tentImg = tentacleObj.AddComponent<Image>();
        tentImg.raycastTarget = true;

        if (tentacleSprite != null)
        {
            tentImg.sprite = tentacleSprite;
            tentImg.type = Image.Type.Simple;
            tentImg.preserveAspect = true;
        }
        else
        {
            tentImg.color = new Color(0.2f, 0.6f, 0.2f);
        }

        Vector2 edgeStart = GetTentacleEdgeStart(holePos, holeIdx);

        float tentacleSize = holeIdx == 1 ? 400f : 300f;
        tentacleClip.sizeDelta = new Vector2(tentacleSize, tentacleSize);

        Vector2 dirToHole = (holePos - edgeStart).normalized;
        float angle = Mathf.Atan2(dirToHole.y, dirToHole.x) * Mathf.Rad2Deg + 90f;

        if (holeIdx >= 2 && tentacleSprite != null)
        {
            Sprite flipped = Sprite.Create(
                tentacleSprite.texture,
                new Rect(0, 0, tentacleSprite.texture.width, tentacleSprite.texture.height),
                new Vector2(0.5f, 0.5f),
                tentacleSprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(0, 0, 0, 0),
                true
            );
            tentImg.sprite = flipped;
        }

        bool clicked = false;
        float creepElapsed = 0f;

        while (creepElapsed < tentacleCreepDuration)
        {
            if (!sortActive) { Destroy(tableMask); sortRenderer.IsAnyKnockingOut = false; yield break; }

            creepElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(creepElapsed / tentacleCreepDuration);
            float easeT = t * t;

            Vector2 currentPos = Vector2.Lerp(edgeStart, holePos, easeT);
            tentacleClip.anchoredPosition = currentPos;
            tentacleClip.rotation = Quaternion.Euler(0f, 0f, angle);

            float flashWarn = Mathf.PingPong(creepElapsed * 4f, 1f);
            tentImg.color = Color.Lerp(new Color(0.2f, 0.8f, 0.2f), new Color(1f, 0.3f, 0.3f), flashWarn);

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(tentacleClip, Mouse.current.position.ReadValue(), null))
                {
                    clicked = true;
                    break;
                }
            }

            yield return null;
        }

        if (clicked)
        {
            sortRenderer.SetHoleWarningColor(holeIdx, new Color(0.3f, 1f, 0.3f));
            float flashTime = 0.3f;
            float fe = 0f;
            while (fe < flashTime)
            {
                fe += Time.deltaTime;
                yield return null;
            }
            sortRenderer.SetHoleWarningColor(holeIdx, Color.white);
            Destroy(tableMask);
            sortRenderer.IsAnyKnockingOut = false;
        }
        else
        {
            Destroy(tableMask);

            RectTransform shapeRect = sortRenderer.ShapeRects[shapeIdx];
            Vector2 startPos = shapeRect.anchoredPosition;
            Vector2 endPos = new Vector2(Random.Range(200f, 520f), Random.Range(-280f, 280f));
            float moveDuration = 0.25f;
            float moveElapsed = 0f;

            sortRenderer.RemoveShapeFromHole(shapeIdx);

            if (playerController != null)
                playerController.RemovePlacement(shapeIdx);

            while (moveElapsed < moveDuration)
            {
                if (!sortActive) { sortRenderer.IsAnyKnockingOut = false; yield break; }

                moveElapsed += Time.deltaTime;
                float t = moveElapsed / moveDuration;
                float easeT = t * t;
                shapeRect.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
                shapeRect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, Random.Range(-25f, 25f), easeT));
                yield return null;
            }

            shapeRect.anchoredPosition = endPos;
            sortRenderer.IsAnyKnockingOut = false;
        }
    }

    private Vector2 GetTentacleEdgeStart(Vector2 holePos, int holeIdx)
    {
        switch (holeIdx)
        {
            case 0:         return new Vector2(-750f, holePos.y);
            case 1: return new Vector2(holePos.x, 550f);
            case 2: return new Vector2(-750f, holePos.y);
            case 3: return new Vector2(holePos.x, -550f);
            default: return new Vector2(-750f, holePos.y);
        }
    }

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

        paperRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    private void UpdateTileColors()
    {
        if (sortRenderer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);
        Color tileColor = Color.Lerp(Color.white, Color.red, urgency);

        sortRenderer.SetTileColors(tileColor);

        if (mazeParticles != null)
            mazeParticles.SetTintColor(tileColor);
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
        if (sortContainer == null) return;

        float urgency = 1f - (timeRemaining / timeLimit);

        float shakeIntensity = Mathf.Clamp01((urgency - 0.5f) * 2f) * 6f;
        float shakeSpeed = 20f;

        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
        float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity * 0.7f;

        sortContainer.anchoredPosition = new Vector2(offsetX, offsetY);
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

        CloseSort();
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
        GameObject canvasObj = new GameObject("SortCountdownCanvas");
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
