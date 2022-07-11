namespace CameraAnimation
{
    public interface ICameraCurve : IClip
    {
        ITransformCurve Transform { get; set; }
        ICurve FocalLength { get; set; }
        IVector2Curve LensShift { get; set; }
        IVector2Curve SensorSize { get; set; }
        ICurve Apature { get; set; }
        ICurve ApatureCopy { get; set; } //there are 2 values for it that need to be animated
        ICurve FocalDistance { get; set; }
        ICurve FocalDistanceCopy { get; set; }//there are 2 values for it that need to be animated
    }
}