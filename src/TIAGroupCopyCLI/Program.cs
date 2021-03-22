using System;
using System.Reflection;
//using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.Library.MasterCopies;

using TiaOpennessHelper.Utils;
using TIAHelper.Services;
using TIAGroupCopyCLI.Models;
using TIAGroupCopyCLI.Para;
using TIAGroupCopyCLI.Models.template;
using TIAGroupCopyCLI.MessagingFct;
using TIAGroupCopyCLI.AppExceptions;
using System.Globalization;

namespace TIAGroupCopyCLI //TIAGroupCopyCLI
{
    class Program
    {
        #region Fileds
        const string TIAP_VERSION_USED_FOR_TESTING = "15.1";
        const string OPENESS_VERSION_USED_FOR_TESTING = "15.1.0.0";

        static Parameters Parameters;


        #endregion


        static void Main(string[] args)
        {

            try
            {
                Heandlers.AddAppExceptionHaenlder();


                //string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
                //string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
                Messaging.Progress("TIA Group copy v" + fileVersion);
                Messaging.Progress("This beta version is a customized solution for now");


                Messaging.Progress("================================================================");

                Parameters = new Parameters(args);

                Heandlers.SelectAssmebly(Parameters.ProjectVersion, TIAP_VERSION_USED_FOR_TESTING, OPENESS_VERSION_USED_FOR_TESTING);
                Heandlers.AddAssemblyResolver();

                GroupCopy();

            }
            catch (TIAGroupCopyCLI.AppExceptions.ParameterException)
            {

            }
            catch (TIAGroupCopyCLI.AppExceptions.GroupCopyException e)
            {
                Messaging.FaultMessage(e.Message);

            }



            Console.WriteLine("");
            Console.WriteLine("Hit enter to exit.");
            Console.ReadLine();
        }


        //=================================================================================================
        private static void GroupCopy()
        {

            Messaging.Progress("Check running TIA Portal");

            using (TiaPortal tiaPortal = Service.OpenProject(Parameters.ProjectPath, out Project project, out bool tiaStartedWithoutInterface))
            {

                if ((tiaPortal == null) || (project == null))
                {
                    throw new GroupCopyException("Could not open project.");
                }

                Messaging.Progress($"Project {project.Path.FullName} is open");



                Messaging.Progress("Searching for template group.");

                uint groupCounter = 1;
                ManageTemplateGroup manageTemplateGroup = new ManageTemplateGroup(
                                                                    project,
                                                                    Parameters.TemplateGroupName,
                                                                    Parameters.TemplateGroupNumber,
                                                                    Parameters.GroupNamePrefix,
                                                                    Parameters.DevicePrefix,
                                                                    Parameters.NumOfGroups,
                                                                    Parameters.IndexFormat
                                                                    );



                
                while (++groupCounter <= Parameters.NumOfGroups)
                {
                    if (!manageTemplateGroup.GroupExists(groupCounter))
                    {
                        string groupNumberStr = (groupCounter).ToString(Parameters.IndexFormat, CultureInfo.InvariantCulture);
                        Messaging.Progress("Creating Group " + groupCounter);
                        ManageGroup newGroup = manageTemplateGroup.CreateNewGroup();

                        newGroup.SaveConfig();
                        newGroup.ChangeGroupNumAndPrefix(Parameters.DevicePrefix, groupNumberStr);
                        newGroup.ChangeIpAddresses(groupCounter - 1);
                        newGroup.CreateNewIoSystem(manageTemplateGroup.OriginalSubnet);
                        newGroup.ConnectPlcToMasterIoSystem(manageTemplateGroup.MasterIoSystem);
                        newGroup.CopyFromTemplate(manageTemplateGroup.TemplateGroup);
                        newGroup.ReconnectAndRestore_WithAdjustments((groupCounter - 1), Parameters.FBaseAddrOffset * (groupCounter - 1), Parameters.FDestAddrOffset * (groupCounter - 1), (Parameters.IDeviceIoAddressOffset * (groupCounter - 1)));
                        newGroup.DelecteOldSubnet();

                    }
                }

                manageTemplateGroup.DeleteMasterCopy();

                Messaging.Progress("");

                Messaging.Progress("Copy complete.");
                if (tiaStartedWithoutInterface == true)
                {
                    Messaging.Progress("Saving project.");
                    project.Save();
                    project.Close();
                }
                else
                {
                    Messaging.Progress("Please save project within TIA Portal!");
                }

                try
                {
                    tiaPortal.Dispose();
                }
                catch
                {

                }
            }
        }

    }
}
