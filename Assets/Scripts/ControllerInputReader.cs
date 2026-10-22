
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

public class ControllerInputReader : MonoBehaviour
{
    private Gamepad selectedGamepad;
    private Joystick selectedJoystick;

    private void Start()
    {
        DetectController();
    }

    private void DetectController()
    {
        selectedGamepad = null;
        selectedJoystick = null;

        foreach (var device in InputSystem.devices)
        {
            // Ігноруємо клавіатуру та мишу
            if (device is Gamepad gamepad)
            {
                selectedGamepad = gamepad;
                Debug.Log($"[ПУЛЬТ] Знайдено Gamepad: {gamepad.displayName}");
                return;
            }

            if (device is Joystick joystick)
            {
                selectedJoystick = joystick;
                Debug.Log($"[ПУЛЬТ] Знайдено Joystick: {joystick.displayName}");
                return;
            }
        }

        Debug.LogWarning("[ПУЛЬТ] Gamepad або Joystick не знайдено. Перевір USB-підключення та режим пульта.");
    }

    private void Update()
    {
        if (selectedGamepad != null)
        {
            ReadGamepad();
        }
        else if (selectedJoystick != null)
        {
            ReadJoystick();
        }
    }

    private void ReadGamepad()
    {
        ReadAxis("Left Stick X", selectedGamepad.leftStick.x.ReadValue());
        ReadAxis("Left Stick Y", selectedGamepad.leftStick.y.ReadValue());
        ReadAxis("Right Stick X", selectedGamepad.rightStick.x.ReadValue());
        ReadAxis("Right Stick Y", selectedGamepad.rightStick.y.ReadValue());
    }

    private void ReadJoystick()
    {
        foreach (var control in selectedJoystick.allControls)
        {
            if (control is AxisControl axis)
            {
                ReadAxis(axis.name, axis.ReadValue());
            }
        }
    }

    private readonly System.Collections.Generic.Dictionary<string, float> previousValues = new();

    private void ReadAxis(string axisName, float value)
    {
        if (!previousValues.TryGetValue(axisName, out float previous))
        {
            previousValues[axisName] = value;
            return;
        }

        if (Mathf.Abs(value - previous) >= 0.05f)
        {
            previousValues[axisName] = value;
            Debug.Log($"[ПУЛЬТ] {axisName}: {value:F2}");
        }
    }
}
