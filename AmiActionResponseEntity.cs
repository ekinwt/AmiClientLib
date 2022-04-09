using System;
using System.Collections.Generic;
using System.Text;

namespace AmiClientLib
{
    public class AmiActionResponseEntity
    {
        private Dictionary<string, string> m_AnswerDictionary;

        public AmiActionResponseEntity()
        {

        }

        public AmiActionResponseEntity(Dictionary<string, string> p_AnswerDictionary)
        {
            this.SetAnswerDictionary(p_AnswerDictionary);
        }

        public void SetAnswerDictionary(Dictionary<string, string> p_AnswerDictionary)
        {
            this.AnswerDictionary = p_AnswerDictionary;
        }

        public string Response
        {
            get
            {
                return this.GetAnswerValue("Response");
            }
        }

        public string Message
        {
            get
            {
                return this.GetAnswerValue("Message");
            }
        }

        public string GetAnswerValue(string p_Key)
        {
            if (this.AnswerDictionary == null)
            {
                throw new Exception("Action NOT received answer!");
            }

            string value;
            bool result = this.AnswerDictionary.TryGetValue(p_Key, out value);
            if (result)
            {
                return value;
            }
            else
            {
                throw new Exception(string.Format("Action Response Key:{0} NOT found", p_Key));
            }
        }

        public Dictionary<string, string> AnswerDictionary
        {
            get
            {
                return this.m_AnswerDictionary;
            }
            set
            {
                this.m_AnswerDictionary = value;
            }
        }
    }
}
