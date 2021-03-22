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
using System.IO;

using TiaOpennessHelper.Utils;
using TIAHelper.Services;


namespace TIAGroupCopyCLI.Models
{

    class ManageIo : ManageDevice , IManageDevice
    {
        #region Fields
        public DeviceType DeviceType { get; } = DeviceType.ioDevice;

        ManageAttributeGroup StartAddresses = new ManageAttributeGroup();
        #endregion Fileds

        #region  Constructor
        public ManageIo(Device aDevice) : base(aDevice)
        {
        }
        #endregion

        #region Methodes
        public new  void SaveConfig()
        {
            StartAddresses.FindAndSaveAddressAttributes(Device, "StartAddress");
            base.SaveConfig();
        }

        public new void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong lowerFDest, ulong upperFDest)
        {
            StartAddresses.Restore();
            base.RestoreConfig_WithAdjustments(pnDeviceNumberOffset, fSourceOffset, fDestOffset, lowerFDest, upperFDest);
        }

        #endregion

    }
}
