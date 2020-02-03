using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostChannel.Types
{
    [StructLayout(LayoutKind.Sequential)]
    struct CommandBuffer
    {
        public int MemoryId;
        public int Offset;
        public int WordsCount;

        public int[] GetData(ARMeilleure.Memory.MemoryManager memory, KProcess owner)
        {
            NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(owner, MemoryId);

            int[] cmdBuffer = new int[WordsCount];

            for (int offset = 0; offset < cmdBuffer.Length; offset++)
            {
                cmdBuffer[offset] = memory.ReadInt32(map.Address + Offset + offset * 4);
            }

            return cmdBuffer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SubmitArguments
    {
        public int CmdBufsCount;
        public int RelocsCount;
        public int SyncptIncrsCount;
        public int WaitchecksCount;
    }
}
