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

using TIAGroupCopyCLI;

namespace TIAHelper.Services
{
    public class PnDeviceNumber
    {
        public object Value;
        public string Name;
    }
    public class AttributeValue
    {

        public object Value;

        /*
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        */

        public AttributeValue()
        {
        }
        public AttributeValue(object aObject)
        {
            Value = aObject;
        }
        public object AddToValue(uint addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public object AddToValue(ulong addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }

        public int GetValueAsInt()
        {
            return (int)Value;
        }

    }
    public class AttributeInfo : AttributeValue
    {
        public string Name;
    }
    public class AttributeAndDeviceItem : AttributeInfo
    {
        public DeviceItem DeviceItem;

        public void Restore()
        {
            
            if (DeviceItem != null)
            {
                DeviceItem.SetAttribute(Name, Value);
            }
        }

        public void Save()
        {
            if (DeviceItem != null)
            {
                Value = DeviceItem.GetAttribute(Name);
            }
        }
    }


    public class AttributeAndTelegram : AttributeInfo
    {
        public Telegram Telegram;

        public void RestoreValue()
        {
            Telegram.SetAttribute(Name, Value);
        }
    }

    public class AttributeAndAddress : AttributeInfo
    {
        public Address Address;
        public void RestoreValue()
        {
            try
            {
                Address.SetAttribute(Name, Value);
            }
            catch(EngineeringNotSupportedException )
            {

            }
            catch (Exception ex)
            {
                Program.FaultMessage("Could not set Attribute. ", ex, "AttributeAndAddress.Address.SetAttribute");
            }

        }

    }



    public static class Service
    {

        public static void OpenProject(string ProjectPath, ref TiaPortal tiaPortal, ref Project project)
        {

            //First check if project already open

            foreach (TiaPortalProcess tiaPortalProcess in TiaPortal.GetProcesses())
            {
                string currentProjectPath = ((tiaPortalProcess.ProjectPath != null && !string.IsNullOrEmpty(tiaPortalProcess.ProjectPath.FullName)) ? Path.GetFullPath(tiaPortalProcess.ProjectPath.FullName) : "None");
                Console.WriteLine("tiaPortalProcess: " + tiaPortalProcess.Mode);
                Console.WriteLine("ProjectPath: " + currentProjectPath);
                Console.WriteLine("");

                //string currentProject = ((tiaPortalProcess.ProjectPath != null && !string.IsNullOrEmpty(tiaPortalProcess.ProjectPath.FullName)) ? tiaPortalProcess.ProjectPath.FullName : "None");
                if (currentProjectPath == ProjectPath)
                {
                    Console.WriteLine("Attaching to TIA Portal");
                    try
                    {
                        tiaPortal = tiaPortalProcess.Attach();
                        project = tiaPortal.Projects[0];
                        return;
                    }
                    catch (Siemens.Engineering.EngineeringSecurityException e)
                    {
                        tiaPortal = null;
                        project = null;
                        Program.CancelGeneration("Could not start TIAP. Please achnoledge the \"Openness access\" security dialog box with [Yes] or [Yes to all] to grant access or if there was no dialog box add the user to the user group \"Siemens TIA Openness\". (see exception message below for details)", e);
                        return;
                    }
                    catch (Exception e)
                    {
                        Program.CancelGeneration("Could not attach to running TIAP with open project.", e);
                        project = null;
                        
                    }
                    
                }
            }

            if ((tiaPortal == null) || (project == null))
            {


                Console.WriteLine("Starting TIA Portal");
                try
                {
                    tiaPortal = new TiaPortal(TiaPortalMode.WithoutUserInterface);
                }
                catch (Siemens.Engineering.EngineeringSecurityException e)  //-2146233088
                {
                    tiaPortal = null;
                    project = null;
                    Program.CancelGeneration("Could not start TIAP. Please achnoledge the \"Openness access\" security dialog box with [Yes] or [Yes to all] to grant access or if there was no dialog box add the user to the user group \"Siemens TIA Openness\". (see exception message below for details)", e);
                    return;
                }
                catch (Exception e)
                {
                    Program.CancelGeneration("Could not start TIAP.", e);
                    return;
                }

                Console.WriteLine("TIA Portal has started");
                ProjectComposition projects = tiaPortal.Projects;
                Console.WriteLine("Opening Project...");
                FileInfo projectPath = new FileInfo(ProjectPath); //edit the path according to your project
                                                                  //Project project = null;
                try
                {
                    project = projects.Open(projectPath);
                }
                catch (Exception e)
                {
                    Program.CancelGeneration("Could not open project " + projectPath.FullName, e);
                    return;
                }
            }

        }

