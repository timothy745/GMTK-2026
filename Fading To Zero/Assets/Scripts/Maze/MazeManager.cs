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
    [SerializeField] private Renderer paperRenderer;
    [SerializeField] private GameObject playerDot;
    [SerializeField] private MazeParticles mazeParticles;

    [Header("Shatter Pieces")]
    [SerializeField] private GameObject shatterPiecePrefab;

    private MazeGenerator generator;
    private MazeRenderer mazeRenderer;
    private MazePlayerController playerController;
    private MazeInteractable interactable;

    private float timeRemaining;
    private bool mazeActive;
    private Vector2Int exitPosition;

    public static bool IsAnyMazeActive { get; private set; }

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
        if (!mazeActive) return;

        timeRemaining -= Time.deltaTime;
        UpdateTimerDisplay();
        UpdatePaperEffects();
        UpdateTileColors();

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

        timeRemaining = timeLimit;
        mazeActive = true;
        IsAnyMazeActive = true;

        MazeGenerator.Cell[,] grid = generator.GenerateMaze(mazeWidth, mazeHeight);
        mazeRenderer.BuildMaze(grid, mazeContainer, mazeWidth, mazeHeight);
        mazeContainer.SetAsLastSibling();
        exitPosition = mazeRenderer.ExitPosition;

        // Destroy old player dot if it exists
        if (playerDot != null)
            Destroy(playerDot);

        // Create fresh player dot
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
        playerDot.SetActive(true);

        playerController = playerDot.AddComponent<MazePlayerController>();
        playerController.Initialize(generator, this, Vector2Int.zero, mazeWidth, mazeHeight);

        mazeCanvas.gameObject.SetActive(true);

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
        IsAnyMazeActive = false;
        mazeCanvas.gameObject.SetActive(false);

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

        if (interactable != null)
            interactable.IsLocked = true;

        CloseMaze();
        Debug.Log("Maze completed! Photo Piece (1/3) collected.");
    }

    public void FailMaze()
    {
        if (!mazeActive) return;

        if (interactable != null)
            interactable.IsLocked = true;

        CloseMaze();

        if (paperRenderer != null)
            StartCoroutine(ShatterPaper());
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

        paperRenderer.material.color = Color.Lerp(Color.white, Color.red, urgency);

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
        paperRenderer.material.color = Color.white;
        paperRenderer.transform.localRotation = Quaternion.identity;
    }

    // --- Paper Shatter ---

    private IEnumerator ShatterPaper()
    {
        if (paperRenderer == null) yield break;

        Vector3 paperPos = paperRenderer.transform.position;
        paperRenderer.gameObject.SetActive(false);

        List<GameObject> pieces = new List<GameObject>();

        for (int i = 0; i < 8; i++)
        {
            GameObject piece;
            if (shatterPiecePrefab != null)
            {
                piece = Instantiate(shatterPiecePrefab, paperPos, Quaternion.identity);
            }
            else
            {
                piece = CreateDefaultPiece(paperPos);
            }

            Rigidbody2D rb = piece.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 force = new Vector2(Random.Range(-3f, 3f), Random.Range(1f, 5f));
                rb.AddForce(force, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-100f, 100f));
            }

            pieces.Add(piece);
        }

        yield return new WaitForSeconds(2f);

        foreach (GameObject piece in pieces)
        {
            if (piece != null)
                Destroy(piece);
        }
    }

    private GameObject CreateDefaultPiece(Vector3 position)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
        piece.transform.position = position;
        piece.transform.localScale = new Vector3(0.05f, 0.08f, 0.01f);
        piece.GetComponent<Renderer>().material.color = new Color(0.9f, 0.85f, 0.8f);

        piece.AddComponent<BoxCollider2D>();
        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;

        Collider col3d = piece.GetComponent<Collider>();
        if (col3d != null) Destroy(col3d);

        return piece;
    }
}
