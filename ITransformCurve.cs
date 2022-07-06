namespace CameraAnimation
{
    public interface ITransformCurve : IClip
    {
        IVector3Curve Position { get; set; }
        IVector3Curve Rotation { get; set; }
    }
}