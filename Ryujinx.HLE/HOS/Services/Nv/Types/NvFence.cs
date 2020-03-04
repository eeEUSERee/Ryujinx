using Ryujinx.Graphics.Gpu;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x8)]
    public struct NvFence
    {
        public const uint InvalidSyncPointId = uint.MaxValue;

        public uint Id;
        public uint Value;

        public bool IsValid()
        {
            return Id != InvalidSyncPointId;
        }

        public bool Wait(GpuContext gpuContext, TimeSpan timeout)
        {
            if (IsValid())
            {
                return gpuContext.Synchronization.WaitOnSyncpoint(Id, Value, timeout);
            }

            return false;
        }
    }
}
