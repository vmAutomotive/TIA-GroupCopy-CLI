using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TIAGroupCopyCLI.MessagingFct;
using System.Globalization;
using System.Text.RegularExpressions;
using TIAGroupCopyCLI.AppExceptions;

namespace TIAGroupCopyCLI.Para
{

    class Parameters
    {
        #region Filed
        public string ProjectPath;
        public string ProjectVersion;
        public string TemplateGroupName;
        public uint TemplateGroupNumber;
        public string GroupNamePrefix;
        //public bool GroupNameIsStartGroup;
        //public uint StartGroupNum ;
        public string DevicePrefix;
        public uint NumOfGroups;
        public string IndexFormat = "D1";
        public uint FBaseAddrOffset ;
        public uint FDestAddrOffset;
        //public uint IDeviceDeviceNumberOffset = 0;
        public uint IDeviceIoAddressOffset ;
        //public bool ParameterOK;
        #endregion Filed

        #region Constructor
        public Parameters(string[] aArgs)
        {

            int currentArgIdx = 0;

            #region general check of arguments
            if ((aArgs == null) || (aArgs.Length == 0) || (aArgs.Length < 4))
            {
                Messaging.Progress("Not enough parameters.");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }

            if ((aArgs[0] == @"\?") || (aArgs[0] == @"/?") || (aArgs[0] == "?") || (aArgs[0] == "-?"))
            {
                Description();
                throw new ParameterException();
            }
            #endregion

            #region Argument ProjectPath

            if (!File.Exists(aArgs[currentArgIdx]))
            {
                Messaging.Progress("File " + aArgs[currentArgIdx] + " does not exisits!");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }
            ProjectPath = aArgs[currentArgIdx];


            uint projectMajorVersion;
            uint projectMinorVersion;

            string extension = Path.GetExtension(ProjectPath);

            if (extension.Substring(1, 2) != "ap")
            {
                Messaging.Progress("The extions does not start with \".ap\"and therefor is not a TIAP project.");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }
            int underscorePosition = extension.IndexOf('_', 3);
            if (underscorePosition == -1) underscorePosition = extension.Length;

            if (underscorePosition <= 3)
            {
                Messaging.Progress("Extension format invalid.");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }
            try
            {
                projectMajorVersion = (uint)Int32.Parse(extension.Substring(3, underscorePosition - 3), CultureInfo.InvariantCulture);
                if (underscorePosition < extension.Length)
                {
                    projectMinorVersion = (uint)Int32.Parse(extension.Substring(underscorePosition + 1, extension.Length - underscorePosition - 1), CultureInfo.InvariantCulture);
                }
                else
                {
                    projectMinorVersion = 0;
                }
            }
            catch (Exception e)
            {
                Messaging.FaultMessage($"Could not convert extension {extension} to version number.", e);
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException("",e);
            }
            ProjectVersion = projectMajorVersion.ToString("D", CultureInfo.InvariantCulture) + "." + projectMinorVersion.ToString("D", CultureInfo.InvariantCulture);

            #endregion

            #region Argument GroupNamePrefix
            currentArgIdx++;
            
            string argGroupName = aArgs[currentArgIdx];
            //uint factor = 1;
            //uint numberCount = 0;

            Match foundGroupNum = Regex.Match(argGroupName, "\\d+$", RegexOptions.IgnoreCase);
            
            int digitCount_TemplateGroupNameNumber = 0;
            if (foundGroupNum.Success)
            {
                TemplateGroupNumber = UInt32.Parse(foundGroupNum.Value, CultureInfo.InvariantCulture);
                if (TemplateGroupNumber != 1)
                {
                    Messaging.FaultMessage("The tempalte group name has to be either:");
                    Messaging.Progress("        1) A group with no number and prefixes (e.g. (Group_, -plc)");
                    Messaging.Progress("        2) A group with number 1 and the number 1 used in the prefix (e.g. Group_01, agv01-plc).");
                    PrintArg(aArgs);
                    Messaging.Progress("");
                    Description();
                    throw new ParameterException();
                }

                digitCount_TemplateGroupNameNumber = foundGroupNum.Value.Length;
                GroupNamePrefix = argGroupName.TrimEnd(foundGroupNum.Value.ToCharArray());
                TemplateGroupName = argGroupName;
            }
            else
            {
                GroupNamePrefix = argGroupName;
                TemplateGroupName = argGroupName.TrimEnd(' ');
            }


            #endregion

            #region Argument DevicePrefix
            currentArgIdx++;
            DevicePrefix = aArgs[currentArgIdx];
            #endregion

            #region Argument NumOfGroups
            currentArgIdx++;
            string argNumOfGroups = aArgs[currentArgIdx];
            try
            {
                NumOfGroups = uint.Parse(argNumOfGroups, CultureInfo.InvariantCulture);
                //uint endGroupNum = StartGroupNum + NumOfGroups - 1;
                //if (!GroupNameIsStartGroup)
                //{
                //    IndexFormat = 'D' + ((uint)endGroupNum.ToString().Count()).ToString("D1", CultureInfo.InvariantCulture);
                //}
            }
            catch (Exception e)
            {
                Messaging.FaultMessage("Parameters NumOfGroups = " + argNumOfGroups + " could not be converted to a number. ", e);
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException("",e);
            }

            if (NumOfGroups < 1)
            {
                Messaging.FaultMessage("Parameters NumOfGroups = " + NumOfGroups + " too small.");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }
            else if (NumOfGroups > 1000)
            {
                Messaging.FaultMessage("Parameters NumOfGroups = " + NumOfGroups + " too large (max 999) ");
                PrintArg(aArgs);
                Messaging.Progress("");
                Description();
                throw new ParameterException();
            }

            int digitCount_NumOfGroups = argNumOfGroups?.Length ?? 0;
            if (digitCount_TemplateGroupNameNumber > 0)
            {
                IndexFormat = 'D' + ((uint)(digitCount_TemplateGroupNameNumber)).ToString("D0", CultureInfo.InvariantCulture);
            }
            else
            {
                IndexFormat = 'D' + ((uint)(digitCount_NumOfGroups)).ToString("D0", CultureInfo.InvariantCulture);
            }
            

            #endregion

            #region Agument FBaseAddrOffset
            currentArgIdx++;
            if (aArgs.Length > currentArgIdx)
            {
                try
                {
                    FBaseAddrOffset = UInt32.Parse(aArgs[currentArgIdx], CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    Messaging.FaultMessage("Parameters FBaseAddrOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ", e);
                    PrintArg(aArgs);
                    Messaging.Progress("");
                    Description();
                    throw new ParameterException("", e);
                }
            }
            #endregion

            #region Agument FDestAddrOffset
            currentArgIdx++;
            if (aArgs.Length > currentArgIdx)
            {
                try
                {
                    FDestAddrOffset = UInt32.Parse(aArgs[currentArgIdx], CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    Messaging.FaultMessage("Parameters FDestAddrOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ", e);
                    PrintArg(aArgs);
                    Messaging.Progress("");
                    Description();
                    throw new ParameterException("", e);
                }
            }
            #endregion

            #region Agument IDeviceIoAddressOffset
            currentArgIdx++;
            if (aArgs.Length > currentArgIdx)
            {
                //currentArgIdx = 10;  //test exeception
                try
                {
                    IDeviceIoAddressOffset = UInt32.Parse(aArgs[currentArgIdx], CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    Messaging.FaultMessage("Parameters IDeviceIoAddressOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ", e);
                    PrintArg(aArgs);
                    Messaging.Progress("");
                    Description();
                    throw new ParameterException("", e);
                }
            }
            #endregion

            PrintSettings();

        }

        #endregion Constructor

        #region Methods
        private static void Description()
        {

            Messaging.Progress("");
            Messaging.Progress("TIAGroupCopyCLI.exe ProjectPath GroupName Prefix NumberOfGroups FBaseAddrOffset FDestAddrOffset IDeviceDeviceNoOffset IDeviceIoAddrOffset");
            Messaging.Progress("");
            Messaging.Progress("Parameters:");
            Messaging.Progress("1. ProjectPath           = path and name of project");
            Messaging.Progress("                           (e.g. C:\\Projects\\MyProject\\MyProjects.ap15_1)");
            Messaging.Progress("2. GroupName             = name of exiting template group in project");
            Messaging.Progress("                           (e.g. Group_ , Group_1 , Group_01)");
            Messaging.Progress("3. DevicePrefix          = Text to be added in fron of device name");
            Messaging.Progress("                           (e.g. AGV, so _plc will become AGV01_plc)");
            Messaging.Progress("4. NumberOfGroups        = how many groups do you want to end up with");
            Messaging.Progress("                           (including the template group)");
            Messaging.Progress("5. FBaseAddrOffset       = by what increment should the central FBaseAddr");
            Messaging.Progress("                           of the PLC be increamented");
            Messaging.Progress("6. FDestAddrOffset       = by what increment should the type 1 F-Dest Address");
            Messaging.Progress("                           of each module be increased as well as the lower");
            Messaging.Progress("                           and uper limit setting of type 1 F-DestAddresses)");
            //Messaging.Progress("7. IDeviceDeviceNoOffset = by what increment should the iDevice DeviceNumber for each PLC be increased");
            Messaging.Progress("7. IDeviceIoAddrOffset   = by what increment should the io address for the");
            Messaging.Progress("                           iDevice connection to the master PLC be incremented");
            Messaging.Progress("                           (on the master PLC side)");
            Messaging.Progress("");
            Messaging.Progress("Example:");
            Messaging.Progress("TIAGroupCopyCLI.exe  C:\\Projects\\MyProject\\MyProjects.ap15_1  Group_ AGV 60 1 50 1 100");
            Messaging.Progress("");

            throw new ParameterException();
        }


        private void PrintSettings()
        {

            Messaging.Progress("");
            Messaging.Progress("The tool starts with the following settings:");
            Messaging.Progress("");
            Messaging.Progress("ProjectPath           = " + ProjectPath);
            Messaging.Progress("Project Version       = " + ProjectVersion);
            Messaging.Progress("TemplateGroupName     = " + TemplateGroupName);
            Messaging.Progress("GroupNamePrefix       = " + GroupNamePrefix);
            
            if (TemplateGroupNumber>0)
            {
            Messaging.Progress("TemplateGroupNumber   = " + TemplateGroupNumber);
            }
            
            Messaging.Progress("NumberFormat          = " + IndexFormat + " ( => " + ((uint)0).ToString(IndexFormat, CultureInfo.InvariantCulture) + " )") ;
            Messaging.Progress("DevicePrefix          = " + DevicePrefix);
            Messaging.Progress("NumberOfGroups        = " + NumOfGroups);
            Messaging.Progress("FBaseAddrOffset       = " + FBaseAddrOffset);
            Messaging.Progress("FDestAddrOffset       = " + FDestAddrOffset);
            Messaging.Progress("IDeviceIoAddrOffset   = " + IDeviceIoAddressOffset);
            Messaging.Progress("");

        }

        private static void PrintArg(string[] aArgs)
        {
            int currentArgIdx = 0;

            Messaging.Progress("");
            if (aArgs == null) return;
            if (aArgs.Count() < currentArgIdx + 1) return;
            Messaging.Progress("Command line arguments:");
            Messaging.Progress("");
            Messaging.Progress((currentArgIdx + 1) + ". ProjectPath           = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". GroupName             = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". DevicePrefix          = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". NumberOfGroups        = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". FBaseAddrOffset       = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". FDestAddrOffset       = " + aArgs[currentArgIdx]);

            currentArgIdx++;
            if (aArgs.Count() < currentArgIdx + 1) return;
            if (aArgs.Length < currentArgIdx) return;
            Messaging.Progress((currentArgIdx + 1) + ". IDeviceIoAddrOffset   = " + aArgs[currentArgIdx]);

        }

        #endregion Methods

    }
}
