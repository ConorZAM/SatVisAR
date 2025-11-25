using UnityEngine;
using UnityEngine.InputSystem;

public class SensorAlignment : MonoBehaviour
{
    public UserLocationManager userLocationManager;
    //AttitudeSensor rotationVector;
    public float initialBeta = 0.2f;
    public float settleTime = 2f;
    public float beta = 0.02f;
    MadgwickAHRS filter = new MadgwickAHRS();

    UnityEngine.InputSystem.Gyroscope gyro;
    Accelerometer acc;

    Quaternion rotationFix = Quaternion.Euler(-90, 0, 0);

    void SetBeta()
    {
        filter.Beta = beta;
    }

    void Start()
    {
        gyro = UnityEngine.InputSystem.Gyroscope.current;
        acc = Accelerometer.current;

        if (gyro != null)
        {
            InputSystem.EnableDevice(gyro);
        }

        if (acc != null)
        {
            InputSystem.EnableDevice(acc);
        }

        filter.Beta = initialBeta;
        Invoke(nameof(SetBeta), settleTime);

        if (acc != null)
        {
            Vector3 a = acc.acceleration.ReadValue();
            filter.Quaternion = rotationFix * Quaternion.LookRotation(a);
        }

        //AttitudeSensor rotationVector = AndroidGameRotationVector.current;// AndroidRotationVector.current;
        //if (rotationVector != null)
        //{
        //    InputSystem.EnableDevice(rotationVector);

        //    // Read Android orientation one time
        //    Quaternion android = rotationVector.attitude.ReadValue();

        //    // Convert to Unity coordinates (same as earlier)
        //    Quaternion unity = new Quaternion(android.x, android.y, -android.z, -android.w);
        //    unity = Quaternion.Euler(-90, 0, 0) * unity;

        //    // Warm start Madgwick
        //    filter.Quaternion = android;
        //}
        //else if (acc != null)
        //{
        //    Vector3 a = acc.acceleration.ReadValue();

        //    // Tilt from gravity
        //    Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, a).normalized;
        //    Vector3 up = -a.normalized;

        //    Quaternion tilt = Quaternion.LookRotation(forward, up);

        //    // Zero yaw or choose your own heading
        //    Vector3 e = tilt.eulerAngles;
        //    e.y = 0f;

        //    filter.Quaternion = Quaternion.Euler(e);
        //}
    }

    //Vector3 DeviceForwardDirection()
    //{
    //    if (rotationVector == null)
    //    {
    //        return Vector3.forward;
    //    }

    //    Quaternion rotation = rotationVector.attitude.ReadValue(); // cameraTransform.rotation;
    //    return rotation * new Vector3(0, 0, 1);
    //}

    //(Quaternion, Vector3) DeviceOrientationAndDirection()
    //{
    //    if (rotationVector == null)
    //    {
    //        return (Quaternion.identity, Vector3.forward);
    //    }

    //    Quaternion rotation = ConvertAndroidToUnity(rotationVector.attitude.ReadValue()); // cameraTransform.rotation;
    //    return (rotation, rotation * new Vector3(0, 0, 1));
    //}

    Quaternion ConvertAndroidToUnity(Quaternion q)
    {
        // Android delivers rotation where Z points toward the user
        // Unity expects Z forward (away from user)
        Quaternion androidToUnity = new Quaternion(q.x, q.y, -q.z, -q.w);

        // Additional correction to align device orientation with Unity's camera
        return rotationFix * androidToUnity;
    }

    //public float smooth = 10;
    //Quaternion smoothedRotation = Quaternion.identity;

    // Update is called once per frame
    void Update()
    {
        if (gyro == null || acc == null)
        {
            return;
        }

        Vector3 g = gyro.angularVelocity.ReadValue();
        Vector3 a = acc.acceleration.ReadValue();

        filter.Update(g.x, g.y, g.z, a.x, a.y, a.z, Time.deltaTime);

        transform.rotation = userLocationManager.worldRotation * ConvertAndroidToUnity(filter.Quaternion);

        // ==================================================================
        // I think this needs the world rotation applied to it as well!
        // ==================================================================

        //if (rotationVector != null)
        //{
        //    Quaternion raw = ConvertAndroidToUnity(rotationVector.attitude.ReadValue()); // your existing converted quaternion

        //    if (smoothedRotation == Quaternion.identity)
        //    {
        //        smoothedRotation = raw;
        //    }

        //    smoothedRotation = Quaternion.Slerp(smoothedRotation, raw, Time.deltaTime * smooth);

        //    transform.rotation = userLocationManager.worldRotation * smoothedRotation;

        //    //transform.rotation = userLocationManager.worldRotation * ConvertAndroidToUnity(rotationVector.attitude.ReadValue());
        //}

    }
}
