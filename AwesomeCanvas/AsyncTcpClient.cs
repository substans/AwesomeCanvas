using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
namespace AwesomeCanvas
{
    public class AsyncTcpClient 
    {
        public delegate void JsonHandler(JToken pData);
        public delegate void LogHandler(string pMessage);
        public event LogHandler MessageLogger;
        public JsonHandler JsonMessageDispatcher;
        
        private Socket client;
        const int PACKETSIZE = 32768;
        private byte[] data = new byte[PACKETSIZE];
        public bool connected{get; private set;}
        public AsyncTcpClient() {
            connected = false;
        }

        public void ConnectTo(string pIP, short pPort) {
            connected = false;
            Log("Connecting...");
            Socket newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse(pIP), pPort);
            try {
                newsock.BeginConnect(iep, new AsyncCallback(Connected), newsock);
            }
            catch (Exception e) {
                Log(e.Message);
            }
        }

        public void SendString(string pData) {
            //Log("<#" + pData);
            byte[] message = Encoding.ASCII.GetBytes(pData);
            client.BeginSend(message, 0, message.Length, SocketFlags.None,
                         new AsyncCallback(SendData), client);
        }

        public void Dissconnect() {
            client.Close();
            Log("Disconnected");
        }

        void Connected(IAsyncResult iar) {
            client = (Socket)iar.AsyncState;
            try {
                client.EndConnect(iar);
                Log("Connected to: " + client.RemoteEndPoint.ToString());
                connected = true;
                client.BeginReceive(data, 0, PACKETSIZE, SocketFlags.None,
                              new AsyncCallback(ReceiveData), client);
            }
            catch (SocketException e) {
                Log("Error connecting: " + e.Message);
                connected = false;
            }
        }
        int jsonDepth = 0;
        StringBuilder jsonBuffer = new StringBuilder();
        void ReceiveData(IAsyncResult iar) {
            Socket remote = (Socket)iar.AsyncState;
            int recv = remote.EndReceive(iar);
            string stringData = Encoding.ASCII.GetString(data, 0, recv);
            //MessageLogger("#>" + stringData);
            foreach (char c in stringData) {
                switch (c) { 
                    case '[':
                    case '{':
                        jsonBuffer.Append(c);
                        jsonDepth++;
                    break;
                    case ']':
                    case '}':
                        jsonBuffer.Append(c);
                        jsonDepth--;
                    break;
                    default:
                    if (jsonDepth > 0)
                        jsonBuffer.Append(c);
                    break;

                }

                
                if (jsonDepth == 0 && jsonBuffer.Length > 0) {
                    try {
                        JToken t = JToken.Parse(jsonBuffer.ToString());
                        jsonBuffer.Clear();
                        if (JsonMessageDispatcher != null)
                            JsonMessageDispatcher(t);
                    }
                    catch (Exception e) {
                        string buffer =jsonBuffer.ToString();
                        Log("buff:" + buffer);
                        Log(e.Message);
                    }

                }
            }
        }

        void SendData(IAsyncResult iar) {
            Socket remote = (Socket)iar.AsyncState;
            int sent = remote.EndSend(iar);
            remote.BeginReceive(data, 0, PACKETSIZE, SocketFlags.None,
                          new AsyncCallback(ReceiveData), remote);
        }
        void Log(string pMessage) {
            if (MessageLogger != null)
                MessageLogger(pMessage);
        }
    }
}