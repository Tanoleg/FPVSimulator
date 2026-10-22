
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class FlightMenuController : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string flightSceneName = "FlightScene";

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartFlight();
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartFlight()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(flightSceneName);
    }
}
