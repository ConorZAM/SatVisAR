using System.Collections;
using UnityEngine;
public class AlignToSensorData : MonoBehaviour
{

    // Observer at (lat, lon, alt)
    double viewerLat = 51.5074;   // London
    double viewerLon = -0.1278;
    double viewerAlt = 0.030;      // km
    float trueNorthDegrees;
    IEnumerator Start()
    {
        Input.compass.enabled = true;

        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled on device or app does not have permission to access location");
        }
        else
        {
            GetLocation();
        }

        // Get gravity vector from accelerometer (points down in device space)
        //Vector3 deviceGravity = Input.acceleration.normalized;

        //Debug.Log(deviceGravity);

        // We want device gravity to align with Unity's -Y (down)
        //calibrationRotation = Quaternion.FromToRotation(deviceGravity, Vector3.down);

        // Convert viewer geodetic position to ECEF
        //Vector3d obsECEF = GeoUtils.LLAtoECEF(viewerLat, viewerLon, viewerAlt);
        //Vector3 unityObsPos = new Vector3((float)obsECEF.x, (float)obsECEF.z, (float)obsECEF.y);

        //// Compute ENU basis
        //double latRad = viewerLat * Mathf.Deg2Rad;
        //double lonRad = viewerLon * Mathf.Deg2Rad;

        //Vector3 east = new Vector3((float)-Mathf.Sin((float)lonRad), (float)Mathf.Cos((float)lonRad), 0);
        //Vector3 north = new Vector3(
        //    (float)(-Mathf.Sin((float)latRad) * Mathf.Cos((float)lonRad)),
        //    (float)(-Mathf.Sin((float)latRad) * Mathf.Sin((float)lonRad)),
        //    (float)Mathf.Cos((float)latRad)
        //);
        //Vector3 up = new Vector3(
        //    (float)(Mathf.Cos((float)latRad) * Mathf.Cos((float)lonRad)),
        //    (float)(Mathf.Cos((float)latRad) * Mathf.Sin((float)lonRad)),
        //    (float)Mathf.Sin((float)latRad)
        //);

        //Debug.Log(north);
        //Debug.Log(up);

        //FindFirstObjectByType<XROrigin>().MatchOriginUpOriginForward(north.normalized, up.normalized);

        //Quaternion enuRotation = Quaternion.LookRotation(up.normalized, north.normalized);

        // Apply transforms
        //transform.position = -unityObsPos;
        //transform.rotation = calibrationRotation * enuRotation;
        //transform.up = unityObsPos.normalized;

        //transform.rotation = Quaternion.Inverse(enuRotation);

        // This one looked promising, I'm just trying to include the compass as well
        //transform.eulerAngles = new Vector3((float)viewerLat - 90f, (float)viewerLon - 90f, 0);

        // Wait for decent compass
        yield return new WaitForSeconds(10);

        trueNorthDegrees = Input.compass.trueHeading;
        //transform.rotation = Quaternion.Euler(0, -90, 0) * Quaternion.Euler(new Vector3((float)viewerLat - 90f, (float)viewerLon - 90f, 0));
        transform.rotation = Quaternion.Euler(0, -trueNorthDegrees, 0) * Quaternion.Euler(new Vector3((float)viewerLat - 90f, (float)viewerLon - 90f, 0));

        //if (AttitudeSensor.current != null)
        //{
        //    InputSystem.EnableDevice(AttitudeSensor.current);

        //    int maxWait = 20;
        //    while (AttitudeSensor.current.attitude.ReadValue() == Quaternion.identity && maxWait > 0)
        //    {
        //        yield return new WaitForSeconds(1);
        //        maxWait--;
        //    }

        //    transform.rotation = Quaternion.Inverse(AttitudeSensor.current.attitude.ReadValue()) * Quaternion.Euler(new Vector3((float)viewerLat - 90f, (float)viewerLon - 90f, 0));
        //    InputSystem.DisableDevice(AttitudeSensor.current);
        //}

        Input.location.Stop();
        Input.compass.enabled = false;

        yield return null;
    }

    IEnumerator GetLocation()
    {
        // Starts the location service.

        float desiredAccuracyInMeters = 10f;
        float updateDistanceInMeters = 10f;

        Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);

        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            Debug.Log("Timed out");
            Input.location.Stop();
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            Input.location.Stop();
            yield break;
        }
        else
        {
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

            viewerLat = Input.location.lastData.latitude;
            viewerLon = Input.location.lastData.longitude;
            viewerAlt = Input.location.lastData.altitude / 1000f;
        }
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 110, 200, 40), "True North (deg) : " + trueNorthDegrees.ToString());

    }
}
