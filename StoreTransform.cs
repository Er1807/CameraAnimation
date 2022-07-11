using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using CameraSettings = MonoBehaviourPublicAcInCaInTeShInMaBoInUnique;

namespace CameraAnimation
{
    public class StoreTransform
    {
        public GameObject Photocamera { get => _photoCamera; set => SetGameObject(value); }
        
        public Vector3 Position => Photocamera?.transform.position ?? _cachedPosition;
        public Vector4 Rotation => Photocamera?.transform.localRotation.ToVector4() ?? _cachedRotation.ToVector4();
        public float Aperture => GetAperture();
        public float FocalDistance => GetFocalDistance();

        public float FocalLength => _cachedCamera?.focalLength ?? _cachedFocalLength;
        public Vector2 LensShift => _cachedCamera?.lensShift ?? _cachedLensShift;
        public Vector2 SensorSize => _cachedCamera?.sensorSize ?? _cachedSensorSize;

        public bool KeyPosition { get; set; } = Settings.Keying.KeyPosition;
        public bool KeyRotation { get; set; } = Settings.Keying.KeyRotation;
        public bool KeyZoom { get; set; } = Settings.Keying.KeyZoom;
        public bool KeyFocus { get; set; } = Settings.Keying.KeyFocus;

        public bool Pickupable
        {
            get => _pickupable;
            set
            {
                SetCameraPickupable(value, Photocamera);
                _pickupable = value;
            }
        }

        public static Action<bool, GameObject> SetCameraPickupable { get; set; } = (x,y)=> throw new NotImplementedException();

        private GameObject _photoCamera;
        private bool _pickupable;
        private Camera _cachedCamera;
        private CameraSettings _cachedSettings;

        private readonly float _cachedAperture = Settings.Camera.DefaultAperture;
        private readonly float _cachedFocalDistance = Settings.Camera.DefaultFocalDistance;
        private readonly float _cachedFocalLength = Settings.Camera.DefaultFocalLength;
        private readonly Vector2 _cachedLensShift = Settings.Camera.DefaultLensShift;
        private readonly Vector2 _cachedSensorSize = Settings.Camera.DefaultSensorSize;
        private readonly Vector3 _cachedPosition;
        private readonly Quaternion _cachedRotation;

        public StoreTransform()
        {
        }

        public StoreTransform(float cachedAperture, float cachedFocalDistance, float cachedFocalLength, Vector2 cachedLensShift, Vector2 cachedSensorSize, Vector3 cachedPosition, Quaternion cachedRotation)
        {
            _cachedAperture = cachedAperture;
            _cachedFocalDistance = cachedFocalDistance;
            _cachedFocalLength = cachedFocalLength;
            _cachedLensShift = cachedLensShift;
            _cachedSensorSize = cachedSensorSize;
            _cachedPosition = cachedPosition;
            _cachedRotation = cachedRotation;
        }

        private void SetGameObject(GameObject value)
        {
            _photoCamera = value;
            _cachedCamera = Photocamera.GetComponent<Camera>();
            _cachedSettings = Photocamera.GetComponent<CameraSettings>();
        }

        private float GetFocalDistance()
        {
            if (CameraAnimationMod.FocalDistanceProps.Count == 0 || _cachedSettings is null)
                return _cachedFocalDistance;

            return (float)(CameraAnimationMod.FocalDistanceProps[0]?.GetValue(_cachedSettings) ?? Settings.Camera.DefaultFocalDistance);
        }

        private float GetAperture()
        {
            if (CameraAnimationMod.ApertureProps.Count == 0 || _cachedSettings is null)
                return _cachedAperture;

            return (float)(CameraAnimationMod.ApertureProps[0]?.GetValue(_cachedSettings) ?? Settings.Camera.DefaultAperture);
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