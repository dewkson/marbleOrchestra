using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Shared math for fitting an orthographic camera to a world-space
    /// Bounds at an arbitrary rotation: projects the bounds' 8 corners onto
    /// the camera's own right/up/forward axes to get exact extents along
    /// each, regardless of how the camera or the framed content happen to
    /// be oriented in the scene. Used by both CameraFitter (2D top-down
    /// planning view) and CameraModeTransition (isometric 3D view) - see
    /// 0029, where the 2D view stopped being a fixed front-on X/Y case the
    /// old hardcoded math assumed.
    /// </summary>
    public static class BoundsCameraMath
    {
        public readonly struct Extents
        {
            public readonly float Right;
            public readonly float Up;
            public readonly float Forward;

            public Extents(float right, float up, float forward)
            {
                Right = right;
                Up = up;
                Forward = forward;
            }
        }

        /// Half-extent of bounds along each of rotation's right/up/forward
        /// axes - i.e. how far the bounds reach from its own center in
        /// each direction, not the world-axis-aligned Bounds.extents.
        public static Extents MeasureExtents(Bounds bounds, Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;

            Vector3 center = bounds.center;
            float maxRight = 0f, maxUp = 0f, maxForward = 0f;

            foreach (Vector3 corner in EnumerateCorners(bounds))
            {
                Vector3 d = corner - center;
                maxRight = Mathf.Max(maxRight, Mathf.Abs(Vector3.Dot(d, right)));
                maxUp = Mathf.Max(maxUp, Mathf.Abs(Vector3.Dot(d, up)));
                maxForward = Mathf.Max(maxForward, Mathf.Abs(Vector3.Dot(d, forward)));
            }

            return new Extents(maxRight, maxUp, maxForward);
        }

        public static IEnumerable<Vector3> EnumerateCorners(Bounds bounds)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;

            yield return new Vector3(min.x, min.y, min.z);
            yield return new Vector3(min.x, min.y, max.z);
            yield return new Vector3(min.x, max.y, min.z);
            yield return new Vector3(min.x, max.y, max.z);
            yield return new Vector3(max.x, min.y, min.z);
            yield return new Vector3(max.x, min.y, max.z);
            yield return new Vector3(max.x, max.y, min.z);
            yield return new Vector3(max.x, max.y, max.z);
        }
    }
}
