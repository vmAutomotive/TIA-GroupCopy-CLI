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


namespace TIAGroupCopyCLI.Models
{
    public class ManageNetworkPort
    {
        #region References to openness object
        private NetworkPort NetworkPort;
        #endregion

        #region Fields for Saved Information
        private readonly List<NetworkPort> PartnerPorts = new List<NetworkPort>();
        private bool isConnected;
        #endregion Fileds

        #region Constructor

        public ManageNetworkPort(NetworkPort networkPort)
        {
            NetworkPort = networkPort;
        }

        #endregion Constuctor

        #region Methods

        public void SaveConfig()
        {
            if (NetworkPort != null)
            {
                foreach (NetworkPort partnerPort in NetworkPort.ConnectedPorts)
                {
                    PartnerPorts.Add(partnerPort);
                    isConnected = true;
                }
            }
        }

        public void RestoreConfig()
        {
            if ((NetworkPort != null) && isConnected)
            {
                foreach (NetworkPort partnerPort in PartnerPorts)
                {
                    try
                    {
                        NetworkPort.ConnectToPort(partnerPort);
                    }
                    catch
                    {
                    }
                }
            }
        }
        #endregion Methods

        #region Static Methods

        public static List<ManageNetworkPort> GetAll_ManageNetworkPortObjects(NetworkInterface networkInterface)
        {
            List<ManageNetworkPort> returnManagePortObjects = new List<ManageNetworkPort>();

            foreach (NetworkPort currentItem in networkInterface.Ports)
            {
                returnManagePortObjects.Add(new ManageNetworkPort(currentItem));
            }

            return returnManagePortObjects;
        }
        public static List<ManageNetworkPort> GetAll_ManageNetworkPortObjects(Device device)
        {
            List<ManageNetworkPort> returnManagePortObjects = new List<ManageNetworkPort>();

            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                List<ManageNetworkPort> newManageNetworkPortObjects = GetAll_ManageNetworkPortObjects(currentDeviceItem);
                if (newManageNetworkPortObjects.Count > 0)
                {
                    returnManagePortObjects.AddRange(newManageNetworkPortObjects);
                }
            }

            return returnManagePortObjects;
        }
        private static List<ManageNetworkPort> GetAll_ManageNetworkPortObjects(DeviceItem deviceItem)
        {
            List<ManageNetworkPort> returnManagePortObjects = new List<ManageNetworkPort>();

            NetworkPort newNetworkPort = deviceItem.GetService<NetworkPort>();
            if (newNetworkPort != null)
            {
                returnManagePortObjects.Add(new ManageNetworkPort(newNetworkPort));
            }

            foreach (DeviceItem currentDeviceItem in deviceItem.DeviceItems)
            {
                //call recursive
                List<ManageNetworkPort> newManageNetworkPortObjects = GetAll_ManageNetworkPortObjects(currentDeviceItem);
                if (newManageNetworkPortObjects.Count > 0)
                {
                    returnManagePortObjects.AddRange(newManageNetworkPortObjects);
                }
            }

            return returnManagePortObjects;
        }

        #endregion
    }
}
