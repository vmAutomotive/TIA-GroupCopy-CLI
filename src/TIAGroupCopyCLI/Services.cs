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
using TIAGroupCopyCLI.AppExceptions;

namespace TIAHelper.Services
{



    public static class Service
    {

        public static TiaPortal OpenProject(string ProjectPath, out Project project, out bool tiaStartedWithoutInterface)
        {

            //First check if project already open
            TiaPortal tiaPortal = null;
            project = null;
            tiaStartedWithoutInterface = false;


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
                        if (tiaPortalProcess.Mode == TiaPortalMode.WithoutUserInterface)
                        {
                            tiaStartedWithoutInterface = true;
                        }
                        return tiaPortal;
                    }
                    catch (Siemens.Engineering.EngineeringSecurityException e)
                    {
                        tiaPortal = null;
                        project = null;
                        throw new GroupCopyException("Could not start TIAP. Please achnoledge the \"Openness access\" security dialog box with [Yes] or [Yes to all] to grant access or if there was no dialog box add the user to the user group \"Siemens TIA Openness\". (see exception message below for details)", e);
                    }
                    catch (Exception e)
                    {
                        throw new GroupCopyException("Could not attach to running TIAP with open project.", e);

                    }

                }
            }

            if ((tiaPortal == null) || (project == null))
            {


                Console.WriteLine("Starting TIA Portal");
                try
                {
                    tiaPortal = new TiaPortal(TiaPortalMode.WithoutUserInterface);
                    tiaStartedWithoutInterface = true;
                }
                catch (Siemens.Engineering.EngineeringSecurityException e)  //-2146233088
                {
                    tiaPortal = null;
                    project = null;
                    throw new GroupCopyException("Could not start TIAP. Please achnoledge the \"Openness access\" security dialog box with [Yes] or [Yes to all] to grant access or if there was no dialog box add the user to the user group \"Siemens TIA Openness\". (see exception message below for details)", e);
                }
                catch (Exception e)
                {
                    throw new GroupCopyException("Could not start TIAP.", e);
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
                    throw new GroupCopyException("Could not open project " + projectPath.FullName, e);
                }
            }

            return tiaPortal;
        }
        

    }
}
