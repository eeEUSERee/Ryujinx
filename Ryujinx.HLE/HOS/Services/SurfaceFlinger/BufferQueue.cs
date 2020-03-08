namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferQueue
    {
        public static void CreateBufferQueue(Switch device, out BufferQueueProducer produer, out BufferQueueConsumer consumer)
        {
            BufferQueueCore core = new BufferQueueCore(device);

            produer  = new BufferQueueProducer(core);
            consumer = new BufferQueueConsumer(core);
        }
    }
}
