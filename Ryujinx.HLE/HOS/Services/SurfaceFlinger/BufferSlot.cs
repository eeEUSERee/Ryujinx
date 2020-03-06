namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferSlot
    {
        public GraphicBuffer GraphicBuffer;
        public bool          HasGraphicBuffer;
        public BufferState   BufferState;
        public bool          RequestBufferCalled;
        public ulong         FrameNumber;
        public AndroidFence  Fence;
        public bool          AcquireCalled;
        public bool          NeedsCleanupOnRelease;
        public bool          AttachedByConsumer;

        public BufferSlot()
        {
            BufferState = BufferState.Free;
        }
    }
}
