using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public class CameraCurve : ICameraCurve
    {

        public ICurve FocalLength { get; set; }
        public IVector2Curve LensShift { get; set; }
        public IVector2Curve SensorSize { get; set; }
        public ITransformCurve Transform { get; set; }
        public ICurve Apature { get; set; }
        public ICurve ApatureCopy { get; set; } //there are 2 values for it that need to be animated
        public ICurve FocalDistance { get; set; }
        public ICurve FocalDistanceCopy { get; set; }

        public int Length => Math.Max(FocalLength.Length, Transform.Length);

        public void Set(AnimationClip clip)
        {
            Transform.Set(clip);
            FocalLength.Set(clip);
            LensShift.Set(clip);
            SensorSize.Set(clip);
            Apature.Set(clip);
            FocalDistance.Set(clip);
        }
    }
}
