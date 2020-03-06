namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferItem
    {
        public GraphicBuffer           GraphicBuffer;
        public bool                    HasGraphicBuffer;
        public AndroidFence            Fence;
        public Rect                    Crop;
        public NativeWindowTransform   Transform;
        public NativeWindowScalingMode ScalingMode;
        public long                    Timestamp;
        public bool                    IsAutoTimestamp;
        public ulong                   FrameNumber;
        public int                     Slot;
        public bool                    IsDroppable;
        public bool                    AcquireCalled;
        public bool                    TransformToDisplayInverse;

        public BufferItem()
        {
            Transform                 = NativeWindowTransform.None;
            ScalingMode               = NativeWindowScalingMode.Freeze;
            Timestamp                 = 0;
            IsAutoTimestamp           = false;
            FrameNumber               = 0;
            Slot                      = BufferSlotArray.InvalidBufferSlot;
            IsDroppable               = false;
            AcquireCalled             = false;
            TransformToDisplayInverse = false;
            Fence                     = AndroidFence.NoFence;

            Crop = new Rect();
            Crop.MakeInvalid();
        }
    }
}
