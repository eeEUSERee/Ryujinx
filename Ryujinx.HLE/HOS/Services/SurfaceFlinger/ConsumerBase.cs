using System;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class ConsumerBase : IConsumerListener
    {
        public class Slot
        {
            public GraphicBuffer GraphicBuffer;
            public bool          HasGraphicBuffer;
            public AndroidFence  Fence;
            public ulong         FrameNumber;
        }

        protected Slot[] Slots = new Slot[BufferSlotArray.NumBufferSlots];

        protected bool IsAbandoned;

        protected BufferQueueConsumer Consumer;

        protected readonly object Lock = new object();

        public ConsumerBase(BufferQueueConsumer consumer, bool controlledByApp)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = new Slot();
            }

            IsAbandoned = false;
            Consumer    = consumer;

            Status connectStatus = consumer.Connect(this, controlledByApp);

            if (connectStatus != Status.Success)
            {
                throw new InvalidOperationException();
            }
        }

        public virtual void onBuffersReleased()
        {
            throw new NotImplementedException();
        }

        public virtual void OnFrameAvailable(ref BufferItem item)
        {

        }

        public virtual void OnFrameReplaced(ref BufferItem item)
        {
            lock (Lock)
            {
                if (IsAbandoned)
                {
                    return;
                }

                Consumer.GetReleasedBuffers(out ulong slotMask);

                for (int i = 0; i < Slots.Length; i++)
                {
                    if ((slotMask & (1UL << i)) != 0)
                    {
                        FreeBufferLocked(i);
                    }
                }
            }
        }

        protected virtual void FreeBufferLocked(int slotIndex)
        {
            Slots[slotIndex].HasGraphicBuffer = false;
            Slots[slotIndex].Fence            = AndroidFence.NoFence;
            Slots[slotIndex].FrameNumber      = 0;
        }

        public void Abandon()
        {
            lock (Lock)
            {
                if (!IsAbandoned)
                {
                    AbandonLocked();

                    IsAbandoned = true;
                }
            }
        }

        protected virtual void AbandonLocked()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                FreeBufferLocked(i);
            }

            Consumer.Disconnect();
        }

        protected virtual Status AcquireBufferLocked(out BufferItem bufferItem, ulong expectedPresent)
        {
            Status acquireStatus = Consumer.AcquireBuffer(out bufferItem, expectedPresent);

            if (acquireStatus != Status.Success)
            {
                return acquireStatus;
            }

            if (bufferItem.HasGraphicBuffer)
            {
                Slots[bufferItem.Slot].GraphicBuffer    = bufferItem.GraphicBuffer;
                Slots[bufferItem.Slot].HasGraphicBuffer = true;
            }

            Slots[bufferItem.Slot].FrameNumber = bufferItem.FrameNumber;
            Slots[bufferItem.Slot].Fence       = bufferItem.Fence;

            return Status.Success;
        }

        protected virtual Status ReleaseBufferLocked(int slot, ref GraphicBuffer graphicBuffer)
        {
            if (!StillTracking(slot, ref graphicBuffer))
            {
                return Status.Success;
            }

            Status result = Consumer.ReleaseBuffer(slot, Slots[slot].FrameNumber, ref Slots[slot].Fence);

            if (result == Status.StaleBufferSlot)
            {
                FreeBufferLocked(slot);
            }

            Slots[slot].Fence = AndroidFence.NoFence;

            return Status.Success;
        }

        protected virtual bool StillTracking(int slotIndex, ref GraphicBuffer graphicBuffer)
        {
            if (slotIndex < 0 || slotIndex > Slots.Length)
            {
                return false;
            }

            Slot slot = Slots[slotIndex];

            // TODO: Check this. On Android, this checks the "handle". I assume NvMapHandle is the handle, but it might not be. 
            return slot.HasGraphicBuffer && slot.GraphicBuffer.Buffer.Surfaces[0].NvMapHandle == graphicBuffer.Buffer.Surfaces[0].NvMapHandle;
        }
    }
}
