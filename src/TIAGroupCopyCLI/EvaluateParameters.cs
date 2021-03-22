using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TIAGroupCopyCLI.Para
{

    class Parameters
    {
        #region Filed
        public string ProjectPath;
        public string ProjectVersion;
        public string TemplateGroupName;
        public string NewGroupNamePrefix;
        public string Prefix = "";
        public uint NumOfGroups = 0;
        public uint FBaseAddrOffset = 0;
        public uint FDestAddrOffset = 0;
        //public uint IDeviceDeviceNumberOffset = 0;
        public uint IDeviceIoAddressOffset = 0;
        public bool ParameterOK = false;
        #endregion Filed

        #region Constructor
        public Parameters(string[] aArgs)
        {

            int currentArgIdx = 0;

            if ( (aArgs == null) || (aArgs.Count() == 0) || (aArgs.Count() < 4) )
            {
                Program.Progress("Not enough parameters.");
                Description();
                return;
            }

            if ( (aArgs[0] == @"\?") || (aArgs[0] == @"/?") || (aArgs[0] == "?") || (aArgs[0] == "-?"))
            {
                Description();
                return;
            }

            
            #region Argument ProjectPath

            if (! File.Exists(aArgs[currentArgIdx]))
            {
                Program.Progress("File " + aArgs[currentArgIdx] + " does not exisits!");
                Description();
                return;
            }
            ProjectPath = aArgs[currentArgIdx];


            uint projectMajorVersion;
            uint projectMinorVersion;

            string extension = Path.GetExtension(ProjectPath);

            if (extension.Substring(1, 2) != "ap")
            {
                Program.Progress("The extions does not start with \".ap\"and therefor is not a TIAP project.");
                Description();
                return;
            }
            int underscorePosition = extension.IndexOf('_',3);
            if (underscorePosition == -1) underscorePosition = extension.Length;

            if (underscorePosition <= 3)
            {
                Program.Progress("Extension format invalid.");
                Description();
                return;
            }
            try
            {
                projectMajorVersion = (uint)Int32.Parse(extension.Substring(3, underscorePosition - 3));
                if (underscorePosition < extension.Length)
                {
                    projectMinorVersion = (uint)Int32.Parse(extension.Substring(underscorePosition+1, extension.Length - underscorePosition - 1));
                }
                else
                {
                    projectMinorVersion = 0;
                }
            }
            catch(Exception e)
            {
                Program.FaultMessage($"Could not convert extension {extension} to version number.", e);
                Description();
                return;
            }
            ProjectVersion = projectMajorVersion.ToString() + "." + projectMinorVersion.ToString();

            #endregion

            #region Argument GroupNamePrefix
            currentArgIdx++;
            NewGroupNamePrefix = aArgs[currentArgIdx];
            TemplateGroupName = NewGroupNamePrefix;

            while (TemplateGroupName[TemplateGroupName.Length - 1].Equals(' '))
            {
                TemplateGroupName = TemplateGroupName.Substring(0, TemplateGroupName.Length - 1);
            }
            #endregion

            #region Argument Prefix
            currentArgIdx++;
            Prefix = aArgs[currentArgIdx];
            #endregion

            #region Argument NumOfGroups
            currentArgIdx++;
            try
            {
                NumOfGroups = UInt32.Parse(aArgs[currentArgIdx]);
            }
            catch (Exception e)
            {
                Program.FaultMessage("Parameters NumOfGroups = " + aArgs[currentArgIdx] + " could not be converted to a number. ", e);
                Description();
                return;
            }
            
            if (NumOfGroups < 1 )
            {
                Program.FaultMessage("Parameters NumOfGroups = " + NumOfGroups + " too small .");
                Description();
                return;
            }else if (NumOfGroups > 1000)
            {
                Program.FaultMessage("Parameters NumOfGroups = " + NumOfGroups + " too larg (max 999 ");
                Description();
                return;
            }
            #endregion

            #region Agument FBaseAddrOffset
            currentArgIdx++;
            if (aArgs.Count() > currentArgIdx)
            {
                try
                {
                    FBaseAddrOffset = UInt32.Parse(aArgs[currentArgIdx]);
                }
                catch (Exception e)
                {
                    Program.FaultMessage("Parameters FBaseAddrOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ",e );
                    Description();
                    return;
                }
            }
            #endregion

            #region Agument FDestAddrOffset
            currentArgIdx++;
            if (aArgs.Count() > currentArgIdx)
            {
                try
                {
                    FDestAddrOffset = UInt32.Parse(aArgs[currentArgIdx]);
                }
                catch (Exception e)
                {
                    Program.FaultMessage("Parameters FDestAddrOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ",e);
                    Description();
                    return;
                }
            }
            #endregion

            #region Agument IDeviceIoAddressOffset
            currentArgIdx++;
            if (aArgs.Count() > currentArgIdx)
            {
                //currentArgIdx = 10;  //test exeception
                try
                {
                    IDeviceIoAddressOffset = UInt32.Parse(aArgs[currentArgIdx]);
                }
                catch (Exception e)
                {
                    Program.FaultMessage("Parameters IDeviceIoAddressOffset = " + aArgs[currentArgIdx] + " could not be converted to a number. ",e);
                    Description();
                    return;
                }
            }
            #endregion
           
            PrintSettings();
            ParameterOK = true;
        }

        #endregion Constructor

        #region Methods
        private void Description()
        {
            
            Program.Progress("");
            Program.Progress("TIAGroupCopyCLI.exe ProjectPath GroupName Prefix NumberOfGroups FBaseAddrOffset FDestAddrOffset IDeviceDeviceNoOffset IDeviceIoAddrOffset");
            Program.Progress("");
            Program.Progress("Parameters:");
            Program.Progress("1. ProjectPath           = path and name of project");
            Program.Progress("                           (e.g. C:\\Projects\\MyProject\\MyProjects.ap15_1)");
            Program.Progress("2. GroupName             = name of exiting template group in project");
            Program.Progress("                           (e.g. Group_ ");
            Program.Progress("3. Prefix                = Text to be added in fron of device name");
            Program.Progress("                           (e.g. AGV, so _plc will become AGV01_plc");
            Program.Progress("4. NumberOfGroups        = how many groups do you want to end up with");
            Program.Progress("                           including the template group");
            Program.Progress("5. FBaseAddrOffset       = by what increment should the central FBaseAddr");
            Program.Progress("                           of the PLC be increamented");
            Program.Progress("6. FDestAddrOffset       = by what increment should the type 1 F-Dest Address");
            Program.Progress("                           of each module be increased as well as the lower");
            Program.Progress("                           and uper limit setting of type 1 F-DestAddresses)");
            //Program.Progress("7. IDeviceDeviceNoOffset = by what increment should the iDevice DeviceNumber for each PLC be increased");
            Program.Progress("7. IDeviceIoAddrOffset   = by what increment should the io address for the");
            Program.Progress("                           iDevice connection to the master PLC be incremented");
            Program.Progress("                           (on the master PLC side)");
            Program.Progress("");
            Program.Progress("Example:");
            Program.Progress("TIAGroupCopyCLI.exe  C:\\Projects\\MyProject\\MyProjects.ap15_1  Group_ AGV 60 1 50 1 100");
            Program.Progress("");

            ParameterOK = false;
        }

        private void PrintSettings()
        {

            Program.Progress("");
            Program.Progress("The tool Starts with the following settings:");
            Program.Progress("");
            Program.Progress("ProjectPath           = " + ProjectPath);
            Program.Progress("Project Version       = " + ProjectVersion);
            Program.Progress("GroupName             = " + TemplateGroupName);
            Program.Progress("Prefix                = " + Prefix);
            Program.Progress("NumberOfGroups        = " + NumOfGroups);
            Program.Progress("FBaseAddrOffset       = " + FBaseAddrOffset);
            Program.Progress("FDestAddrOffset       = " + FDestAddrOffset);
            Program.Progress("IDeviceIoAddrOffset   = " + IDeviceIoAddressOffset);
            Program.Progress("");

            ParameterOK = false;
        }

        #endregion Methods

    }
}
