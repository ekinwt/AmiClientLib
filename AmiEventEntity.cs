using System;
using System.Collections.Generic;
using System.Text;

namespace AmiClientLib
{
    public class AmiEventEntity
    {
        private Dictionary<string, string> m_MessageDictionary;

        public AmiEventEntity()
        {

        }

        public AmiEventEntity(Dictionary<string, string> p_MessageDictionary)
        {
            this.MessageDictionary = p_MessageDictionary;
        }

        public string EventName
        {
            get
            {
                return this.GetEventValue("Event");
            }
        }

        public string Uniqueid
        {
            get
            {
                return this.GetEventValue("Uniqueid");
            }
        }

        public string Linkedid
        {
            get
            {
                return this.GetEventValue("Linkedid");
            }
        }

        public string GetEventValue(string p_Key)
        {
            string value;
            bool result = this.MessageDictionary.TryGetValue(p_Key, out value);
            if (result)
            {
                return value;
            }
            else
            {
                throw new Exception(string.Format("Event Key:{0} NOT found", p_Key));
            }
        }

        public Dictionary<string, string> MessageDictionary
        {
            get
            {
                return this.m_MessageDictionary;
            }
            set
            {
                this.m_MessageDictionary = value;
            }
        }
    }
}
