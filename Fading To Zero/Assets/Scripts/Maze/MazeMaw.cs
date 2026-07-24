using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MazeMaw : MonoBehaviour
{
    private Image mawImage;
    private RectTransform mawRect;

    public void Setup(Canvas canvas, Sprite sprite)
    {
        GameObject mawObj = new GameObject("MawOverlay");
        mawObj.transform.SetParent(canvas.transform, false);

        mawRect = mawObj.AddComponent<RectTransform>();
        mawRect.anchorMin = Vector2.zero;
        mawRect.anchorMax = Vector2.one;
        mawRect.offsetMin = Vector2.zero;
        mawRect.offsetMax = Vector2.zero;

        mawImage = mawObj.AddComponent<Image>();
        mawImage.sprite = sprite;
        mawImage.type = Image.Type.Simple;
        mawImage.preserveAspect = true;
        mawImage.raycastTarget = false;
        mawImage.color = new Color(1f, 1f, 1f, 0f);

        mawObj.transform.SetAsFirstSibling();
    }

    public void SetVisibility(float urgency)
    {
        if (mawImage == null) return;

        Color c = mawImage.color;
        c.a = Mathf.Clamp01(urgency);
        mawImage.color = c;
    }

    public void ResetVisibility()
    {
        if (mawImage == null) return;

        Color c = mawImage.color;
        c.a = 0f;
        mawImage.color = c;
    }

    public Coroutine PlayJumpscare(Sprite maw2, Sprite maw3, System.Action onSequenceDone)
    {
        return StartCoroutine(JumpscareSequence(maw2, maw3, onSequenceDone));
    }

    private IEnumerator JumpscareSequence(Sprite maw2, Sprite maw3, System.Action onSequenceDone)
    {
        if (mawImage == null) yield break;

        Camera cam = Camera.main;
        Vector3 originalCamPos = cam != null ? cam.transform.position : Vector3.zero;

        // Maw2 - flash on full alpha with shake
        mawImage.sprite = maw2;
        SetVisibility(1f);
        mawImage.transform.SetAsLastSibling();
        yield return StartCoroutine(ShakeWhileVisible(0.15f, 0.15f, originalCamPos));

        // Maw3 - swap and hold longer with shake
        mawImage.sprite = maw3;
        yield return StartCoroutine(ShakeWhileVisible(0.5f, 0.2f, originalCamPos));

        // Fade out
        float elapsed = 0f;
        float fadeDuration = 0.2f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            Color c = mawImage.color;
            c.a = a;
            mawImage.color = c;
            yield return null;
        }

        if (cam != null)
            cam.transform.position = originalCamPos;

        ResetVisibility();
        onSequenceDone?.Invoke();
    }

    private IEnumerator ShakeWhileVisible(float duration, float intensity, Vector3 originalCamPos)
    {
        Camera cam = Camera.main;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (cam != null)
            {
                float offsetX = Random.Range(-intensity, intensity);
                float offsetY = Random.Range(-intensity, intensity);
                cam.transform.position = originalCamPos + new Vector3(offsetX, offsetY, 0f);
            }

            yield return null;
        }

        if (cam != null)
            cam.transform.position = originalCamPos;
    }
}
