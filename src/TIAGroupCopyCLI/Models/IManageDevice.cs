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
    public enum DeviceType
    {
        udefined = 0,
        Plc = 1,
        Hmi = 2,
        Drive = 3,
        ioDevice = 10,
        others = 99

    }

    interface IManageDevice
    {

        #region Fields
        //List<Device> AllDevices { get; }
        Device Device { get; }
        DeviceType DeviceType { get; }
        List<ManageNetworkInterface> NetworkInterfaces { get; }

        #endregion Fields

        #region Methods

        void SaveConfig();

        void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong lowerFDest, ulong upperFDest);

        void StripGroupNumAndPrefix(string devicePrefix);

        void RestoreGroupNumAndPrefix();

        void ChangeGroupNumAndPrefix(string devicePrefix, string groupNumber);

        void AddOffsetToIpAddresse(ulong aIpOffset);

        void Reconnect(Subnet aSubnet, IoSystem aIoSystem);

        void DisconnectFromSubnet();

        void ConnectToSubnet(Subnet aSubnet);

        void ConnectToIoSystem(IoSystem aIoSystem);

        #endregion Methods

    }
}
