using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    class NvHostSyncpt
    {
        public const int EventsCount = 64;

        private int[] _counterMin;
        private int[] _counterMax;

        private Switch _device;

        public NvHostEvent[] Events { get; private set; }

        public NvHostSyncpt(Switch device)
        {
            _device     = device;
            Events      = new NvHostEvent[EventsCount];
            _counterMin = new int[Synchronization.MaxHarwareSyncpoints];
            _counterMax = new int[Synchronization.MaxHarwareSyncpoints];
        }

        public NvHostEvent GetFreeEvent(uint id, out uint eventIndex)
        {
            eventIndex = EventsCount;

            uint nullIndex = EventsCount;

            for (uint index = 0; index < EventsCount; index++)
            {
                NvHostEvent Event = Events[index];

                if (Event != null)
                {
                    if (Event.State == NvHostEventState.Registered ||
                        Event.State == NvHostEventState.Free)
                    {
                        eventIndex = index;

                        if (Event.Fence.Id == id)
                        {
                            return Event;
                        }
                    }
                }
                else if (nullIndex == EventsCount)
                {
                    nullIndex = index;
                }
            }

            if (nullIndex < EventsCount)
            {
                eventIndex = nullIndex;

                RegisterEvent(eventIndex);

                return Events[nullIndex];
            }

            if (eventIndex < EventsCount)
            {
                return Events[eventIndex];
            }

            return null;
        }

        public NvInternalResult RegisterEvent(uint eventId)
        {
            NvInternalResult result = UnregisterEvent(eventId);

            if (result == NvInternalResult.Success)
            {
                Events[eventId] = new NvHostEvent(this, eventId, _device.System);
            }

            return result;
        }

        public NvInternalResult UnregisterEvent(uint eventId)
        {
            if (eventId >= EventsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            NvHostEvent evnt = Events[eventId];

            if (evnt == null)
            {
                return NvInternalResult.Success;
            }

            if (evnt.State == NvHostEventState.Registered || evnt.State == NvHostEventState.Registered)
            {
                Events[eventId].Cancel(_device.Gpu);
                Events[eventId] = null;

                return NvInternalResult.Success;
            }

            return NvInternalResult.Busy;
        }

        public NvInternalResult KillEvent(ulong eventMask)
        {
            NvInternalResult result = NvInternalResult.Success;

            for (uint eventId = 0; eventId < EventsCount; eventId++)
            {
                if ((eventMask & (1UL << (int)eventId)) != 0)
                {
                    NvInternalResult tmp = UnregisterEvent(eventId);

                    if (tmp != NvInternalResult.Success)
                    {
                        result = tmp;
                    }
                }
            }

            return result;
        }

        public NvInternalResult SignalEvent(uint eventId)
        {
            if (eventId >= EventsCount)
            {
                return NvInternalResult.InvalidInput;
            }

            NvHostEvent evnt = Events[eventId];

            if (evnt == null)
            {
                return NvInternalResult.InvalidInput;
            }

            if (evnt.State == NvHostEventState.Registered)
            {
                evnt.State = NvHostEventState.Busy;
            }

            Events[eventId].Cancel(_device.Gpu);

            return NvInternalResult.Busy;
        }

        public uint ReadSyncpointValue(uint id)
        {
            return UpdateMin(id);
        }

        public uint ReadSyncpointMinValue(uint id)
        {
            return (uint)_counterMin[id];
        }

        public uint ReadSyncpointMaxValue(uint id)
        {
            return (uint)_counterMax[id];
        }

        public KEvent QueryEvent(uint eventId)
        {
            uint eventSlot;

            if ((eventId >> 28) == 1)
            {
                eventSlot = eventId & 0xFFFF;
            }
            else
            {
                eventSlot = eventId & 0xFF;
            }

            if (eventSlot >= EventsCount)
            {
                return null;
            }

            return Events[eventSlot].Event;
        }

        private bool IsClientManaged(uint id)
        {
            // Assume all syncpoints are regular hardware one (like nvhost for the T210s)
            return true;
        }

        public void Increment(uint id)
        {
            if (IsClientManaged(id))
            {
                IncrementSyncpointMax(id);
            }

            IncrementSyncpointCPU(id);
        }

        public uint UpdateMin(uint id)
        {
            uint newValue = _device.Gpu.Synchronization.GetSyncpointValue(id);

            Interlocked.Exchange(ref _counterMin[id], (int)newValue);

            return newValue;
        }

        private void IncrementSyncpointCPU(uint id)
        {
            _device.Gpu.Synchronization.IncrementSyncpoint(id);
        }

        private void IncrementSyncpointMax(uint id)
        {
            Interlocked.Increment(ref _counterMax[id]);
        }

        public bool IsSyncpointExpired(uint id, uint threshold)
        {
            return MinCompare(id, _counterMin[id], _counterMax[id], (int)threshold);
        }

        private bool MinCompare(uint id, int min, int max, int threshold)
        {
            int minDiff = min - threshold;
            int maxDiff = max - threshold;

            if (IsClientManaged(id))
            {
                return minDiff >= 0;
            }
            else
            {
                return (uint)maxDiff >= (uint)minDiff;
            }
        }
    }
}