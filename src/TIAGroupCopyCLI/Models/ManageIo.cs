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

    class ManageIo : ManageDevice
    {
        #region Fields
        List<AttributeAndAddress> AllStartAddresses = new List<AttributeAndAddress>();
        #endregion Fileds

        #region  Constructor

        public ManageIo(Device aDevice) : base(aDevice)
        {
            //AllDevices.Add(aDevice);
            Save();
        }
        public ManageIo(IList<Device> aDevices) : base(aDevices)
        {
            //AllDevices.AddRange(aDevices);
            Save();
        }

        /*
        public ManageIo(DeviceUserGroup aGroup)
        {
            //AllDevices.AddRange(aDevices);
            IList<Device> devices = Service.GetAllDevicesInGroup(aGroup);
            AllDevices = (List<Device>)devices;
            Save();
        }
        */
        #endregion

        #region Methodes
        public new  void Save()
        {
            List<AttributeAndAddress> returnStartAddressAndAddressObjects = new List<AttributeAndAddress>();
            IList<AttributeAndAddress> addStartAddressAndAddressObjects;

            foreach (Device currentDevice in AllDevices)
            {
                addStartAddressAndAddressObjects =  (List<AttributeAndAddress>)Service.GetValueAndAddressWithAttribute(currentDevice.DeviceItems, "StartAddress");
                returnStartAddressAndAddressObjects.AddRange(addStartAddressAndAddressObjects);
            }
            AllStartAddresses = returnStartAddressAndAddressObjects;
        }

        
        public new void Restore()
        {
            foreach (AttributeAndAddress currentAddress in AllStartAddresses)
            {

                currentAddress.RestoreValue();
            }

            base.Restore();

        }

        #endregion

    }
}
