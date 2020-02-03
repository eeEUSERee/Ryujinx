using Ryujinx.Common.Logging;
using System.Collections.Generic;

namespace Ryujinx.Graphics
{
    public struct ChCommand
    {
        public ChClassId ClassId { get; private set; }

        public int MethodOffset { get; private set; }

        public int[] Arguments { get; private set; }

        public ChCommand(ChClassId classId, int methodOffset, params int[] arguments)
        {
            ClassId      = classId;
            MethodOffset = methodOffset;
            Arguments    = arguments;
        }

        public void Dump()
        {
            Logger.PrintWarning(LogClass.Gpu, "\t\tChCommand:");
            Logger.PrintWarning(LogClass.Gpu, $"\t\t\tClassId: {ClassId}");
            Logger.PrintWarning(LogClass.Gpu, $"\t\t\tMethodOffset: {MethodOffset}");
            Logger.PrintWarning(LogClass.Gpu, $"\t\t\tArguments: {string.Join(", ", Arguments)}");
        }

        public static ChCommand[] ParseCommandBuffer(int[] cmdBuffer)
        {
            List<ChCommand> commands = new List<ChCommand>();

            ChClassId currentClass = 0;

            for (int index = 0; index < cmdBuffer.Length; index++)
            {
                int cmd = cmdBuffer[index];

                int value = (cmd >> 0) & 0xffff;
                int methodOffset = (cmd >> 16) & 0xfff;

                ChSubmissionMode submissionMode = (ChSubmissionMode)((cmd >> 28) & 0xf);

                switch (submissionMode)
                {
                    case ChSubmissionMode.SetClass: currentClass = (ChClassId)(value >> 6); break;

                    case ChSubmissionMode.Incrementing:
                        {
                            int count = value;

                            for (int argIdx = 0; argIdx < count; argIdx++)
                            {
                                int argument = cmdBuffer[++index];

                                commands.Add(new ChCommand(currentClass, methodOffset + argIdx, argument));
                            }

                            break;
                        }

                    case ChSubmissionMode.NonIncrementing:
                        {
                            int count = value;

                            int[] arguments = new int[count];

                            for (int argIdx = 0; argIdx < count; argIdx++)
                            {
                                arguments[argIdx] = cmdBuffer[++index];
                            }

                            commands.Add(new ChCommand(currentClass, methodOffset, arguments));

                            break;
                        }
                }
            }

            return commands.ToArray();
        }
    }
}