        public static void AttachToTIA(string ProjectPath, ref TiaPortal tiaPortal, ref Project project)
        {

            foreach (TiaPortalProcess tiaPortalProcess in TiaPortal.GetProcesses())
            {
                string currentProjectPath = ((tiaPortalProcess.ProjectPath != null && !string.IsNullOrEmpty(tiaPortalProcess.ProjectPath.FullName)) ? Path.GetFullPath(tiaPortalProcess.ProjectPath.FullName) : "None");
                Console.WriteLine("tiaPortalProcess: " + tiaPortalProcess.Mode);
                Console.WriteLine("ProjectPath: " + currentProjectPath);
                Console.WriteLine("");

                //string currentProject = ((tiaPortalProcess.ProjectPath != null && !string.IsNullOrEmpty(tiaPortalProcess.ProjectPath.FullName)) ? tiaPortalProcess.ProjectPath.FullName : "None");
                if (currentProjectPath == ProjectPath)
                {
                    Console.WriteLine("Attaching to TIA Portal");
                    try
                    {
                        tiaPortal = tiaPortalProcess.Attach();
                        project = tiaPortal.Projects[0];
                    }
                    catch ( Siemens.Engineering.EngineeringSecurityException e)
                    {
                        Program.CancelGeneration("Could not attach to running TIAP with open project.", e);
                        project = null;
                    }
                    catch (Exception e)
                    {
                        Program.CancelGeneration("Could not attach to running TIAP with open project.", e);
                        project = null;
                    }
                    return;
                }
            }

        }

        public static void OpenProjectOnly(string ProjectPath, ref TiaPortal tiaPortal, ref Project project)
        {
            Console.WriteLine("Starting TIA Portal");
            try
            {
                tiaPortal = new TiaPortal(TiaPortalMode.WithoutUserInterface);
            }
            catch (Exception e)
            {
                Program.CancelGeneration("Could not start TIAP.", e);
                return;
            }

            Console.WriteLine("TIA Portal has started");
            ProjectComposition projects = tiaPortal.Projects;
            Console.WriteLine("Opening Project...");
            FileInfo projectPath = new FileInfo(ProjectPath); //edit the path according to your project
                                                              //Project project = null;
            try
            {
                project = projects.Open(projectPath);
            }
            catch (Exception e)
            {
                Program.CancelGeneration("Could not open project " + projectPath.FullName, e);
                return;
            }

        }

        public static void ChangeDeviceNames(DeviceUserGroup aDeviceUserGroup, string aPrefix)
        {
            if (aDeviceUserGroup != null)
            {
                //get PLCs in sub folders - recursive
                foreach (Device device in aDeviceUserGroup.Devices)
                {
                    try
                    {
                        //change device name 
                        device.DeviceItems[1].Name = aPrefix + device.DeviceItems[1].Name;
                    }
                    catch
                    {
                    }
                }
                //get PLCs in sub folders - recursive
                foreach (DeviceUserGroup deviceUserGroup in aDeviceUserGroup.Groups)
                {
                    ChangeDeviceNames(deviceUserGroup, aPrefix);
                }
            }
        }

