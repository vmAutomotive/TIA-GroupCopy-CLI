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
using System.Text.RegularExpressions;
using TIAGroupCopyCLI.AppExceptions;

namespace TIAGroupCopyCLI.Models
{


    class ManageDevice
    {

        #region References to openness object and managed objcts

        public Device Device { get; set; }
        public List<ManageNetworkInterface> NetworkInterfaces { get; set; }  = new List<ManageNetworkInterface>();

        #endregion

        #region Fields
        
        protected readonly ManageAttributeGroup FDestinationAddress_attribues = new ManageAttributeGroup();
        protected readonly string OriginalTiaDeviceName;
        string TemplateTiaDeviceName;

        #endregion Fields

        #region constructors

        public ManageDevice(Device device)
        {
            Device = device;
            NetworkInterfaces = ManageNetworkInterface.GetAll_ManageNetworkInterfaceObjects(Device);
            try
            {
                OriginalTiaDeviceName = Device.DeviceItems[1].Name;
            }
            catch { }
        }
        #endregion

        #region Methods


        public virtual void SaveConfig()
        {
            FDestinationAddress_attribues.FindAndSaveDeviceItemAtributes(Device, "Failsafe_FDestinationAddress");
            foreach(ManageNetworkInterface currentItem in NetworkInterfaces)
            {
                currentItem.SaveConfig();
            }
        }

        public void CopyFromTemplate(ManageDevice templateDevice)
        {

            for (int i = 0; i < templateDevice.FDestinationAddress_attribues.Count; i++)
            {
                FDestinationAddress_attribues[i].Value = templateDevice.FDestinationAddress_attribues[i].Value;
            }

            for (int i = 0; i < templateDevice.NetworkInterfaces.Count; i++)
            {
                NetworkInterfaces[i]?.CopyFromTemplate(templateDevice.NetworkInterfaces[i]);


            }
        }

        public virtual void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong lowerFDest, ulong upperFDest)
        {
            foreach (SingleAttribute currentItem in FDestinationAddress_attribues) 
            {
                if (((ulong)currentItem.Value >= lowerFDest) && ((ulong)currentItem.Value <= upperFDest))
                {
                    currentItem.RestoreWithOffset(fDestOffset);
                }
            }
            foreach (ManageNetworkInterface currentItem in NetworkInterfaces)
            {
                currentItem.RestoreConfig_WithAdjustments(pnDeviceNumberOffset);
            }
        }


        public virtual void StripGroupNumAndPrefix(string devicePrefix)
        {
            TemplateTiaDeviceName = "temp" + Regex.Replace(OriginalTiaDeviceName, "^" + devicePrefix + "\\d+", "", RegexOptions.IgnoreCase);
            try
            {
                Device.DeviceItems[1].Name = TemplateTiaDeviceName;
                if (Device.DeviceItems[1].Name != TemplateTiaDeviceName)
                {
                    throw new GroupCopyException($"Could not rename TIA object \"{OriginalTiaDeviceName}\" in selected template group to name \"{TemplateTiaDeviceName}\", probably because that name already exsits.");
                }
            }
            catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
            {
                throw;
            }
            catch
            {
            }

            if (NetworkInterfaces.Count > 0)
                NetworkInterfaces[0].StripGroupNumAndPrefixFromPnDeviceName(devicePrefix);
        }

        public virtual void RestoreGroupNumAndPrefix()
        {
            if (OriginalTiaDeviceName.Length == 0)
            {
                throw new ProgrammingException($"Could not restore TIA object of template Group because the orignal name is blank \"{OriginalTiaDeviceName}\".");
            }

            try
            {
                Device.DeviceItems[1].Name = OriginalTiaDeviceName;
                if (Device.DeviceItems[1].Name != OriginalTiaDeviceName)
                {
                    throw new GroupCopyException($"Could not restore TIA object name in selected template group from \"{TemplateTiaDeviceName}\"  to orignal name \"{OriginalTiaDeviceName}\".");
                }
            }
            catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
            {
                throw;
            }
            catch (Exception e)
            {
            }
            if (NetworkInterfaces.Count > 0)
                NetworkInterfaces[0].RestoreGroupNumAndPrefixToPnDeviceName();
        }

        public virtual void ChangeGroupNumAndPrefix(string devicePrefix, string groupNumber)
        {
            if (groupNumber.Length == 0)
            {
                throw new ProgrammingException($"Could not change TIA Name of object because of invalid group number.");
            }

            try
            {
                string newName = Regex.Replace(Device.DeviceItems[1].Name, "^temp", devicePrefix + groupNumber);
                if (newName.Length == 0)
                {
                    throw new GroupCopyException($"Could not change TIA Name of object.");
                }
                Device.DeviceItems[1].Name = newName;
                if (Device.DeviceItems[1].Name != newName)
                {
                    throw new GroupCopyException($"Could not chnage TIA object name to \"{newName}\".");
                }
            }
            catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
            {
                throw;
            }
            catch (Exception e)
            {
            }
            if (NetworkInterfaces.Count > 0)
                NetworkInterfaces[0].ChangeGroupNumAndPrefixToPnDeviceName(devicePrefix, groupNumber);

        }

        public void xAddPrefixToTiaName(string aPrefix)
        {
            try
            {
                Device.DeviceItems[1].Name = aPrefix + Device.DeviceItems[1].Name;
            }
            catch
            {
            }
        }

        public void xAddPrefixToPnDeviceName(string aPrefix)
        {
            if (NetworkInterfaces.Count > 0)
                NetworkInterfaces[0].xAddPrefixToPnDeviceName(aPrefix);
        }

        public void AddOffsetToIpAddresse(ulong aIpOffset)
        {
            if (NetworkInterfaces.Count > 0)
            {
                    NetworkInterfaces[0].AddOffsetToIpAddress(aIpOffset);
            }
        }

        public Subnet Get_Subnet()
        {

            if (NetworkInterfaces.Count > 0)
            {
                return  NetworkInterfaces[0].Get_Subnet();
            }
            return null;
        }

        public IoSystem Get_ioSystem()
        {
            if (NetworkInterfaces.Count > 0)
            {
                return NetworkInterfaces[0].Get_ioSystem();
            }
            return null;
        }
        public void Reconnect(Subnet subnet, IoSystem ioSystem)
        {
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].Reconnect(subnet, ioSystem);
            }
        }
        public void SwitchIoSystem(Subnet aSubnet, IoSystem aIoSystem)
        {
            DisconnectFromSubnet();
            ConnectToSubnet(aSubnet);
            ConnectToIoSystem(aIoSystem);
        }
        public void DisconnectFromSubnet()
        {
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].DisconnectFromSubnet();
            }
        }
        public void ConnectToSubnet(Subnet aSubnet)
        {
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].ConnectToSubnet(aSubnet);
            }
        }
        public void ConnectToIoSystem(IoSystem aIoSystem)
        {
            if (NetworkInterfaces.Count > 0)
            {
                NetworkInterfaces[0].ConnectToIoSystem(aIoSystem);
            }
        }

        #endregion Methods

    }
}
