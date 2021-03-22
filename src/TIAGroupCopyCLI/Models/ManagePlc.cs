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
using Siemens.Engineering.SW.TechnologicalObjects;
using Siemens.Engineering.SW.TechnologicalObjects.Motion;
using System.IO;

using TiaOpennessHelper.Utils;
using TIAHelper.Services;

namespace TIAGroupCopyCLI.Models
{



    public class ConnectionProviderAndAttributes
    {
        #region Fileds
        private readonly AxisHardwareConnectionProvider AxisHardwareConnection;
        Int32 addressIn;
        Int32 addressOut;
        ConnectOption connectOption;
        bool isConnected;
        #endregion Fileds

        #region Constructor
        public ConnectionProviderAndAttributes(AxisHardwareConnectionProvider connectionProvider)
        {
            AxisHardwareConnection = connectionProvider;
            SaveConfig();
        }
        #endregion Constructor

        #region Methods
        public void SaveConfig()
        {
            if (AxisHardwareConnection != null)
            {
                addressIn = AxisHardwareConnection.ActorInterface.InputAddress;
                addressOut = AxisHardwareConnection.ActorInterface.OutputAddress;
                connectOption = AxisHardwareConnection.ActorInterface.ConnectOption;
                isConnected = AxisHardwareConnection.ActorInterface.IsConnected;
            }

        }

        public void RestoreConfig()
        {
            if ((AxisHardwareConnection != null) && isConnected)
            {
                try
                {
                    AxisHardwareConnection.ActorInterface.Disconnect();
                    AxisHardwareConnection.ActorInterface.Connect(addressIn, addressOut, connectOption);
                }
                catch (EngineeringTargetInvocationException)
                { }
            }

        }

        #endregion Methods
    }

    class ManagePlc : ManageDevice , IManageDevice
    {
        #region Fileds  
        public DeviceType DeviceType { get; } = DeviceType.Plc;
        public PlcSoftware plcSoftware;

        public SingleAttribute LowerBoundForFDestinationAddresses_attribue;
        public SingleAttribute UpperBoundForFDestinationAddresses_attribue;
        public SingleAttribute CentralFSourceAddress_attribue;
        
        private readonly List<ConnectionProviderAndAttributes> AllToConnections = new List<ConnectionProviderAndAttributes>();

        #endregion Fileds

        #region Constructor
        public ManagePlc(Device aDevice) : base(aDevice)
        {
            plcSoftware = Get_PlcSoftware(aDevice);
        }
        #endregion constructor

        #region Methods
        
        public new void SaveConfig()
        {
            CentralFSourceAddress_attribue = SingleAttribute.FindAndSaveFirstDeviceItemAtribute(Device, "Failsafe_CentralFSourceAddress");
            LowerBoundForFDestinationAddresses_attribue = SingleAttribute.FindAndSaveFirstDeviceItemAtribute(Device, "Failsafe_LowerBoundForFDestinationAddresses");
            UpperBoundForFDestinationAddresses_attribue = SingleAttribute.FindAndSaveFirstDeviceItemAtribute(Device, "Failsafe_UpperBoundForFDestinationAddresses");

            Save_iDeviceParnerIoAdresses();
            Save_ToConnections();

            base.SaveConfig();
        }

        public (ulong, ulong) CopyFromTemplate(ManagePlc aTemplatePlc)
        {
            ulong lowerFDest = 0;
            ulong upperFDest = 0;

            if (aTemplatePlc?.CentralFSourceAddress_attribue?.Value != null) CentralFSourceAddress_attribue.Value = aTemplatePlc.CentralFSourceAddress_attribue?.Value;
            if (aTemplatePlc?.LowerBoundForFDestinationAddresses_attribue?.Value != null)
            {
                lowerFDest = (ulong)aTemplatePlc.LowerBoundForFDestinationAddresses_attribue.Value;
                LowerBoundForFDestinationAddresses_attribue.Value = aTemplatePlc.LowerBoundForFDestinationAddresses_attribue.Value;
            }
            if (aTemplatePlc?.UpperBoundForFDestinationAddresses_attribue?.Value != null)
            {
                upperFDest = (ulong)aTemplatePlc.UpperBoundForFDestinationAddresses_attribue.Value;
                UpperBoundForFDestinationAddresses_attribue.Value = aTemplatePlc.UpperBoundForFDestinationAddresses_attribue.Value;
            }

            base.CopyFromTemplate(aTemplatePlc);

            return (lowerFDest, upperFDest);
        }

