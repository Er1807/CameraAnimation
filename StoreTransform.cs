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

        public Vector3 Position => GetPosition();
        public Vector4 Rotation => GetRotation();
        public float Aperture => GetAperture();
        public float FocalDistance => GetFocalDistance();
        public float Zoom => GetZoom();

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

        public static Action<bool, GameObject> SetCameraPickupable { get; set; } = (x, y) => throw new NotImplementedException();

        private GameObject _photoCamera;
        private bool _pickupable;
        private CameraSettings _cachedSettings;

        private readonly float? _cachedAperture = null;
        private readonly float? _cachedFocalDistance = null;
        private readonly float? _cachedZoom = null;
        private readonly Vector3? _cachedPosition = null;
        private readonly Quaternion? _cachedRotation = null;

        public StoreTransform(GameObject photoCamera)
        {
            Photocamera = photoCamera;
        }

        public StoreTransform(float cachedAperture, float cachedFocalDistance, float cachedZoom, Vector3 cachedPosition, Quaternion cachedRotation)
        {
            _cachedAperture = cachedAperture;
            _cachedFocalDistance = cachedFocalDistance;
            _cachedZoom = cachedZoom;
            _cachedPosition = cachedPosition;
            _cachedRotation = cachedRotation;
        }

        private void SetGameObject(GameObject value)
        {
            _photoCamera = value;
            _cachedSettings = Photocamera.GetComponent<CameraSettings>();
        }

        private float GetFocalDistance()
        {
            if (CameraAnimationMod.FocalDistanceProps.Count == 0 || _cachedSettings is null)
                return _cachedFocalDistance ?? Settings.Camera.DefaultFocalDistance;

            return CameraAnimationMod.FocalDistanceProps[0].GetValue(_cachedSettings).Unbox<float>();
        }

        private float GetAperture()
        {
            if (CameraAnimationMod.ApertureProps.Count == 0 || _cachedSettings is null)
                return _cachedAperture ?? Settings.Camera.DefaultAperture;

            return CameraAnimationMod.ApertureProps[0].GetValue(_cachedSettings).Unbox<float>();
        }

        public float GetZoom()
        {
            if (CameraAnimationMod.ZoomProps.Count == 0 || _cachedSettings is null)
                return _cachedZoom ?? Settings.Camera.DefaultAperture;

            return CameraAnimationMod.ZoomProps[0].GetValue(_cachedSettings).Unbox<float>();

        }

        public Vector3 GetPosition()
        {
            if (_cachedPosition.HasValue)
                return _cachedPosition.Value;

            return Photocamera.transform.position;

        }

        public Vector4 GetRotation()
        {
            if (_cachedRotation.HasValue)
                return _cachedRotation.Value.ToVector4();

            return Photocamera.transform.localRotation.ToVector4();
        }

        public void Serialize(StringBuilder builder)
        {
            Position.Serialize(builder);
            Rotation.Serialize(builder);
            Zoom.Serialize(builder, ';');
            Aperture.Serialize(builder, ';');
            FocalDistance.Serialize(builder, ';');
            KeyPosition.Serialize(builder);
            KeyRotation.Serialize(builder);
            KeyZoom.Serialize(builder);
            KeyFocus.Serialize(builder);
        }
    }
}