        public static AttributeValue GetAttribute(IEngineeringObject aIEngineeringObject, string aAttributeName)
        {
            if (aIEngineeringObject != null)
            {
                try
                {
                    object attributeValue = aIEngineeringObject.GetAttribute(aAttributeName);
                    AttributeValue newItem = new AttributeValue(attributeValue);
                    //newItem.Value = attributeValue;
                    return newItem;

                }
                catch (EngineeringNotSupportedException)
                {

                }
                catch (Exception ex)
                {
                    Program.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttribute");
                }
            }

            return null;
        }
        
        public static List<AttributeValue> GetAttributes(IEngineeringComposition aIEngineeringComposition, string aAttributeName)
        {
            List<AttributeValue> returnItems = new List<AttributeValue>();

            if (aIEngineeringComposition != null)
            {
                foreach (IEngineeringObject currentItem in aIEngineeringComposition)
                {

                    try
                    {
                        AttributeValue newItem = GetAttribute(currentItem, aAttributeName);
                        if (newItem != null)
                        {
                            returnItems.Add(newItem);
                        }
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttributes");
                    }
                }
            }
            return returnItems;
        }

        public static AttributeInfo GetAttributeInfo(IEngineeringObject aEngineeringObject, string aAttributeName)
        {
            if (aEngineeringObject != null)
            {
                try
                {
                    object attributeValue = aEngineeringObject.GetAttribute(aAttributeName);
                    AttributeInfo returnObject = new AttributeInfo
                    {
                        Name = aAttributeName,
                        Value = attributeValue
                    };
                    return returnObject;
                }
                catch (EngineeringNotSupportedException )
                {

                }
                catch (Exception ex)
                {
                    Program.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttributeInfo");
                }
            }
            return null;
        }

        public static List<AttributeInfo> GetAttributesInfo(IEngineeringComposition aIEngineeringComposition, string aAttributeName)
        {
            List<AttributeInfo> returnItems = new List<AttributeInfo>();

            if (aIEngineeringComposition != null)
            {
                foreach (IEngineeringObject currentItem in aIEngineeringComposition)
                {

                    try
                    {
                        AttributeInfo newItem = GetAttributeInfo(currentItem, aAttributeName);
                        if (newItem != null)
                        {
                            returnItems.Add(newItem);
                        }
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttributesInfo");
                    }
                }
            }
            return returnItems;
        }

        public static bool SetAttribute(IEngineeringObject aIEngineeringObject, string aAttributeName, AttributeValue aAttributeValue)
        {
            if (aIEngineeringObject != null)
            {
                try
                {
                    aIEngineeringObject.SetAttribute(aAttributeName, aAttributeValue.Value);
                    return true;
                }
                catch (Exception ex)
                {
                    Program.FaultMessage("Could not set Attribute", ex, "AttributeValue.SetAttribute");
                }

            }
            return false;
        }

        public static bool SetAttribute(IEngineeringObject aIEngineeringObject, AttributeInfo aAttributeInfo)
        {
            if ( (aIEngineeringObject != null) && (aAttributeInfo.Value != null) )
            {
                try
                {
                    aIEngineeringObject.SetAttribute(aAttributeInfo.Name, aAttributeInfo.Value);
                    return true;
                }
                catch (Exception ex)
                {
                    Program.FaultMessage("Could not set Attribute", ex, "AttributeValue.SetAttribute");
                }

            }
            return false;
        }
        public static void SetAttributes(IEngineeringComposition aIEngineeringComposition, string aAttributeName, List<AttributeValue> aAttributeValues)
        {


            if (aIEngineeringComposition != null)
            {
                int i = 0;
                foreach (IEngineeringObject currentItem in aIEngineeringComposition)
                {

                    SetAttribute(currentItem, aAttributeName, aAttributeValues[i]);
                    i++;

                }
            }

        }

