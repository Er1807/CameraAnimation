using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRC.UserCamera;
using CameraSettings = MonoBehaviourPublicAcInCaInTeShInMaBoInUnique;

namespace CameraAnimation
{
    public class VRCCamera : IVRCCamera
    {
        public GameObject PhotoCamera => GetPhotoCamera();
        private GameObject _photoCamera;

        public GameObject VideoCamera => GetVideoCamera();
        private GameObject _videoCamera;

        public CameraSettings Settings => GetSettings();
        private CameraSettings _settings;

        private GameObject GetPhotoCamera()
        {
            if (_photoCamera == null)
            {
                _photoCamera = RetrieveCamera("PhotoCamera");
            }

            // if unity would ever update it's damn c# version this could be reduced to ??= RetrieveCamera(); FFS...
            return _photoCamera;
        }

        private GameObject GetVideoCamera()
        {
            if (_videoCamera == null)
            {
                _videoCamera = RetrieveCamera("VideoCamera");
            }

            // if unity would ever update it's damn c# version this could be reduced to ??= RetrieveCamera(); FFS...
            return _videoCamera;
        }

        private CameraSettings GetSettings()
        {
            if (_settings != null) return _settings;
            if (PhotoCamera == null) return null;
            _settings = PhotoCamera.GetComponent<CameraSettings>();
            return _settings;
        }

        private GameObject RetrieveCamera(string name)
        {
            UserCameraController controller = UserCameraController.field_Internal_Static_UserCameraController_0;

            if (controller == null) return null;

            foreach (var prop in typeof(UserCameraController).GetProperties().Where(x => x.Name.StartsWith("field_Public_GameObject_")))
            {
                var obj = prop.GetValue(controller) as GameObject;
                if (obj != null && obj.name == name)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
