using UnityEngine;
using UnityEngine.SceneManagement; // Wajib ditambahkan untuk manajemen scene

public class MainMenuManager : MonoBehaviour
{
    // Method harus public agar bisa dibaca oleh Button
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
