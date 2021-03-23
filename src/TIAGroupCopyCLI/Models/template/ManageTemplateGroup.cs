using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.MC.Drives;
using Siemens.Engineering.Hmi;

using TIAGroupCopyCLI.MessagingFct;
using TIAGroupCopyCLI.AppExceptions;
using Siemens.Engineering.Library.MasterCopies;
using System.Globalization;

namespace TIAGroupCopyCLI.Models.template
{
    class ManageTemplateGroup
    {

        private readonly DeviceUserGroupComposition tiaUserGroups;
        private readonly DeviceUserGroup tiaTemplateGroup;
        private ManageGroup _templateGroup;
        private readonly MasterCopy templateMasterCopy;
        private Dictionary<uint, (string NamePrefix, string Number)> FoundGroups = new Dictionary<uint, (string NamePrefix, string Number)>();


        public ManageGroup TemplateGroup { get { return _templateGroup; }  }
        public Subnet OriginalSubnet => _templateGroup.originalSubnet;
        public IoSystem MasterIoSystem => _templateGroup.masterIoSystem;

        #region Constructor
        public ManageTemplateGroup(Project project, string templateGroupName, uint templateGroupNumber, string groupNamePrefix, string devicePrefix, uint numOfGroups, string indexFormat)
        {

            tiaUserGroups = project.DeviceGroups;
            tiaTemplateGroup = project.DeviceGroups.Find(templateGroupName);
            if (tiaTemplateGroup == null)
            {
                throw new GroupCopyException("Group not found.");
            }

            _templateGroup = new ManageGroup(tiaTemplateGroup);
            if (_templateGroup.Devices.Where(d => d.DeviceType == DeviceType.Plc).Count() != 1)
            {
                throw new GroupCopyException("No PLC or more than 1 PLC found in group.");
            }

            Messaging.Progress("Preparing template group.");
            _templateGroup.StripGroupNumAndPrefix(devicePrefix);
            _templateGroup.SaveConfig();

            //=======copy to master copy========
            try
            {
                templateMasterCopy = project.ProjectLibrary.MasterCopyFolder.MasterCopies.Create(tiaTemplateGroup);
            }
            catch (Exception ex)
            {
                throw new GroupCopyException("Could not create master copy.", ex);
            }

            if (templateMasterCopy == null) throw new GroupCopyException("Could not create master copy.");


            if (templateGroupNumber != 0)
            {
                _templateGroup.RestoreGroupNumAndPrefix();
            }
            else
            {
                Messaging.Progress("Adjusting template group.");
                FoundGroups = GetGroupNames(project.DeviceGroups, groupNamePrefix);
                if (GroupExists(1))
                {
                    throw new GroupCopyException("Could not rename template group to group 1 because group 1 already exisits.");
                }
                else
                {
                    _templateGroup.ChangeGroupNumAndPrefix(devicePrefix, (1).ToString(indexFormat, CultureInfo.InvariantCulture));
                }
            }

            FoundGroups.Clear();
            FoundGroups = GetGroupNames(project.DeviceGroups, groupNamePrefix);


        }
        #endregion Constructor

        public void DeleteMasterCopy()
        {
            try
            {
                //MasterCopy deleteMasterCopy = project.ProjectLibrary.MasterCopyFolder.MasterCopies.Find(templateMasterCopy.Name);
                templateMasterCopy.Delete();
            }
            catch (Exception ex)
            {
                Messaging.FaultMessage("Could not delete Mastercopy.", ex);
            }

        }
   

        public bool GroupExists(uint groupNumber)
        {
            return FoundGroups.ContainsKey(groupNumber);
        }

        public ManageGroup CreateNewGroup()
        {

            ManageGroup newGroup;
            try
            {
                DeviceUserGroup newTiaGroup = tiaUserGroups.CreateFrom(templateMasterCopy);
                if (newTiaGroup == null)
                {
                    throw new GroupCopyException("Could not create new Group.");
                }
                else
                {
                    newGroup = new ManageGroup(newTiaGroup);
                }
            }
            catch (Exception e)
            {
                throw new GroupCopyException("Could not create new Group.", e);
            }
            return newGroup;

        }
        private static Dictionary<uint, (string NamePrefix, string Number)> GetGroupNames(DeviceUserGroupComposition deviceUserGroups, string onlyWithGroupNamePrefix)
        {

            Dictionary<uint, (string NamePrefix, string Number)> returnGroupNames = new Dictionary<uint, (string NamePrefix, string Number)>();

            foreach (DeviceUserGroup deviceUserGroup in deviceUserGroups)
            {
                uint groupNumberInt;
                string groupNumberStr;
                string groupNamePrefix;

                try
                {
                    Match foundGroupNum = Regex.Match(deviceUserGroup.Name, "\\d+$", RegexOptions.IgnoreCase);
                    if (foundGroupNum.Success)
                    {

                        groupNumberStr = foundGroupNum.Value;
                        groupNumberInt = UInt32.Parse(groupNumberStr, CultureInfo.InvariantCulture);
                        groupNamePrefix = deviceUserGroup.Name.TrimEnd(groupNumberStr.ToCharArray());
                        if (groupNamePrefix ==  onlyWithGroupNamePrefix)
                            returnGroupNames.Add(groupNumberInt, (groupNamePrefix, groupNumberStr));

                    }
                }
                catch { }

            }

            return returnGroupNames;
        }

    }
}
