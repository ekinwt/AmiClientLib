using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AmiClientLib
{
    /// <summary>
    /// KEY
    /// 1: send action to AMI server will block thread until server response, designed for sync, NOT async;
    /// 2: only one action can send at the same time;
    /// 3: when AMI server raise event, will raise onAmiRaiseEvent, can listen this event for some thing you want, like incoming call.
    /// </summary>
    public class AmiClient
    {
        private string m_AmiServerHost;
        private int m_AmiServerPort;
        private string m_AmiUsername;
        private string m_AmiPassword;

        private Socket m_Socket;

        private Thread m_ResponseThread;
        private object m_ActionLock = new object();
        private AutoResetEvent m_ActionAutoReset = new AutoResetEvent(false);

        private AmiActionResponseEntity m_LastActionResponseEntity;

        public delegate void AmiEventHandler(object p_Sender, AmiEventEntity p_AmiEvent);
        public event AmiEventHandler onAmiRaiseEvent;

        public AmiClient()
        {

        }

        public AmiClient(string p_ServerHost, int p_ServerPort)
        {
            this.AmiServerHost = p_ServerHost;
            this.AmiServerPort = p_ServerPort;
        }

        public AmiClient(string p_ServerHost, int p_ServerPort, string p_UserName, string p_Password) : this(p_ServerHost, p_ServerPort)
        {
            this.AmiUsername = p_UserName;
            this.AmiPassword = p_Password;
        }

        ~AmiClient()
        {
            if (this.m_Socket == null)
            {
                this.m_Socket.Shutdown(SocketShutdown.Both);
                this.m_Socket.Disconnect(false);
                this.m_Socket.Close();
            }
        }

        public void Connect()
        {
            if (this.m_Socket == null)
            {
                this.m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            if (!this.m_Socket.Connected)
            {
                IPEndPoint oEndPoint = new IPEndPoint(IPAddress.Parse(this.m_AmiServerHost), this.m_AmiServerPort);
                this.m_Socket.Connect(oEndPoint);

                if (this.m_ResponseThread == null)
                {
                    this.m_ResponseThread = new Thread(ReadResponse);
                    this.m_ResponseThread.IsBackground = true;

                    this.m_ResponseThread.Start();
                }
            }
        }

        public bool Login(out string p_out_Message)
        {
            AmiActionEntity oAction = new AmiActionEntity();
            oAction.AddParameter("Action", "Login");
            oAction.AddParameter("Username", this.AmiUsername);
            oAction.AddParameter("Secret", this.AmiPassword);

            AmiActionResponseEntity oAmiResponse = this.SendAction(oAction);
            p_out_Message = oAmiResponse.Message;
            if (oAmiResponse.Response == "Success")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Login(string p_UserName, string p_Password, out string p_out_Message)
        {
            this.AmiUsername = p_UserName;
            this.AmiPassword = p_Password;

            return this.Login(out p_out_Message);
        }

        public bool Logoff(out string p_out_Message)
        {
            AmiActionEntity oAction = new AmiActionEntity();
            oAction.AddParameter("Action", "Logoff");

            AmiActionResponseEntity oAmiResponse = this.SendAction(oAction);
            p_out_Message = oAmiResponse.Message;
            if (oAmiResponse.Response == "Goodbye")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public AmiActionResponseEntity SendAction(string p_ActionString)
        {
            AmiActionResponseEntity oActionResponse = null;

            lock (this.m_ActionLock)
            {
                Socket oSocket = this.GetSocket();
                oSocket.Send(Encoding.UTF8.GetBytes(p_ActionString));

                this.m_ActionAutoReset.WaitOne();

                oActionResponse = this.m_LastActionResponseEntity;
                this.m_LastActionResponseEntity = null;
            }

            return oActionResponse;
        }

        public AmiActionResponseEntity SendAction(AmiActionEntity p_Action)
        {
            AmiActionResponseEntity oActionResponse = this.SendAction(p_Action.GetActionSendString());
            return oActionResponse;
        }

        public Socket GetSocket()
        {
            return this.m_Socket;
        }

        public void ReadResponse()
        {
            Socket oSocket = this.GetSocket();

            const int MAX = 4096;
            byte[] buffer;
            string messageString;
            while (true)
            {
                if (!oSocket.Connected)
                {
                    break;
                }

                int offset = 0;
                buffer = new byte[MAX];
                messageString = string.Empty;
                while (!messageString.EndsWith("\r\n\r\n"))
                {
                    int bytesReceived = oSocket.Receive(buffer, offset, 1, SocketFlags.None);
                    if (bytesReceived <= 0)
                    {
                        continue;
                    }

                    char lastChar = BitConverter.ToChar(buffer, offset);
                    if (lastChar == '\n')
                    {
                        messageString = Encoding.UTF8.GetString(buffer, 0, offset + 1);
                    }

                    offset++;
                }

                Dictionary<string, string> oMessageDictionary = this.ParseResponseData(messageString);
                if (oMessageDictionary.ContainsKey("Event")) // Ami server event message
                {
                    AmiEventEntity oAmiEvent = new AmiEventEntity(oMessageDictionary);
                    onAmiRaiseEvent(this, oAmiEvent);
                }
                else if (oMessageDictionary.ContainsKey("Response")) // action response message
                {
                    this.m_LastActionResponseEntity = new AmiActionResponseEntity(oMessageDictionary);
                    this.m_ActionAutoReset.Set();
                }
                else // ignore others message now, for example, welcome message 
                {

                }
            }
        }

        private Dictionary<string, string> ParseResponseData(string p_ResponseString)
        {
            Dictionary<string, string> oResult = new Dictionary<string, string>();
            string[] oMessageLineArray = p_ResponseString.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < oMessageLineArray.Length; i++)
            {
                string line = oMessageLineArray[i].Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    int seperatorIndex = line.IndexOf(':');
                    if (seperatorIndex > 0)
                    {
                        string key = line.Substring(0, seperatorIndex).Trim();
                        string value = string.Empty;
                        if (line.Length > seperatorIndex + 1)
                        {
                            value = line.Substring(seperatorIndex + 1).Trim();
                        }

                        oResult.Add(key, value);
                    }
                }
            }
            return oResult;
        }

        public string AmiServerHost
        {
            get { return this.m_AmiServerHost; }
            set { this.m_AmiServerHost = value; }
        }

        public int AmiServerPort
        {
            get { return this.m_AmiServerPort; }
            set { this.m_AmiServerPort = value; }
        }

        public string AmiUsername
        {
            get { return this.m_AmiUsername; }
            set { this.m_AmiUsername = value; }
        }

        public string AmiPassword
        {
            get { return this.m_AmiPassword; }
            set { this.m_AmiPassword = value; }
        }
    }
}
