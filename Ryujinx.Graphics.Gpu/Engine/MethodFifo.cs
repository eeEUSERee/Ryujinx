using Ryujinx.Graphics.Gpu.State;

namespace Ryujinx.Graphics.Gpu.Engine
{
    partial class Methods
    {
        /// <summary>
        /// Wait for the GPU to be idle
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="argument">Method call argument</param>
        public void WaitForIdle(GpuState state, int argument)
        {
            PerformDeferredDraws();

            _context.Renderer.Pipeline.Barrier();
        }

        /// <summary>
        /// Send macro code/data to the MME
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="argument">Method call argument</param>
        public void SendMacroCodeData(GpuState state, int argument)
        {
            int macroUploadAddress = state.Get<int>(MethodOffset.MacroUploadAddress);

            _context.Fifo.SendMacroCodeData(macroUploadAddress++, argument);

            state.Write((int)MethodOffset.MacroUploadAddress, macroUploadAddress);
        }

        /// <summary>
        /// Bind a macro index to a position for the MME
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="argument">Method call argument</param>
        public void BindMacro(GpuState state, int argument)
        {
            int macroBindingIndex = state.Get<int>(MethodOffset.MacroBindingIndex);

            _context.Fifo.BindMacro(macroBindingIndex++, argument);

            state.Write((int)MethodOffset.MacroBindingIndex, macroBindingIndex);
        }
    }
}
