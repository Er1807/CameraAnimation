using UnityEngine;

namespace CameraAnimation
{
    public class StoreTransform
    {
        public GameObject Photocamera;
        public Vector3 Position => Photocamera.transform.position;
        public Vector3 EulerAngles => Photocamera.transform.eulerAngles;
        public Vector3 EulerAnglesCorrected;

        public StoreTransform(GameObject camera)
        {
            Photocamera = camera;
        }
    }
}