using UnityEngine;

namespace MiniGameTemplate.Utils
{
    /// <summary>
    /// Common math utility methods.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Remap a value from one range to another.
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }

        /// <summary>
        /// Check if a value is approximately equal to another (float comparison).
        /// </summary>
        public static bool Approximately(float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// Wrap an angle to the range [0, 360).
        /// </summary>
        public static float WrapAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Clamp an integer to a range.
        /// </summary>
        public static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
