using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Gpu.Synchronization;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using Ryujinx.HLE.HOS.Services.Settings;

using System;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    internal class NvHostCtrlDeviceFile : NvDeviceFile
    {

        private bool          _isProductionMode;
        private NvHostSyncpt  _syncpt;
        private GpuContext    _gpuContext;

        public NvHostCtrlDeviceFile(ServiceCtx context) : base(context)
        {
            if (NxSettings.Settings.TryGetValue("nv!rmos_set_production_mode", out object productionModeSetting))
            {
                _isProductionMode = ((string)productionModeSetting) != "0"; // Default value is ""
            }
            else
            {
                _isProductionMode = true;
            }

            _syncpt     = new NvHostSyncpt(context.Device);
            _gpuContext = context.Device.Gpu;
        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            if (command.Type == NvIoctl.NvHostCustomMagic)
            {
                switch (command.Number)
                {
                    case 0x14:
                        result = CallIoctlMethod<NvFence>(SyncptRead, arguments);
                        break;
                    case 0x15:
                        result = CallIoctlMethod<uint>(SyncptIncr, arguments);
                        break;
                    case 0x16:
                        result = CallIoctlMethod<SyncptWaitArguments>(SyncptWait, arguments);
                        break;
                    case 0x19:
                        result = CallIoctlMethod<SyncptWaitExArguments>(SyncptWaitEx, arguments);
                        break;
                    case 0x1a:
                        result = CallIoctlMethod<NvFence>(SyncptReadMax, arguments);
                        break;
                    case 0x1b:
                        // As Marshal cannot handle unaligned arrays, we do everything by hand here.
                        GetConfigurationArguments configArgument = GetConfigurationArguments.FromSpan(arguments);
                        result = GetConfig(configArgument);

                        if (result == NvInternalResult.Success)
                        {
                            configArgument.CopyTo(arguments);
                        }
                        break;
                    case 0x1c:
                        result = CallIoctlMethod<uint>(EventSignal, arguments);
                        break;
                    case 0x1d:
                        result = CallIoctlMethod<EventWaitArguments>(EventWait, arguments);
                        break;
                    case 0x1e:
                        result = CallIoctlMethod<EventWaitArguments>(EventWaitAsync, arguments);
                        break;
                    case 0x1f:
                        result = CallIoctlMethod<uint>(EventRegister, arguments);
                        break;
                    case 0x20:
                        result = CallIoctlMethod<uint>(EventUnregister, arguments);
                        break;
                    case 0x21:
                        result = CallIoctlMethod<ulong>(EventKill, arguments);
                        break;
                }
            }

            return result;
        }

        public override NvInternalResult QueryEvent(out int eventHandle, uint eventId)
        {
            KEvent targetEvent = _syncpt.QueryEvent(eventId);

            if (targetEvent != null)
            {
                if (Owner.HandleTable.GenerateHandle(targetEvent.ReadableEvent, out eventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }
            else
            {
                eventHandle = 0;

                return NvInternalResult.InvalidInput;
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult SyncptRead(ref NvFence arguments)
        {
            return SyncptReadMinOrMax(ref arguments, max: false);
        }

        private NvInternalResult SyncptIncr(ref uint id)
        {
            if (id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            _syncpt.Increment(id);

            return NvInternalResult.Success;
        }

        private NvInternalResult SyncptWait(ref SyncptWaitArguments arguments)
        {
            return SyncptWait(ref arguments, out _);
        }

        private NvInternalResult SyncptWaitEx(ref SyncptWaitExArguments arguments)
        {
            return SyncptWait(ref arguments.Input, out arguments.Value);
        }

        private NvInternalResult SyncptReadMax(ref NvFence arguments)
        {
            return SyncptReadMinOrMax(ref arguments, max: true);
        }

        private NvInternalResult GetConfig(GetConfigurationArguments arguments)
        {
            if (!_isProductionMode && NxSettings.Settings.TryGetValue($"{arguments.Domain}!{arguments.Parameter}".ToLower(), out object nvSetting))
            {
                byte[] settingBuffer = new byte[0x101];

                if (nvSetting is string stringValue)
                {
                    if (stringValue.Length > 0x100)
                    {
                        Logger.PrintError(LogClass.ServiceNv, $"{arguments.Domain}!{arguments.Parameter} String value size is too big!");
                    }
                    else
                    {
                        settingBuffer = Encoding.ASCII.GetBytes(stringValue + "\0");
                    }
                }
                else if (nvSetting is int intValue)
                {
                    settingBuffer = BitConverter.GetBytes(intValue);
                }
                else if (nvSetting is bool boolValue)
                {
                    settingBuffer[0] = boolValue ? (byte)1 : (byte)0;
                }
                else
                {
                    throw new NotImplementedException(nvSetting.GetType().Name);
                }

                Logger.PrintDebug(LogClass.ServiceNv, $"Got setting {arguments.Domain}!{arguments.Parameter}");

                arguments.Configuration = settingBuffer;

                return NvInternalResult.Success;
            }

            // NOTE: This actually return NotAvailableInProduction but this is directly translated as a InvalidInput before returning the ioctl.
            //return NvInternalResult.NotAvailableInProduction;
            return NvInternalResult.InvalidInput;
        }

        private NvInternalResult EventWait(ref EventWaitArguments arguments)
        {
            return EventWait(ref arguments, async: false);
        }

        private NvInternalResult EventWaitAsync(ref EventWaitArguments arguments)
        {
            return EventWait(ref arguments, async: true);
        }

        private NvInternalResult EventRegister(ref uint userEventId)
        {
            return _syncpt.RegisterEvent(userEventId);
        }

        private NvInternalResult EventUnregister(ref uint userEventId)
        {
            return _syncpt.UnregisterEvent(userEventId);
        }

        private NvInternalResult EventKill(ref ulong eventMask)
        {
            return _syncpt.KillEvent(eventMask);
        }

        private NvInternalResult EventSignal(ref uint userEventId)
        {
            return _syncpt.SignalEvent(userEventId);
        }

        private NvInternalResult SyncptReadMinOrMax(ref NvFence arguments, bool max)
        {
            if (arguments.Id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            if (max)
            {
                arguments.Value = _syncpt.ReadSyncpointMaxValue(arguments.Id);
            }
            else
            {
                arguments.Value = _syncpt.ReadSyncpointValue(arguments.Id);
            }

            return NvInternalResult.Success;
        }

        private NvInternalResult SyncptWait(ref SyncptWaitArguments arguments, out uint value)
        {
            if (arguments.Id >= Synchronization.MaxHarwareSyncpoints)
            {
                value = 0;

                return NvInternalResult.InvalidInput;
            }

            NvInternalResult result;

            if (_syncpt.IsSyncpointExpired(arguments.Id, arguments.Thresh))
            {
                result = NvInternalResult.Success;
            }
            else if (arguments.Timeout == 0)
            {
                result = NvInternalResult.TryAgain;
            }
            else
            {
                Logger.PrintDebug(LogClass.ServiceNv, $"Waiting syncpt with timeout of {arguments.Timeout}ms...");

                using (ManualResetEvent waitEvent = new ManualResetEvent(false))
                {
                    var waiterInformation = _syncpt.AddWaiter(arguments.Id, arguments.Thresh, waitEvent);

                    // Note: Negative (> INT_MAX) timeouts aren't valid on .NET,
                    // in this case we just use the maximum timeout possible.
                    int timeout = arguments.Timeout;

                    if (timeout < -1)
                    {
                        timeout = int.MaxValue;
                    }

                    if (timeout == -1)
                    {
                        waitEvent.WaitOne();

                        result = NvInternalResult.Success;
                    }
                    else if (waitEvent.WaitOne(timeout))
                    {
                        result = NvInternalResult.Success;
                    }
                    else
                    {
                        _syncpt.RemoveWaiter(arguments.Id, waiterInformation);

                        result = NvInternalResult.TimedOut;
                    }
                }

                Logger.PrintDebug(LogClass.ServiceNv, "Resuming...");
            }

            value = _syncpt.ReadSyncpointValue(arguments.Id);

            return result;
        }

        private NvInternalResult EventWait(ref EventWaitArguments arguments, bool async)
        {
            if (arguments.Fence.Id >= Synchronization.MaxHarwareSyncpoints)
            {
                return NvInternalResult.InvalidInput;
            }

            // First try to check if the syncpoint is already expired on the CPU side
            if (_syncpt.IsSyncpointExpired(arguments.Fence.Id, arguments.Fence.Value))
            {
                arguments.Value = _syncpt.ReadSyncpointMinValue(arguments.Fence.Id);

                return NvInternalResult.Success;
            }

            // Try to invalidate the CPU cache and check for expiration again.
            uint newCachedValueSyncpointValue = _syncpt.UpdateMin(arguments.Fence.Id);

            // Has the fence already expired?
            if (_syncpt.IsSyncpointExpired(arguments.Fence.Id, arguments.Fence.Value))
            {
                arguments.Value = newCachedValueSyncpointValue;

                return NvInternalResult.Success;
            }

            // The syncpoint value isn't at the fence yet, we need to wait.

            if (!async)
            {
                arguments.Value = 0;
            }

            if (arguments.Timeout == 0)
            {
                return NvInternalResult.TryAgain;
            }

            NvHostEvent Event;

            NvInternalResult result;

            uint eventIndex;

            if (async)
            {
                eventIndex = arguments.Value;

                if (eventIndex >= NvHostSyncpt.EventsCount)
                {
                    return NvInternalResult.InvalidInput;
                }

                Event = _syncpt.Events[eventIndex];
            }
            else
            {
                Event = _syncpt.GetFreeEvent(arguments.Fence.Id, out eventIndex);
            }

            if (Event != null &&
               (Event.State == NvHostEventState.Registered ||
                Event.State == NvHostEventState.Free))
            {
                Event.Wait(_gpuContext, arguments.Fence);

                if (!async)
                {
                    arguments.Value = ((arguments.Fence.Id & 0xfff) << 16) | 0x10000000;
                }
                else
                {
                    arguments.Value = arguments.Fence.Id << 4;
                }

                arguments.Value |= eventIndex;

                result = NvInternalResult.TryAgain;
            }
            else
            {
                result = NvInternalResult.InvalidInput;
            }

            return result;
        }

        public override void Close() { }
    }
}
