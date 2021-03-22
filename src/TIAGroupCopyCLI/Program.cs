using System;
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
using System.Net;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

using Siemens.Engineering.Library.MasterCopies;

using TiaOpennessHelper.Utils;
using TIAHelper.Services;
using TIAGroupCopyCLI.Models;
using TIAGroupCopyCLI.Para;



//using System.Windows.Forms;
//string pructverion2 = Application.ProductVersion;

namespace TIAGroupCopyCLI //TIAGroupCopyCLI
{
    
    class Program
    {
        const string TIAP_VERSION_USED_FOR_TESTING = "15.1";
        const string OPENESS_VERSION_USED_FOR_TESTING = TIAP_VERSION_USED_FOR_TESTING + ".0.0";


        static Parameters Parameters;

        private static TiaPortal tiaPortal;
        private static Project project;

        static void Main(string[] args)
        {
            
            //Heandlers.AddAppExceptionHaenlder();

            //string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            //string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
            Progress("TIA Group copy v" + fileVersion);
            Progress("This beta version is a customized solution for now");


            Progress("================================================================");

            Parameters = new Parameters(args);

            if (!Parameters.ParameterOK)
            {
                Console.ReadLine();
                return;
            }

            if (!Heandlers.SelectAssmebly(Parameters.ProjectVersion, TIAP_VERSION_USED_FOR_TESTING, OPENESS_VERSION_USED_FOR_TESTING))
            {
                Console.ReadLine();
                return;
            }
            
            Heandlers.AddAssemblyResolver();
            //MyResolverClass.AddAssemblyResolver();


            RunTiaPortal();




            Console.WriteLine("");
            Console.WriteLine("Hit enter to exit.");
            Console.ReadLine();
        }


