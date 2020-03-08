namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class BufferQueue
    {
        public static void CreateBufferQueue(Horizon system, out BufferQueueProducer produer, out BufferQueueConsumer consumer)
        {
            BufferQueueCore core = new BufferQueueCore(system);

            produer  = new BufferQueueProducer(core);
            consumer = new BufferQueueConsumer(core);
        }
    }
}
