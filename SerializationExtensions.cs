using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraAnimation
{
    public static class SerializationExtensions
    {
        public static void Serialize(this Vector4 vector, StringBuilder builder)
        {
            vector.x.Serialize(builder, ';');
            vector.y.Serialize(builder, ';');
            vector.z.Serialize(builder, ';');
            vector.w.Serialize(builder, ';');
        }

        public static void Serialize(this Vector3 vector, StringBuilder builder)
        {
            vector.x.Serialize(builder, ';');
            vector.y.Serialize(builder, ';');
            vector.z.Serialize(builder, ';');
        }

        public static void Serialize(this Vector2 vector, StringBuilder builder)
        {
            vector.x.Serialize(builder, ';');
            vector.y.Serialize(builder, ';');
        }

        public static void Serialize(this float value, StringBuilder builder, char? postFix = null)
        {
            builder.Append(Math.Round(value, 5).ToString(CultureInfo.InvariantCulture));
            if (postFix != null)
            {
                builder.Append(postFix);
            }
        }

        public static void Serialize(this bool value, StringBuilder builder)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
            builder.Append(';');
        }
    }
}
