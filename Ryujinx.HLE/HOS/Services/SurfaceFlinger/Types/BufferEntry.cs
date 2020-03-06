namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    struct BufferEntry
    {
        public BufferState State;

        public HalTransform Transform;

        public Rect Crop;

        public AndroidFence Fence;

        public GraphicBuffer Data;
    }
}