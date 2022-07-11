using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public static class Settings
    {
        public static class Camera {
            public static float DefaultFieldOfView { get; set; } = 50.0f;
            public static float DefaultAperture { get; set; } = 15.0f;
            public static float DefaultFocalDistance { get; set; } = 1.5f;
            public static float DefaultFocalLength { get; set; }
            public static Vector2 DefaultSensorSize { get; set; } = new Vector2(70, 51);
            public static Vector2 DefaultLensShift { get; set; } = new Vector2(0, 0);
        }

        public static class Keying
        {
            public static bool KeyPosition { get; set; } = true;
            public static bool KeyRotation { get; set; } = true;
            public static bool KeyZoom { get; set; } = true;
            public static bool KeyFocus { get; set; } = true;
            public static float DefaultMaxTimeBetweenKeyFrames { get; set; } = 5f;
            public static float MaxTimeBetweenKeyFrames { get; set; } = 5f;
            public static float DefaultMinTimeBetweenKeyFrames { get; set; } = 0.2f;
            public static float MinTimeBetweenKeyFrames { get; set; } = 0.2f;
        }

        public static class UI
        {
            public static bool ShowPath { get; set; } = true;
            public static float PathWidth { get; set; } = 0.025f;
        }

        public static class Interaction
        {
            public static bool AllowCameraPickup { get; set; } = false;
        }

        public static class Animation
        {
            public static float Speed { get; set; } = 0.5f;
        }
    }
}
