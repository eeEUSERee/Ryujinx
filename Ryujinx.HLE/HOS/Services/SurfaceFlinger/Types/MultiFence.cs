using Ryujinx.Common.Utilities;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Services.Nv.Types;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x24)]
    struct MultiFence
    {
        public int FenceCount;

        private byte _fenceStorageStart;

        public Span<byte> Storage => MemoryMarshal.CreateSpan(ref _fenceStorageStart, Unsafe.SizeOf<NvFence>() * 4);

        public Span<NvFence> Fences => MemoryMarshal.Cast<byte, NvFence>(Storage);


        public void Wait(GpuContext gpuContext)
        {
            for (int i = 0; i < FenceCount; i++)
            {
                Fences[i].Wait(gpuContext, Timeout.InfiniteTimeSpan);
            }
        }
    }
}