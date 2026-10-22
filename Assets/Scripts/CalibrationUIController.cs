
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalibrationUIController : MonoBehaviour
{
    [Header("Вікно калібрування")]
    public GameObject calibrationPanel;
    public TMP_Text instructionsText;

    [Header("Кнопки")]
    public Button startButton;
    public Button cancelButton;

    [Header("Контролер апаратури")]
    public HardwareSettingsController hardwareController;

    private void Start()
    {
        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(StartCalibration);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelCalibration);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartCalibration);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelCalibration);
    }

    public void OpenCalibration()
    {
        if (calibrationPanel != null)
            calibrationPanel.SetActive(true);

        if (instructionsText != null)
        {
            instructionsText.text =
                "1. Постав стіки в нейтральне положення.\n\n" +
                "2. Натисни «Почати калібрування».\n\n" +
                "3. Протягом 5 секунд рухай стіками в усі крайні положення.";
        }
    }

    private void StartCalibration()
    {
        if (hardwareController == null)
        {
            if (instructionsText != null)
                instructionsText.text =
                    "Помилка: HardwareSettingsController не призначений.";

            return;
        }

        hardwareController.BeginCalibration();

        if (instructionsText != null)
        {
            instructionsText.text =
                "КАЛІБРУВАННЯ...\n\n" +
                "Рухай усіма стіками в усі крайні положення.";
        }
    }

    private void CancelCalibration()
    {
        if (hardwareController != null)
            hardwareController.CancelCalibration();

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);
    }
}
