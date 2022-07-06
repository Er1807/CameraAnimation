using UnityEngine;

namespace CameraAnimation
{
    public interface ICurve : IClip
    {
        Keyframe this[int index] { get; }
        Keyframe First { get; }
        Keyframe Last { get; }

        int Add(float time, float value);
        float Evaluate(float time);
    }

    public interface IClip
    {
        int Length { get; }
        void Set(AnimationClip clip);
    }
}