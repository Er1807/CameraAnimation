using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public class CurveWrapper<T> : ICurve
    {
        private readonly AnimationCurve curve;

        private readonly List<(string path, string key)> pathKeys = new List<(string, string)>();
        private readonly Il2CppSystem.Type type;

        public CurveWrapper(AnimationCurve curve, string key)
        {
            pathKeys.Add((string.Empty, key));
            this.curve = curve;
            this.type = UnhollowerRuntimeLib.Il2CppType.Of<T>();
        }

        public CurveWrapper(AnimationCurve curve, List<(string path, string key)> keys)
        {
            pathKeys = keys;
            this.curve = curve;
            this.type = UnhollowerRuntimeLib.Il2CppType.Of<T>();
        }

        public Keyframe this[int index] => curve.keys[index];

        public int Length => curve.length;

        public Keyframe First => curve.length > 0 ? curve.keys[0] : default;
        public Keyframe Last => curve.length > 0 ? curve.keys[curve.length-1] : default;

        public int Add(float time, float value) => curve.AddKey(time, value);

        public float Evaluate(float time) => curve.Evaluate(time);

        public void Set(AnimationClip clip)
        {
            foreach (var (path, key) in pathKeys)
            {
                clip.SetCurve(path, type, key, curve);
            }
        }
    }
}
