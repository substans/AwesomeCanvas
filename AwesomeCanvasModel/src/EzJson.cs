using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace AwesomeCanvas
{
    public class EzJson
    {
        List<Dictionary<string, object>> l = new List<Dictionary<string, object>>();
        JTokenWriter writer;
        public EzJson() {
            writer = new JTokenWriter();
            //writer.WriteStartArray();

        }
        public void BeginFunction(string pName)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("func");
            writer.WriteValue(pName);
        }
        public void AddField(string pKey, object pData) {
            writer.WritePropertyName(pKey);
            writer.WriteValue(pData.ToString());
        }
        public void AddObject(string pKey, object pData) {
            writer.WritePropertyName(pKey);
            var r = new JTokenReader( Newtonsoft.Json.Linq.JToken.FromObject(pData));
            writer.WriteToken(r);
        }
        public Dictionary<string, object> current { get { return l.Last(); } }
        public JToken Finish() {
            //writer.WriteEndArray();
            return writer.Token;
        }
    }
}
