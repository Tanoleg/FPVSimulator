
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using TMPro;

public class HardwareSettingsController : MonoBehaviour
{
    [Header("Індикатори стіків")]
    public RectTransform leftKnob;
    public RectTransform rightKnob;
    public float stickRadius = 55f;

    [Header("Відображення осей")]
    public TMP_Text throttleText;
    public TMP_Text yawText;
    public TMP_Text pitchText;
    public TMP_Text rollText;

    [Header("Інверсія осей")]
    public Toggle invertThrottleToggle;
    public Toggle invertYawToggle;
    public Toggle invertPitchToggle;
    public Toggle invertRollToggle;

    [Header("Калібрування")]
    public Button calibrateButton;
    public TMP_Text calibrationStatusText;
    public float calibrationDuration = 5f;

    private Joystick tx12;

    private AxisControl axisX;
    private AxisControl axisY;
    private AxisControl axisZ;
    private AxisControl axisRX;

    private bool invertThrottle;
    private bool invertYaw;
    private bool invertPitch;
    private bool invertRoll;

    private bool isCalibrating;

    private class AxisCalibration
    {
        public float min = -1f;
        public float max = 1f;
        public float center = 0f;
    }

    private readonly AxisCalibration calX = new AxisCalibration();
    private readonly AxisCalibration calY = new AxisCalibration();
    private readonly AxisCalibration calZ = new AxisCalibration();
    private readonly AxisCalibration calRX = new AxisCalibration();

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;

        if (calibrateButton != null)
            calibrateButton.onClick.RemoveListener(BeginCalibration);

