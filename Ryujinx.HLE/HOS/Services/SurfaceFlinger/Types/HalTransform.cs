using System;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    [Flags]
    enum HalTransform : uint
    {
        FlipX     = 1,
        FlipY     = 2,
        Rotate90  = 4,
        Rotate180 = FlipX    | FlipY,
        Rotate270 = Rotate90 | Rotate180
    }
}