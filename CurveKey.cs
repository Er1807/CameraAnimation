using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraAnimation
{
    /// <summary>
    /// Represents an enumerable key value pair that when iterated returns several versions of the same key
    /// </summary>
    public struct CurveKey : ICurveKey, IEnumerable<ICurveKey>
    {
        public string Path { get; set; }
        public string Key { get; set; }
        public string AlternateKey { get; set; }

        public CurveKey(string path, string key, string alternateKey = null)
        {
            Path = path;
            Key = key;
            AlternateKey = alternateKey;
        }

        public IEnumerator<ICurveKey> GetEnumerator()
        {
            yield return new CurveKey(string.Empty, Key);
            yield return this;
            if (AlternateKey != null)
            {
                yield return new CurveKey(Path, AlternateKey);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }
    }
}
