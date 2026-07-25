using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MazeParticles : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 40;
    [SerializeField] private float moveSpeed = 1500f;
    [SerializeField] private Sprite particleSprite;

    private List<ParticleData> particles = new List<ParticleData>();
    private bool isActive;
    private Canvas targetCanvas;
    private float screenW;
    private float screenH;
    private Color baseColor = Color.white;

    private struct ParticleData
    {
        public GameObject obj;
        public RectTransform rect;
        public Image img;
        public float speed;
        public float flickerOffset;
        public float flickerSpeed;
    }

    public void SetActive(bool active, Canvas canvas)
    {
        isActive = active;
        targetCanvas = canvas;

        if (active)
        {
            screenW = Screen.width;
            screenH = Screen.height;

            if (particles.Count == 0 && canvas != null)
                SpawnParticles(canvas);

            foreach (var p in particles)
            {
                if (p.obj != null)
                {
                    p.obj.transform.SetParent(canvas.transform, false);
                    p.obj.SetActive(true);
                }
            }
        }
        else
        {
            foreach (var p in particles)
            {
                if (p.obj != null)
                    p.obj.SetActive(false);
            }
        }
    }

    public void SetActive(bool active)
    {
        SetActive(active, targetCanvas);
    }

    public void SetTintColor(Color color)
    {
        baseColor = color;
    }

    void Update()
    {
        if (!isActive) return;

        float halfH = screenH / 2f;
        float fadeTop = -halfH * 0.3f;
        float spawnY = -halfH - 20f;
        float time = Time.time;

        for (int i = 0; i < particles.Count; i++)
        {
            var p = particles[i];
            if (p.obj == null || !p.obj.activeSelf) continue;

            Vector2 pos = p.rect.anchoredPosition;
            pos.y += p.speed * Time.deltaTime;

            // Flicker alpha like real embers
            float flicker = Mathf.Sin(time * p.flickerSpeed + p.flickerOffset) * 0.3f + 0.7f;
            float fade = Mathf.InverseLerp(fadeTop, spawnY, pos.y);
            float alpha = Mathf.Clamp01(fade) * flicker;

            Color c = baseColor;
            c.a = alpha;
            p.img.color = c;

            if (pos.y > fadeTop)
            {
                pos.y = spawnY;
                pos.x = Random.Range(-screenW * 0.45f, screenW * 0.45f);
            }

            p.rect.anchoredPosition = pos;
        }
    }

    private void SpawnParticles(Canvas canvas)
    {
        float halfH = screenH / 2f;
        float spawnY = -halfH - 20f;
        float fadeTop = -halfH * 0.3f;

        GameObject container = new GameObject("ParticlesContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = Vector2.zero;

        for (int i = 0; i < particleCount; i++)
        {
            GameObject obj = new GameObject("Ember_" + i);
            obj.transform.SetParent(container.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float size = Random.Range(12f, 20f);
            rect.sizeDelta = new Vector2(size, size);

            Image img = obj.AddComponent<Image>();
            img.raycastTarget = false;

            if (particleSprite != null)
            {
                img.sprite = particleSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }

            img.color = Color.white;

            float x = Random.Range(-screenW * 0.45f, screenW * 0.45f);
            float y = Random.Range(spawnY, fadeTop);
            rect.anchoredPosition = new Vector2(x, y);

            particles.Add(new ParticleData
            {
                obj = obj,
                rect = rect,
                img = img,
                speed = moveSpeed * Random.Range(0.7f, 1.3f),
                flickerOffset = Random.Range(0f, 6.28f),
                flickerSpeed = Random.Range(8f, 15f)
            });
        }
    }
}
