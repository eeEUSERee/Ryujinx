using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Services.Nifm
{
    class IGeneralService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IGeneralService()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 4, CreateRequest           },
                { 12, GetCurrentIpAddress    },
                { 15, GetCurrentIpConfigInfo }
            };
        }

        public long CreateRequest(ServiceCtx context)
        {
            int unknown = context.RequestData.ReadInt32();

            MakeObject(context, new IRequest(context.Device.System));

            Logger.PrintStub(LogClass.ServiceNifm);

            return 0;
        }

        public long GetCurrentIpAddress(ServiceCtx context)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return MakeError(ErrorModule.Nifm, NifmErr.NoInternetConnection);
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress address = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            context.ResponseData.Write(BitConverter.ToUInt32(address.GetAddressBytes()));

            Logger.PrintInfo(LogClass.ServiceNifm, $"Console's local IP is \"{address}\".");

            return 0;
        }

        public long GetCurrentIpConfigInfo(ServiceCtx context)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return MakeError(ErrorModule.Nifm, NifmErr.NoInternetConnection);
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress address = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            IPInterfaceProperties targetProperties = null;
            UnicastIPAddressInformation targetAddress = null;

            foreach (NetworkInterface adapter in interfaces)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();

                    UnicastIPAddressInformationCollection unicastCollection = properties.UnicastAddresses;
                    if (unicastCollection != null && unicastCollection.Count > 0 )
                    {
                        foreach (UnicastIPAddressInformation info in unicastCollection)
                        {
                            if (info.Address == address && properties.GatewayAddresses.Count > 0
                                && properties.DnsAddresses.Count > 1)
                            {
                                targetProperties = properties;
                                targetAddress = info;
                                break;
                            }
                        }
                    }

                    if (targetProperties != null)
                    {
                        break;
                    }
                }
            }

            if (targetProperties != null)
            {
                Logger.PrintInfo(LogClass.ServiceNifm, $"Console's local IP is \"{address}\".");

                // Unknown
                context.ResponseData.Write(false);

                // Ip address
                context.ResponseData.Write(BitConverter.ToUInt32(targetAddress.Address.GetAddressBytes()));

                // Sub network mask
                context.ResponseData.Write(BitConverter.ToUInt32(targetAddress.IPv4Mask.GetAddressBytes()));

                // Gateway address
                context.ResponseData.Write(BitConverter.ToUInt32(targetProperties.GatewayAddresses[0].Address.GetAddressBytes()));



                // Unknown
                context.ResponseData.Write(false);

                // Primary DNS address
                context.ResponseData.Write(BitConverter.ToUInt32(targetProperties.DnsAddresses[0].GetAddressBytes()));

                // Secondary DNS address
                context.ResponseData.Write(BitConverter.ToUInt32(targetProperties.DnsAddresses[1].GetAddressBytes()));
            }
            else
            {
                return MakeError(ErrorModule.Nifm, NifmErr.NoInternetConnection);
            }

            return 0;
        }
    }
}
