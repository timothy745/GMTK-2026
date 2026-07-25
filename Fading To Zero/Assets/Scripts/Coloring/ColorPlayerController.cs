using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ColorPlayerController : MonoBehaviour
{
    private ColorRenderer colorRenderer;
    private ColorManager colorManager;
    private bool isActive;
    private bool wasPainting;

    public void Initialize(ColorRenderer renderer, ColorManager manager)
    {
        colorRenderer = renderer;
        colorManager = manager;
        isActive = true;
        wasPainting = false;
    }

    public void Deactivate()
    {
        isActive = false;
    }

    void Update()
    {
        if (!isActive) return;
        if (Mouse.current == null) return;

        bool mouseDown = Mouse.current.leftButton.wasPressedThisFrame;
        bool mouseHeld = Mouse.current.leftButton.isPressed;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mouseDown)
        {
            for (int i = 0; i < colorRenderer.PaletteRects.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(colorRenderer.PaletteRects[i], mousePos, null))
                {
                    colorRenderer.SelectPaletteColor(i);
                    wasPainting = false;
                    return;
                }
            }

            if (colorManager.SubmitButtonRect != null &&
                RectTransformUtility.RectangleContainsScreenPoint(colorManager.SubmitButtonRect, mousePos, null))
            {
                colorManager.OnSubmitClicked();
                wasPainting = false;
                return;
            }

            PaintAtPosition(mousePos);
            wasPainting = true;
            return;
        }

        if (mouseHeld && wasPainting)
        {
            PaintAtPosition(mousePos);
        }

        if (!mouseHeld)
        {
            wasPainting = false;
        }
    }

    private void PaintAtPosition(Vector2 mousePos)
    {
        for (int y = 0; y < colorRenderer.GridSize; y++)
        {
            for (int x = 0; x < colorRenderer.GridSize; x++)
            {
                Image cell = colorRenderer.GridCells[x, y];
                if (RectTransformUtility.RectangleContainsScreenPoint(cell.rectTransform, mousePos, null))
                {
                    colorRenderer.PaintCell(x, y);
                    return;
                }
            }
        }
    }
}
