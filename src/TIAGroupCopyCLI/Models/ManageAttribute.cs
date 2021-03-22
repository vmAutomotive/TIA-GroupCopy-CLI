using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

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


using TIAHelper.Services;
using TIAGroupCopyCLI.Models;
using TIAGroupCopyCLI.MessagingFct;

namespace TIAGroupCopyCLI.Models
{
    //=========================================================================================================
    public class AttributeValue
    {

        public object Value;

        #region Constructor
        public AttributeValue()
        {
        }
        public AttributeValue(object aObject)
        {
            Value = aObject;
        }
        #endregion

        #region add methods
        public object AddToValue(uint addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public object AddToValue(int addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            else if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public object AddToValue(ulong addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public object AddToValue(object addToValue)
        {
            if (Value is ulong)
            {
                Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                Value = (uint)Value + (uint)addToValue;
            }
            else if (Value is int)
            {
                Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public object AddToValueIfInBetween(object addToValue, object loverLinit, object UpperLimit)
        {

            if (Value is ulong)
            {
                if (((ulong)Value >= (ulong)loverLinit) && ((ulong)Value <= (ulong)UpperLimit))
                    Value = (ulong)Value + (ulong)addToValue;
            }
            else if (Value is uint)
            {
                if (((uint)Value >= (uint)loverLinit) && ((uint)Value <= (uint)UpperLimit))
                    Value = (uint)Value + (uint)addToValue;
            }
            else if (Value is int)
            {
                if (((int)Value >= (int)loverLinit) && ((int)Value <= (int)UpperLimit))
                    Value = (int)Value + (int)addToValue;
            }

            return Value;
        }
        public int GetValueAsInt()
        {
            return (int)Value;
        }
        #endregion

        #region static methods
        public static AttributeValue GetAttribute(IEngineeringObject aIEngineeringObject, string aAttributeName)
        {
            if (aIEngineeringObject != null)
            {
                try
                {
                    object attributeValue = aIEngineeringObject.GetAttribute(aAttributeName);
                    AttributeValue newItem = new AttributeValue(attributeValue);
                    //newItem.Value = attributeValue;
                    return newItem;

                }
                catch (EngineeringNotSupportedException)
                {

                }
                catch (Exception ex)
                {
                    Messaging.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttribute");
                }
            }

            return null;
        }

        public static List<AttributeValue> GetAttributes(IEngineeringComposition aIEngineeringComposition, string aAttributeName)
        {
            List<AttributeValue> returnItems = new List<AttributeValue>();

            if (aIEngineeringComposition != null)
            {
                foreach (IEngineeringObject currentItem in aIEngineeringComposition)
                {

                    try
                    {
                        AttributeValue newItem = GetAttribute(currentItem, aAttributeName);
                        if (newItem != null)
                        {
                            returnItems.Add(newItem);
                        }
                    }
                    catch (EngineeringNotSupportedException)
                    {

                    }
                    catch (Exception ex)
                    {
                        Messaging.FaultMessage("Could not get Attribute", ex, "AttributeValue.GetAttributes");
                    }
                }
            }
            return returnItems;
        }

        public static bool SetAttribute(IEngineeringObject aIEngineeringObject, string aAttributeName, AttributeValue aAttributeValue)
        {
            if ((aIEngineeringObject != null) && (aAttributeValue != null) )
            {
                try
                {
                    aIEngineeringObject.SetAttribute(aAttributeName, aAttributeValue.Value);
                    return true;
                }
                catch (Exception ex)
                {
                    Messaging.FaultMessage("Could not set Attribute.", ex, "AttributeValue.SetAttribute");
                }

            }
            return false;
        }

        #endregion

    }

    //=========================================================================================================
    public class TransferAreaAndAttributes
    {
        #region Fileds

        public AttributeValue PartnerStartAddress;
        private readonly TransferArea TransferArea;
        #endregion Fileds

        #region Constructor
        public TransferAreaAndAttributes(TransferArea aTransferArea)
        {
            TransferArea = aTransferArea;
            if (aTransferArea != null)
            {
                PartnerStartAddress = AttributeValue.GetAttribute(aTransferArea.PartnerAddresses[0], "StartAddress");
            }
        }
        public TransferAreaAndAttributes(TransferArea aTransferArea, object aValue)
        {
            TransferArea = aTransferArea;
            PartnerStartAddress = new AttributeValue()
            {
                Value = aValue
            };
        }

        #endregion Constructor

        #region methods
        public void SavePartnerStartAddress()
        {
            if (TransferArea != null)
            {

                PartnerStartAddress = AttributeValue.GetAttribute(TransferArea.PartnerAddresses[0], "StartAddress");
            }

        }

        public void RestorePartnerStartAddress()
        {
            if (TransferArea != null)
            {
                if (PartnerStartAddress != null)
                {
                    //Service.SetAttribute(TransferArea.PartnerAddresses[0], "StartAddress", PartnerStartAddress);
                    SingleAttribute.SetAttribute_Wrapper(TransferArea.PartnerAddresses[0], "StartAddress", PartnerStartAddress?.Value);
                }
            }

        }

        public void RestorePartnerStartAddressWitOffset(ulong aIDeviceOffsett)
        {
            if (TransferArea != null)
            {
                if (PartnerStartAddress != null)
                {
                    SingleAttribute.SetAttribute_Wrapper(TransferArea.PartnerAddresses[0], "StartAddress", PartnerStartAddress.AddToValue(aIDeviceOffsett));
                }
            }

        }
        #endregion methods
    }

    //=========================================================================================================
    class SingleAttribute : AttributeValue
    {
        #region References to openness object
        private IEngineeringObject EngineeringObject;
        #endregion

        #region Fileds
        public string Name;
        //public object Value;

        #endregion

        #region constructor

        public SingleAttribute(IEngineeringObject engineeringObject, string name, object value):base(value)
        {
            EngineeringObject = engineeringObject;
            Name = name;
            //Value = value;
        }

        #endregion constructor

        #region methods

        public bool AddToValueIfNameEquals(string name, object addToValue)
        {
            if (name == this.Name)
            {
                AddToValue(addToValue);
                return true;
            }
            return false;
        }

        #region Save/Restore
        public bool ReGetAttribute()
        {
            object value = GetAttribute_Wrapper(EngineeringObject, Name);
            if (value != null)
            {
                Value = value;
            }
            return true;
        }
        public void Restore()
        {
            if ((Value != null) && (EngineeringObject != null))
            {
                SetAttribute_Wrapper(EngineeringObject, Name, Value);
            }
        }
        public void RestoreWithPrefix(string prefix)
        {
            if ((Value != null) && (EngineeringObject != null))
            {
                SetAttribute_Wrapper(EngineeringObject, Name, prefix + Value);
            }
        }
        public void RestoreWithSuffix(string suffix)
        {
            if ((Value != null) && (EngineeringObject != null))
            {
                SetAttribute_Wrapper(EngineeringObject, Name, Value + suffix);
            }
        }
        public void RestoreWithOffset(object offset)
        {
            if ((Value != null) && (EngineeringObject != null))
            {
                object newValue;

                if (Value is ulong)
                {
                    newValue = (ulong)Value + (ulong)offset;
                }
                else if (Value is uint)
                {
                    newValue = (uint)Value + (uint)offset;
                }
                else if (Value is int)
                {
                    newValue = (int)Value + (int)offset;
                }
                else
                {
                    newValue = null;
                }

                SetAttribute_Wrapper(EngineeringObject, Name, newValue);
            }
        }
        #endregion

        #endregion

        #region  static methods

        public static SingleAttribute GetSimpleAttributeObject(IEngineeringObject engineeringObject, string attributeName)
        {
            object attributeValue = GetAttribute_Wrapper(engineeringObject, attributeName);
            if (attributeValue != null)
            {
                return new SingleAttribute(engineeringObject, attributeName, attributeValue);
            }
            return null;
        }

        public static bool SetAttribute_Wrapper(IEngineeringObject engineeringObject, string attributeName, AttributeValue attributeValueObject)
        {
            try
            {
                engineeringObject.SetAttribute(attributeName, attributeValueObject.Value);
                return true;
            }
            catch (Exception ex)
            {
                Messaging.FaultMessage("Could not set Attribute \"" + attributeName + "\" in engineeringObject.", ex);
            }
            return false;
        }

        public static bool SetAttribute_Wrapper(IEngineeringObject engineeringObject, string attributeName, object attributeValue)
        {
            try
            {
                engineeringObject.SetAttribute(attributeName, attributeValue);
                return true;
            }
            catch (Exception ex)
            {
                Messaging.FaultMessage("Could not set Attribute \"" + attributeName + "\" in engineeringObject.", ex);
            }
            return false;
        }

        public static object GetAttribute_Wrapper(IEngineeringObject engineeringObject, string attributeName)
        {
            object attributeValue;
            try
            {

                attributeValue = engineeringObject.GetAttribute(attributeName);
                return attributeValue;
            }
            catch (EngineeringNotSupportedException) //Attribute Name not found  = OK, move on
            {

            }
            catch (Exception ex) // all other exception 
            {
                Messaging.FaultMessage("Could not get Attribute \"" + attributeName + "\" in engineeringObject.", ex);
            }
            return null;
        }

        #region FindAndSaveFirstDeviceItemAtribute
        public static SingleAttribute FindAndSaveFirstDeviceItemAtribute(Device device, string attributeName)
        {
            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                SingleAttribute found = FindAndSaveFirstDeviceItemAtribute(currentDeviceItem, attributeName);
                if (found != null ) return found;
            }

            return null;
        }

        static SingleAttribute FindAndSaveFirstDeviceItemAtribute(DeviceItem deviceItem, string attributeName)
        {
            object attributeValue = SingleAttribute.GetAttribute_Wrapper(deviceItem, attributeName);
            if (attributeValue != null)
            {
                return new SingleAttribute(deviceItem, attributeName, attributeValue);
            }

            foreach (DeviceItem currentDeviceItem in deviceItem.DeviceItems)
            {
                //call recursive
                SingleAttribute found = FindAndSaveFirstDeviceItemAtribute(currentDeviceItem, attributeName);
                if (found != null) return found;
            }

            return null;
        }

        #endregion DeviceItem Atribute

        #endregion
    }

    //=========================================================================================================
    class ManageAttributeGroup : IEnumerable
    {

        #region Fields
        public List<SingleAttribute> SavedAttributes = new List<SingleAttribute>();

        public int Count
        {
            get
            {
                return SavedAttributes.Count;
            }
        }
             
        #endregion Fields

        #region indexer
        public SingleAttribute this[int index]
        {
            get
            {
                if ((index < SavedAttributes.Count) && (index >= 0))
                {
                    return SavedAttributes[index];
                }
                else
                {
                    int reverseIndex = SavedAttributes.Count + index;
                    if ((reverseIndex < SavedAttributes.Count) && (reverseIndex >= 0))
                    {
                        return SavedAttributes[reverseIndex];
                    }
                }
                return null;
            }
            private set
            {
                if ((index < SavedAttributes.Count) && (index >= 0))
                {
                    SavedAttributes[index] = value;
                }
                else
                {
                    SavedAttributes.Add(value);
                }
            }
        }
        #endregion

        #region Constructor
        public ManageAttributeGroup()
        {

        }

        #endregion Constructor
        
        #region Enumerator
        // Implementation for the GetEnumerator method.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public AttributeEnum GetEnumerator()
        {
            return new AttributeEnum(SavedAttributes);
        }
        #endregion

        #region methods
        
        public bool AddEngineeringObjecAndAtributes(IEngineeringObject engineeringObject, string attributeName)
        {
            object attributeValue = SingleAttribute.GetAttribute_Wrapper(engineeringObject, attributeName);
            if (attributeValue != null)
            {
                SavedAttributes.Add(new SingleAttribute(engineeringObject, attributeName, attributeValue));
                return true;
            }

            return false;
        }

        public void AddAttribute(SingleAttribute attribute)
        {
            if (attribute != null)
            {
                SavedAttributes.Add(attribute);
            }

        }

        public bool GetAndAddAttribute(IEngineeringObject engineeringObject, string attributeName)
        {
            object attributeValue = SingleAttribute.GetAttribute_Wrapper(engineeringObject, attributeName);
            if (attributeValue != null)
            {
                SavedAttributes.Add(new SingleAttribute(engineeringObject, attributeName, attributeValue));
                return true;
            }
            return false;
        }

        public void Restore()
        {
            foreach (SingleAttribute currentAttribute in SavedAttributes)
            {
                currentAttribute.Restore();
            }
        }

        public void RestoreWithPrefix(string prefix)
        {
            foreach (SingleAttribute currentAttribute in SavedAttributes)
            {
                currentAttribute.RestoreWithPrefix(prefix);
            }
        }

        public void RestoreWithSuffix(string suffix)
        {
            foreach (SingleAttribute currentAttribute in SavedAttributes)
            {
                currentAttribute.RestoreWithSuffix(suffix);
            }
        }

        public void RestoreWithOffset(object offset)
        {
            foreach (SingleAttribute currentAttribute in SavedAttributes)
            {
                currentAttribute.RestoreWithOffset(offset);
            }
        }


        #region DeviceItem Atribute
        public bool FindAndSaveDeviceItemAtributes(Device device, string attributeName, bool onlyFirstFind = false)
        {
            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                bool found = FindAndSaveAllDeviceItemAttributes(currentDeviceItem, attributeName, onlyFirstFind);
                if (found && onlyFirstFind) return true;
            }

            return false;
        }

        bool FindAndSaveAllDeviceItemAttributes(DeviceItem deviceItem, string attributeName, bool onlyFirstFind = false)
        {
            object attributeValue = SingleAttribute.GetAttribute_Wrapper(deviceItem, attributeName);
            if (attributeValue != null)
            {
                SavedAttributes.Add(new SingleAttribute(deviceItem, attributeName, attributeValue));
                return true;
            }

            foreach (DeviceItem currentDeviceItem in deviceItem.DeviceItems)
            {
                //call recursive
                bool found = FindAndSaveAllDeviceItemAttributes(currentDeviceItem, attributeName, onlyFirstFind);
                if (found && onlyFirstFind) return true;
            }

            return false;
        }

        #endregion DeviceItem Atribute

        #region Address Atributes
        public bool FindAndSaveAddressAttributes(Device device, string attributeName, bool onlyFirstFind = false)
        {
            foreach (DeviceItem currentDeviceItem in device.DeviceItems)
            {
                bool found = FindAndSaveAllDeviceItemAttributes(currentDeviceItem, attributeName, onlyFirstFind);
                if (found && onlyFirstFind) return true;
            }

            return false;
        }

        public bool FindAndSaveAddressAttributes(DeviceItem deviceItem, string attributeName, bool onlyFirstFind = false)
        {
            foreach (Address currentAddress in deviceItem.Addresses)
            {
                object attributeValue = SingleAttribute.GetAttribute_Wrapper(currentAddress, attributeName);
                if (attributeValue != null)
                {
                    SavedAttributes.Add(new SingleAttribute(currentAddress, attributeName, attributeValue));
                    return true;
                }
            }

            foreach (DeviceItem currentDeviceItem in deviceItem.DeviceItems)
            {
                //call recursive
                bool found = FindAndSaveAddressAttributes(currentDeviceItem, attributeName, onlyFirstFind);
                if (found && onlyFirstFind) return true;
            }

            return false;
        }

        #endregion  Address Atributes

        #endregion methods
        
    }

    
    //===============================================================================================
    class AttributeEnum : IEnumerator
    {
        public List<SingleAttribute> SavedAttributes;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public AttributeEnum(List<SingleAttribute> savedAttributes)
        {
            SavedAttributes = savedAttributes;
        }

        public bool MoveNext()
        {
            position++;
            return (position < SavedAttributes.Count);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public SingleAttribute Current
        {
            get
            {
                try
                {
                    return SavedAttributes[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

    }

}