        StopAllCoroutines();
        isCalibrating = false;
    }

    private void Start()
    {
        LoadSettings();
        SetupToggles();

        if (calibrateButton != null)
            calibrateButton.onClick.AddListener(BeginCalibration);

        FindTX12();
    }

    private void OnDeviceChange(
        InputDevice device,
        InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Reconnected ||
            change == InputDeviceChange.Disconnected ||
            change == InputDeviceChange.Removed)
        {
            FindTX12();
        }
    }

    private void FindTX12()
    {
        tx12 = null;
        axisX = null;
        axisY = null;
        axisZ = null;
        axisRX = null;

        foreach (var device in InputSystem.devices)
        {
            if (device is Joystick joystick &&
                (joystick.displayName.Contains(
                    "TX12", System.StringComparison.OrdinalIgnoreCase) ||
                 joystick.displayName.Contains(
                    "OpenTX", System.StringComparison.OrdinalIgnoreCase) ||
                 joystick.displayName.Contains(
                    "EdgeTX", System.StringComparison.OrdinalIgnoreCase)))
            {
                tx12 = joystick;
                break;
            }
        }

        if (tx12 == null)
        {
            Debug.LogWarning("[Hardware] TX12 не знайдено.");

            foreach (var device in InputSystem.devices)
                Debug.Log($"Пристрій: {device.displayName}");

            SetStatus("Пульт не знайдено. Перевір USB-підключення.");
            return;
        }

        foreach (var control in tx12.allControls)
        {
            if (!(control is AxisControl axis))
                continue;

            switch (axis.name.ToLowerInvariant())
            {
                case "x":
                    axisX = axis;
                    break;
                case "y":
                    axisY = axis;
                    break;
                case "z":
                    axisZ = axis;
                    break;
                case "rx":
                    axisRX = axis;
                    break;
            }
        }

        Debug.Log($"[Hardware] Пульт: {tx12.displayName}");
        Debug.Log(
            $"Осі: X={axisX != null}, Y={axisY != null}, " +
            $"Z={axisZ != null}, RX={axisRX != null}");

        if (axisX == null || axisY == null ||
            axisZ == null || axisRX == null)
        {
            SetStatus("Пульт знайдено, але не всі осі доступні.");
        }
        else
        {
            SetStatus("TX12 підключено. Рухай стіками.");
        }
    }

    private void LoadSettings()
    {
        invertThrottle =
            PlayerPrefs.GetInt("TX12_InvertThrottle", 0) == 1;
        invertYaw =
            PlayerPrefs.GetInt("TX12_InvertYaw", 0) == 1;
        invertPitch =
            PlayerPrefs.GetInt("TX12_InvertPitch", 0) == 1;
        invertRoll =
            PlayerPrefs.GetInt("TX12_InvertRoll", 0) == 1;

        LoadCalibration("X", calX);
        LoadCalibration("Y", calY);
        LoadCalibration("Z", calZ);
        LoadCalibration("RX", calRX);
    }

    private void LoadCalibration(string key, AxisCalibration cal)
    {
        cal.min = PlayerPrefs.GetFloat("TX12_" + key + "_Min", -1f);
        cal.max = PlayerPrefs.GetFloat("TX12_" + key + "_Max", 1f);
        cal.center = PlayerPrefs.GetFloat("TX12_" + key + "_Center", 0f);
    }

    private void SaveCalibration(string key, AxisCalibration cal)
    {
        PlayerPrefs.SetFloat("TX12_" + key + "_Min", cal.min);
        PlayerPrefs.SetFloat("TX12_" + key + "_Max", cal.max);
        PlayerPrefs.SetFloat("TX12_" + key + "_Center", cal.center);
    }

    private void SetupToggles()
    {
        SetupToggle(
            invertThrottleToggle, invertThrottle,
            "TX12_InvertThrottle", value => invertThrottle = value);

        SetupToggle(
            invertYawToggle, invertYaw,
            "TX12_InvertYaw", value => invertYaw = value);

        SetupToggle(
            invertPitchToggle, invertPitch,
            "TX12_InvertPitch", value => invertPitch = value);

        SetupToggle(
            invertRollToggle, invertRoll,
            "TX12_InvertRoll", value => invertRoll = value);
    }

    private void SetupToggle(
        Toggle toggle,
        bool initialValue,
        string key,
        System.Action<bool> setter)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(initialValue);
        toggle.onValueChanged.AddListener(value =>
        {
            setter(value);
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        });
    }

    private float ReadRaw(AxisControl axis)
    {
        return axis != null ? axis.ReadValue() : 0f;
    }

    private float NormalizeCentered(float raw, AxisCalibration cal)
    {
        float halfRange = Mathf.Max(
            (cal.max - cal.min) * 0.5f, 0.01f);

        return Mathf.Clamp(
            (raw - cal.center) / halfRange, -1f, 1f);
    }

    private float NormalizeThrottle(float raw)
    {
        float range = Mathf.Max(calZ.max - calZ.min, 0.01f);
        float value = Mathf.Clamp01((raw - calZ.min) / range);

        return invertThrottle ? 1f - value : value;
    }

    private void Update()
    {
        if (tx12 == null)
            return;

        float rawX = ReadRaw(axisX);
        float rawY = ReadRaw(axisY);
        float rawZ = ReadRaw(axisZ);
        float rawRX = ReadRaw(axisRX);

        if (isCalibrating)
            UpdateCalibration(rawX, rawY, rawZ, rawRX);

        float roll = NormalizeCentered(rawX, calX);
        float pitch = NormalizeCentered(rawY, calY);
        float yaw = NormalizeCentered(rawRX, calRX);
        float throttle = NormalizeThrottle(rawZ);

        if (invertRoll)
            roll = -roll;

        if (invertPitch)
            pitch = -pitch;

        if (invertYaw)
            yaw = -yaw;

        if (leftKnob != null)
        {
            leftKnob.anchoredPosition =
                new Vector2(yaw, throttle * 2f - 1f) * stickRadius;
        }

        if (rightKnob != null)
        {
            rightKnob.anchoredPosition =
                new Vector2(roll, pitch) * stickRadius;
        }

        SetAxisText(throttleText, "Газ", throttle, true);
        SetAxisText(yawText, "Yaw", yaw);
        SetAxisText(pitchText, "Pitch", pitch);
        SetAxisText(rollText, "Roll", roll);
    }

    private void SetAxisText(
        TMP_Text label,
        string axisName,
        float value,
        bool isThrottle = false)
    {
        if (label == null)
            return;

        int percent = Mathf.RoundToInt(value * 100f);

        label.text = isThrottle
            ? $"{axisName}: {percent}%"
            : $"{axisName}: {percent:+0;-0;0}%";
    }

    public void BeginCalibration()
    {
        if (tx12 == null)
        {
            FindTX12();

            if (tx12 == null)
            {
                SetStatus("Спочатку підключи TX12.");
                return;
            }
        }

        if (isCalibrating)
            return;

        StartCoroutine(CalibrationRoutine());
    }

    public void CancelCalibration()
    {
        if (!isCalibrating)
            return;

        StopAllCoroutines();
        isCalibrating = false;
        SetStatus("Калібрування скасовано.");
    }

    private IEnumerator CalibrationRoutine()
    {
        isCalibrating = true;

        SetStatus(
            "Калібрування! Рухай стіками в усі крайні положення.");

        float rawX = ReadRaw(axisX);
        float rawY = ReadRaw(axisY);
        float rawZ = ReadRaw(axisZ);
        float rawRX = ReadRaw(axisRX);

        ResetCalibrationRange(calX, rawX);
        ResetCalibrationRange(calY, rawY);
        ResetCalibrationRange(calZ, rawZ);
        ResetCalibrationRange(calRX, rawRX);

        float elapsed = 0f;

        while (elapsed < calibrationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        FinishCalibration();
    }

    private void ResetCalibrationRange(
        AxisCalibration cal, float initialValue)
    {
        cal.min = initialValue;
        cal.max = initialValue;
        cal.center = initialValue;
    }

    private void UpdateCalibration(
        float x, float y, float z, float rx)
    {
        UpdateRange(calX, x);
        UpdateRange(calY, y);
        UpdateRange(calZ, z);
        UpdateRange(calRX, rx);
    }

    private void UpdateRange(AxisCalibration cal, float value)
    {
        cal.min = Mathf.Min(cal.min, value);
        cal.max = Mathf.Max(cal.max, value);
    }

    private void FinishCalibration()
    {
        if (calX.max - calX.min < 0.1f ||
            calY.max - calY.min < 0.1f ||
            calZ.max - calZ.min < 0.1f ||
            calRX.max - calRX.min < 0.1f)
        {
            isCalibrating = false;
            SetStatus(
                "Калібрування не вдалося: рухай усіма стіками.");
            return;
        }

        calX.center = (calX.min + calX.max) * 0.5f;
        calY.center = (calY.min + calY.max) * 0.5f;
        calRX.center = (calRX.min + calRX.max) * 0.5f;

        SaveCalibration("X", calX);
        SaveCalibration("Y", calY);
        SaveCalibration("Z", calZ);
        SaveCalibration("RX", calRX);

        PlayerPrefs.Save();

        isCalibrating = false;

        SetStatus("Калібрування завершено. Налаштування збережено.");
        Debug.Log("[Hardware] Калібрування завершено.");
    }

    private void SetStatus(string message)
    {
        Debug.Log("[Hardware] " + message);

        if (calibrationStatusText != null)
            calibrationStatusText.text = message;
    }
}
