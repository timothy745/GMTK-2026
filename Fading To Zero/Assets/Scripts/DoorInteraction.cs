using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DoorInteraction : MonoBehaviour
{
    [Header("Scene Settings")]
    public string sceneToLoad = "Interior_House";
    public string targetSpawnPoint; // 👈 TAMBAHAN: Nama GameObject titik spawn di scene tujuan

    public static string nextSpawnPoint; // 👈 TAMBAHAN: Menyimpan variabel spawn antar-scene

    [Header("UI Settings")]
    public TextMeshProUGUI promptText; // Drag teks dari Hierarchy ke sini
    public string customMessage = "Press [E] to Enter House";

    private bool isPlayerNearby = false;

    void Start()
    {
        // Sembunyikan teks saat game baru mulai
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Kalau player ada di dekat wall dan tekan tombol E
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                nextSpawnPoint = targetSpawnPoint; // 👈 Simpan titik spawn sebelum pindah
                SceneManager.LoadScene(sceneToLoad);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = true;

            // MUNCULKAN TEKS
            if (promptText != null)
            {
                promptText.text = customMessage;
                promptText.gameObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearby = false;

            // HILANGKAN TEKS
            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
        }
    }
}