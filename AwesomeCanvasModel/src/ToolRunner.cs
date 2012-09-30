using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace AwesomeCanvas
{
    public delegate void FunctionEventHandler(ToolRunner pTarget, string pFunctionName, JToken inputMessage);
    public class ToolRunner
    {
        public bool FunctionEventsEnabled {get;set;}
        Dictionary<string, List<FunctionEventHandler>> _functionHandlers = new Dictionary<string,List<FunctionEventHandler>>();
        Picture m_picture;
        string m_username;
        Dictionary<string, Tool> m_tools = new Dictionary<string, Tool>();
        
        Tool m_currentTool = null; //when making strokes we cache these for optimization
        Layer m_currentLayer = null; //when making strokes we cache these for optimization

        public ToolRunner( string pUsername, Picture pPicture) {
            m_tools.Add("brush", new BrushTool(this));
            m_tools.Add("pen", new PenTool(this));
            m_tools.Add("pointer", new PointerTool(this));
            m_username = pUsername;
            m_picture = pPicture;
            FunctionEventsEnabled = true;
        }
        
        // Decypher the JSON command and execute the corrasponding function
        public void ParseJSON(string pJsonArray) {
            if (m_picture == null)
                return;
            JArray jarr = JArray.Parse(pJsonArray);
            

            //Console.WriteLine(pJson);
            Dictionary<string, object>[] input = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>[]>(pJsonArray);
            ExecuteCommands(jarr);

        }

        void ToolDown(JToken inputMessage) {
            float pressure = inputMessage.Value<float>("pressure");
            int x = inputMessage.Value<int>("x");
            int y = inputMessage.Value<int>("y");
            Tool tool = m_tools[inputMessage.Value<string>("tool")]; //swap tool on tool down
            string layerID = inputMessage.Value<string>("layer");
            m_currentLayer = m_picture.GetLayer(layerID);
            m_currentTool = tool;
            m_currentTool.Down(x, y, pressure, m_picture, m_currentLayer, inputMessage["options"]);
            m_currentLayer.History.BeginNewUndoLevel();
            m_currentLayer.History.StoreUndoData(inputMessage);
        }

        void ToolMove(JToken inputMessage) {
            float pressure = inputMessage.Value<float>("pressure");
            int x =inputMessage.Value<int>("x");
            int y = inputMessage.Value<int>("y");
            m_currentTool.Move(x, y, pressure);
            m_currentLayer.History.StoreUndoData(inputMessage);
        }

        void ToolUp(JToken inputMessage) {
            float pressure = inputMessage.Value<float>("pressure");
            int x = inputMessage.Value<int>("x");
            int y = inputMessage.Value<int>("y");
            m_currentTool.Up(x, y, pressure);
            m_currentLayer.History.StoreUndoData(inputMessage);
            m_currentTool = null;
            m_currentLayer = null;        
        }
        void Undo(JToken inputMessage) {
            string layerID = inputMessage.Value<string>("layer");
            Layer l = m_picture.GetLayer(layerID);
            m_picture.Clear(layerID);
            l.History.PopUndoLevel();
            JToken[] h = l.History.ToArray();//important to copy to array since the history will be modified!
            l.History.Clear();
            FunctionEventsEnabled = false; //we disable events so that the gui doesn't update while we re-paint the image
            ExecuteCommands(h);
            FunctionEventsEnabled = true;
        }
        void RenameLayer(JToken inputMessage) {
            string layerID = inputMessage.Value<string>("layer");
            m_picture.GetLayer(layerID).Name = inputMessage.Value<string>("name");
        }
        void RemoveLayer(JToken inputMessage) {
            string layerID = inputMessage.Value<string>("layer");
            m_picture.RemoveLayer(layerID);
        }
        void ReorderLayers(JToken inputMessage) {
            var t = inputMessage["order"] as Newtonsoft.Json.Linq.JToken;
            m_picture.Reorder_layers( t.ToObject<string[]>());
        
        }
        void ClearLayer(JToken inputMessage) {
            string layerID = inputMessage.Value<string>("layer");
            m_picture.Clear(layerID);
            m_picture.GetLayer(layerID).History.StoreUndoData(inputMessage);
        }
        void ExecuteCommands(IEnumerable<JToken> pInput) 
        {
            foreach (JToken inputMessage in pInput) 
            {
                string functionName = inputMessage.Value<string>("func");
                switch (functionName) 
                {
                    case "tool_down": //tool_down comes with all the tool options
                    ToolDown(inputMessage);
                    break;
                    case "tool_move":
                    if (m_currentTool != null) //tool_move can happend without tool beeing down
                        ToolMove(inputMessage);
                    break;
                    case "tool_up":
                    if (m_currentTool != null) //tool_up can happend without tool beeing down
                        ToolUp(inputMessage);
                    break;
                    case "clear":
                    ClearLayer(inputMessage);
                    break;
                    case "undo":
                    Undo(inputMessage);
                    break;
                    case "create_layer":
                    m_picture.AddLayer(inputMessage.Value<string>("layer"));
                    break;
                    case "remove_layer":
                    RemoveLayer(inputMessage);
                    break;
                    case "reorder_layers":
                    ReorderLayers(inputMessage);
                    break;
                    case "rename_layer":
                    RenameLayer(inputMessage);
                    break;
                    default:
                    break;
                }
                if (FunctionEventsEnabled) {
                    //Fire off the events!
                    List<FunctionEventHandler> handlers;
                    if (_functionHandlers.TryGetValue(functionName, out handlers) && handlers != null) {
                        foreach (FunctionEventHandler h in handlers)
                            h(this, functionName, inputMessage);
                    }
                }
                
            }
        }
        public void AddFunctionListener(FunctionEventHandler pHandler, params string[] pFunctionNames) {
            foreach (string s in pFunctionNames) {
                AddFunctionListener(pHandler, s);
            }
        }
        public void AddFunctionListener(FunctionEventHandler pHandler, string pFunctionName) {
            if (!_functionHandlers.ContainsKey(pFunctionName))
                _functionHandlers.Add(pFunctionName, new List<FunctionEventHandler>());
            _functionHandlers[pFunctionName].Add(pHandler);
        }
        public void RemoveFunctionListener(FunctionEventHandler pHandler, params string[] pFunctionNames) {
            foreach (string s in pFunctionNames) {
                RemoveFunctionListener(pHandler, s);
            }
        }
        public void RemoveFunctionListener(FunctionEventHandler pHandler, string pFunctionName) {
            
            _functionHandlers[pFunctionName].Remove(pHandler);
        }
    }
}