using System.Collections;
using System.Collections.Generic;

namespace CameraAnimation
{
    public interface ICurveKey
    {
        string Key { get; set; }
        string Path { get; set; }
    }
}