namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    struct DequeueBufferObject
    {
        public int  Async;
        public int  Width;
        public int  Height;
        public int  Format;
        public uint Usage;
    }
}
