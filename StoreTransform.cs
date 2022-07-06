using System;
using UnityEngine;

namespace CameraAnimation
{
    public class StoreTransform
    {
        public GameObject Photocamera;

        public Vector3 Position => Photocamera.transform.position;

        public Vector3 EulerAngles => Photocamera.transform.eulerAngles;
        public Vector3 EulerAnglesCorrected;

        public float FocalLength => _cachedCamera.focalLength;
        public Vector2 LensShift => _cachedCamera.lensShift;

        public Vector2 SensorSize => _cachedCamera.sensorSize;


        private readonly Camera _cachedCamera;

        public StoreTransform(GameObject camera)
        {
            Photocamera = camera;
            _cachedCamera = Photocamera.GetComponent<Camera>();
        }
    }
}