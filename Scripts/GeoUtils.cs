using System;
using UnityEngine;

public static class GeoUtils
{
    // WGS-84 (km)
    const double a = 6378.137;                // semi-major axis [km]
    const double f = 1.0 / 298.257223563;
    const double e2 = f * (2 - f);             // eccentricity^2
    const double OMEGA_E = 7.2921150e-5;       // Earth rotation [rad/s]

    // ---------- LLA → ECEF (km) ----------
    public static Vector3d LLAtoECEF(double latDeg, double lonDeg, double altKm)
    {
        double lat = latDeg * Mathf.Deg2Rad;
        double lon = lonDeg * Mathf.Deg2Rad;

        double sLat = Math.Sin(lat), cLat = Math.Cos(lat);
        double sLon = Math.Sin(lon), cLon = Math.Cos(lon);

        double N = a / Math.Sqrt(1.0 - (e2 * sLat * sLat));

        double x = (N + altKm) * cLat * cLon;
        double y = (N + altKm) * cLat * sLon;
        double z = ((N * (1.0 - e2)) + altKm) * sLat;

        return new Vector3d(x, y, z);
    }

    // ---------- ECEF pos → ENU pos (km) ----------
    public static Vector3 ECEFposToENU(Vector3d satECEF, double obsLatDeg, double obsLonDeg, double obsAltKm)
    {
        Vector3d rObs = LLAtoECEF(obsLatDeg, obsLonDeg, obsAltKm);
        Vector3d d = satECEF - rObs; // relative vector sat - observer (ECEF, km)

        double lat = obsLatDeg * Mathf.Deg2Rad;
        double lon = obsLonDeg * Mathf.Deg2Rad;

        double sLat = Math.Sin(lat), cLat = Math.Cos(lat);
        double sLon = Math.Sin(lon), cLon = Math.Cos(lon);

        double east = (-sLon * d.x) + (cLon * d.y);
        double north = (-sLat * cLon * d.x) - (sLat * sLon * d.y) + (cLat * d.z);
        double up = (cLat * cLon * d.x) + (cLat * sLon * d.y) + (sLat * d.z);

        return new Vector3((float)east, (float)north, (float)up);
    }

    // ---------- ECEF vel → ENU vel (km/s), relative to rotating observer ----------
    // satVelECEF: satellite velocity components expressed in the ECEF frame [km/s]
    // Returns velocity of satellite relative to observer, expressed in ENU [km/s]
    public static Vector3 ECEFvelToENU(Vector3d satECEF, Vector3d satVelECEF,
                                       double obsLatDeg, double obsLonDeg, double obsAltKm)
    {
        // Observer ECEF position
        Vector3d rObs = LLAtoECEF(obsLatDeg, obsLonDeg, obsAltKm);

        // Observer ECEF velocity due to Earth rotation: v_obs = ω × r_obs
        // ω = (0, 0, OMEGA_E) rad/s; r in km → v in km/s
        Vector3d vObs = new Vector3d(
            -OMEGA_E * rObs.y,
             OMEGA_E * rObs.x,
             0.0
        );

        // Relative velocity in ECEF
        Vector3d vRelECEF = satVelECEF - vObs;

        // Rotate ECEF → ENU (same rotation as for position; no translation needed)
        double lat = obsLatDeg * Mathf.Deg2Rad;
        double lon = obsLonDeg * Mathf.Deg2Rad;

        double sLat = Math.Sin(lat), cLat = Math.Cos(lat);
        double sLon = Math.Sin(lon), cLon = Math.Cos(lon);

        double east = (-sLon * vRelECEF.x) + (cLon * vRelECEF.y);
        double north = (-sLat * cLon * vRelECEF.x) - (sLat * sLon * vRelECEF.y) + (cLat * vRelECEF.z);
        double up = (cLat * cLon * vRelECEF.x) + (cLat * sLon * vRelECEF.y) + (sLat * vRelECEF.z);

        return new Vector3((float)east, (float)north, (float)up);
    }

    // ---------- Convenience: unit direction of velocity in ENU ----------
    public static Vector3 ECEFvelDirectionENU(Vector3d satECEF, Vector3d satVelECEF,
                                              double obsLatDeg, double obsLonDeg, double obsAltKm)
    {
        Vector3 vENU = ECEFvelToENU(satECEF, satVelECEF, obsLatDeg, obsLonDeg, obsAltKm);
        float m = vENU.magnitude;
        return m > 1e-9f ? vENU / m : Vector3.zero;
    }
}

// Lightweight double-precision vector (km)
public struct Vector3d
{
    public double x, y, z;
    public Vector3d(double X, double Y, double Z)
    {
        x = X;
        y = Y;
        z = Z;
    }

    public static Vector3d operator -(Vector3d a, Vector3d b)
    {
        return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
    }
}