        public static DeviceItem GetCpu1Interface1DeviceItem(DeviceUserGroup aDeviceUserGroup)
        {
            if (aDeviceUserGroup != null)
            {
                IEnumerable<DeviceItem> deviceItemsPLC = null;

                foreach (Device currentDevice in aDeviceUserGroup.Devices)
                {
                    deviceItemsPLC = currentDevice.DeviceItems.Where(d => d.Classification.ToString() == "CPU");

                    //In diesem Fall gehen wir davon aus, dass in dem TIA-Projekt nur eine CPU existiert.
                    if (deviceItemsPLC != null)
                        break;
                }

                if (deviceItemsPLC != null)
                {
                    for (int i = 0; i < deviceItemsPLC.Count(); i++)
                    {
                        IList<object> attributeList = null;


                        //DeviceItems der CPU durchsuchen
                        foreach (DeviceItem devItem in deviceItemsPLC.ElementAt(i).DeviceItems)
                        {
                            bool breakLoop = false;

                            try
                            {
                                attributeList = devItem.GetAttributes(new string[] { "InterfaceType" });
                            }
                            catch (EngineeringNotSupportedException )
                            {

                            }
                            catch (Exception ex)
                            {
                                Program.FaultMessage("Could not get Attribute", ex, "Service.GetCpu1Interface1DeviceItem");
                            }

                            if (attributeList != null)
                            {
                                if (devItem.GetAttribute("InterfaceType").ToString() == "Ethernet")
                                {
                                    return devItem;

                                }
                            }
                            //Breche die äußere foreach-Schleife ab die durch die DeviceItems der CPU iteriert
                            if (breakLoop)
                                break;
                        }
                    }

                }

            }

            return null;
        }

        public static IList<Device> GetAllDevicesInGroup(DeviceUserGroup aDeviceUserGroup)
        {
            List<Device> returnDevices = new List<Device>();
            IList<Device> addDevices;

            if (aDeviceUserGroup != null)
            {
                returnDevices = aDeviceUserGroup.Devices.ToList();
            }
            //get PLCs in sub folders - recursive
            foreach (DeviceUserGroup group in aDeviceUserGroup.Groups)
            {
                addDevices = GetAllDevicesInGroup(group);
                returnDevices.AddRange(addDevices);
            }

            return returnDevices;
        }

        public static IList<Device> GetPlcDevicesInGroup(DeviceUserGroup aDeviceUserGroup)
        {
            List<Device> returnPlcDevices = new List<Device>();
            IList<Device> addPlcDevices ;

            if (aDeviceUserGroup != null)
            {
                returnPlcDevices = aDeviceUserGroup.Devices.Where(d => d.DeviceItems.Where(di => di.Classification.ToString() == "CPU").Count() >0).ToList();
            }
            //get PLCs in sub folders - recursive
            foreach (DeviceUserGroup group in aDeviceUserGroup.Groups)
            {
                addPlcDevices = GetPlcDevicesInGroup(group);
                returnPlcDevices.AddRange(addPlcDevices);
            }

            return returnPlcDevices;
        }

        public static PlcSoftware GetPlcSoftware(Device device)
        {
            //PlcSoftware plcSoftware = null;
            foreach (DeviceItem currentDeviceItem in device.DeviceItems.Where(d => d.Classification.ToString() == "CPU"))
            {
                //hole Softwareblöcke, die PLC_1 untergeordnet sind
                SoftwareContainer softwareContainer = currentDeviceItem.GetService<SoftwareContainer>();
                if (softwareContainer != null)
                {
                    //Console.WriteLine("DeviceItem: " + currentDeviceItem.Name);
                    return softwareContainer.Software as PlcSoftware;
                }
            }
            return null;
        }

