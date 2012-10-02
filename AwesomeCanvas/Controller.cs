using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace AwesomeCanvas
{
    public class Controller
    {
        public delegate void ToolrunnerHandler( ToolRunner p);

        public const string LOCAL_USER = "local_user";
        Dictionary<string, ToolRunner> _users = new Dictionary<string, ToolRunner>();
        CanvasWindow m_canvasWindow;
        Picture m_picture;
        AsyncTcpClient client;
        string my_room;
        string my_name;
        public ToolrunnerHandler NewUserConnected;
        public Controller(Picture pPicture, CanvasWindow pWindow) {
            m_picture = pPicture;
            m_canvasWindow = pWindow;
            client = null;
        }
        public void Connect( Action pOnConnectionComplete ) {
            client = new AsyncTcpClient("217.147.82.190", 9150, pOnConnectionComplete);
            client.JsonMessageDispatcher = OnJsonArrived;
            client.LogDispatcher += (o) => { Console.WriteLine(o); };
            client.Connect();
            //client.ConnectTo("192.168.0.16", 9150, pOnConnectionComplete);
        }
        public void CreateLocalUser() {
            AddUser(LOCAL_USER);
        }

        public void GuiInput( JToken pData ) {
            if (client != null && client.connected != false) {
                client.Write(pData.ToString());
            }
            _users[LOCAL_USER].ParseJSON(pData);
        }
        public void OnJsonArrived(JToken pJson) {
            if (m_canvasWindow.InvokeRequired) {
                m_canvasWindow.Invoke(new AsyncTcpClient.JsonHandler(OnJsonArrived), pJson);
            }
            else {
                string func = pJson.Value<string>("func");

                switch (func) {
                    case "credentials":
                    my_room = pJson.Value<string>("room");
                    my_name = pJson.Value<string>("user");
                    break;
                    case "user_quit":
                    RemoveUser(pJson.Value<string>("user"));
                    break;
                    case "user_join":
                    AddUser(pJson.Value<string>("user"));
                    break;
                    default:
                    Execute(pJson.Value<string>("user"), pJson);
                    break;
                }
            }
        }

        private void RemoveUser(string p) {
            _users.Remove(p);
        }

        private void AddUser(string p) {
            _users.Add(p, new ToolRunner(p, m_picture));
            if (NewUserConnected != null)
                NewUserConnected(_users[p]);
        }
        private void Execute(string p, JToken pToken ) {
            ToolRunner t = _users[p];
            t.ParseJSON(pToken);
        }

        internal void EmptyDrawing() {

            
            
        }
    }
}
