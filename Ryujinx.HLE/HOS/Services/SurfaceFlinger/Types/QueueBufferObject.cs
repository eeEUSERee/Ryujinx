using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x54)]
    struct QueueBufferObject
    {
        public long Timestamp;

        public int IsAutoTimestamp;

        public Rect Crop;

        public int ScalingMode;

        public HalTransform Transform;

        public int StickyTransform;

        public int Unknown;

        public int SwapInterval;

        public MultiFence Fence;
    }
}