
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody))]
public class FPVDronePhysics : MonoBehaviour
{
    [Header("TX12: інверсія")]
    public bool invertThrottle;
    public bool invertYaw;
    public bool invertPitch;
    public bool invertRoll;

    [Header("Тяга")]
    public float maxThrust = 14f;

    [Header("Acro: кутова швидкість, градусів/сек")]
    public float maxRollRate = 250f;
    public float maxPitchRate = 250f;
    public float maxYawRate = 180f;

    [Header("Відгук")]
    [Range(0f, 1f)]
    public float stickExpo = 0.25f;

    public float angularAcceleration = 800f;
    [Range(0f, 0.2f)]
    public float deadzone = 0.04f;

    [Header("Підключення")]
    public bool showDebug = true;

    private Rigidbody rb;
    private Joystick tx12;

    private AxisControl axisX;
    private AxisControl axisY;
    private AxisControl axisZ;
    private AxisControl axisRX;

    private float throttle;
    private float yaw;
    private float pitch;
    private float roll;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        LoadInversions();
        FindTX12();
    }

    private void LoadInversions()
    {
        invertThrottle =
            PlayerPrefs.GetInt("TX12_InvertThrottle", 0) == 1;

        invertYaw =
            PlayerPrefs.GetInt("TX12_InvertYaw", 0) == 1;

        invertPitch =
            PlayerPrefs.GetInt("TX12_InvertPitch", 0) == 1;

        invertRoll =
            PlayerPrefs.GetInt("TX12_InvertRoll", 0) == 1;
    }

    private void FindTX12()
    {
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
            Debug.LogError("[FPV] TX12 не знайдено.");
            return;
        }

        foreach (var control in tx12.allControls)
        {
            if (!(control is AxisControl axis))
                continue;

            switch (axis.name.ToLowerInvariant())
            {
                case "x": axisX = axis; break;
                case "y": axisY = axis; break;
                case "z": axisZ = axis; break;
                case "rx": axisRX = axis; break;
            }
        }

        Debug.Log($"[FPV] Підключено: {tx12.displayName}");
    }

    private float ReadAxis(AxisControl axis)
    {
        return axis != null ? axis.ReadValue() : 0f;
    }

    private float ApplyDeadzone(float value)
    {
        if (Mathf.Abs(value) < deadzone)
            return 0f;

        float sign = Mathf.Sign(value);
        float magnitude =
            (Mathf.Abs(value) - deadzone) / (1f - deadzone);

        return sign * Mathf.Clamp01(magnitude);
    }

    private float ApplyExpo(float value)
    {
        float expo = Mathf.Clamp01(stickExpo);

        return (1f - expo) * value
             + expo * value * value * value;
    }

    private void Update()
    {
        if (tx12 == null)
            return;

        throttle = ReadAxis(axisZ);
        yaw = ReadAxis(axisRX);
        pitch = ReadAxis(axisY);
        roll = ReadAxis(axisX);

        if (invertThrottle) throttle = -throttle;
        if (invertYaw) yaw = -yaw;
        if (invertPitch) pitch = -pitch;
        if (invertRoll) roll = -roll;

        yaw = ApplyExpo(ApplyDeadzone(yaw));
        pitch = ApplyExpo(ApplyDeadzone(pitch));
        roll = ApplyExpo(ApplyDeadzone(roll));
    }

    private void FixedUpdate()
    {
        if (tx12 == null)
            return;

        // Перетворення газу з -1..1 у 0..1.
        float throttle01 = Mathf.Clamp01((throttle + 1f) * 0.5f);

        // Тяга діє вздовж вертикальної осі корпуса.
        rb.AddForce(
            transform.up * throttle01 * maxThrust,
            ForceMode.Force
        );

        // Acro: стіки задають швидкість обертання,
        // а не цільовий горизонтальний кут.
        Vector3 targetLocalAngularVelocity = new Vector3(
            pitch * maxPitchRate,
            yaw * maxYawRate,
            -roll * maxRollRate
        ) * Mathf.Deg2Rad;

        // Поточна кутова швидкість у локальних координатах.
        Vector3 currentLocalAngularVelocity =
            transform.InverseTransformDirection(rb.angularVelocity);

        // Плавне досягнення заданої кутової швидкості.
        Vector3 newLocalAngularVelocity = Vector3.MoveTowards(
            currentLocalAngularVelocity,
            targetLocalAngularVelocity,
            angularAcceleration * Mathf.Deg2Rad *
            Time.fixedDeltaTime
        );

        rb.angularVelocity =
            transform.TransformDirection(newLocalAngularVelocity);

        if (showDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log(
                $"[FPV] Газ: {throttle:F2}, " +
                $"Roll: {roll:F2}, Pitch: {pitch:F2}, Yaw: {yaw:F2}"
            );
        }
    }
}
