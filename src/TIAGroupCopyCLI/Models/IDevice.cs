using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.SW.Tags;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.Hmi;
using HmiTarget = Siemens.Engineering.Hmi.HmiTarget;
using Siemens.Engineering.Hmi.Tag;
using Siemens.Engineering.Hmi.Screen;
using Siemens.Engineering.Hmi.Cycle;
using Siemens.Engineering.Hmi.Communication;
using Siemens.Engineering.Hmi.Globalization;
using Siemens.Engineering.Hmi.TextGraphicList;
using Siemens.Engineering.Hmi.RuntimeScripting;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.Library;
using Siemens.Engineering.MC.Drives;

using TiaOpennessHelper.Utils;
using TIAHelper.Services;

namespace TIAGroupCopyCLI.Models
{
    interface IDevice
    {
        #region Fields
        List<Device> AllDevices { get; }

        #endregion Fields


        #region Methods
        void Save();

        void Restore();

        void AdjustFDestinationAddress(ulong aFDestOffset, ulong aLower, ulong aUpper);

        void Get1PnInterfaces();

        void GetAllPortsAndPartners();

        void RestoreAllPartnerPorts();

        void ChangeNames(string aPrefix);

        void ChangePnDeviceNames(string aPrefix);

        void ChangeIpAddresses(ulong aIpOffset);

        void SwitchIoSystem(Subnet aSubnet, IoSystem aIoSystem);

        void DisconnectFromSubnet();

        void ConnectToSubnet(Subnet aSubnet);

        void ConnectToSubnet(Subnet aSubnet, ulong aIpOffset);

        void ConnectToIoSystem(IoSystem aIoSystem);

        #endregion Methods


    }
}
