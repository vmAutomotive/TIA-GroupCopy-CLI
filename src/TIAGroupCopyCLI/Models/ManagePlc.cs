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

    public class TransferAreaAndAttributes
    {
        #region Fileds

        public AttributeValue PartnerStartAddress;
        private readonly TransferArea TransferArea;
        #endregion Fileds

        #region Constructor
        public TransferAreaAndAttributes(TransferArea aTransferArea)
        {
            TransferArea = aTransferArea;
            if (aTransferArea != null)
            {
                PartnerStartAddress = Service.GetAttribute(aTransferArea.PartnerAddresses[0], "StartAddress");
            }
        }

        #endregion Constructor

        #region methods
        public void SavePartnerStartAddress()
        {
            if (TransferArea != null)
            {
                PartnerStartAddress = Service.GetAttribute(TransferArea.PartnerAddresses[0], "StartAddress");
            }

        }

        public void RestorePartnerStartAddress()
        {
            if (TransferArea != null)
            {
                if (PartnerStartAddress != null)
                {
                    Service.SetAttribute(TransferArea.PartnerAddresses[0], "StartAddress", PartnerStartAddress);
                }
            }

        }
        #endregion methods
    }

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
            if (AxisHardwareConnection != null)
            {
                addressIn = AxisHardwareConnection.ActorInterface.InputAddress;
                addressOut = AxisHardwareConnection.ActorInterface.OutputAddress;
                connectOption = AxisHardwareConnection.ActorInterface.ConnectOption;
                isConnected = AxisHardwareConnection.ActorInterface.IsConnected;
            }
        }
        #endregion Constructor

        #region Methods
        public void Save()
        {
            if (AxisHardwareConnection != null)
            {
                addressIn = AxisHardwareConnection.ActorInterface.InputAddress;
                addressOut = AxisHardwareConnection.ActorInterface.OutputAddress;
                connectOption = AxisHardwareConnection.ActorInterface.ConnectOption;
                isConnected = AxisHardwareConnection.ActorInterface.IsConnected;
            }

        }

        public void Restore()
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

    class ManagePlc : ManageDevice
    {
        #region Fileds  


        public AttributeAndDeviceItem LowerBoundForFDestinationAddresses_attribues;
        public AttributeAndDeviceItem UpperBoundForFDestinationAddresses_attribues;

        public Subnet originalSubnet;
        public IoSystem originalIoSystem;
        public IoSystem newIoSystem;

        private readonly List<TransferAreaAndAttributes> AllIDevicePartnerIoAddrsses = new List<TransferAreaAndAttributes>();
        private readonly List<ConnectionProviderAndAttributes> AllToConnections = new List<ConnectionProviderAndAttributes>();
        AttributeAndDeviceItem CentralFSourceAddress_attribue;

        PlcSoftware plcSoftware;

        #endregion Fileds

        #region Constructor

        public ManagePlc(Device aDevice) : base(aDevice)
        {
            Save();
        }


        public ManagePlc(IList<Device> aDevices) : base(aDevices)
        {
            Save();
        }

        #endregion constructor

        #region Methods
        public void ChangeIoSystemName(string aPrefix)
        {
            //get PLCs in sub folders - recursive


            try
            {
                FirstPnNetworkInterfaces[0].IoControllers[0].IoSystem.Name = aPrefix + FirstPnNetworkInterfaces[0].IoControllers[0].IoSystem.Name;
            }
            catch
            {
            }
        }

        public new void Save()
        {

            Device currentDevice = AllDevices[0];
            CentralFSourceAddress_attribue = Service.Get1ValueAndDeviceItemWithAttribute(currentDevice.DeviceItems, "Failsafe_CentralFSourceAddress");
            LowerBoundForFDestinationAddresses_attribues = Service.Get1ValueAndDeviceItemWithAttribute(currentDevice.DeviceItems, "Failsafe_LowerBoundForFDestinationAddresses");
            UpperBoundForFDestinationAddresses_attribues = Service.Get1ValueAndDeviceItemWithAttribute(currentDevice.DeviceItems, "Failsafe_UpperBoundForFDestinationAddresses");
            //xFDestinationAddress_attribues = Service.GetValueAndDeviceItemsWithAttribute(currentDevice.DeviceItems, "Failsafe_FDestinationAddress");

            try
            {
                originalSubnet = FirstPnNetworkInterfaces[0].Nodes[0].ConnectedSubnet;
                originalIoSystem = FirstPnNetworkInterfaces[0].IoConnectors[0].ConnectedToIoSystem;
            }
            catch (EngineeringTargetInvocationException)
            {
            }
            //GetAll_I_DeviceParnerAdresses();

            plcSoftware = Service.GetPlcSoftware(currentDevice);
            GetAllToConnections();

        }

        public new  void  Restore()
        {
            if (CentralFSourceAddress_attribue != null)
            {
                CentralFSourceAddress_attribue.Restore();
                LowerBoundForFDestinationAddresses_attribues.Restore();
                UpperBoundForFDestinationAddresses_attribues.Restore();

                //ulong lower = (ulong)LowerBoundForFDestinationAddresses_attribues.Value;
                //ulong upper = (ulong)UpperBoundForFDestinationAddresses_attribues.Value;
            }
            base.Restore();

            //foreach (AttributeAndDeviceItem item in xFDestinationAddress_attribues)  //.Where(i => true)
            //{
            //    if (((ulong)item.Value >= lower) && ((ulong)item.Value <= upper))
            //    {
             //       item.Restore();
            //    }
           // }

            RestorePnDeviceNumber();
            SetAllIDeviceParnerAdresses();
        }

        public void GetAll_iDeviceParnerIoAdresses()
        {
            try
            {
                GetAll_iDeviceParnerIoAdresses_Internal(); //this is not possible in V15.0
            }
            catch(MissingMethodException) 
            {
            }
        }

        private void GetAll_iDeviceParnerIoAdresses_Internal()
        {
            foreach (TransferArea currentTransferArea in FirstPnNetworkInterfaces[0].TransferAreas)
            {

                if (currentTransferArea.PartnerAddresses.Count >= 0)
                {
                    TransferAreaAndAttributes newTransferArea = new TransferAreaAndAttributes(currentTransferArea);
                    if (newTransferArea != null)
                    {
                        AllIDevicePartnerIoAddrsses.Add(newTransferArea);
                    }
                }
            }
        }

        public void SetAllIDeviceParnerAdresses()
        {
            foreach (TransferAreaAndAttributes item in AllIDevicePartnerIoAddrsses)
            {
                    item.RestorePartnerStartAddress();
            }
        }

        public void GetAllToConnections()
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

        public void SetAllToConnections()
        {
            foreach (ConnectionProviderAndAttributes item in AllToConnections)
            {
                item.Restore();
            }
        }

        public void CopyFromTemplate(ManagePlc aTemplatePlc)
        {
            if (aTemplatePlc.CentralFSourceAddress_attribue?.Value != null) CentralFSourceAddress_attribue.Value = aTemplatePlc.CentralFSourceAddress_attribue?.Value;
            if (aTemplatePlc.LowerBoundForFDestinationAddresses_attribues?.Value != null) LowerBoundForFDestinationAddresses_attribues.Value = aTemplatePlc.LowerBoundForFDestinationAddresses_attribues?.Value;
            if (aTemplatePlc.UpperBoundForFDestinationAddresses_attribues?.Value != null) UpperBoundForFDestinationAddresses_attribues.Value = aTemplatePlc.UpperBoundForFDestinationAddresses_attribues?.Value;


            for (int i = 0; i < aTemplatePlc.FDestinationAddress_attribues.Count; i++)
            {
                FDestinationAddress_attribues[i].Value = aTemplatePlc.FDestinationAddress_attribues[i].Value;
            }

            //AllIDevicePartnerAddrsses = aTemplatePlc.AllIDevicePartnerAddrsses.CopyTo;
            for (int i = 0; i < aTemplatePlc.AllIDevicePartnerIoAddrsses.Count; i++)
            {

                AllIDevicePartnerIoAddrsses[i].PartnerStartAddress.Value = aTemplatePlc.AllIDevicePartnerIoAddrsses[i].PartnerStartAddress.Value;
            }

            for (int i = 0; i < aTemplatePlc.PnDeviceNumberOfFirstPnNetworkInterfaces.Count; i++)
            {
                //AllIDevicePartnerAddrsses[i].PartnerStartAddress.Value = aTemplatePlc.AllIDevicePartnerAddrsses[i].PartnerStartAddress.Value;
                if (PnDeviceNumberOfFirstPnNetworkInterfaces.Count < i)
                {
                    PnDeviceNumberOfFirstPnNetworkInterfaces.Add(new AttributeInfo());
                }
                if (PnDeviceNumberOfFirstPnNetworkInterfaces[i] == null)
                {
                    PnDeviceNumberOfFirstPnNetworkInterfaces[i] = new AttributeInfo()
                    {
                        Name = "PnDeviceNumber"
                    };
                }

                PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value = aTemplatePlc.PnDeviceNumberOfFirstPnNetworkInterfaces[i]?.Value;
            }

        }
        public  void  AdjustFSettings(ulong FSourceOffset, ulong aFDestOffset)
        {
            if (CentralFSourceAddress_attribue != null)
            {
                ulong oldLower = (ulong)LowerBoundForFDestinationAddresses_attribues.Value;
                ulong oldUpper = (ulong)UpperBoundForFDestinationAddresses_attribues.Value;

                CentralFSourceAddress_attribue.AddToValue(FSourceOffset);
                LowerBoundForFDestinationAddresses_attribues.AddToValue(aFDestOffset);
                UpperBoundForFDestinationAddresses_attribues.AddToValue(aFDestOffset);

                base.AdjustFDestinationAddress(aFDestOffset, oldLower, oldUpper);
                //foreach (AttributeAndDeviceItem item in xFDestinationAddress_attribues)  //.Where(i => true)
                //{
                //   if (((ulong)item.Value >= oldUower) && ((ulong)item.Value <= oldLpper))
                //    {
                //        item.AddToValue(aFDestOffset);
                //    }
                //}
            }

        }

        public void AdjustPartnerIoAddresses(ulong aIDeviceOffsett)
        {

            foreach (TransferAreaAndAttributes item in AllIDevicePartnerIoAddrsses)  //.Where(i => true)
            {
                {
                    item.PartnerStartAddress.AddToValue(aIDeviceOffsett);
                }
            }

        }

        public void CreateNewIoSystem(Subnet aSubnet, string aPrefix)
        {
            try
            {
                //string tempIPaddress = (string)FirstPnNetworkInterfaces[0].Nodes[0].GetAttribute("Address");
                string IoSystemName = FirstPnNetworkInterfaces[0].IoControllers[0].IoSystem.Name;
                FirstPnNetworkInterfaces[0].Nodes[0].DisconnectFromSubnet();
                FirstPnNetworkInterfaces[0].Nodes[0].ConnectToSubnet(aSubnet);
                newIoSystem = FirstPnNetworkInterfaces[0].IoControllers[0].CreateIoSystem(aPrefix + IoSystemName);
                //FirstPnNetworkInterfaces[0].Nodes[0].SetAttribute("Address", tempIPaddress);
            }
            catch (NullReferenceException)
            { }

        }

        public void ConnectToMasterIoSystem(IoSystem aIoSystem)
        {
            if (aIoSystem != null )
            {
                FirstPnNetworkInterfaces[0].IoConnectors[0].ConnectToIoSystem(aIoSystem);
            }
        }


        public void RestorePnDeviceNumber()
        {
            if ((FirstPnNetworkInterfaces[0].IoConnectors.Count > 0))
            {
                if ((PnDeviceNumberOfFirstPnNetworkInterfaces[0]?.Value ?? null) != null)
                {
                    FirstPnNetworkInterfaces[0].IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[0].Name, PnDeviceNumberOfFirstPnNetworkInterfaces[0].Value);
                }
            }
        }

        public void AdjustPnDeviceNumberWithOffset(uint aOffset)
        {
            if ((FirstPnNetworkInterfaces[0].IoConnectors.Count > 0))
            {
                if ((PnDeviceNumberOfFirstPnNetworkInterfaces[0]?.Value ?? null) != null)
                {
                    PnDeviceNumberOfFirstPnNetworkInterfaces[0].AddToValue(aOffset);
                }
            }
        }
        public void DelecteOldSubnet()
        {
            try
            {
                originalSubnet.Delete();
            }
            catch (NullReferenceException)
            { }
        }

        #endregion methods
    }

}

