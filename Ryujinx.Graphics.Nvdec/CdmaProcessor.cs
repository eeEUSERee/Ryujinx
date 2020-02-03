using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.VDec;
using Ryujinx.Graphics.Vic;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public class CdmaProcessor
    {
        private const int MethSetMethod = 0x10;
        private const int MethSetData   = 0x11;

        private readonly VideoDecoder _videoDecoder;
        private readonly VideoImageComposer _videoImageComposer;

        public CdmaProcessor()
        {
            _videoDecoder = new VideoDecoder();
            _videoImageComposer = new VideoImageComposer(_videoDecoder);
        }

        public void PushCommands(GpuContext gpu, ChCommand[] commands)
        {
            int methodOffset = 0;

            foreach (ChCommand command in commands)
            {
                switch (command.MethodOffset)
                {
                    case MethSetMethod: methodOffset = command.Arguments[0]; break;

                    case MethSetData:
                    {
                        if (command.ClassId == ChClassId.NvDec)
                        {
                            _videoDecoder.Process(gpu, methodOffset, command.Arguments);
                        }
                        else if (command.ClassId == ChClassId.GraphicsVic)
                        {
                            _videoImageComposer.Process(gpu, methodOffset, command.Arguments);
                        }

                        break;
                    }

                    default:
                        Logger.PrintWarning(LogClass.Gpu, $"Unhandled ChCommand");
                        command.Dump();

                        break;
                }
            }
        }
    }
}