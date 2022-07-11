using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using CameraSettings = MonoBehaviourPublicAcInCaInTeShInMaBoInUnique;

namespace CameraAnimation
{
    public class StoreTransform
    {
        public GameObject Photocamera;

        public Vector3 Position => Photocamera.transform.position;

        public Vector4 Rotation => new Vector4(Photocamera.transform.localRotation.x, Photocamera.transform.localRotation.y,
                                                Photocamera.transform.localRotation.z, Photocamera.transform.localRotation.w);

        public float FocalLength => _cachedCamera.focalLength;
        public Vector2 LensShift => _cachedCamera.lensShift;

        public Vector2 SensorSize => _cachedCamera.sensorSize;

        public float Aperture
        {
            get
            {
                if(CameraAnimationMod.AperatureProps.Count==0)
                    return 0;
                return (float) CameraAnimationMod.AperatureProps[0].GetValue(_cachedSettings);
            }
        }
        public float FocalDistance
        {
            get
            {
                if (CameraAnimationMod.FocalDistanceProps.Count == 0)
                    return 0;
                return (float)CameraAnimationMod.FocalDistanceProps[0].GetValue(_cachedSettings);
            }
        }

        public bool KeyPosition { get; set; } = true;
        public bool KeyRotation { get; set; } = true;
        public bool KeyZoom { get; set; } = true;
        public bool KeyFocus { get; set; } = true;

        public bool Pickupable
        {
            get
            {
                return _pickupable;
            }
            set
            {
                setCameraPickupable(value, Photocamera);
                _pickupable = value;
            }
        }

        private bool _pickupable;

        private readonly Camera _cachedCamera;
        private readonly CameraSettings _cachedSettings;
        private readonly Action<bool, GameObject> setCameraPickupable;

        public StoreTransform(GameObject camera, Action<bool, GameObject> setCameraPickupable)
        {
            Photocamera = camera;
            _cachedCamera = Photocamera.GetComponent<Camera>();
            _cachedSettings = Photocamera.GetComponent<CameraSettings>();
            this.setCameraPickupable = setCameraPickupable;
        }

        public void Serialize(StringBuilder builder)
        {
            Position.Serialize(builder);
            Rotation.Serialize(builder);
            FocalLength.Serialize(builder, ';');
            LensShift.Serialize(builder);
            SensorSize.Serialize(builder);
            Aperture.Serialize(builder);
            FocalDistance.Serialize(builder);
            KeyPosition.Serialize(builder);
            KeyRotation.Serialize(builder);
            KeyZoom.Serialize(builder);
            KeyFocus.Serialize(builder);
        }
    }
}