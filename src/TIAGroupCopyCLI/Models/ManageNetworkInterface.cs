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
using TIAGroupCopyCLI.MessagingFct;
using System.Text.RegularExpressions;
using TIAGroupCopyCLI.AppExceptions;

namespace TIAGroupCopyCLI.Models
{

    public class ManageNetworkInterface
    {
        #region References to openness object and managed objcts
        private readonly DeviceItem DeviceItem;
        private readonly NetworkInterface NetworkInterface;
        private List<ManageNetworkPort> DevicePorts { get; set; } = new List<ManageNetworkPort>();
        #endregion

        #region Fields for Saved Information
        //private SingleAttribute PnDeviceName;
        private SingleAttribute PnDeviceNumber;
        private List<TransferAreaAndAttributes> IDevicePartnerIoAddrsses = new List<TransferAreaAndAttributes>();
        private bool isConnectedToIoSystem;
        private bool isConnectedtoNetwork;
        private readonly string OriginalIoSystem0TiaName;
        private string TemplateIoSystem0TiaName;
        private readonly string OriginalPnDeviceName;
        private string TemplatePnDeviceName;
        #endregion Fileds

        #region Constructor
        public ManageNetworkInterface(DeviceItem deviceItem, NetworkInterface networkInterface = null)
        {
            DeviceItem = deviceItem ?? throw new ArgumentNullException(nameof(deviceItem), "Invalid deviceItem");
            if (networkInterface != null)
            {
                NetworkInterface = networkInterface;
            }
            else
            {
                NetworkInterface = deviceItem.GetService<NetworkInterface>();
            }
            DevicePorts = ManageNetworkPort.GetAll_ManageNetworkPortObjects(NetworkInterface);

            try
            {
                if (NetworkInterface.IoControllers.Count > 0)
                {
                    OriginalIoSystem0TiaName = NetworkInterface.IoControllers[0].IoSystem.Name;
                }
            }
            catch(System.NullReferenceException)
            { }
            catch (Siemens.Engineering.EngineeringTargetInvocationException)
            { }

            try
            {
                if (NetworkInterface?.Nodes.Count > 0)
                {
                    object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceNameAutoGeneration");
                    if (attributeValue is bool value)
                        if (value == false)
                        {
                            OriginalPnDeviceName = (string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName");
                        }
                }
            }
            catch
            {  }

        }                
        #endregion Constuctor

        #region Methods

        public void SaveConfig()
        {
            if (NetworkInterface?.IoConnectors?.Count > 0)
            {
                PnDeviceNumber = SingleAttribute.GetSimpleAttributeObject(NetworkInterface.IoConnectors[0], "PnDeviceNumber");
                if (NetworkInterface.IoConnectors[0].ConnectedToIoSystem != null) isConnectedToIoSystem = true;
            }

            
            if (NetworkInterface?.Nodes.Count > 0)
            {
                /*
                object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceNameAutoGeneration");
                if (attributeValue is bool value)
                    if (value == false)
                    {
                        PnDeviceName = SingleAttribute.GetSimpleAttributeObject(NetworkInterface.Nodes[0], "PnDeviceName");
                    }
                */
                if (NetworkInterface.Nodes[0].ConnectedSubnet != null) isConnectedtoNetwork = true;
            }
            

                foreach (ManageNetworkPort currentPort in DevicePorts)
            {
                currentPort.SaveConfig();
            }

        }

        public void CopyFromTemplate(ManageNetworkInterface templateManageNetworkInterface)
        {
            if (templateManageNetworkInterface?.PnDeviceNumber != null)
            {
                if (PnDeviceNumber == null)
                {
                    if (NetworkInterface?.IoConnectors?.Count > 0)
                    {
                        PnDeviceNumber = new SingleAttribute(NetworkInterface.IoConnectors[0], "PnDeviceNumber", templateManageNetworkInterface.PnDeviceNumber.Value);
                    }
                    
                }
                else
                {
                    PnDeviceNumber.Value = templateManageNetworkInterface.PnDeviceNumber.Value;
                }
            }

            if (IDevicePartnerIoAddrsses.Count < templateManageNetworkInterface.IDevicePartnerIoAddrsses.Count)
            {
                IDevicePartnerIoAddrsses.Clear();
                Save_iDeviceParnerIoAdresses();
            }
            for (int i = 0; i < templateManageNetworkInterface.IDevicePartnerIoAddrsses.Count; i++)
            {
                IDevicePartnerIoAddrsses[i].PartnerStartAddress.Value = templateManageNetworkInterface.IDevicePartnerIoAddrsses[i].PartnerStartAddress.Value;
            }
        }


        public void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset)
        {
            foreach (ManageNetworkPort currentItem in DevicePorts)
            {
                currentItem.RestoreConfig();
            }
            
            PnDeviceNumber?.RestoreWithOffset((int)pnDeviceNumberOffset);
            //PnDeviceName?.RestoreWithPrefix(prefix);
        }

