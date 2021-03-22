using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using TIAGroupCopyCLI;
using TIAGroupCopyCLI.MessagingFct;

namespace TiaOpennessHelper.Utils
{

    public static class Heandlers
    {
        private const string BASE_PATH = "SOFTWARE\\Siemens\\Automation\\Openness\\";
        private const string BASE_PATH64 = "SOFTWARE\\WOW6432Node\\Siemens\\Automation\\Openness";
        private const string INSTALL_PATH_PRE = @"C:\Program Files\Siemens\Automation\Portal V";
        private const string BIN_PATH_POST = @"\Bin\Siemens.Automation.Portal.exe";
        private const string API_PATH_SUBFOLDER = @"\PublicAPI\V";
        private const string API_PATH_POST = @"\Siemens.Engineering.dll";
        private static string AssemblyPath = "";


        public static bool SelectAssmebly(string projectVersion, string preferedTiaVersion, string preferedAssemblyVersion)
        {

            string selectedTiaVersion = "";
            string selectedAssemblyVersion = "";
            AssemblyPath = "";

            Version projectV = new Version(projectVersion);
            Version preferedTiaV = new Version(preferedTiaVersion);
            Version preferedAssemblyV = new Version(preferedAssemblyVersion);

            //check if TIA protal version for this project is isntalled
            List<string> tiaVersionsString = GetEngineeringVersions();
            if (tiaVersionsString.Count > 0)
            {
                foreach (string currentTiaV in tiaVersionsString)
                {
                    Version tiaVersion = new Version(currentTiaV);
                    if (tiaVersion == projectV)
                    {
                        selectedTiaVersion = currentTiaV;

                        //select openness version closest to what was used during development
                        List<string> assmblyVersionsString = GetOpennessAssmblyVersions(selectedTiaVersion);
                        if (assmblyVersionsString.Count > 0)
                        {
                            foreach (string currentAssemblyV in assmblyVersionsString)
                            {
                                selectedAssemblyVersion = currentAssemblyV;
                                Version assemblyVersion = new Version(currentAssemblyV);
                                AssemblyPath = GetOpennessAssemblyPath(selectedTiaVersion, selectedAssemblyVersion);
                                if (assemblyVersion >= preferedAssemblyV)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (AssemblyPath == "")
            {
                if (File.Exists(INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + BIN_PATH_POST))
                {
                    selectedTiaVersion = projectV.Major + "." + projectV.Minor;

                    if (projectV < preferedTiaV)
                    {
                        if (File.Exists(INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + projectV.Major + "_" + projectV.Minor + API_PATH_POST))
                        {
                            selectedAssemblyVersion = projectV.Major + "." + projectV.Minor;
                            AssemblyPath = INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + preferedTiaV.Major + "." + preferedTiaV.Minor + API_PATH_POST;
                        }
                        else
                        {
                            Messaging.Progress($"Openness dll version {projectVersion} not found (" + INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + projectV.Major + "_" + projectV.Minor + API_PATH_POST + ")");
                            return false;
                        }
                    }
                    else
                    {
                        if (File.Exists(INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + preferedTiaV.Major + "." + preferedTiaV.Minor + API_PATH_POST))
                        {
                            selectedAssemblyVersion = preferedTiaV.Major + "." + preferedTiaV.Minor + ".0.0";
                            AssemblyPath = INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + preferedTiaV.Major + "." + preferedTiaV.Minor + API_PATH_POST;
                        }
                        else
                        {
                            Messaging.Progress($"Openness dll version {projectVersion} not found (" + INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + API_PATH_SUBFOLDER + preferedTiaV.Major + "." + preferedTiaV.Minor + API_PATH_POST + ")");
                            return false;
                        }
                    }
                }
                else
                {
                    Messaging.Progress($"TIA version {projectVersion} not found (" + INSTALL_PATH_PRE + projectV.Major + "_" + projectV.Minor + BIN_PATH_POST + ")");
                    return false;
                }
            }


            if (AssemblyPath == "")
            {
                Messaging.Progress("Could not find Openness DLL.");
                return false;
            }
            if (!File.Exists(AssemblyPath))
            {
                Messaging.Progress("The following DLL does not exits: " + AssemblyPath);
                return false;
            }


            if (selectedTiaVersion != preferedTiaVersion)
            {
                Messaging.Progress($"Application was tested with TIAP version {preferedTiaVersion}");
                Messaging.Progress($"However, application will run with TIAP version {selectedTiaVersion}");
            }
            else
            {
                Messaging.Progress($"Application will run with TIAP version {selectedTiaVersion}");
            }


            if (selectedAssemblyVersion != preferedAssemblyVersion)
            {
                Messaging.Progress($"Application was tested with Openness version {preferedAssemblyVersion}");
                Messaging.Progress($"However, application will run with Openness version {selectedAssemblyVersion}");
            }
            else
            {
                Messaging.Progress($"Application will run with Openness version {selectedAssemblyVersion}");
            }

            return true;
        }

        public static List<string> GetEngineeringVersions()
        {
            RegistryKey key32 = GetRegistryKey(BASE_PATH);

            if (key32 != null)
            {
                var names = key32.GetSubKeyNames().OrderBy(x => x).ToList();
                key32.Dispose();

                if (names != null) return names;
            }

            RegistryKey key64 = GetRegistryKey(BASE_PATH);

            if (key64 != null)
            {
                var names = key64.GetSubKeyNames().OrderBy(x => x).ToList();
                key64.Dispose();

                if (names != null) return names;
            }


            return new List<string>();
        }

        public static List<string> GetOpennessAssmblyVersions(string tiaVersion)
        {
            RegistryKey key32 = GetRegistryKey(BASE_PATH + tiaVersion);

            if (key32 != null)
            {
                try
                {
                    var subKey = key32.OpenSubKey("PublicAPI");

                    var result = subKey.GetSubKeyNames().OrderBy(x => x).ToList();

                    subKey.Dispose();

                    if (result != null) return result;
                }
                catch
                { }
                finally
                {
                    key32.Dispose();
                }
            }

            RegistryKey key64 = GetRegistryKey(BASE_PATH64 + tiaVersion);

            if (key64 != null)
            {
                try
                {
                    var subKey = key64.OpenSubKey("PublicAPI");

                    var result = subKey.GetSubKeyNames().OrderBy(x => x).ToList();

                    subKey.Dispose();

                    if (result != null) return result;
                }
                catch { }
                finally
                {
                    key64.Dispose();
                }
            }


            return new List<string>();
        }

        public static string GetOpennessAssemblyPath(string tiaVersion, string opennessAssemblyVersion)
        {
            RegistryKey key = GetRegistryKey(BASE_PATH + tiaVersion + "\\PublicAPI\\" + opennessAssemblyVersion);

            if (key != null)
            {
                try
                {
                    string assemblyPath = key.GetValue("Siemens.Engineering").ToString();

                    return assemblyPath;
                }
                catch { }
                finally
                {
                    key.Dispose();
                }
            }

            return null;
        }

        private static RegistryKey GetRegistryKey(string keyname)
        {
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey key = baseKey.OpenSubKey(keyname);
            if (key == null)
            {
                baseKey.Dispose();
                baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);
                key = baseKey.OpenSubKey(keyname);
            }
            if (key == null)
            {
                baseKey.Dispose();
                baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                key = baseKey.OpenSubKey(keyname);
            }
            baseKey.Dispose();

            return key;
        }

        public static void AddAssemblyResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
        }

        public static Assembly OnResolve(object sender, ResolveEventArgs args)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var assemblyName = new AssemblyName(args.Name);

            if (assemblyName.Name.EndsWith("Siemens.Engineering"))
            {
                if (string.IsNullOrEmpty(AssemblyPath) == true)
                {
                    Messaging.Progress("loading Assembly error: No Assembly defined");
                }
                else if (!File.Exists(AssemblyPath))
                {
                    Messaging.Progress("loading Assemblyerror: " + AssemblyPath + " does not exists");
                }
                else
                {
                    Messaging.Progress("loading Assembly: " + AssemblyPath);
                    return Assembly.LoadFrom(AssemblyPath);
                }
            }

            return null;
        }

        public static void AddAppExceptionHaenlder()
        {
            if (!Debugger.IsAttached)
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnUnhandledException);
            }
        }
        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            string exceptionStr = args.ExceptionObject.ToString();
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("Ups -> Runtime terminating: {0}", args.IsTerminating);

            // Get stack trace for the exception with source file information
            var st = new StackTrace(e, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            //AppDomain.Unload(AppDomain.CurrentDomain);
        }
    }
}
