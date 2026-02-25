using UnityEngine;
using UnityEngine.InputSystem;

public class SensorAlignment : MonoBehaviour
{
    public UserLocationManager userLocationManager;
    //AttitudeSensor rotationVector;
    public float initialBeta = 0.2f;
    public float settleTime = 10f;
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

        TryWarmStart();
    }

    Quaternion ConvertAndroidToUnity(Quaternion q)
    {
        // Android delivers rotation where Z points toward the user
        // Unity expects Z forward (away from user)
        Quaternion androidToUnity = new Quaternion(q.x, q.y, -q.z, -q.w);

        // Additional correction to align device orientation with Unity's camera
        return rotationFix * androidToUnity;
    }

    Vector3 ConvertAndroidToUnity(Vector3 v)
    {
        Vector3 androidToUnity = new Vector3(v.x, v.y, -v.z);
        return rotationFix * androidToUnity;
    }

    Quaternion ConvertUnityToAndroid(Quaternion q)
    {
        Quaternion unityToAndroid = Quaternion.Inverse(rotationFix) * q;
        return new Quaternion(unityToAndroid.x, unityToAndroid.y, -unityToAndroid.z, -unityToAndroid.w);
    }

    void TryWarmStart()
    {
        if (acc == null)
        {
            return;
        }

        Vector3 a = ConvertAndroidToUnity(acc.acceleration.ReadValue());
        if (a.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 up = -a.normalized;

        Vector3 referenceForward = userLocationManager != null
            ? userLocationManager.worldRotation * Vector3.forward
            : Vector3.forward;

        Vector3 forward = Vector3.ProjectOnPlane(referenceForward, up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(Vector3.forward, up);
        }

        Quaternion deviceToWorld = Quaternion.LookRotation(forward.normalized, up);
        filter.Quaternion = ConvertUnityToAndroid(deviceToWorld);
    }

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
