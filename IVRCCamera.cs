using UnityEngine;

using CameraSettings = MonoBehaviourPublicAcInCaInTeShInMaBoInUnique;

namespace CameraAnimation
{
    public interface IVRCCamera
    {
        GameObject PhotoCamera { get; }
        CameraSettings Settings { get; }
        GameObject VideoCamera { get; }
    }
}