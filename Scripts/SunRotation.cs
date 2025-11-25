using System;
using UnityEngine;

public class SunRotation : MonoBehaviour
{
    public UserLocationManager userLocation;

    // Start is called before the first frame update
    void Start()
    {
        // Periodically update the sun's rotation
        InvokeRepeating(nameof(UpdateSunRotation), 60f, 60f);
    }

    public Light sunLight;

    void UpdateSunRotation()
    {
        DateTime utcNow = DateTime.UtcNow;
        (double azimuth, double elevation) = GetSunPosition(utcNow, userLocation.userLat, userLocation.userLon);
        Vector3 sunDirection = SunDirectionFromAzEl(azimuth, elevation);
        sunLight.transform.rotation = Quaternion.LookRotation(sunDirection);
    }

    /// <summary>
    /// Calculate Sun azimuth and elevation (degrees) from UTC and location.
    /// Uses simplified NOAA algorithm.
    /// </summary>
    private (double azimuth, double elevation) GetSunPosition(DateTime utcTime, double lat, double lon)
    {
        // Convert to Julian Day
        int year = utcTime.Year;
        int month = utcTime.Month;
        int day = utcTime.Day;
        double hour = utcTime.Hour + (utcTime.Minute / 60.0) + (utcTime.Second / 3600.0);

        double d = (367 * year) - (7 * (year + ((month + 9) / 12)) / 4) +
                   (275 * month / 9) + day - 730530 + (hour / 24.0);

        // Mean anomaly of the Sun
        double M = (357.5291 + (0.98560028 * d)) % 360;
        if (M < 0)
        {
            M += 360;
        }

        // Mean longitude of the Sun
        double L = (280.459 + (0.98564736 * d)) % 360;
        if (L < 0)
        {
            L += 360;
        }

        // Ecliptic longitude
        double lambda = L + (1.915 * Math.Sin(M * Mathf.Deg2Rad)) + (0.020 * Math.Sin(2 * M * Mathf.Deg2Rad));

        // Obliquity of the ecliptic
        double epsilon = 23.439 - (0.00000036 * d);

        // Right ascension
        double alpha = Math.Atan2(Math.Cos(epsilon * Mathf.Deg2Rad) * Math.Sin(lambda * Mathf.Deg2Rad),
                                  Math.Cos(lambda * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
        if (alpha < 0)
        {
            alpha += 360;
        }

        // Declination
        double delta = Math.Asin(Math.Sin(epsilon * Mathf.Deg2Rad) * Math.Sin(lambda * Mathf.Deg2Rad)) * Mathf.Rad2Deg;

        // Sidereal time
        double GMST = (18.697374558 + (24.06570982441908 * d)) % 24;
        double LMST = ((GMST * 15) + lon) % 360;
        if (LMST < 0)
        {
            LMST += 360;
        }

        // Hour angle
        double H = LMST - alpha;
        if (H < -180)
        {
            H += 360;
        }

        if (H > 180)
        {
            H -= 360;
        }

        // Elevation
        double elevation = Math.Asin(
            (Math.Sin(lat * Mathf.Deg2Rad) * Math.Sin(delta * Mathf.Deg2Rad)) +
            (Math.Cos(lat * Mathf.Deg2Rad) * Math.Cos(delta * Mathf.Deg2Rad) * Math.Cos(H * Mathf.Deg2Rad))
        ) * Mathf.Rad2Deg;

        // Azimuth
        double azimuth = Math.Atan2(
            -Math.Sin(H * Mathf.Deg2Rad),
            (Math.Tan(delta * Mathf.Deg2Rad) * Math.Cos(lat * Mathf.Deg2Rad)) -
            (Math.Sin(lat * Mathf.Deg2Rad) * Math.Cos(H * Mathf.Deg2Rad))
        ) * Mathf.Rad2Deg;

        if (azimuth < 0)
        {
            azimuth += 360;
        }

        return (azimuth, elevation);
    }

    /// <summary>
    /// Convert azimuth & elevation (degrees) into Unity world direction.
    /// Azimuth: degrees clockwise from North.
    /// Elevation: degrees above horizon.
    /// </summary>
    private Vector3 SunDirectionFromAzEl(double azimuth, double elevation)
    {
        float azRad = Mathf.Deg2Rad * (float)azimuth;
        float elRad = Mathf.Deg2Rad * (float)elevation;

        // Convert spherical (az, el) to Cartesian (x, y, z)
        float x = Mathf.Cos(elRad) * Mathf.Sin(azRad);
        float y = Mathf.Sin(elRad);
        float z = Mathf.Cos(elRad) * Mathf.Cos(azRad);

        // Unity: Y up, Z forward
        return new Vector3(x, y, z);
    }
}
