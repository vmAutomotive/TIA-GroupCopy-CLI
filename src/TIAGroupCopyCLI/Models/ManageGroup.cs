using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.MC.Drives;
using Siemens.Engineering.Hmi;
using System.Globalization;
using System.Text.RegularExpressions;
using TIAGroupCopyCLI.AppExceptions;

//using HmiTarget = Siemens.Engineering.Hmi.HmiTarget;
//using PlcSoftware = Siemens.Engineering.SW.PlcSoftware;
//using DriveObject = Siemens.Engineering.MC.Drives.DriveObject;

namespace TIAGroupCopyCLI.Models
{

    //=========================================================================================================
    class ManageGroup : IEnumerable
    {

        #region Fields
        public DeviceUserGroup tiaGroup;
        public List<IManageDevice> Devices = new List<IManageDevice>();
        public ManagePlc Plc;
        public int Count
        {
            get
            {
                return Devices.Count;
            }
        }

        public Subnet originalSubnet;
        public Subnet masterSubnet;
        public IoSystem masterIoSystem;
        public IoSystem newIoSystem;

        ulong lowerFDest;
        ulong upperFDest;

        //readonly string currentPrefix;  //TODO delete
        readonly string originalGroupName;//TODO use
        string templateGroupName;//TODO use

        #endregion Fields

        #region indexer
        public IManageDevice this[int index]
        {
            get
            {
                if ((index < Devices.Count) && (index >= 0))
                {
                    return Devices[index];
                }
                else
                {
                    int reverseIndex = Devices.Count + index;
                    if ((reverseIndex < Devices.Count) && (reverseIndex >= 0))
                    {
                        return Devices[reverseIndex];
                    }
                }
                return null;
            }
            private set
            {
                if ((index < Devices.Count) && (index >= 0))
                {
                    Devices[index] = value;
                }
                else
                {
                    Devices.Add(value);
                }
            }
        }

        #endregion

        #region Constructor

        public ManageGroup(DeviceUserGroup deviceUserGroup)
        {
            tiaGroup = deviceUserGroup;
            originalGroupName = tiaGroup.Name;

            GetAll_DevicesInGroup(tiaGroup);
            
        }

        #endregion Constructor


        #region Enumerator
        // Implementation for the GetEnumerator method.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public DeviceEnum GetEnumerator()
        {
            return new DeviceEnum(Devices);
        }
        #endregion

        #region methods

        private void GetAll_DevicesInGroup(DeviceUserGroup deviceUserGroup)
        {

            if (deviceUserGroup != null)
            {
                foreach (Device currentDevice in deviceUserGroup.Devices)
                {

                    DeviceType currentDevicetype = Get_DeviceType(currentDevice);

                    switch (currentDevicetype)
                    {
                        case DeviceType.Plc:

                            Plc = new ManagePlc(currentDevice);
                            Devices.Add(Plc);
                            break;

                        case DeviceType.Hmi:
                            Devices.Add(new ManageHmi(currentDevice));
                            break;

                        case DeviceType.Drive:
                            Devices.Add(new ManageDrive(currentDevice));
                            break;

                        case DeviceType.ioDevice:
                            Devices.Add(new ManageIo(currentDevice));
                            break;

                        default:
                            break;
                    }
                    
                }

            }
            //get PLCs in sub folders - recursive
            foreach (DeviceUserGroup group in deviceUserGroup.Groups)
            {
                GetAll_DevicesInGroup(group);
            }

        }
        
        public void SaveConfig()
        {
            foreach (IManageDevice currentdevice in Devices)
            {
                currentdevice.SaveConfig();

            }
            originalSubnet = Plc?.Get_Subnet();
            masterIoSystem = Plc?.Get_ioSystem();

        }

        public void CopyFromTemplate(ManageGroup tempateGroup)
        {
            int i = 0;

            foreach (ManagePlc currentPLC in Devices.OfType<ManagePlc>())
            {
                ManagePlc tempatePLC = tempateGroup.Devices.OfType<ManagePlc>().ElementAt(i);

                (lowerFDest, upperFDest) = currentPLC.CopyFromTemplate(tempatePLC);

                i++;
            }
        }

        public void ReconnectAndRestore_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong iDeviceOffset)
        {
            foreach (ManagePlc currentPLC in Devices.Where(d => d.DeviceType == DeviceType.Plc))
            {
                currentPLC.RestoreConfig_WithAdjustments(pnDeviceNumberOffset, fSourceOffset, fDestOffset, lowerFDest, upperFDest);
                currentPLC.Restore_iDeviceParnerAdresses(iDeviceOffset);
            }
            foreach (IManageDevice currentDevice in Devices.Where(d => d.DeviceType != DeviceType.Plc))
            {
                currentDevice.Reconnect(masterSubnet, newIoSystem);
                currentDevice.RestoreConfig_WithAdjustments( 0, fSourceOffset, fDestOffset, lowerFDest, upperFDest);
            }
            foreach (ManagePlc currentPLC in Devices.Where(d => d.DeviceType == DeviceType.Plc))
            {
                currentPLC.Restore_ToConnections();
            }
        }

