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

    public class TelegramAndAttributes
    {
        #region Fileds
        AttributeValue FDestinationAddr;
        private readonly Telegram Telegram;
        List<AttributeValue> StartAddress = new List<AttributeValue>();
        #endregion Fileds

        #region Constructor
        public TelegramAndAttributes(Telegram aTelegram)
        {
            Telegram = aTelegram;
            if (aTelegram != null)
            {
                FDestinationAddr = AttributeValue.GetAttribute(aTelegram, "Failsafe_FDestinationAddress");
                StartAddress = AttributeValue.GetAttributes(aTelegram.Addresses, "StartAddress");
            }
        }
        #endregion Constructor

        #region Methods

        public void RestoreConfig_WithAdjustments(ulong aOffset, ulong aLower, ulong aUpper)
        {
            if (Telegram != null)
            {
                if (FDestinationAddr != null)
                {

                    if (((uint)FDestinationAddr.Value >= aLower) && ((uint)FDestinationAddr.Value <= aUpper))
                    {
                        FDestinationAddr.AddToValue(aOffset);
                    }
                    SingleAttribute.SetAttribute_Wrapper(Telegram, "Failsafe_FDestinationAddress", FDestinationAddr.Value);
                }

                int i = 0;
                foreach (Address currentAddress in Telegram.Addresses)
                {
                    SingleAttribute.SetAttribute_Wrapper(currentAddress, "StartAddress", StartAddress[i].Value);
                    i++;
                }
            }

        }
        #endregion
    }

    class ManageDrive : ManageDevice , IManageDevice
    {
        #region Fields
        private readonly List<TelegramAndAttributes> AllTelegrams = new List<TelegramAndAttributes>();
        public DeviceType DeviceType { get; } = DeviceType.Drive;
        private DriveObject driveObject;
        #endregion Fields

        #region Constructors 
        public ManageDrive(Device aDevice) : base(aDevice)
        {
            driveObject = Get_DriveObject(aDevice);
        }

        #endregion Constructors

        #region Methods
        public new void SaveConfig()
        {

            foreach (Telegram currentTelegram in driveObject.Telegrams)
            {
                TelegramAndAttributes newTelegram = new TelegramAndAttributes(currentTelegram);
                if (newTelegram != null)
                {
                    AllTelegrams.Add(newTelegram);
                }

            }
            base.SaveConfig();
        }
        
        public new void RestoreConfig_WithAdjustments(ulong pnDeviceNumberOffset, ulong fSourceOffset, ulong fDestOffset, ulong lowerFDest, ulong upperFDest)
        {
            foreach (TelegramAndAttributes currentTelegram in AllTelegrams)
            {
                currentTelegram.RestoreConfig_WithAdjustments(fDestOffset, lowerFDest, upperFDest);
            }

            base.RestoreConfig_WithAdjustments(pnDeviceNumberOffset, fSourceOffset, fDestOffset, lowerFDest, upperFDest);
        }

        #endregion methods

        #region static methods
        public static DriveObject Get_DriveObject(Device device)
        {
            //PlcSoftware plcSoftware = null;
            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                DriveObjectContainer doContainer = currentDeviceItem.GetService<DriveObjectContainer>();
                if (doContainer != null)
                {
                    if (doContainer.DriveObjects[0] is DriveObject drive)
                    {
                        return drive;
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
