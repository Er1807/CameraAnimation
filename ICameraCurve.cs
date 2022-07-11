namespace CameraAnimation
{
    public interface ICameraCurve : IClip
    {
        ITransformCurve Transform { get; set; }
        ICurve Zoom { get; set; }
        ICurve Aperture { get; set; }
        ICurve FocalDistance { get; set; }
    }
}