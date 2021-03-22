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
                FDestinationAddr = Service.GetAttribute(aTelegram, "Failsafe_FDestinationAddress");
                StartAddress = Service.GetAttributes(aTelegram.Addresses, "StartAddress");
            }
        }
        #endregion Constructor

        #region Methods
        public void SaveFDestAndIoAddresses()
        {
            if (Telegram != null)
            {
                FDestinationAddr = Service.GetAttribute(Telegram, "Failsafe_FDestinationAddress");
                StartAddress = Service.GetAttributes(Telegram.Addresses, "StartAddress");
            }

        }

        public new void AdjustFDestinationAddress(ulong aOffset, ulong aLower, ulong aUpper)
        {
            if (FDestinationAddr != null)
            {

                if (((uint)FDestinationAddr.Value >= aLower) && ((uint)FDestinationAddr.Value <= aUpper))
                {
                    FDestinationAddr.AddToValue(aOffset);
                }

            }
        }
        public void RestoreFDestAndIoAddresses()
        {
            if (Telegram != null)
            {
                if (FDestinationAddr != null)
                {
                    Service.SetAttribute(Telegram, "Failsafe_FDestinationAddress", FDestinationAddr);
                }
                int i = 0;
                foreach (Address currentAddress in Telegram.Addresses)
                {
                    Service.SetAttribute(currentAddress, "StartAddress", StartAddress[i]);
                    i++;
                }
            }

        }
        #endregion methods
    }

    class ManageDrive : ManageDevice
    {
        #region Fields
        private readonly List<TelegramAndAttributes> AllTelegrams = new List<TelegramAndAttributes>();
        #endregion Fields

        #region Constructors 
        public ManageDrive(Device aDevice) : base(aDevice)
        {
            //AllDevices.Add(aDevice);
            Save();
        }
        public ManageDrive(IList<Device> aDevices) : base( aDevices)
        {
            //AllDevices.AddRange(aDevices);
            Save();
        }
        #endregion Constructors

        #region Methods
        public new void Save()
        {
            SaveFDestAndIoAddresses();
        }

        public new void Restore()
        {
            RestoreFDestAndIoAddresses();
            base.Restore();
        }

        public void SaveFDestAndIoAddresses()
        {
            foreach(Device currentDrive in AllDevices)
            {
                DriveObject tempDrive = currentDrive.DeviceItems[1].GetService<DriveObjectContainer>().DriveObjects[0];
                foreach (Telegram currentTelegram in tempDrive.Telegrams)
                {
                    TelegramAndAttributes newTelegram = new TelegramAndAttributes(currentTelegram);
                    if (newTelegram != null)
                    {
                        AllTelegrams.Add(newTelegram);
                    }
                    
                }
            }
        }

        public void RestoreFDestAndIoAddresses()
        {
            foreach (TelegramAndAttributes currentTelegram in AllTelegrams)
            {

                currentTelegram.RestoreFDestAndIoAddresses();
            }
        }

        public new void AdjustFDestinationAddress(ulong aOffset, ulong aLower, ulong aUpper)
        {
            foreach (TelegramAndAttributes currentTelegram in AllTelegrams)
            {
                currentTelegram.AdjustFDestinationAddress(aOffset, aLower, aUpper);
            }
        }
        #endregion methods
    }
}
