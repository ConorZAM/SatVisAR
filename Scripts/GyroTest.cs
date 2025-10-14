using UnityEngine;

public class GyroTest : MonoBehaviour
{
    UnityEngine.InputSystem.AttitudeSensor m_Gyro;

    void Start()
    {
        //Set up and enable the gyroscope (check your device has one)
        m_Gyro = UnityEngine.InputSystem.AttitudeSensor.current;
    }

    //This is a legacy function, check out the UI section for other ways to create your UI
    void OnGUI()
    {
        //Output the rotation rate, attitude and the enabled state of the gyroscope as a Label
        //GUI.Label(new Rect(10, 10, 200, 40), "Gyro rotation rate " + m_Gyro.rotationRate);
        GUI.Label(new Rect(10, 60, 200, 40), "Gyro attitude" + m_Gyro.attitude.ReadValue());
        GUI.Label(new Rect(10, 110, 200, 40), "Gyro enabled : " + m_Gyro.enabled);
    }
}