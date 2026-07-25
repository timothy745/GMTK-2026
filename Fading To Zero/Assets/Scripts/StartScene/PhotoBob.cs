using UnityEngine;

public class PhotoBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private float amplitude = 0.3f;
    [SerializeField] private float speed = 1.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
    }
}
