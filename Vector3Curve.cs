using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public class Vector3Curve : Vector2Curve, IVector3Curve
    {
        public ICurve Z { get; set; }

        public new int Length => Math.Max( X.Length, Math.Max( Y.Length, Z.Length ));

        public (int, int, int) Add(float time, float x, float y, float z)
        {
            return (X.Add(time, x), Y.Add(time, y), Z.Add(time, z) );
        }

        public (int, int, int) Add(float time, Vector3 value)
        {
            return Add(time, value.x, value.y, value.z);
        }

        public new Vector3 Evaluate(float time)
        {
            return new Vector3( X.Evaluate(time), Y.Evaluate(time), Z.Evaluate(time) );
        }

        public new void Set(AnimationClip clip)
        {
            base.Set(clip);
            Z.Set(clip);
        }
    }

    public class Vector2Curve : IVector2Curve
    {
        public ICurve X { get; set; }
        public ICurve Y { get; set; }

        public int Length => Math.Max(X.Length,Y.Length);

        public (int, int) Add(float time, float x, float y)
        {
            return (X.Add(time, x), Y.Add(time, y));
        }

        public (int, int) Add(float time, Vector2 value)
        {
            return Add(time, value.x, value.y);
        }

        public Vector2 Evaluate(float time)
        {
            return new Vector2(X.Evaluate(time), Y.Evaluate(time));
        }

        public void Set(AnimationClip clip)
        {
            X.Set(clip);
            Y.Set(clip);
        }
    }
}
