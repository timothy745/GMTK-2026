using UnityEngine;

public class StartSceneSetup : MonoBehaviour
{
    [Header("Portal Position")]
    [SerializeField] private Vector3 portalOffset = new Vector3(-4f, 0f, 0f);

    void Awake()
    {
        SetupPhoto();
        SetupPortal();
    }

    private void SetupPhoto()
    {
        GameObject photo = GameObject.Find("photo_0");
        if (photo == null)
        {
            Debug.LogWarning("StartSceneSetup: photo_0 not found");
            return;
        }

        if (photo.GetComponent<PhotoBob>() == null)
            photo.AddComponent<PhotoBob>();

        if (photo.GetComponentInChildren<PhotoBacklight>() == null)
        {
            GameObject glowObj = new GameObject("PhotoGlow");
            glowObj.transform.SetParent(photo.transform, false);
            glowObj.transform.localPosition = new Vector3(0f, 0f, 1f);
            glowObj.transform.localScale = new Vector3(5f, 5f, 1f);

            SpriteRenderer photoSr = photo.GetComponent<SpriteRenderer>();
            SpriteRenderer glowSr = glowObj.AddComponent<SpriteRenderer>();
            glowSr.sprite = CreateCircleSprite();
            glowSr.color = new Color(1f, 0.9f, 0.7f, 0.3f);
            if (photoSr != null)
                glowSr.sortingLayerID = photoSr.sortingLayerID;
            glowSr.sortingOrder = -1;

            glowObj.AddComponent<PhotoBacklight>();
        }
    }

    private void SetupPortal()
    {
        if (GameObject.Find("FigmaPortal") != null) return;

        GameObject photo = GameObject.Find("photo_0");
        Vector3 portalPos = Vector3.zero;
        if (photo != null)
            portalPos = photo.transform.position + portalOffset;

        GameObject portal = new GameObject("FigmaPortal");
        portal.transform.position = portalPos;

        SpriteRenderer sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.8f, 0.6f, 1f, 0.8f);
        sr.sortingOrder = 5;

        BoxCollider2D col = portal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 3f);

        portal.AddComponent<StartScenePortal>();
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.filterMode = FilterMode.Point;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = size * 0.5f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist / radius));
                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, alpha));
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
