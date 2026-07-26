using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Load any scene by passing its exact name
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Reload the active scene (Great for Restart buttons)
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Exit the game application
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Exited"); // Visible only in the Unity Editor
    }
}