using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ssl
{
    class ISslContext : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public ISslContext()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                {4, ImportServerPki}
            };
        }

        public long ImportServerPki(ServiceCtx context) {

            Logger.PrintStub(LogClass.ServiceSsl);

            return 0;
        }
    }
}