        public void AddOffsetToIpAddress(ulong ipAddressOffset, int nodeNumber = 0)
        {
            if (NetworkInterface?.Nodes?.Count > nodeNumber)
            {
                string[] tempIPaddress = null;
                try
                {
                    tempIPaddress = ((string)NetworkInterface.Nodes[nodeNumber].GetAttribute("Address")).Split('.');
                }
                catch
                {  }

                if (tempIPaddress != null)
                {
                    try
                    {
                        NetworkInterface.Nodes[nodeNumber].SetAttribute("Address", tempIPaddress[0] + "." + tempIPaddress[1] + "." + (Convert.ToInt32(tempIPaddress[2]) + (uint)ipAddressOffset) + "." + tempIPaddress[3]);
                    }
                    catch (Exception ex)
                    {
                        Messaging.FaultMessage("Could not set IP address of " + DeviceItem?.Name ?? "{null}" + "." + NetworkInterface?.Nodes?[nodeNumber]?.Name ?? "{null}", ex, "ManageNetworkInterface.ChangeIpAddresses");
                    }
                }
            }
        }

        public void StripGroupNumAndPrefixFromPnDeviceName(string devicePrefix)
        {
            try
            {
                if (NetworkInterface?.Nodes.Count > 0)
                {
                    object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceNameAutoGeneration");
                    if (attributeValue is bool value)
                        if (value == false)
                        {
                            TemplatePnDeviceName = "temp" + Regex.Replace(OriginalPnDeviceName, "^" + devicePrefix + "\\d+", "", RegexOptions.IgnoreCase);
                            SingleAttribute.SetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName", TemplatePnDeviceName);

                            //check
                            string tempCheckPnDeviceName = (string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName");
                            if (tempCheckPnDeviceName != TemplatePnDeviceName)
                            {
                                throw new GroupCopyException($"Could not rename PN DevicceName \"{OriginalPnDeviceName}\" in selected template group to generic name \"{TemplatePnDeviceName}\", probably because that name already exsits.");
                            }
                        }
                }
            }
            catch
            {
            }
        }

        public void RestoreGroupNumAndPrefixToPnDeviceName()
        {

            try
            {
                if (NetworkInterface?.Nodes.Count > 0)
                {
                    object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceNameAutoGeneration");
                    if (attributeValue is bool value)
                        if (value == false)
                        {
                            if (OriginalPnDeviceName.Length == 0)
                            {
                                throw new ProgrammingException($"Could not restore PN DeviceName of template Group because the orignal name is blank \"{OriginalPnDeviceName}\".");
                            }
                            SingleAttribute.SetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName", OriginalPnDeviceName);

                            //check
                            string tempCheckPnDeviceName = (string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName");
                            if (tempCheckPnDeviceName != OriginalPnDeviceName)
                            {
                                throw new GroupCopyException($"Could not rename PN DevicceName back to \"{OriginalPnDeviceName}\" in selected template group.");
                            }
                        }
                }
            }
            catch
            {
            }
        }

        public void ChangeGroupNumAndPrefixToPnDeviceName(string devicePrefix, string groupNumber, int nodeNumber = 0)
        {
            if (NetworkInterface?.Nodes?.Count > nodeNumber)
            {
                try
                {

                    object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceNameAutoGeneration");
                    if (attributeValue is bool value)
                        if (value == false)
                        {
                            if (groupNumber?.Length == 0)
                            {
                                throw new ProgrammingException($"Could not change PN Device Name because of invalid group number.");
                            }
                            string currentPnDeviceName = (string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[0], "PnDeviceName");

                            string newPnDeviceName = Regex.Replace(currentPnDeviceName, "^temp", devicePrefix + groupNumber);
                            if (newPnDeviceName.Length == 0)
                            {
                                throw new GroupCopyException($"Could not change PN Device Name.");
                            }
                            SingleAttribute.SetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceName", newPnDeviceName);

                            //check
                            string tempCheckPnDeviceName = (string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceName");
                            if (tempCheckPnDeviceName != newPnDeviceName)
                            {
                                throw new GroupCopyException($"Could not rename PN DevicceName to \"{newPnDeviceName}\".");
                            }
                        }
                }
                catch
                {
                }
            }
        }

        public void xAddPrefixToPnDeviceName(string prefix, int nodeNumber = 0)
        {

            if (NetworkInterface?.Nodes?.Count >  nodeNumber)
            {
                string tempPnDeviceName = null;

                object attributeValue = SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceNameAutoGeneration");
                if (attributeValue is bool value)
                    if (value == false)
                    {
                        tempPnDeviceName = ((string)SingleAttribute.GetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceName"));
                    }


                if (tempPnDeviceName != null)
                {
                    SingleAttribute.SetAttribute_Wrapper(NetworkInterface.Nodes[nodeNumber], "PnDeviceName", prefix + tempPnDeviceName);
                }
            }
        }

        #region Networking 

        public void StripGroupNumAndPrefixFromIoSystemName(string devicePrefix)
        {
            TemplateIoSystem0TiaName = "temp" + Regex.Replace(OriginalIoSystem0TiaName, "^" + devicePrefix + "\\d+", "", RegexOptions.IgnoreCase);

            if (NetworkInterface?.IoControllers?.Count > 0)
            {
                try
                {
                    NetworkInterface.IoControllers[0].IoSystem.Name = TemplateIoSystem0TiaName;
                    if (NetworkInterface.IoControllers[0].IoSystem.Name != TemplateIoSystem0TiaName)
                    {
                        throw new GroupCopyException($"Could not rename IO Sytem \"{OriginalIoSystem0TiaName}\" in selected template group to generic name \"{TemplateIoSystem0TiaName}\", probably because that name already exsits.");
                    }
                }
                catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                }
            }
        }

        public void RestoreGroupNumAndPrefixToIoSystemName()
        {
            if (NetworkInterface?.IoControllers?.Count > 0)
            {
                try
                {
                    if (OriginalIoSystem0TiaName.Length == 0)
                    {
                        throw new ProgrammingException($"Could not restore IO System of template Group because the orignal name is blank \"{OriginalIoSystem0TiaName}\".");
                    }

                    NetworkInterface.IoControllers[0].IoSystem.Name = OriginalIoSystem0TiaName;
                    if (NetworkInterface.IoControllers[0].IoSystem.Name != OriginalIoSystem0TiaName)
                    {
                        throw new GroupCopyException($"Could not restore IO Sytem name in selected template group from \"{TemplateIoSystem0TiaName}\"  to orignal name \"{OriginalIoSystem0TiaName}\".");
                    }
                }
                catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                }
            }
        }

        public void ChangeGroupNumAndPrefixToIoSystemName(string devicePrefix, string groupNumber, int IoControllerNumber = 0)
        {
            if (NetworkInterface?.IoControllers?.Count > 0)
            {
                try
                {
                    if (groupNumber?.Length == 0)
                    {
                        throw new ProgrammingException($"Could not rename IO System oname because of invalid group number");
                    }
                    string newName = Regex.Replace(NetworkInterface.IoControllers[IoControllerNumber].IoSystem.Name, "^temp", devicePrefix + groupNumber);

                    NetworkInterface.IoControllers[IoControllerNumber].IoSystem.Name = newName;
                    if (NetworkInterface.IoControllers[IoControllerNumber].IoSystem.Name != newName)
                    {
                        throw new GroupCopyException($"Could not rename IO Sytem name in selected template group to new name \"{newName}\".");
                    }
                }
                catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
                {
                    throw;
                }
                catch (Exception e)
                {
                }
            }
        }

        public Subnet Get_Subnet(int nodeNumber = 0)
        {

            if (NetworkInterface?.Nodes?.Count >  nodeNumber)
            {
                try
                {
                    return NetworkInterface.Nodes[nodeNumber].ConnectedSubnet;
                }
                catch
                {
                }
            }
            return null;
        }

        public IoSystem Get_ioSystem(int ioConnectorNumber = 0)
        {

            if (NetworkInterface?.Nodes?.Count >  ioConnectorNumber)
            {
                try
                {
                    return NetworkInterface.IoConnectors[ioConnectorNumber].ConnectedToIoSystem;
                }
                catch
                {
                }
            }
            return null;
        }

        public void Reconnect(Subnet subnet, IoSystem ioSystem,int nodeNumber = 0, int ioConnectorNumber = 0)
        {
            if (isConnectedtoNetwork && subnet != null)
            {

                DisconnectFromSubnet();
                ConnectToSubnet(subnet, nodeNumber);

                if (isConnectedToIoSystem && ioSystem != null)
                {
                    ConnectToIoSystem(ioSystem, ioConnectorNumber);
                }
            }

        }
        public void DisconnectFromSubnet(int nodeNumber = 0)
        {
            if (NetworkInterface?.Nodes?.Count >  nodeNumber)
            {
                try
                {
                    NetworkInterface.Nodes[nodeNumber].DisconnectFromSubnet();
                }
                catch
                {
                }
            }
        }

        public void ConnectToSubnet(Subnet aSubnet, int nodeNumber = 0)
        {
            if ((NetworkInterface?.Nodes?.Count >  nodeNumber) && (aSubnet != null) )
            {
                NetworkInterface.Nodes[nodeNumber].ConnectToSubnet(aSubnet);
            }
        }
         
        public void ConnectToIoSystem(IoSystem aIoSystem, int ioConnectorNumber = 0)
        {
            if ((NetworkInterface?.IoConnectors?.Count >  ioConnectorNumber) && (aIoSystem != null))
            {
                NetworkInterface.IoConnectors[ioConnectorNumber].ConnectToIoSystem(aIoSystem);
            }
        }

        public IoSystem CreateNewIoSystem(Subnet aSubnet, int ioConnectorNumber = 0, int nodeNumber = 0)
        {
            try
            {
                if ((NetworkInterface?.IoConnectors?.Count >  ioConnectorNumber) && (NetworkInterface?.Nodes?.Count >  nodeNumber) && (aSubnet != null))
                {
                    string IoSystemName = NetworkInterface.IoControllers[0].IoSystem.Name;
                    NetworkInterface.Nodes[nodeNumber].DisconnectFromSubnet();
                    NetworkInterface.Nodes[nodeNumber].ConnectToSubnet(aSubnet);
                    IoSystem newIoSystem = NetworkInterface.IoControllers[ioConnectorNumber].CreateIoSystem(IoSystemName);
                    return newIoSystem;
                }

            }
            catch (NullReferenceException)
            { }

            return null;

        }

        #endregion Networking

        #region i-device
        public void Save_iDeviceParnerIoAdresses()
        {

            foreach (TransferArea currentTransferArea in NetworkInterface.TransferAreas)
            {

                if (currentTransferArea.PartnerAddresses.Count > 0)
                {
                    TransferAreaAndAttributes newTransferArea = new TransferAreaAndAttributes(currentTransferArea);
                    if (newTransferArea != null)
                    {
                        IDevicePartnerIoAddrsses.Add(newTransferArea);
                    }
                }
            }
        }

        public void Restore_iDeviceParnerAdressesWithOffest(ulong aIDeviceOffsett)
        {
            foreach (TransferAreaAndAttributes item in IDevicePartnerIoAddrsses)
            {
                item.RestorePartnerStartAddressWitOffset(aIDeviceOffsett);
            }
        }

        #endregion i-device


        #endregion Methods

        #region Static Methods

        public static List<ManageNetworkInterface> GetAll_ManageNetworkInterfaceObjects(Device device)
        {
            if (device == null ) throw new ArgumentNullException(nameof(device), "Invalid device");

            List<ManageNetworkInterface> returnManageNetworkInterfaceObjects = new List<ManageNetworkInterface>();

            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                List<ManageNetworkInterface> newManageNetworkInterfaceObjects = GetAll_ManageNetworkInterfaceObjects(currentDeviceItem);
                if (newManageNetworkInterfaceObjects.Count > 0)
                {
                    returnManageNetworkInterfaceObjects.AddRange(newManageNetworkInterfaceObjects);
                }
            }

            return returnManageNetworkInterfaceObjects;
        }

        private static List<ManageNetworkInterface> GetAll_ManageNetworkInterfaceObjects(DeviceItem deviceItem)
        {
            List<ManageNetworkInterface> returnManageNetworkInterfaceObjects = new List<ManageNetworkInterface>();

            NetworkInterface newNetworkInterface = deviceItem.GetService<NetworkInterface>();
            if (newNetworkInterface != null)
            {
                returnManageNetworkInterfaceObjects.Add(new ManageNetworkInterface(deviceItem, newNetworkInterface));
            }

            foreach (DeviceItem currentDeviceItem in deviceItem.DeviceItems)
            {
                //call recursive
                List<ManageNetworkInterface> newManageNetworkInterfaceObjects = GetAll_ManageNetworkInterfaceObjects(currentDeviceItem);
                if (newManageNetworkInterfaceObjects.Count > 0)
                {
                    returnManageNetworkInterfaceObjects.AddRange(newManageNetworkInterfaceObjects);
                }
            }

            return returnManageNetworkInterfaceObjects;
        }


        #endregion
    }

}
