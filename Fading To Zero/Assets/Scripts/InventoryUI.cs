using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Backpack Button")]
    [SerializeField] private Sprite backpackSprite;

    private Canvas inventoryCanvas;
    private GameObject inventoryPanel;
    private Transform itemGrid;
    private bool isOpen;

    public static bool IsOpen { get; private set; }

    private static List<InventoryItem> collectedItems = new List<InventoryItem>();

    private struct InventoryItem
    {
        public Sprite sprite;
        public string itemName;
    }

    void Awake()
    {
        SetupCanvas();
    }

    void Update()
    {
        if (MazeManager.IsAnyMazeActive || SortManager.IsAnySortActive || ColorManager.IsAnyColorActive) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (MazeManager.IsAnyMazeActive || SortManager.IsAnySortActive || ColorManager.IsAnyColorActive) return;

        isOpen = !isOpen;
        IsOpen = isOpen;
        inventoryPanel.SetActive(isOpen);

        if (isOpen)
            RefreshItemGrid();
    }

    public void AddItem(Sprite sprite, string itemName)
    {
        collectedItems.Add(new InventoryItem { sprite = sprite, itemName = itemName });
        StartCoroutine(ShowPickupPopup(sprite, itemName));
        TryPlayPhotoPieceAnimation(itemName);
    }

    public static int GetPhotoPieceCount()
    {
        int count = 0;
        foreach (var item in collectedItems)
        {
            if (item.itemName.Contains("Photo Piece"))
                count++;
        }
        return count;
    }

    private void TryPlayPhotoPieceAnimation(string itemName)
    {
        if (itemName.Contains("PhotoPiece_1"))
            PlayPhotoPieceCountdown(3, 2);
        else if (itemName.Contains("PhotoPiece_2"))
            PlayPhotoPieceCountdown(2, 1);
        else if (itemName.Contains("PhotoPiece_3"))
            PlayPhotoPieceCountdown(1, 0);
    }

    private void PlayPhotoPieceCountdown(int from, int to)
    {
        Canvas canvas = CreateOverlayCanvas("PhotoPieceCountdownCanvas", 201);

        GameObject tmpObj = new GameObject("CountdownText");
        tmpObj.transform.SetParent(canvas.transform, false);

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

        Destroy(canvas.gameObject, 8f);
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

    private IEnumerator ShowPickupPopup(Sprite sprite, string itemName)
    {
        GameObject popupCanvasObj = new GameObject("PopupCanvas");
        Canvas popupCanvas = popupCanvasObj.AddComponent<Canvas>();
        popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        popupCanvas.sortingOrder = 110;

        CanvasScaler popupScaler = popupCanvasObj.AddComponent<CanvasScaler>();
        popupScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        popupScaler.referenceResolution = new Vector2(1920, 1080);
        popupScaler.matchWidthOrHeight = 0.5f;

        popupCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject popup = new GameObject("PickupPopup");
        popup.transform.SetParent(popupCanvasObj.transform, false);

        RectTransform popupRect = popup.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = new Vector2(0f, -80f);
        popupRect.sizeDelta = new Vector2(400f, 120f);

        Image popupBg = popup.AddComponent<Image>();
        popupBg.color = new Color(0f, 0f, 0f, 0.85f);

        // Item icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(popup.transform, false);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(60f, 0f);
        iconRect.sizeDelta = new Vector2(80f, 80f);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = sprite;
        iconImg.type = Image.Type.Simple;
        iconImg.preserveAspect = true;

        // Item name text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(popup.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(120f, 0f);
        textRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = itemName;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        // Slide up and fade out
        float duration = 2.5f;
        float elapsed = 0f;
        Vector2 startPos = popupRect.anchoredPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            popupRect.anchoredPosition = Vector2.Lerp(startPos, startPos + Vector2.up * 40f, t);

            float alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            popupBg.color = new Color(0f, 0f, 0f, 0.85f * alpha);
            iconImg.color = new Color(1f, 1f, 1f, alpha);
            tmp.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        Destroy(popupCanvasObj);
    }

    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        float center = resolution / 2f;
        float radius = center - 1f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    private void RefreshItemGrid()
    {
        foreach (Transform child in itemGrid)
            Destroy(child.gameObject);

        if (collectedItems.Count == 0)
        {
            Transform placeholder = inventoryPanel.transform.Find("PlaceholderText");
            if (placeholder != null)
                placeholder.gameObject.SetActive(true);
            return;
        }

        Transform ph = inventoryPanel.transform.Find("PlaceholderText");
        if (ph != null)
            ph.gameObject.SetActive(false);

        for (int i = 0; i < collectedItems.Count; i++)
        {
            InventoryItem item = collectedItems[i];

            GameObject slot = new GameObject("Item_" + i);
            slot.transform.SetParent(itemGrid, false);

            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(100f, 120f);

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slot.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.3f);
            iconRect.anchorMax = new Vector2(1f, 1f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = item.sprite;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;

            // Name
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(slot.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.3f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
            nameTmp.text = item.itemName;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.fontSize = 16;
            nameTmp.color = Color.white;
            nameTmp.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        }
    }

    private void SetupCanvas()
    {
        // Canvas
        GameObject canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(transform, false);
        inventoryCanvas = canvasObj.AddComponent<Canvas>();
        inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        inventoryCanvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Backpack button (top-left)
        GameObject btnObj = new GameObject("BackpackButton");
        btnObj.transform.SetParent(canvasObj.transform, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = new Vector2(20f, -20f);
        btnRect.sizeDelta = new Vector2(100f, 100f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.raycastTarget = true;
        if (backpackSprite != null)
        {
            btnImg.sprite = backpackSprite;
            btnImg.type = Image.Type.Simple;
            btnImg.preserveAspect = true;
        }

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(ToggleInventory);

        // Inventory panel (hidden by default)
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = inventoryPanel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.8f);
        panelBg.raycastTarget = true;

        // Placeholder text
        GameObject textObj = new GameObject("PlaceholderText");
        textObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "INVENTORY\n\nPlaceholder - Figma not ready yet";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36;
        tmp.color = Color.white;
        tmp.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        tmp.raycastTarget = false;

        // Item grid
        GameObject gridObj = new GameObject("ItemGrid");
        gridObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.1f, 0.1f);
        gridRect.anchorMax = new Vector2(0.9f, 0.85f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(100f, 120f);
        grid.spacing = new Vector2(15f, 15f);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        itemGrid = gridObj.transform;

        inventoryPanel.SetActive(false);
    }
}
