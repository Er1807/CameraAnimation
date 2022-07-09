using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public class TransformCurve : ITransformCurve
    {
        public IVector3Curve Position { get; set; }
        public IVector4Curve Rotation { get; set; }

        public int Length => Math.Max( Position.Length, Rotation.Length );

        public void Set(AnimationClip clip)
        {
            Position.Set(clip);
            Rotation.Set(clip);
        }
    }
}
