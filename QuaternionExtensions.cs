using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public static class QuaternionExtensions
    {
        // Creates new vector4 with the real and imaginary parts of the provided quaternion
        // Why is this not implemented as a explicit/implicit cast overload unity.....
        public static Vector4 ToVector4(this Quaternion value)
        { 
            return new Vector4(value.x, value.y, value.z, value.w);
        }

        // Creates new Quaternion using the values within the provided vector4 as the real and imaginary parts of the quaternion
        // Why is this not implemented as a explicit/implicit cast overload unity.....
        public static Quaternion ToQuaternion(this Vector4 value)
        { 
            return new Quaternion(value.x, value.y, value.z, value.w);
        }
    }
}
