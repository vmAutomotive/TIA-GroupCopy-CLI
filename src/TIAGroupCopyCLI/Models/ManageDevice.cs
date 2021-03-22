using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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

    public class PortAndPartnerPort
    {
        #region Fields
        private readonly NetworkPort DevicePort;
        private readonly List<NetworkPort> PartnerPorts = new List<NetworkPort>() ;
        private bool isConnected;
        #endregion Fileds

        #region Constructor
        public PortAndPartnerPort(NetworkPort devicePort)
        {

            DevicePort = devicePort;
            if (DevicePort != null)
            {
                foreach (NetworkPort partnerPort in DevicePort.ConnectedPorts)
                {
                    PartnerPorts.Add( partnerPort);
                    isConnected = true;
                }
            }
        }
        #endregion Constuctor

        #region Methods
        public void Save()
        {
            if (DevicePort != null)
            {
                foreach (NetworkPort partnerPort in DevicePort.ConnectedPorts)
                {
                    PartnerPorts.Add(partnerPort);
                    isConnected = true;
                }
            }
        }

        public void Restore()
        {
            if ( (DevicePort != null) && isConnected)
            {
                foreach (NetworkPort partnerPort in PartnerPorts)
                {
                    try
                    {
                        DevicePort.ConnectToPort(partnerPort);
                    }
                    catch
                    {
                    }
                }
            }
        }
        #endregion Methods
    }


    class ManageDevice : IDevice
    {

        #region Fields
        public List<Device> AllDevices { get; } = new List<Device>();

        protected List<NetworkInterface> FirstPnNetworkInterfaces { get; } = new List<NetworkInterface>();
               
        protected List<AttributeInfo> PnDeviceNumberOfFirstPnNetworkInterfaces { get; set; } = new List<AttributeInfo>();

        protected List<AttributeAndDeviceItem> FDestinationAddress_attribues { get; } = new List<AttributeAndDeviceItem>();


        private readonly List<PortAndPartnerPort> NetworkPortsAndPartners = new List<PortAndPartnerPort>();
        private readonly List<AttributeInfo> PnDeviceNameOfFirstPnNetworkInterfaces = new List<AttributeInfo>();
        private readonly List<AttributeInfo> PnDeviceNameAutoGenOfFirstPnNetworkInterfaces  = new List<AttributeInfo>();


        #endregion Fields

        #region constructors
        /*
        public ManageDevice()
        {
            //AllDevices = new List<Device>();

        }
        */
        public ManageDevice(Device aDevice)
        {
            AllDevices.Add(aDevice);
            Get1PnInterfaces();
            GetAllPortsAndPartners();
            Save();

        }
        public ManageDevice(IList<Device> aDevices)
        {
            AllDevices.AddRange(aDevices);
            Get1PnInterfaces();
            GetAllPortsAndPartners();
            Save();
        }


        #endregion

        #region Methods
        public virtual void Save()
        {
            foreach (Device currentDevice in AllDevices)
            {
                IList<AttributeAndDeviceItem> newAttributes = Service.GetValueAndDeviceItemsWithAttribute(currentDevice.DeviceItems, "Failsafe_FDestinationAddress");

                if (newAttributes != null)
                {
                    FDestinationAddress_attribues.AddRange(newAttributes);
                }

            }

        }

        public virtual  void Restore()
        {
            foreach (AttributeAndDeviceItem item in FDestinationAddress_attribues)  //.Where(i => true)
            {
                //if (((ulong)item.Value >= aLower) && ((ulong)item.Value <= aUpper))
                //{
                    item.Restore();
                //}
            }
        }

        public virtual void AdjustFDestinationAddress(ulong aFDestOffset, ulong aLower, ulong aUpper)
        {

            foreach (AttributeAndDeviceItem item in FDestinationAddress_attribues)  //.Where(i => true)
            {
                if (((ulong)item.Value >= aLower) && ((ulong)item.Value <= aUpper))
                {
                    item.AddToValue(aFDestOffset);
                }
            }

        }

        public void Get1PnInterfaces()
        {

            foreach (Device currentDevice in AllDevices)
            {
                IList<DeviceItem> tempPnInterfacesDeviceItems = Service.GetDeviceItemsWithAttribute(currentDevice.DeviceItems, "InterfaceType", "Ethernet");
                if (tempPnInterfacesDeviceItems != null)
                {
                    NetworkInterface tempPnInterface = tempPnInterfacesDeviceItems[0].GetService<NetworkInterface>();
                    FirstPnNetworkInterfaces.Add(tempPnInterface);

                    AttributeInfo tempPnDeviceNumber = null;
                    AttributeInfo tempPnDeviceName = null;
                    AttributeInfo tempPnDeviceNameAutoGen = null;

                    if (tempPnInterface != null)
                    {
                        if (tempPnInterface.IoConnectors.Count > 0)
                        {
                            AttributeValue tempAttributeValue;

                            tempAttributeValue = Service.GetAttribute(tempPnInterface.IoConnectors[0], "PnDeviceNumber");
                            if (tempAttributeValue != null)
                                tempPnDeviceNumber = new AttributeInfo()
                                {
                                    Name = "PnDeviceNumber",
                                    Value = tempAttributeValue.Value
                                };
                            else tempPnDeviceNumber = null;
                        }

                        if (tempPnInterface.Nodes.Count > 0)
                        {
                            AttributeValue tempAttributeValue;

                            tempAttributeValue = Service.GetAttribute(tempPnInterface.Nodes[0], "PnDeviceName");
                            if (tempAttributeValue != null)
                                tempPnDeviceName = new AttributeInfo()
                                {
                                    Name = "PnDeviceName",
                                    Value = tempAttributeValue.Value
                                };
                            else tempPnDeviceName = null;

                            tempAttributeValue = Service.GetAttribute(tempPnInterface.Nodes[0], "PnDeviceNameAutoGeneration");
                            if (tempAttributeValue != null)
                                tempPnDeviceNameAutoGen = new AttributeInfo()
                                {
                                    Name = "PnDeviceNameAutoGeneration",
                                    Value = tempAttributeValue.Value
                                };
                            else tempPnDeviceNameAutoGen = null;

                        }
                    }

                    PnDeviceNumberOfFirstPnNetworkInterfaces.Add(tempPnDeviceNumber);
                    PnDeviceNameOfFirstPnNetworkInterfaces.Add(tempPnDeviceName);
                    PnDeviceNameAutoGenOfFirstPnNetworkInterfaces.Add(tempPnDeviceNameAutoGen);
                }

            }
        }

        public void GetAllPortsAndPartners()
        {

            foreach (NetworkInterface currentInterface in FirstPnNetworkInterfaces)
            {
                foreach(NetworkPort currentPort in currentInterface.Ports)
                {

                    NetworkPortsAndPartners.Add(new PortAndPartnerPort(currentPort));

                }
            }
        }

        public void xGetAllPortsAndPartners()
        {

            foreach (Device currentDevice in AllDevices)
            {
                IList<DeviceItem> tempPnInterfacesDeviceItems = Service.GetDeviceItemsWithAttribute(currentDevice.DeviceItems, "InterfaceType", "Ethernet");
                if (tempPnInterfacesDeviceItems != null)
                {

                    foreach (DeviceItem currentPnInterfacesDeviceItems in tempPnInterfacesDeviceItems)
                    {
                        foreach (DeviceItem currentSubDeviceItems in currentPnInterfacesDeviceItems.DeviceItems)
                        {
                            try
                            {

                                NetworkPort tempPort = currentSubDeviceItems.GetService<NetworkPort>();
                                if (tempPort != null)
                                {
                                    PortAndPartnerPort newPortAndPartnerPort = new PortAndPartnerPort(tempPort);

                                    NetworkPortsAndPartners.Add(newPortAndPartnerPort);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }

                }

            }
        }

        public void RestoreAllPartnerPorts()
        {
            foreach (PortAndPartnerPort currentPortandPArtners in NetworkPortsAndPartners)
            {
                currentPortandPArtners.Restore();
            }
        }

        public void ChangeNames(string aPrefix)
        {

            if (AllDevices != null)
            {
                //get PLCs in sub folders - recursive
                foreach (Device device in AllDevices)
                {
                    try
                    {
                        device.DeviceItems[1].Name = aPrefix + device.DeviceItems[1].Name;
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void ChangePnDeviceNames(string aPrefix)
        {

            if (AllDevices != null)
            {
                int i = 0;
                foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
                {
                    
                    if ((networkInterface.Nodes.Count > 0) && (PnDeviceNameAutoGenOfFirstPnNetworkInterfaces[i].Value != null))
                    {
                        if (PnDeviceNameAutoGenOfFirstPnNetworkInterfaces[i].Value is bool value)
                            if (value == false)
                            {
                                //Service.SetAttribute(networkInterface.IoConnectors[0], PnDeviceNumberOfFirstPnNetworkInterfaces[i]);
                                networkInterface.Nodes[0].SetAttribute(PnDeviceNameOfFirstPnNetworkInterfaces[i].Name, aPrefix + PnDeviceNameOfFirstPnNetworkInterfaces[i].Value);
                            }
                    }
                    i++;
                }
                
            }
        }

        public void ChangeIpAddresses(ulong aIpOffset)
        {
            foreach (NetworkInterface currentInterface in FirstPnNetworkInterfaces)
            {
                string[] tempIPaddress = ((string)currentInterface.Nodes[0].GetAttribute("Address")).Split('.');

                currentInterface.Nodes[0].SetAttribute("Address", tempIPaddress[0] + "." + tempIPaddress[1] + "." + (Convert.ToInt32(tempIPaddress[2]) + (uint)aIpOffset) + "." + tempIPaddress[3]); //ip of PLC
            }
        }

        public void XxSwitchIoSystem(Subnet aSubnet, IoSystem aIoSystem, ulong aIpOffset)
        {

            int i = 0;
            foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
            {
                try
                {
                    networkInterface.Nodes[0].DisconnectFromSubnet();
                }
                catch
                {
                }
                if (aSubnet != null)
                {
                    string[] tempIPaddress = ((string)networkInterface.Nodes[0].GetAttribute("Address")).Split('.');
                    networkInterface.Nodes[0].SetAttribute("Address", tempIPaddress[0] + "." + tempIPaddress[1] + "." + (Convert.ToInt32(tempIPaddress[2]) + (uint)aIpOffset) + "." + tempIPaddress[3]); //ip of PLC
                    networkInterface.Nodes[0].ConnectToSubnet(aSubnet);
                    if (aIoSystem != null)
                    {

                        networkInterface.IoConnectors[0].ConnectToIoSystem(aIoSystem);
                        if ( (networkInterface.IoConnectors.Count > 0) && (PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value != null)  )
                        {
                            //PnDeviceNumberOfFirstPnNetworkInterfaces[i].AddToValue(10);
                            //Service.SetAttribute(networkInterface.IoConnectors[0], PnDeviceNumberOfFirstPnNetworkInterfaces[i]);
                            //var x = networkInterface.IoConnectors[0].GetAttribute("PnDeviceNumber");
                            //networkInterface.IoConnectors[0].SetAttribute("PnDeviceNumber", 2);
                            //networkInterface.IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[i].Name, 4);

                            //int tempDeviceNumber = PnDeviceNumberOfFirstPnNetworkInterfaces[i].GetValueAsInt();
                            //networkInterface.IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[i].Name, tempDeviceNumber);
                            networkInterface.IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[i].Name, PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value);
                            
                        }
                    }
                }
                i++;
            }
        }


        public void SwitchIoSystem(Subnet aSubnet, IoSystem aIoSystem)
        {

            int i = 0;
            foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
            {
                try
                {
                    networkInterface.Nodes[0].DisconnectFromSubnet();
                }
                catch
                {
                }
                if (aSubnet != null)
                {
                    networkInterface.Nodes[0].ConnectToSubnet(aSubnet);
                    if (aIoSystem != null)
                    {
                        if ((networkInterface.IoConnectors.Count > 0) )
                        {
                            networkInterface.IoConnectors[0].ConnectToIoSystem(aIoSystem);
                            //Service.SetAttribute(networkInterface.IoConnectors[0], PnDeviceNumberOfFirstPnNetworkInterfaces[i]);
                            if ( (PnDeviceNumberOfFirstPnNetworkInterfaces[i]?.Value ?? null) != null)
                            {
                                networkInterface.IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[i].Name, PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value);
                            }
                        }
                    }
                }
                i++;
            }
        }

        public void DisconnectFromSubnet()
        {
            foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
            {
                networkInterface.Nodes[0].DisconnectFromSubnet();
            }
        }

        public void ConnectToSubnet(Subnet aSubnet)
        {
            if (aSubnet != null)
            {
                foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
                {
                    networkInterface.Nodes[0].ConnectToSubnet(aSubnet);
                }
            }
        }

        public void ConnectToSubnet(Subnet aSubnet, ulong aIpOffset)
        {
            if (aSubnet != null)
            {
                foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
                {
                    string[] tempIPaddress = ((string)networkInterface.Nodes[0].GetAttribute("Address")).Split('.');
                    networkInterface.Nodes[0].SetAttribute("Address", tempIPaddress[0] + "." + tempIPaddress[1] + "." + (Convert.ToInt32(tempIPaddress[2]) + (uint)aIpOffset) + "." + tempIPaddress[3]); //ip of PLC

                    networkInterface.Nodes[0].ConnectToSubnet(aSubnet);
                }
            }
        }

        public void ConnectToIoSystem(IoSystem aIoSystem)
        {
            if (aIoSystem != null)
            {
                int i = 0;
                foreach (NetworkInterface networkInterface in FirstPnNetworkInterfaces)
                {
                    networkInterface.IoConnectors[0].ConnectToIoSystem(aIoSystem);
                    if ((networkInterface.IoConnectors.Count > 0) && (PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value != null))
                    {
                        //Service.SetAttribute(networkInterface.IoConnectors[0], PnDeviceNumberOfFirstPnNetworkInterfaces[i]);
                        networkInterface.IoConnectors[0].SetAttribute(PnDeviceNumberOfFirstPnNetworkInterfaces[i].Name, PnDeviceNumberOfFirstPnNetworkInterfaces[i].Value);
                    }
                    i++;
                }
                
            }
        }

        #endregion Methods

    }
}
