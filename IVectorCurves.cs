using UnityEngine;

namespace CameraAnimation
{

    public interface IVector4Curve : IVector3Curve
    {
        ICurve W { get; set; }

        (int, int, int, int) Add(float time, float x, float y, float z, float w);
        (int, int, int, int) Add(float time, Vector4 value);

        new Vector4 Evaluate(float time);
    }

    public interface IVector3Curve : IVector2Curve
    {
        ICurve Z { get; set; }

        (int, int, int) Add(float time, float x, float y, float z);
        (int, int, int) Add(float time, Vector3 value);

        new Vector3 Evaluate(float time);
    }

    public interface IVector2Curve : IClip
    {
        ICurve X { get; set; }
        ICurve Y { get; set; }

        (int, int) Add(float time, float x, float y);
        (int, int) Add(float time, Vector2 value);

        Vector2 Evaluate(float time);
    }
}