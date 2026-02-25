using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class UserLocationManager : MonoBehaviour
{
    public Vector3 globeNormal;
    public Vector3 eastDirection; // Assuming the user isn't at one of the poles...
    public Vector3 northTangent;
    public Quaternion worldRotation;

    public float userLat, userLon, userAlt;
    public float initialHeading;
    public string alignmentResult;
    public Vector3 userPositionECEF;

    public float desiredAccuracyInMeters = 10f;
    public float updateDistanceInMeters = 10f;

    public bool skipLocationServiceForTesting = false;

    private void Start()
    {
        UpdateOrientation();
    }

    public void UpdateOrientation()
    {

        // Calling this here in case the align view fails to pick up any sensors and returns early
        UpdateWorldRotation();

        if (skipLocationServiceForTesting)
        {
            alignmentResult = "Skipped for testing";
            return;
        }

        StartCoroutine(AlignView());
    }

    public Transform cam;
    public void Update()
    {
        if (skipLocationServiceForTesting)
        {
            cam.rotation = worldRotation;
        }

    }

    private void OnValidate()
    {
        UpdateWorldRotation();
    }

    public void SetManualHeading(float heading)
    {
        initialHeading = heading;
        UpdateWorldRotation();
    }

    public void UpdateWorldRotation()
    {
        // Convert viewer geodetic position to ECEF
        Vector3d obsECEF = GeoUtils.LLAtoECEF(userLat, userLon, userAlt);
        userPositionECEF = new Vector3((float)obsECEF.x, (float)obsECEF.z, (float)obsECEF.y);

        //origin.transform.rotation = Quaternion.Euler(new Vector3((float)viewerLat - 90f, (float)viewerLon - 90f, 0)) * Quaternion.Euler(0, -initialHeading, 0);
        globeNormal = Vector3.Normalize(userPositionECEF);

        // Assuming the user isn't at one of the poles...
        //eastDirection = new Vector3((float)Math.Cos(viewerLon * Mathf.Deg2Rad), 0, (float)Math.Sin(viewerLon * Mathf.Deg2Rad));
        eastDirection = Vector3.Normalize(new Vector3(globeNormal.z, globeNormal.y, globeNormal.x));

        northTangent = Quaternion.AngleAxis(-initialHeading, globeNormal).normalized * Vector3.Normalize(Vector3.Cross(globeNormal, eastDirection));
        //origin.transform.rotation = Quaternion.LookRotation(northTangent, globeNormal);

        worldRotation = Quaternion.LookRotation(northTangent, globeNormal);
    }

    //void UpdateWorldRotation()
    //{
    //    // Convert viewer geodetic position to ECEF
    //    Vector3d obsECEF = GeoUtils.LLAtoECEF(userLat, userLon, userAlt);
    //    userPositionECEF = new Vector3((float)obsECEF.x, (float)obsECEF.z, (float)obsECEF.y);

    //    globeNormal = Vector3.Normalize(userPositionECEF);

    //    float latRad = userLat * Mathf.Deg2Rad;
    //    float lonRad = userLon * Mathf.Deg2Rad;

    //    // ECEF -> Unity axis swap: (x, y, z) = (X, Z, Y)
    //    Vector3 east = new Vector3(-Mathf.Sin(lonRad), 0f, Mathf.Cos(lonRad));
    //    Vector3 north = new Vector3(
    //        -Mathf.Sin(latRad) * Mathf.Cos(lonRad),
    //        Mathf.Cos(latRad),
    //        -Mathf.Sin(latRad) * Mathf.Sin(lonRad));

    //    eastDirection = east.normalized;

    //    // Apply heading (clockwise from north) around local up
    //    northTangent = (Quaternion.AngleAxis(-initialHeading, globeNormal) * north).normalized;

    //    worldRotation = Quaternion.LookRotation(northTangent, globeNormal);
    //}

    IEnumerator AlignView()
    {
        alignmentResult = "Initialising";
        //yield return null;
        // Request permission if not granted
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
            yield return new WaitForSeconds(1f);
        }

        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location not enabled on device or app does not have permission to access location");
            alignmentResult = "Location not enabled";
            yield return null;
        }

        // Starts the location service.
        Debug.LogWarning("Starting location service");

        Input.compass.enabled = true;

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
            alignmentResult = "Location service timed out";
            Debug.LogError(alignmentResult);
            Input.location.Stop();
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            alignmentResult = "Unable to determine device location";
            Debug.LogError(alignmentResult);
            Input.location.Stop();
            yield break;
        }

        alignmentResult = "Getting compass reading";

        // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
        Debug.Log($"Location: {Input.location.lastData.latitude}, {Input.location.lastData.longitude}, {Input.location.lastData.altitude}, {Input.location.lastData.horizontalAccuracy}, {Input.location.lastData.timestamp}");
        Debug.Log($"Heading: {Input.compass.trueHeading}");
        userLat = Input.location.lastData.latitude;
        userLon = Input.location.lastData.longitude;
        userAlt = Input.location.lastData.altitude / 1000f;

        // Let the compass wake up - seems to take longer than GPS?
        maxWait = 20;
        while (Input.compass.trueHeading == 0 && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        initialHeading = Input.compass.trueHeading;

        alignmentResult = "Success";

        Input.location.Stop();
        Input.compass.enabled = false;

        UpdateWorldRotation();

        yield return null;
    }

    public float gizmoLength = 2f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (gizmoLength * globeNormal));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (gizmoLength * eastDirection));

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (gizmoLength * northTangent));
    }
}
