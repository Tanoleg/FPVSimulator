
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void StartFlight() => SceneManager.LoadScene("FlightScene");

    public void OpenSettings() => SceneManager.LoadScene("SettingsScene");

    public void ExitGame() => Application.Quit();
}
