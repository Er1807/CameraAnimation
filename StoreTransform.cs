using System;
using System.Globalization;
using System.Text;
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

        public bool KeyPosition { get; set; } = true;
        public bool KeyRotation { get; set; } = true;
        public bool KeyZoom { get; set; } = true;

        public bool Pickupable { 
            get {
                return _pickupable;
            } 
            set {
                setCameraPickupable(value, Photocamera);
                _pickupable = value;
            } 
        }

        private bool _pickupable;

        private readonly Camera _cachedCamera;
        private readonly Action<bool, GameObject> setCameraPickupable;

        public StoreTransform(GameObject camera, Action<bool, GameObject> setCameraPickupable)
        {
            Photocamera = camera;
            _cachedCamera = Photocamera.GetComponent<Camera>();
            this.setCameraPickupable = setCameraPickupable;
        }

        public void Serialize(StringBuilder builder)
        {
            Position.Serialize(builder);
            EulerAngles.Serialize(builder);
            FocalLength.Serialize(builder, ';');
            LensShift.Serialize(builder);
            SensorSize.Serialize(builder);
            KeyPosition.Serialize(builder);
            KeyRotation.Serialize(builder);
            KeyZoom.Serialize(builder);
        }
    }
}