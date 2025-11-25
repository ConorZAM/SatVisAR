using UnityEngine;

public class MadgwickAHRS
{
    public float Beta = 0.1f; // filter gain
    public Quaternion Quaternion = Quaternion.identity;

    private float invSqrt(float x)
    {
        return 1.0f / Mathf.Sqrt(x);
    }

    public void Update(float gx, float gy, float gz, float ax, float ay, float az, float deltaTime)
    {
        float q1 = Quaternion.w;
        float q2 = Quaternion.x;
        float q3 = Quaternion.y;
        float q4 = Quaternion.z;

        // Normalise accelerometer
        float norm = Mathf.Sqrt((ax * ax) + (ay * ay) + (az * az));
        if (norm < 1e-12f)
        {
            return;
        }

        norm = 1.0f / norm;
        ax *= norm;
        ay *= norm;
        az *= norm;

        // Gradient descent algorithm corrective step
        float f1 = (2 * ((q2 * q4) - (q1 * q3))) - ax;
        float f2 = (2 * ((q1 * q2) + (q3 * q4))) - ay;
        float f3 = (2 * (0.5f - (q2 * q2) - (q3 * q3))) - az;

        float J_11or24 = 2 * q3;
        float J_12or23 = 2 * q4;
        float J_13or22 = 2 * q1;
        float J_14or21 = 2 * q2;

        float grad1 = (J_14or21 * f2) - (J_11or24 * f1);
        float grad2 = (J_12or23 * f1) + (J_13or22 * f2);
        float grad3 = (J_12or23 * f2) - (J_13or22 * f1);
        float grad4 = (J_14or21 * f1) + (J_11or24 * f2);

        norm = Mathf.Sqrt((grad1 * grad1) + (grad2 * grad2) + (grad3 * grad3) + (grad4 * grad4));
        norm = 1.0f / norm;

        grad1 *= norm;
        grad2 *= norm;
        grad3 *= norm;
        grad4 *= norm;

        // Gyro is in rad/s
        float halfDt = 0.5f * deltaTime;

        float qDot1 = (0.5f * ((-q2 * gx) - (q3 * gy) - (q4 * gz))) - (Beta * grad1);
        float qDot2 = (0.5f * ((q1 * gx) + (q3 * gz) - (q4 * gy))) - (Beta * grad2);
        float qDot3 = (0.5f * ((q1 * gy) - (q2 * gz) + (q4 * gx))) - (Beta * grad3);
        float qDot4 = (0.5f * ((q1 * gz) + (q2 * gy) - (q3 * gx))) - (Beta * grad4);

        q1 += qDot1 * deltaTime;
        q2 += qDot2 * deltaTime;
        q3 += qDot3 * deltaTime;
        q4 += qDot4 * deltaTime;

        // Normalise quaternion
        norm = Mathf.Sqrt((q1 * q1) + (q2 * q2) + (q3 * q3) + (q4 * q4));
        norm = 1.0f / norm;

        Quaternion = new Quaternion(q2 * norm, q3 * norm, q4 * norm, q1 * norm);
    }
}
