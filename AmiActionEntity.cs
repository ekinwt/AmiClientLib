using System;
using System.Collections.Generic;
using System.Text;

namespace AmiClientLib
{
    public class AmiActionEntity
    {
        private Dictionary<string, string> m_SendDictionary;

        public AmiActionEntity()
        {

        }

        public AmiActionEntity(Dictionary<string, string> p_SendDictionary)
        {
            this.SendDictionary = p_SendDictionary;
        }

        public void AddParameter(string p_Key, string p_Value)
        {
            this.SendDictionary[p_Key] = p_Value;
        }

        public string GetActionSendString()
        {
            if (this.SendDictionary.Count == 0)
            {
                throw new Exception("Action NOT set");
            }

            StringBuilder oActionBuilder = new StringBuilder();
            foreach (var item in this.SendDictionary)
            {
                oActionBuilder.AppendFormat("{0}:{1}\r\n", item.Key, item.Value);
            }
            oActionBuilder.Append("\r\n");

            return oActionBuilder.ToString();
        }

        public Dictionary<string, string> SendDictionary
        {
            get
            {
                if (this.m_SendDictionary == null)
                {
                    this.m_SendDictionary = new Dictionary<string, string>();
                }

                return this.m_SendDictionary;
            }
            set
            {
                this.m_SendDictionary = value;
            }
        }
    }
}
