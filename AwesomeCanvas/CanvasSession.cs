using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using AwesomeCanvas;
using System.IO;
using Newtonsoft.Json.Linq;
using WintabDN;
namespace AwesomeCanvas
{
    /// <summary>
    ///  this class collects input from the gui
    ///  it also handles the controllers associated with the canvas
    /// </summary>
    public class CanvasSession
    {
        Controller m_controller;
        CanvasWindow m_canvasWindow;
        MainForm m_mainForm;
        LayerControlForm m_layerControl;
        
        public CanvasWindow canvasWindow { get { return m_canvasWindow; } }
        //-------------------------------------------------------------------------
        // Constructor
        //-------------------------------------------------------------------------
        public CanvasSession(MainForm pMainForm, CanvasWindow pCanvasWindow, LayerControlForm pLayerControlForm)
        {
            m_controller = new Controller(pCanvasWindow.GetPicture(), pCanvasWindow);
            m_controller.NewUserConnected = OnNewUserConnected;
            m_controller.Connect();
            m_mainForm = pMainForm;
            m_canvasWindow = pCanvasWindow;
            m_canvasWindow.m_session = this;
            //m_layerControl = pLayerControlForm;
            //m_layerControl.SetCanvasSession(this);
            //setup first layer       
            m_controller.CreateLocalUser();
            selectedLayerID = Gui_CreateLayer();
            //Gui_ClearSelectedLayer();
        }

        void OnNewUserConnected(ToolRunner pNewToolrunner) {
            if (m_canvasWindow.InvokeRequired) {
                m_canvasWindow.Invoke(new Controller.ToolrunnerHandler(OnNewUserConnected), pNewToolrunner);
            }
            else {
                //add listeners for all functions that should redraw the main canvas
                pNewToolrunner.AddFunctionListener((pToolRunner, pFuncName, pToken) => { m_canvasWindow.Redraw(pToolRunner); }, "tool_down", "tool_up", "tool_move", "undo", "clear", "reorder_layers", "remove_layer");

                //add listeners for all functions that should rebuild the layer list
                //pNewToolrunner.AddFunctionListener((pToolRunner, pFuncName, pToken) => { m_layerControl.RebuildLayerControls(); }, "reorder_layers", "rename_layer", "remove_layer", "create_layer");
                //ToolRunner pTarget, string pFunctionName, JToken inputMessage
                //add listeners for all functions that should update a layer thumbnail
                //pNewToolrunner.AddFunctionListener((pToolRunner, pFuncName, pToken) => { m_layerControl.UpdateThumbnail(pToken.Value<string>("layer")); }, "tool_up", "undo", "clear");

                //add listeners for updating the status bar (:
                pNewToolrunner.AddFunctionListener((pA, pB, pC) => { m_mainForm.SetStatus("last action: " + pB); }, "tool_down", "tool_up", "tool_move", "undo", "clear", "reorder_layers", "remove_layer", "create_layer");
            }
        }

        internal void GuiInput_PointerUp(object sender, int x, int y, float pressure = 1.0f)
        {
            EzJson j = new EzJson();
            j.BeginFunction("tool_up");
            j.AddField("x", (int)(x / m_canvasWindow.magnification));
            j.AddField("y", (int)(y / m_canvasWindow.magnification));
            j.AddField("pressure", pressure);
            j.AddField("layer", selectedLayerID);
            m_controller.GuiInput(j.Finish());
        }

        internal void GuiInput_PointerDown(object sender, int x, int y, float pressure = 1.0f)
        {
            string toolName = m_mainForm.GetToolName();
            EzJson j = new EzJson();
            j.BeginFunction("tool_down");
            j.AddField("pressure", (pressure).ToString());
            j.AddField("x", (int)(x / m_canvasWindow.magnification));
            j.AddField("y", (int)(y / m_canvasWindow.magnification));
            j.AddField("layer", selectedLayerID);
            j.AddField("tool", toolName);
            switch (toolName) {
                case "brush":
                    j.AddObject("options", m_mainForm.GetBrushOptions());
                break;
                case "pen":
                    j.AddObject("options", m_mainForm.GetPenOptions());
                break;
                default:
                    j.AddField("options", "");
                break;
            }
            m_controller.GuiInput(j.Finish());
        }

        internal void GuiInput_PointerMove(object sender, int x, int y, float pressure = 1.0f)
        {
            EzJson j = new EzJson();
            j.BeginFunction("tool_move");
            j.AddField("pressure", (pressure).ToString());
            j.AddField("x", (int)(x / m_canvasWindow.magnification));
            j.AddField("y", (int)(y / m_canvasWindow.magnification));
            m_controller.GuiInput( j.Finish());
        }

        //internal void GuiInput_TabletMove(object sender, WintabPacket pkt)
        //{
        //    EzJson j = new EzJson();
        //    j.BeginFunction("tool_move");
        //    j.AddData("pressure", pkt.pkNormalPressure.pkAbsoluteNormalPressure.ToString());
        //    j.AddData("x", (int)(pkt.pkX / m_canvasWindow.magnification));
        //    j.AddData("y", (int)(pkt.pkY / m_canvasWindow.magnification));
        //    m_toolRunner.ParseJSON(j.Finish());
        //}

        internal void Gui_Undo() {
            EzJson j = new EzJson();
            j.BeginFunction("undo");
            j.AddField("layer", selectedLayerID);
            m_controller.GuiInput(j.Finish());
            Console.WriteLine("undo!");
        }

        internal void Gui_ClearSelectedLayer() {
            EzJson j = new EzJson();
            j.BeginFunction("clear");
            j.AddField("layer", selectedLayerID);
            m_controller.GuiInput(j.Finish());

            Console.WriteLine("clear!");
        }
        internal void Gui_RenameLayer(string pLayerID, string pNewName) {
            EzJson j = new EzJson();
            j.BeginFunction("rename_layer");
            j.AddField("layer", pLayerID);
            j.AddField("name", pNewName);
            m_controller.GuiInput(j.Finish());
        }

        internal string Gui_CreateLayer() {
            string id = Guid.NewGuid().ToString(); //create a globally unique id
            EzJson j = new EzJson();
            j.BeginFunction("create_layer");
            j.AddField("layer", id);
            m_controller.GuiInput(j.Finish());
            return id;
        }
        internal void Gui_RemoveLayer( string pLayerID) {
            EzJson j = new EzJson();
            j.BeginFunction("remove_layer");
            j.AddField("layer", pLayerID);
            m_controller.GuiInput(j.Finish());
        }
        internal void Gui_SetLayerOrder(string[] pOrderedIDs) {
            EzJson j = new EzJson();
            j.BeginFunction("reorder_layers");
            j.AddField("order", pOrderedIDs);
            m_controller.GuiInput(j.Finish());
        }

        internal void SaveCanvasToFile(string pFileName) 
        {
            using (StreamWriter newTask = new StreamWriter(pFileName, false)) {
                
            }
            Console.WriteLine("trying to save file: " + pFileName);
        
        }

        internal Picture GetPicture() {
            return m_canvasWindow.GetPicture();
        }



        public string selectedLayerID { get; set; }
    }
}
