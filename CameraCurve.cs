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

        public ICurve Zoom { get; set; }
        public ITransformCurve Transform { get; set; }
        public ICurve Apature { get; set; }
        public ICurve ApatureAlternate { get; set; } //there are 2 values for it that need to be animated
        public ICurve FocalDistance { get; set; }
        public ICurve FocalDistanceAlternate { get; set; }

        public int Length => Math.Max(Zoom.Length, Transform.Length);

        public void Set(AnimationClip clip)
        {
            Transform.Set(clip);
            Zoom.Set(clip);
            Apature.Set(clip);
            FocalDistance.Set(clip);
        }
    }
}
