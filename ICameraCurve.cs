namespace CameraAnimation
{
    public interface ICameraCurve : IClip
    {
        ITransformCurve Transform { get; set; }
        ICurve FocalLength { get; set; }
        IVector2Curve LensShift { get; set; }
        IVector2Curve SensorSize { get; set; }
    }
}