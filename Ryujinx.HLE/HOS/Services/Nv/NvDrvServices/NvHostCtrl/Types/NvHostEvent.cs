using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    class NvHostEvent
    {
        public NvFence          Fence;
        public NvHostEventState State;
        public KEvent           Event;

        private uint                       _eventId;
        private NvHostSyncpt               _syncpointManager;
        private SyncpointWaiterInformation _waiterInformation;

        public NvHostEvent(NvHostSyncpt syncpointManager, uint eventId, Horizon system)
        {
            Fence.Id = NvFence.InvalidSyncPointId;

            State = NvHostEventState.Registered;

            Event = new KEvent(system);

            _eventId = eventId;

            _syncpointManager = syncpointManager;
        }

        public void Reset()
        {
            Fence.Id    = NvFence.InvalidSyncPointId;
            Fence.Value = 0;
            State       = NvHostEventState.Registered;
        }

        private void GpuSignaled()
        {
            State = NvHostEventState.Free;

            Event.WritableEvent.Signal();
        }

        public void Cancel(GpuContext gpuContext)
        {
            if (_waiterInformation != null)
            {
                gpuContext.Synchronization.UnregisterCallback(Fence.Id, _waiterInformation);

                GpuSignaled();
            }
        }

        public bool Wait(GpuContext gpuContext, NvFence fence)
        {
            Fence = fence;
            State = NvHostEventState.Waiting;

            _waiterInformation = gpuContext.Synchronization.RegisterCallbackOnSyncpoint(Fence.Id, Fence.Value, GpuSignaled);

            return _waiterInformation == null;
        }
    }
}