        //=================================================================================================
        private static void RunTiaPortal()
        {

            #region tia and project
            Progress("Check running TIA Portal");
            bool tiaStartedWithoutInterface = false;

            Service.OpenProject(Parameters.ProjectPath, ref tiaPortal, ref project);
                        
            if ((tiaPortal == null) || (project == null))
            {
                CancelGeneration("Could not open project.");
                return;
            }

            Progress(String.Format("Project {0} is open", project.Path.FullName));
            #endregion

            #region test models

            /*
            Console.WriteLine("!!! TESTING !!!");
            DeviceUserGroup testGroup = project.DeviceGroups.Find(Parameters.TemplateGroupName);
            ManagePlc testPlcs = new ManagePlc(testGroup);


            NetworkPort testPlcPort = testPlcs.AllDevices[0].DeviceItems[1].DeviceItems[6].DeviceItems[0].GetService<NetworkPort>();
            NetworkPort patnerPort =  testPlcPort.ConnectedPorts[0];

            AttributeValue thisname = Service.GetAttribute(patnerPort, "Name");

            testPlcPort.DisconnectFromPort(patnerPort);
            testPlcPort.ConnectToPort(patnerPort);
            */

            #endregion

            #region master copy
            Progress("Creating master copy.");

            DeviceUserGroup templateGroup = project.DeviceGroups.Find(Parameters.TemplateGroupName);
            if (templateGroup == null)
            {
                CancelGeneration("Group not found.");
                return;
            }

            //=======copy to master copy========
            //MasterCopyComposition masterCopies = project.ProjectLibrary.MasterCopyFolder.MasterCopies;
            MasterCopy templateCopy = null;
            try
            {

                templateCopy = project.ProjectLibrary.MasterCopyFolder.MasterCopies.Create(templateGroup);
            }
            catch (Exception ex)
            {
                CancelGeneration("Could not create master copy.",ex);
                return;
            }

            if (templateCopy == null)
            {
                CancelGeneration("Could not create master copy.");
                return;
            }

            MasterCopy deleteMasterCopy = project.ProjectLibrary.MasterCopyFolder.MasterCopies.Find(templateCopy.Name);
            #endregion

            #region get basic info from template group
            IList<Device> templatePlcDevices = Service.GetPlcDevicesInGroup(templateGroup);
            ManagePlc templatePlcs = new ManagePlc(templatePlcDevices);


            templatePlcs.GetAll_iDeviceParnerIoAdresses();


            if (templatePlcs.AllDevices.Count != 1)
            {
                CancelGeneration("No PLC or more than 1 PLC in group.");
                return;
            }

            #endregion

            #region change name and IP of first group (template Group)
            string indexformat = "D2";
            uint groupCounter = 1;

            Progress("Adjusting template group.");
            string currentPrefix = Parameters.Prefix + groupCounter.ToString(indexformat);
            //templateGroup.Name = templateGroup.Name + groupCounter.ToString(indexformat);
            templateGroup.Name = Parameters.NewGroupNamePrefix + groupCounter.ToString(indexformat);
            //templateNetworkInterface.IoControllers[0].IoSystem.Name = currentPrefix + temlateIoSystemName;

            Service.ChangeDeviceNames(templateGroup, currentPrefix);
            templatePlcs.ChangeIoSystemName(currentPrefix);

            #endregion

            #region copy group loop
            DeviceUserGroupComposition userGroups = project.DeviceGroups;
 
            while (++groupCounter <= Parameters.NumOfGroups)
            {
                #region copy group
                Progress("Creating Group " + groupCounter);
                currentPrefix = Parameters.Prefix + groupCounter.ToString(indexformat);

                DeviceUserGroup newGroup;
                try
                {
                    newGroup = userGroups.CreateFrom(templateCopy);

                }
                catch(Exception e)
                {
                    CancelGeneration("Could not create new Group", e);
                    return;
                }

                #endregion

                #region read in devices
                //newGroup.Name = newGroup.Name + groupCounter.ToString(indexformat); ;
                newGroup.Name = Parameters.NewGroupNamePrefix + groupCounter.ToString(indexformat); ;
                Service.ChangeDeviceNames(newGroup, currentPrefix);

                IList<Device> plcDevices = Service.GetPlcDevicesInGroup(newGroup);
                ManagePlc plcs = new ManagePlc(plcDevices);

                IList<Device> hmiDevices = Service.GetHmiDevicesInGroup(newGroup);
                ManageHmi hmis = new ManageHmi(hmiDevices);

                IList<Device> driveDevices = Service.GetG120DevicesInGroup(newGroup);
                ManageDrive drives = new ManageDrive(driveDevices);

                IList<Device> allDevices = Service.GetAllDevicesInGroup(newGroup);
                IList<Device> tempIoDevices = allDevices.Except(hmis.AllDevices).Except(drives.AllDevices).ToList();
                tempIoDevices.Remove(plcs.AllDevices[0]);
                ManageIo ioDevices = new ManageIo(tempIoDevices);

                #endregion

                #region change settigns 
                plcs.ChangeIpAddresses(groupCounter - 1);
                plcs.CreateNewIoSystem(templatePlcs.originalSubnet, currentPrefix);
                plcs.ConnectToMasterIoSystem(templatePlcs.originalIoSystem);
                plcs.GetAll_iDeviceParnerIoAdresses();
                plcs.CopyFromTemplate(templatePlcs);
                plcs.AdjustFSettings(Parameters.FBaseAddrOffset * (groupCounter - 1), Parameters.FDestAddrOffset * (groupCounter - 1));
                plcs.AdjustPnDeviceNumberWithOffset((groupCounter - 1));
                plcs.AdjustPartnerIoAddresses(Parameters.IDeviceIoAddressOffset * (groupCounter - 1));
                plcs.Restore();
                plcs.ChangePnDeviceNames(currentPrefix);
                //plcs.SetAllIDeviceParnerAdresses();

                ioDevices.ChangeIpAddresses(groupCounter - 1);
                ioDevices.SwitchIoSystem(templatePlcs.originalSubnet, plcs.newIoSystem);
                if (templatePlcs.LowerBoundForFDestinationAddresses_attribues?.Value != null)
                    ioDevices.AdjustFDestinationAddress(Parameters.FDestAddrOffset * (groupCounter - 1), (ulong)templatePlcs.LowerBoundForFDestinationAddresses_attribues.Value, (ulong)templatePlcs.UpperBoundForFDestinationAddresses_attribues.Value);
                ioDevices.Restore();
                ioDevices.ChangePnDeviceNames(currentPrefix);

                hmis.ChangeIpAddresses(groupCounter - 1);
                hmis.DisconnectFromSubnet();
                hmis.ConnectToSubnet(templatePlcs.originalSubnet);
                hmis.Restore();
                hmis.ChangePnDeviceNames(currentPrefix);

                drives.ChangeIpAddresses(groupCounter - 1);
                drives.SwitchIoSystem(templatePlcs.originalSubnet, plcs.newIoSystem);
                if (templatePlcs.LowerBoundForFDestinationAddresses_attribues?.Value != null)
                    drives.AdjustFDestinationAddress(Parameters.FDestAddrOffset * (groupCounter - 1), (ulong)templatePlcs.LowerBoundForFDestinationAddresses_attribues.Value, (ulong)templatePlcs.UpperBoundForFDestinationAddresses_attribues.Value);
                drives.Restore();
                drives.ChangePnDeviceNames(currentPrefix);

                plcs.SetAllToConnections();


                plcs.RestoreAllPartnerPorts();
                hmis.RestoreAllPartnerPorts();
                drives.RestoreAllPartnerPorts();
                ioDevices.RestoreAllPartnerPorts();

                #endregion

                plcs.DelecteOldSubnet();
                //deleteNetworkSubnet.Delete();

            }

            #endregion

            try
            {

                deleteMasterCopy.Delete();
            }
            catch(Exception ex)
            {
                Program.FaultMessage("Could not delete Mastercopy.", ex);
            }

            Progress("");

            Console.WriteLine("Copy complete.");
            if (tiaStartedWithoutInterface == true)
            {
                Console.WriteLine("Saving project.");
                project.Save();
                project.Close();
            }
            else
            {
                Console.WriteLine("Please save project within TIAP.");
            }

            try
            {
                tiaPortal.Dispose();
            }
            catch
            {

            }


        }

        #region messaging
        public static void CancelGeneration(string message, Exception e = null)
        {
            //MessageBox.Show(message);
            //GenerateText = notInProgressText;
            //ProgressMessage = "";
            Console.WriteLine("");
            Console.WriteLine(message);
            if (e != null)
            {
                Console.WriteLine(e.Message);
            }
            //Console.ReadLine();
            try
            {
                tiaPortal.Dispose();
            }
            catch
            {

            }
            Console.WriteLine("");
        }

        public static void Progress(string message)
        {
            //MessageBox.Show(message);
            //GenerateText = notInProgressText;
            //ProgressMessage = "";
            Console.WriteLine(message);
        }
        public static void FaultMessage(string message, Exception ex = null, string functionName = "")
        {
            //MessageBox.Show(message);
            //GenerateText = notInProgressText;
            //ProgressMessage = "";
            Console.WriteLine("");
            Console.WriteLine(message);
            if (functionName!="")
            {
                Console.WriteLine("Exception in " + functionName + " : ");
            }
            if (ex != null)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("");
        }

        #endregion
    }
}