        public static IList<Device> GetHmiDevicesInGroup(DeviceUserGroup aDeviceUserGroup)
        {

            List<Device> returnHmiDevices = new List<Device>();
            IList<Device> addHmiDevices;

            if (aDeviceUserGroup != null)
            {
                foreach (Device device in aDeviceUserGroup.Devices)
                    foreach (DeviceItem deviceItem in device.DeviceItems)
                    {
                        DeviceItem deviceItemToGetService = deviceItem as DeviceItem;
                        SoftwareContainer container =   deviceItemToGetService.GetService<SoftwareContainer>();
                        if (container != null)
                        {
                            if (container.Software is HmiTarget hmi)
                            {
                                returnHmiDevices.Add(device);
                                break;
                            }
                        }
                    }
                //get PLCs in sub folders - recursive
                foreach (DeviceUserGroup group in aDeviceUserGroup.Groups)
                {
                    addHmiDevices = GetPlcDevicesInGroup(group);
                    returnHmiDevices.AddRange(addHmiDevices);
                }
            }


            return returnHmiDevices;

        }

        public static IList<Device> GetG120DevicesInGroup(DeviceUserGroup aDeviceUserGroup)
        {

            List<Device> returnG120Devices = new List<Device>();
            IList<Device> addG120Devices;

            if (aDeviceUserGroup != null)
            {
                foreach (Device device in aDeviceUserGroup.Devices)
                    foreach (DeviceItem deviceItem in device.DeviceItems)
                    {
                        DeviceItem deviceItemToGetService = deviceItem as DeviceItem;
                        DriveObjectContainer container = deviceItemToGetService.GetService<DriveObjectContainer>();//DriveObject
                        if (container != null)
                        {
                            if (container.DriveObjects[0] is DriveObject G120)
                            {
                                returnG120Devices.Add(device);
                                break;
                            }
                        }
                    }
                //get PLCs in sub folders - recursive
                foreach (DeviceUserGroup group in aDeviceUserGroup.Groups)
                {
                    addG120Devices = GetPlcDevicesInGroup(group);
                    returnG120Devices.AddRange(addG120Devices);
                }
            }


            return returnG120Devices;

        }


        public static IList<DeviceItem> GetDeviceItemsWithAttribute(DeviceItemComposition aDeviceItems, string aAttributeName, string aAttributeValue)
        {
            List<DeviceItem> returnDeviceItems = new List<DeviceItem>();
            IList<DeviceItem> addDeviceItems;

            if (aDeviceItems != null)
            {
                foreach (DeviceItem currentDeviceItem in aDeviceItems)
                {
                    try
                    {
                        string attributeValue = currentDeviceItem.GetAttribute(aAttributeName).ToString();
                        if ((attributeValue == aAttributeValue) || (aAttributeValue == "*"))
                        {
                            returnDeviceItems.Add(currentDeviceItem);
                        }
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "Service.GetDeviceItemsWithAttribute");
                    }

                    //check sub DeviceItems - recursive
                    addDeviceItems = GetDeviceItemsWithAttribute(currentDeviceItem.DeviceItems, aAttributeName, aAttributeValue);
                    returnDeviceItems.AddRange(addDeviceItems);
                }
            }
            return returnDeviceItems;
        }



        public static IList<AttributeAndDeviceItem> GetValueAndDeviceItemsWithAttribute(DeviceItemComposition aDeviceItems, string aAttributeName)
        {
            List<AttributeAndDeviceItem> returnDeviceItems = new List<AttributeAndDeviceItem>();
            IList<AttributeAndDeviceItem> addDeviceItems;

            if (aDeviceItems != null)
            {
                foreach (DeviceItem currentDeviceItem in aDeviceItems)
                {
                    try
                    {
                        object attributeValue = currentDeviceItem.GetAttribute(aAttributeName);
                        AttributeAndDeviceItem newItem = new AttributeAndDeviceItem();
                        newItem.Name = aAttributeName;
                        newItem.Value = attributeValue;
                        newItem.DeviceItem = currentDeviceItem;
                        returnDeviceItems.Add(newItem);
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "Service.GetValueAndDeviceItemsWithAttribute");
                    }

                    //check sub DeviceItems - recursive
                    addDeviceItems = GetValueAndDeviceItemsWithAttribute(currentDeviceItem.DeviceItems, aAttributeName);
                    returnDeviceItems.AddRange(addDeviceItems);
                }
            }
            return returnDeviceItems;
        }

