namespace CameraAnimation
{
    public interface ITransformCurve : IClip
    {
        IVector3Curve Position { get; set; }
        IVector4Curve Rotation { get; set; }
        
    }
}