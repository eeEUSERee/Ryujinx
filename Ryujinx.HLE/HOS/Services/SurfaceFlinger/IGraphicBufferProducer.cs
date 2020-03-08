using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    abstract class IGraphicBufferProducer : IBinder
    {
        public string InterfaceToken => "android.gui.IGraphicBufferProducer";

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
            SetPreallocatedBuffer,
            Reserved15,
            GetBufferInfo,
            GetBufferHistory
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x54)]
        public struct QueueBufferInput : IFlattenable
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

            public void Flattern(Parcel parcel)
            {
                parcel.WriteUnmanagedType(ref this);
            }

            public uint GetFdCount()
            {
                return 0;
            }

            public uint GetFlattenedSize()
            {
                return (uint)Unsafe.SizeOf<QueueBufferInput>();
            }

            public void Unflatten(Parcel parcel)
            {
                this = parcel.ReadUnmanagedType<QueueBufferInput>();
            }
        }

        public struct QueueBufferOutput
        {
            public uint                  Width;
            public uint                  Height;
            public NativeWindowTransform TransformHint;
            public uint                  NumPendingBuffers;
        }

        public ResultCode AdjustRefcount(int addVal, int type)
        {
            // TODO?
            return ResultCode.Success;
        }

        public void GetNativeHandle(uint typeId, out KReadableEvent readableEvent)
        {
            if (typeId == 0xF)
            {
                readableEvent = GetWaitBufferFreeEvent();
            }
            else
            {
                throw new NotImplementedException($"Unimplemented native event type {typeId}!");
            }
        }

        public void OnTransact(uint code, uint flags, Parcel inputParcel, Parcel outputParcel)
        {
            Status            status = Status.Success;
            int               slot;
            GraphicBuffer     graphicBuffer;
            QueueBufferInput  queueInput;
            QueueBufferOutput queueOutput;

            switch ((TransactionCode)code)
            {
                case TransactionCode.RequestBuffer:
                    slot = inputParcel.ReadInt32();

                    status = RequestBuffer(slot, out graphicBuffer);

                    // TODO: wrap GraphicBuffer to another object so we can know when it's set or not. For now assume always present.
                    outputParcel.WriteBoolean(true);
                    outputParcel.WriteFlattenable(ref graphicBuffer);

                    outputParcel.WriteStatus(status);

                    break;
                case TransactionCode.DequeueBuffer:
                    bool        async  = inputParcel.ReadBoolean();
                    uint        width  = inputParcel.ReadUInt32();
                    uint        height = inputParcel.ReadUInt32();
                    PixelFormat format = inputParcel.ReadUnmanagedType<PixelFormat>();
                    uint        usage  = inputParcel.ReadUInt32();

                    status = DequeueBuffer(out int dequeueSlot, out AndroidFence fence, async, width, height, format, usage);

                    outputParcel.WriteInt32(dequeueSlot);
                    // TODO: wrap AndroidFence to another object so we can know when it's set or not. For now assume always present.
                    outputParcel.WriteBoolean(true);
                    outputParcel.WriteFlattenable(ref fence);

                    outputParcel.WriteStatus(status);

                    break;
                case TransactionCode.QueueBuffer:
                    slot       = inputParcel.ReadInt32();
                    queueInput = inputParcel.ReadFlattenable<QueueBufferInput>();

                    status = QueueBuffer(slot, ref queueInput, out queueOutput);

                    outputParcel.WriteUnmanagedType(ref queueOutput);

                    outputParcel.WriteStatus(status);

                    break;
                case TransactionCode.CancelBuffer:
                    slot  = inputParcel.ReadInt32();
                    fence = inputParcel.ReadFlattenable<AndroidFence>();

                    CancelBuffer(slot, ref fence);

                    outputParcel.WriteStatus(Status.Success);

                    break;
                case TransactionCode.Query:
                    NativeWindowAttribute what = inputParcel.ReadUnmanagedType<NativeWindowAttribute>();

                    status = Query(what, out int outValue);

                    outputParcel.WriteInt32(outValue);

                    outputParcel.WriteStatus(status);

                    break;
                case TransactionCode.Connect:
                    bool hasListener = inputParcel.ReadBoolean();

                    IProducerListener listener = null;

                    if (hasListener)
                    {
                        throw new NotImplementedException($"Connect with a strong binder listener isn't implemented");
                    }

                    NativeWindowApi api                     = inputParcel.ReadUnmanagedType<NativeWindowApi>();
                    bool            producerControlledByApp = inputParcel.ReadBoolean();

                    status = Connect(listener, api, producerControlledByApp, out queueOutput);

                    outputParcel.WriteUnmanagedType(ref queueOutput);

                    outputParcel.WriteStatus(status);

                    break;
                case TransactionCode.SetPreallocatedBuffer:
                    slot = inputParcel.ReadInt32();

                    bool hasGraphicBuffer = inputParcel.ReadBoolean();

                    graphicBuffer = new GraphicBuffer();

                    if (hasGraphicBuffer)
                    {
                        graphicBuffer = inputParcel.ReadFlattenable<GraphicBuffer>();
                    }


                    status = SetPreallocatedBuffer(slot, hasGraphicBuffer, ref graphicBuffer);

                    outputParcel.WriteStatus(status);

                    break;
                default:
                    throw new NotImplementedException($"Transaction {(TransactionCode)code} not implemented");
            }

            if (status != Status.Success)
            {
                Logger.PrintError(LogClass.SurfaceFlinger, $"Error returned by transaction {(TransactionCode)code}: {status}");
            }
        }

        protected abstract KReadableEvent GetWaitBufferFreeEvent();

        public abstract Status RequestBuffer(int slot, out GraphicBuffer graphicBuffer);

        public abstract Status SetBufferCount(int bufferCount);

        public abstract Status DequeueBuffer(out int slot, out AndroidFence fence, bool async, uint width, uint height, PixelFormat format, uint usage);

        public abstract Status DetachBuffer(int slot);

        public abstract Status DetachNextBuffer(out GraphicBuffer graphicBuffer, out AndroidFence fence);

        public abstract Status AttachBuffer(out int slot, ref GraphicBuffer graphicBuffer);

        public abstract Status QueueBuffer(int slot, ref QueueBufferInput input, out QueueBufferOutput output);

        public abstract void CancelBuffer(int slot, ref AndroidFence fence);

        public abstract Status Query(NativeWindowAttribute what, out int outValue);

        public abstract Status Connect(IProducerListener listener, NativeWindowApi api, bool producerControlledByApp, out QueueBufferOutput output);

        public abstract Status Disconnect(NativeWindowApi api);

        public abstract Status SetPreallocatedBuffer(int slot, bool hasGraphicBuffer, ref GraphicBuffer graphicBuffer);
    }
}
