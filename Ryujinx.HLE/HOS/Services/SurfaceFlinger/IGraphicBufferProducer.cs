using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    abstract class IGraphicBufferProducer : IHOSBinderDriver
    {
        enum TransactionCode : uint
        {
            RequestBuffer  = 1,
            SetBufferCount,
            DequeueBuffer,
            DetachBuffer,
            DetachNextBuffer,
            AttachBuffer,
            QueueBuffer,
            CancelBuffer,
            Query,
            Connect,
            Disconnect,
            SetSidebandStream,
            AllocateBuffers,
            SetPreallocatedBuffer
        }

        public struct QueueBufferInput
        {
            public long                    Timestamp;
            public int                     IsAutoTimestamp;
            public Rect                    Crop;
            public NativeWindowScalingMode ScalingMode;
            public NativeWindowTransform   Transform;
            public uint                    StickyTransform;
            public int                     Async;
            public int                     SwapInterval;
            public AndroidFence            Fence;
        }

        public struct QueueBufferOutput
        {
            public uint                  Width;
            public uint                  Height;
            public NativeWindowTransform TransformHint;
            public uint                  NumPendingBuffers;
        }

        protected override ResultCode AdjustRefcount(int binderId, int addVal, int type)
        {
            // TODO?
            return ResultCode.Success;
        }

        protected override void GetNativeHandle(int binderId, uint typeId, out KReadableEvent readableEvent)
        {
            throw new NotImplementedException();
        }

        protected override ResultCode OnTransact(int binderId, uint code, uint flags, ReadOnlySpan<byte> inputParcel, Span<byte> outputParcel)
        {
            switch ((TransactionCode)code)
            {
                default:
                    throw new NotImplementedException($"Transaction {(TransactionCode)code} not implemneted");
            }
        }

        public abstract Status RequestBuffer(int slot, out GraphicBuffer graphicBuffer);

        public abstract Status SetBufferCount(int bufferCount);

        public abstract Status DequeueBuffer(out int slot, out AndroidFence fence, bool async, uint width, uint height, uint format, uint usage);

        public abstract Status DetachBuffer(int slot);

        public abstract Status DetachNextBuffer(out GraphicBuffer graphicBuffer, out AndroidFence fence);

        public abstract Status AttachBuffer(out int slot, ref GraphicBuffer graphicBuffer);

        public abstract Status QueueBuffer(int slot, ref QueueBufferInput input, out QueueBufferOutput output);

        public abstract void CancelBuffer(int slot, ref AndroidFence fence);

        public abstract Status Query(NativeWindowAttribute what, out int outValue);

        public abstract Status Connect(IProducerListener listener, NativeWindowApi api, bool producerControlledByApp);

        public abstract Status Disconnect(NativeWindowApi api);

        public abstract Status SetPreallocatedBuffer(int slot, ref GraphicBuffer graphicBuffer);
    }
}