        public static AttributeAndDeviceItem Get1ValueAndDeviceItemWithAttribute(DeviceItemComposition aDeviceItems, string aAttributeName)
        {
            AttributeAndDeviceItem returnDeviceItem;


            if (aDeviceItems != null)
            {
                foreach (DeviceItem currentDeviceItem in aDeviceItems)
                {
                    try
                    {
                        object attributeValue = currentDeviceItem.GetAttribute(aAttributeName);
                        returnDeviceItem = new AttributeAndDeviceItem();
                        returnDeviceItem.Name = aAttributeName;
                        returnDeviceItem.Value = attributeValue;
                        returnDeviceItem.DeviceItem = currentDeviceItem;
                        return returnDeviceItem;
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "Service.Get1ValueAndDeviceItemWithAttribute");
                    }

                    //check sub DeviceItems - recursive
                    returnDeviceItem = Get1ValueAndDeviceItemWithAttribute(currentDeviceItem.DeviceItems, aAttributeName);

                    if (returnDeviceItem !=null)
                    {
                        return returnDeviceItem;
                    }
                    
                }
            }
            return null;
        }


        public static IList<AttributeAndAddress> GetValueAndAddressWithAttribute(DriveObject aDrive, string aAttributeName)
        {
            List<AttributeAndAddress> returnDeviceItems = new List<AttributeAndAddress>();

             
            if (aDrive != null)
            {
                foreach (Telegram currentTelegram in aDrive.Telegrams)
                {
                    foreach (Address currentAddress in currentTelegram.Addresses)
                    {

                        try
                        {
                            object attributeValue = currentAddress.GetAttribute(aAttributeName);
                            AttributeAndAddress newItem = new AttributeAndAddress();
                            newItem.Name = aAttributeName;
                            newItem.Value = attributeValue;
                            newItem.Address = currentAddress;
                            returnDeviceItems.Add(newItem);
                        }
                        catch (EngineeringNotSupportedException )
                        {

                        }
                        catch (Exception ex)
                        {
                            Program.FaultMessage("Could not get Attribute", ex, "Service.GetValueAndAddressWithAttribute");
                        }
                    }
                }
            }
            return returnDeviceItems;
        }

        public static IList<AttributeAndTelegram> GetValueAndTelegramWithAttribute(DriveObject aDrive, string aAttributeName)
        {
            List<AttributeAndTelegram> returnDeviceItems = new List<AttributeAndTelegram>();


            if (aDrive != null)
            {
                foreach (Telegram currentTelegram in aDrive.Telegrams)
                {
                    try
                    {
                        object attributeValue = currentTelegram.GetAttribute(aAttributeName);
                        AttributeAndTelegram newItem = new AttributeAndTelegram();
                        newItem.Name = aAttributeName;
                        newItem.Value = attributeValue;
                        newItem.Telegram = currentTelegram;
                        returnDeviceItems.Add(newItem);
                    }
                    catch (EngineeringNotSupportedException)
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "Service.GetValueAndTelegramWithAttribute");
                    }

                }
            }
            return returnDeviceItems;
        }


        public static IList<AttributeAndAddress> GetValueAndAddressWithAttribute(DeviceItemComposition aDeviceItems, string aAttributeName)
        {
            List<AttributeAndAddress> returnDeviceItems = new List<AttributeAndAddress>();
            IList<AttributeAndAddress> addDeviceItems;

            if (aDeviceItems != null)
            {
                foreach (DeviceItem currentDeviceItem in aDeviceItems)
                {

                    addDeviceItems = GetValueAndAddressWithAttribute(currentDeviceItem.Addresses, aAttributeName);
                    returnDeviceItems.AddRange(addDeviceItems);

                    addDeviceItems = GetValueAndAddressWithAttribute(currentDeviceItem.DeviceItems, aAttributeName);
                    returnDeviceItems.AddRange(addDeviceItems);
                }
            }
            return returnDeviceItems;
        }

        public static IList<AttributeAndAddress> GetValueAndAddressWithAttribute(AddressComposition aAddressComposition, string aAttributeName)
        {
            List<AttributeAndAddress> returnDeviceItems = new List<AttributeAndAddress>();

            if (aAddressComposition != null)
            {
                foreach (Address currentAddress in aAddressComposition)
                {

                    try
                    {
                        object attributeValue = currentAddress.GetAttribute(aAttributeName);
                        if ((int)attributeValue >= 0)
                        {
                            AttributeAndAddress newItem = new AttributeAndAddress();
                            newItem.Name = aAttributeName;
                            newItem.Value = attributeValue;
                            newItem.Address = currentAddress;
                            returnDeviceItems.Add(newItem);
                        }
                    }
                    catch (EngineeringNotSupportedException )
                    {

                    }
                    catch (Exception ex)
                    {
                        Program.FaultMessage("Could not get Attribute", ex, "Service.GetValueAndAddressWithAttribute");
                    }

                }
            }
            return returnDeviceItems;
        }


        public static IList<NetworkInterface> GetAllPnInterfaces(Device aDevice)
        {
            
            List<NetworkInterface> returnPnInterfaces = new List<NetworkInterface>();
            //IList<DeviceItem> addPnInterfaceDeviceItems;

            if (aDevice != null)
            {

                foreach (DeviceItem currentDeviceItem in aDevice.DeviceItems)
                {
                    foreach (DeviceItem currentSubDeviceItems in currentDeviceItem.DeviceItems)
                    {
                        try
                        {
                            if (currentSubDeviceItems.GetAttribute("InterfaceType").ToString() == "Ethernet")
                            {

                                NetworkInterface tempPnInterface = currentSubDeviceItems.GetService<NetworkInterface>();
                                returnPnInterfaces.Add(tempPnInterface);
                            }

                        }
                        catch (EngineeringNotSupportedException )
                        {

                        }
                        catch (Exception ex)
                        {
                            Program.FaultMessage("Could not get Attribute", ex, "Service.GetAllPnInterfaces");
                        }

                    }
                }

            }

            return returnPnInterfaces;
        }


        public static IList<NetworkPort> GetAllPorts(Device aDevice)
        {

            List<NetworkPort> returnPorts = new List<NetworkPort>();
            //IList<DeviceItem> addPnInterfaceDeviceItems;

            if (aDevice != null)
            {

                foreach (DeviceItem currentDeviceItem in aDevice.DeviceItems)
                {
                    foreach (DeviceItem currentSub1DeviceItems in currentDeviceItem.DeviceItems)
                    {
                        try
                        {
                            if (currentSub1DeviceItems.GetAttribute("InterfaceType").ToString() == "Ethernet")
                            {
                                foreach (DeviceItem currentSub2DeviceItems in currentDeviceItem.DeviceItems)
                                {
                                    try
                                    {

                                        NetworkPort tempPort = currentSub2DeviceItems.GetService<NetworkPort>();
                                        if (tempPort != null)
                                        {
                                            returnPorts.Add(tempPort);
                                        }
                                    }
                                    catch
                                    {
                                    }
                                }
                            }

                        }
                        catch (EngineeringNotSupportedException )
                        {

                        }
                        catch (Exception ex)
                        {
                            Program.FaultMessage("Could not get Attribute", ex, "Service.GetAllPorts");
                        }

                    }
                }

            }

            return returnPorts;
        }


    }
}