        public new void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong lowerFDest, ulong upperFDest)
        {
            if (CentralFSourceAddress_attribue != null)
            {
                CentralFSourceAddress_attribue.RestoreWithOffset(fSourceOffset);
                LowerBoundForFDestinationAddresses_attribue.RestoreWithOffset(fDestOffset);
                UpperBoundForFDestinationAddresses_attribue.RestoreWithOffset(fDestOffset);
            }

            base.RestoreConfig_WithAdjustments(pnDeviceNumberOffset, fSourceOffset, fDestOffset, lowerFDest, upperFDest);
        }

        public override void StripGroupNumAndPrefix(string devicePrefix)
        {
            base.StripGroupNumAndPrefix(devicePrefix);
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].StripGroupNumAndPrefixFromIoSystemName(devicePrefix);
            }
        }

        public override void RestoreGroupNumAndPrefix()
        {
            base.RestoreGroupNumAndPrefix();
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].RestoreGroupNumAndPrefixToIoSystemName();
            }
        }

        public override void ChangeGroupNumAndPrefix(string devicePrefix, string groupNumber)
        {
            base.ChangeGroupNumAndPrefix(devicePrefix, groupNumber);
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].ChangeGroupNumAndPrefixToIoSystemName(devicePrefix, groupNumber);
            }
        }

        public void Save_iDeviceParnerIoAdresses()
        {
            try
            {
                foreach (ManageNetworkInterface currentNetworkInterface in NetworkInterfaces)
                {
                    currentNetworkInterface.Save_iDeviceParnerIoAdresses(); //this is not possible in V15.0
                }
            }
            catch (MissingMethodException)
            {
            }
        }
        public void Restore_iDeviceParnerAdresses(ulong aIDeviceOffsett = 0)
        {
            foreach (ManageNetworkInterface currentNetworkInterface in NetworkInterfaces)
            {
                currentNetworkInterface.Restore_iDeviceParnerAdressesWithOffest(aIDeviceOffsett);
            }

        }
        
        public void Save_ToConnections()
        {
            foreach (TechnologicalInstanceDB currentTechnologicalInstanceDB in plcSoftware.TechnologicalObjectGroup.TechnologicalObjects)
            {
                AxisHardwareConnectionProvider connectionProvider = currentTechnologicalInstanceDB.GetService<AxisHardwareConnectionProvider>();

                if (connectionProvider != null)
                {
                    ConnectionProviderAndAttributes newItem = new ConnectionProviderAndAttributes(connectionProvider);
                    if (newItem != null)
                    {
                        AllToConnections.Add(newItem);
                    }
                }
            }
        }

        public void Restore_ToConnections()
        {
            foreach (ConnectionProviderAndAttributes item in AllToConnections)
            {
                item.RestoreConfig();
            }
        }
                     
        public IoSystem CreateNewIoSystem(Subnet aSubnet, int aNetworkInterfaceNumber = 0)
        {
            IoSystem newIoSystem = NetworkInterfaces[aNetworkInterfaceNumber]?.CreateNewIoSystem(aSubnet);
            return newIoSystem;
        }

        public void ConnectToIoSystem(IoSystem aIoSystem, int aNetworkInterfaceNumber = 0, int aIoConnectorNumber = 0)
        {
            NetworkInterfaces[aNetworkInterfaceNumber]?.ConnectToIoSystem(aIoSystem, aIoConnectorNumber);

        }

        #endregion methods

        #region static methods
        public static PlcSoftware Get_PlcSoftware(Device device)
        {
            //PlcSoftware plcSoftware = null;
            foreach (DeviceItem currentDeviceItem in device.DeviceItems.Where(d => d.Classification.ToString() == "CPU"))
            {
                //hole Softwareblöcke, die PLC_1 untergeordnet sind
                SoftwareContainer softwareContainer = currentDeviceItem.GetService<SoftwareContainer>();
                if (softwareContainer != null)
                {
                    if (softwareContainer.Software is PlcSoftware plc)
                    {
                        return plc;
                    }
                }
            }
            return null;
        }

        #endregion
    }

}

