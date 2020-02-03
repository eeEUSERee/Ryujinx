using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.VDec
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct H264ParameterSets
    {
        public int  Log2MaxPicOrderCntLsbMinus4;
        [MarshalAs(UnmanagedType.I1)]
        public bool DeltaPicOrderAlwaysZeroFlag;
        [MarshalAs(UnmanagedType.I1)]
        public bool FrameMbsOnlyFlag;
        public int  PicWidthInMbs;
        public int  PicHeightInMapUnits;
        public int  Reserved6C;
        [MarshalAs(UnmanagedType.I1)]
        public bool EntropyCodingModeFlag;
        [MarshalAs(UnmanagedType.I1)]
        public bool BottomFieldPicOrderInFramePresentFlag;
        public int  NumRefIdxL0DefaultActiveMinus1;
        public int  NumRefIdxL1DefaultActiveMinus1;
        [MarshalAs(UnmanagedType.I1)]
        public bool DeblockingFilterControlPresentFlag;
        [MarshalAs(UnmanagedType.I1)]
        public bool RedundantPicCntPresentFlag;
        [MarshalAs(UnmanagedType.I1)]
        public bool Transform8x8ModeFlag;
        public int  Unknown8C;
        public int  Unknown90;
        public int  Reserved94;
        public int  Unknown98;
        public int  Reserved9C;
        public int  ReservedA0;
        public int  UnknownA4;
        public int  ReservedA8;
        public int  UnknownAC;
        public long Flags;
        public int  FrameNumber;
        public int  FrameNumber2;
    }
}