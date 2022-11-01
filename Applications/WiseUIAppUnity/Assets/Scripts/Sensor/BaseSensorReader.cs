using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SensorStream
{
    public enum PVCameraType { r640x360xf30 = 0, r760x428xf30 = 1/*, r1280x720xf30 = 2 */};

    public class BaseSensorReader : MonoBehaviour
    {
        protected int frameIdx = 0;
        //protected object lockObject = new object();
        //critical section ( shared with object_detector.)
        //protected Texture2D latestTexture = null;

        //protected byte[] latestFrameBuffer;


        public virtual void GrabCurrentTexture(ref Texture2D dest) { }
        public virtual void StopCapture() { }

    }

}
