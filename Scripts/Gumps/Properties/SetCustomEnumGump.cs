using System;
using System.Collections;
using System.Reflection;
using Server.Commands;
using Server.Network;

namespace Server.Gumps
{
    public class SetCustomEnumGump : SetListOptionGump
    {
        private static readonly Type typeofIDynamicEnum = typeof(IDynamicEnum);

        private readonly string[] m_Names;
        public SetCustomEnumGump(PropertyInfo prop, Mobile mobile, object o, Stack stack, int propspage, ArrayList list, string[] names)
            : base(prop, mobile, o, stack, propspage, list, names, null)
        {
            this.m_Names = names;
        }

        public override void OnResponse(NetState sender, RelayInfo relayInfo)
        {
            int index = relayInfo.ButtonID - 1;

            if (index >= 0 && index < this.m_Names.Length)
            {
                try
                {
                    MethodInfo info = this.m_Property.PropertyType.GetMethod("Parse", new Type[] { typeof(string) });

                    string result = "";

                    if (info != null)
                        result = Properties.SetDirect(this.m_Mobile, this.m_Object, this.m_Object, this.m_Property, this.m_Property.Name, info.Invoke(null, new object[] { this.m_Names[index] }), true);
                    else if (this.m_Property.PropertyType == typeof(Enum) || this.m_Property.PropertyType.IsSubclassOf(typeof(Enum)))
                        result = Properties.SetDirect(this.m_Mobile, this.m_Object, this.m_Object, this.m_Property, this.m_Property.Name, Enum.Parse(this.m_Property.PropertyType, this.m_Names[index], false), true);
                    else if (typeofIDynamicEnum.IsAssignableFrom(this.m_Property.PropertyType))
                    {
                        IDynamicEnum ienum = (IDynamicEnum)this.m_Property.GetValue(this.m_Object);

                        if (ienum != null)
                            ienum.Value = this.m_Names[index];

                        result = Properties.SetDirect(this.m_Mobile, this.m_Object, this.m_Object, this.m_Property, this.m_Property.Name, ienum, true);
                    }

                    this.m_Mobile.SendMessage(result);

                    if (result == "Property has been set.")
                        PropertiesGump.OnValueChanged(this.m_Object, this.m_Property, this.m_Stack);
                }
                catch
                {
                    this.m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            this.m_Mobile.SendGump(new PropertiesGump(this.m_Mobile, this.m_Object, this.m_Stack, this.m_List, this.m_Page));
        }
    }
}