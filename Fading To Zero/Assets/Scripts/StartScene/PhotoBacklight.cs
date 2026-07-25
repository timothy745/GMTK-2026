using UnityEngine;

public class PhotoBacklight : MonoBehaviour
{
    [Header("Pulse Settings")]
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float pulseSpeed = 1.2f;

    [Header("Color")]
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.8f, 1f);

    private SpriteRenderer glowRenderer;
    private float baseIntensity = 1f;

    void Awake()
    {
        glowRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (glowRenderer == null) return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = glowColor;
        c.a = alpha * baseIntensity;
        glowRenderer.color = c;
    }
}
