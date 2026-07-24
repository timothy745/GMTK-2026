using UnityEngine;

public class MazeInteractable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    public bool IsPlayerInRange { get; private set; }
    public bool IsLocked { get; set; }

    private CircleCollider2D triggerCollider;

    void Awake()
    {
        SetupTriggerCollider();
    }

    void OnValidate()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = interactionRadius;
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (!IsPlayerInRange) return;
        if (IsLocked) return;

        if (Input.GetKeyDown(interactKey))
        {
            MazeManager manager = GetComponent<MazeManager>();
            if (manager != null && !manager.IsMazeActive())
            {
                manager.OpenMaze();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IsPlayerInRange = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    void OnGUI()
    {
        if (MazeManager.IsAnyMazeActive) return;

        if (IsLocked)
        {
            Vector3 worldPos = transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            GUIStyle lockedStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            lockedStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 15, 120, 25),
                "[Locked]", lockedStyle);
            return;
        }

        if (IsPlayerInRange)
        {
            Vector3 worldPos = transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            GUIStyle promptStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(screenPos.x - 60, Screen.height - screenPos.y - 15, 120, 25),
                "[E] Open Maze", promptStyle);
        }
    }

    private void SetupTriggerCollider()
    {
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null)
            triggerCollider = gameObject.AddComponent<CircleCollider2D>();

        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionRadius;
    }
}
