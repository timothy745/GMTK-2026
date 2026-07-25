using System.Collections;
using UnityEngine;
using TMPro;

public class CountdownMotionBlur : MonoBehaviour
{
    [Header("Countdown")]
    [SerializeField] private int defaultFrom = 3;
    [SerializeField] private int defaultTo = 0;
    [SerializeField] private float holdBeforeRoll = 0.15f;
    [SerializeField] private float rollDuration = 1.5f;
    [SerializeField] private float holdAfterRoll = 0.15f;

    [Header("Fade Scale")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 1.5f;
    [SerializeField] private float startScale = 8f;
    [SerializeField] private float endScale = 8f;

    [Header("Motion Blur")]
    [SerializeField] private int blurCopies = 8;
    [SerializeField] private float blurSpacing = 15f;
    [SerializeField] private float blurAlphaFalloff = 0.7f;

    private TextMeshProUGUI mainText;
    private TextMeshProUGUI[] blurTexts;

    void Awake()
    {
        mainText = GetComponent<TextMeshProUGUI>();
        CreateBlurLayers();
    }

    public void StartCountdown()
    {
        StartCoroutine(RunCountdown(defaultFrom, defaultTo));
    }

    public void StartCountdown(int from, int to)
    {
        StartCoroutine(RunCountdown(from, to));
    }

    private IEnumerator RunCountdown(int from, int to)
    {
        if (mainText == null) yield break;

        yield return StartCoroutine(FadeScaleIn(from.ToString()));
        yield return new WaitForSeconds(holdBeforeRoll);
        yield return StartCoroutine(RollToNumber(to));
        yield return new WaitForSeconds(holdAfterRoll);
        yield return StartCoroutine(FadeScaleOut(to.ToString()));

        ClearAll();
    }

    private IEnumerator FadeScaleIn(string numStr)
    {
        ClearBlurLayers();

        mainText.text = numStr;
        mainText.alpha = 0f;
        mainText.rectTransform.localScale = Vector3.one * startScale;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float ease = EaseOutCubic(t);

            mainText.alpha = t;
            mainText.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, ease);

            yield return null;
        }

        mainText.alpha = 1f;
        mainText.rectTransform.localScale = Vector3.one;
    }

    private IEnumerator FadeScaleOut(string numStr)
    {
        ClearBlurLayers();

        mainText.text = numStr;
        mainText.alpha = 1f;
        mainText.rectTransform.localScale = Vector3.one;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float ease = EaseInQuad(t);

            mainText.alpha = 1f - t;
            mainText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, endScale, ease);

            yield return null;
        }

        mainText.alpha = 0f;
    }

    private IEnumerator RollToNumber(int target)
    {
        string targetStr = target.ToString();
        float elapsed = 0f;

        while (elapsed < rollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rollDuration);

            int digit = t >= 1f ? target : Random.Range(0, 10);
            string numStr = digit.ToString();

            mainText.text = numStr;
            mainText.alpha = 1f;

            float offsetY = Mathf.Lerp(40f, 0f, EaseOutCubic(t));
            mainText.rectTransform.anchoredPosition = new Vector2(0f, offsetY);

            UpdateBlurLayers(numStr, EaseOutCubic(t), t);

            yield return null;
        }

        mainText.text = targetStr;
        mainText.rectTransform.anchoredPosition = Vector2.zero;

        foreach (var b in blurTexts)
            if (b != null) { b.text = targetStr; b.alpha = 0f; }
    }

    private void CreateBlurLayers()
    {
        if (mainText == null) return;

        blurTexts = new TextMeshProUGUI[blurCopies];

        for (int i = 0; i < blurCopies; i++)
        {
            GameObject obj = new GameObject($"Blur_{i}");
            obj.transform.SetParent(mainText.transform.parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = mainText.rectTransform.anchorMin;
            rt.anchorMax = mainText.rectTransform.anchorMax;
            rt.pivot = mainText.rectTransform.pivot;
            rt.sizeDelta = mainText.rectTransform.sizeDelta;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.font = mainText.font;
            tmp.fontSize = mainText.fontSize;
            tmp.alignment = mainText.alignment;
            tmp.color = Color.clear;
            tmp.raycastTarget = false;

            blurTexts[i] = tmp;
        }

        for (int i = 0; i < blurCopies; i++)
            blurTexts[i].transform.SetAsFirstSibling();
        mainText.transform.SetAsLastSibling();
    }

    private void UpdateBlurLayers(string text, float easeProgress, float linearProgress)
    {
        for (int i = 0; i < blurTexts.Length; i++)
        {
            float layerT = (float)i / blurCopies;
            float alpha = Mathf.Pow(blurAlphaFalloff, i) * Mathf.Lerp(0.6f, 0f, linearProgress);
            float offsetY = blurSpacing * (blurCopies - i) * Mathf.Lerp(0.5f, 0.2f, easeProgress);
            float scaleX = Mathf.Lerp(1.1f, 1f, easeProgress) - layerT * 0.05f;

            blurTexts[i].text = text;
            blurTexts[i].alpha = alpha;
            blurTexts[i].rectTransform.anchoredPosition = new Vector2(0f, offsetY);
            blurTexts[i].rectTransform.localScale = new Vector3(scaleX, 1f, 1f);
        }
    }

    private void ClearBlurLayers()
    {
        if (blurTexts == null) return;
        foreach (var b in blurTexts)
            if (b != null) { b.text = ""; b.alpha = 0f; }
    }

    private void ClearAll()
    {
        mainText.text = "";
        mainText.alpha = 0f;
        foreach (var b in blurTexts)
            if (b != null) { b.text = ""; b.alpha = 0f; }
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInQuad(float t) => t * t;
}
