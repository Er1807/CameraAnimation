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
        public ICurve Aperture { get; set; }
        public ICurve FocalDistance { get; set; }

        public int Length => Math.Max(Zoom.Length, Transform.Length);

        public void Set(AnimationClip clip)
        {
            Transform.Set(clip);
            Zoom.Set(clip);
            Aperture.Set(clip);
            FocalDistance.Set(clip);
        }
    }
}