        public void StripGroupNumAndPrefix(string devicePrefix)
        {

            Match findTemplateGroupName = Regex.Match(originalGroupName, ".*?\\D(?=\\d*$)", RegexOptions.IgnoreCase);

            if (findTemplateGroupName.Success)
            {
                templateGroupName = findTemplateGroupName.Value + "temp";
            }
            else
            {
                templateGroupName = "temp";
            }


            tiaGroup.Name = templateGroupName;
            if (tiaGroup.Name != templateGroupName)
            {
                throw new GroupCopyException($"Could not rename selected template group \"{originalGroupName}\" to generic group name \"{templateGroupName}\", probably because a group with that name already exits.");
            }

            //Plc?.StripGroupNumAndPrefixFromIoSytem(devicePrefix);

            foreach (IManageDevice currentDevice in Devices)
            {
                currentDevice.StripGroupNumAndPrefix(devicePrefix);
            }

        }


        public void RestoreGroupNumAndPrefix()
        {

            if (originalGroupName.Length == 0)
            {
                throw new ProgrammingException($"Could not restore TIA object of template Group because the orignal name is blank \"{originalGroupName}\".");
            }


            tiaGroup.Name = originalGroupName;
            if (tiaGroup.Name != originalGroupName)
            {
                throw new GroupCopyException($"Could not rename selected template group \"{templateGroupName}\" back to original group name \"{originalGroupName}\".");
            }

            //Plc?.StripGroupNumAndPrefixFromIoSytem(devicePrefix);

            foreach (IManageDevice currentDevice in Devices)
            {
                currentDevice.RestoreGroupNumAndPrefix();
                
            }

        }

        public void ChangeGroupNumAndPrefix(string devicePrefix, string groupNumber)
        {
            if (groupNumber.Length == 0 )
            {
                throw new ProgrammingException($"Could not change TIA Name of object because of invalid group number.");
            }
            //tiaGroup.Name = currentGroupName;
            //templateNetworkInterface.IoControllers[0].IoSystem.Name = currentPrefix + temlateIoSystemName;
            string newGroupName = Regex.Replace(tiaGroup.Name, "temp$", groupNumber);
            if (newGroupName.Length == 0)
            {
                throw new GroupCopyException($"Could not change TIA Name of object.");
            }
            tiaGroup.Name = newGroupName;
            if (tiaGroup.Name != newGroupName)
            {
                throw new GroupCopyException($"Could not rename selected template group \"{templateGroupName}\" back to original group name \"{newGroupName}\".");
            }

            //Plc?.AddPrefixToIoSystemName(currentPrefix);

            foreach (IManageDevice currentDevice in Devices)
            {
                //currentDevice.xAddPrefixToTiaName(currentPrefix);
                //currentDevice.xAddPrefixToPnDeviceName(currentPrefix);
                currentDevice.ChangeGroupNumAndPrefix(devicePrefix, groupNumber);
            }

        }


        public void ChangeIpAddresses(ulong aIpOffset)
        {
            foreach (IManageDevice currentDevice in Devices)
            {
                currentDevice.AddOffsetToIpAddresse(aIpOffset);
            }
        }

        public void ConnectPlcToMasterIoSystem(IoSystem aIoSystem)
        {
            if (aIoSystem != null)
            {
                Plc.ConnectToIoSystem(aIoSystem);
                masterSubnet = aIoSystem.Subnet;
            }
            
        }

        public void CreateNewIoSystem(Subnet aSubnet)
        {
            newIoSystem = Plc?.CreateNewIoSystem(aSubnet);
            masterSubnet = aSubnet;
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

        #region static methodes

        public static DeviceType Get_DeviceType(Device device)
        {
            foreach (DeviceItem deviceItem in device.DeviceItems)
            {
                DeviceItem deviceItemToGetService = deviceItem as DeviceItem;
                SoftwareContainer swContainer = deviceItemToGetService.GetService<SoftwareContainer>();//DriveObject
                if (swContainer != null)
                {
                    if (swContainer.Software is PlcSoftware plc)
                    {
                        return DeviceType.Plc;
                    }
                    else if (swContainer.Software is HmiTarget hmi)
                    {
                        return DeviceType.Hmi;
                    }
                }
                else
                {
                    DriveObjectContainer doContainer = deviceItemToGetService.GetService<DriveObjectContainer>();//DriveObject
                    if (doContainer != null)
                    {
                        if (doContainer.DriveObjects[0] is DriveObject drive)
                        {
                            return DeviceType.Drive;
                        }
                    }
                }
            }

            return DeviceType.ioDevice;
        }

        #endregion

    }


    //===============================================================================================
    class DeviceEnum : IEnumerator
    {
        public List<IManageDevice> Devices;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public DeviceEnum(List<IManageDevice> devices)
        {
            Devices = devices;
        }

        public bool MoveNext()
        {
            position++;
            return (position < Devices.Count);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public IManageDevice Current
        {
            get
            {
                try
                {
                    return Devices[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

    }

}
