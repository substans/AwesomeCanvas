using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
namespace AwesomeCanvas
{
    public class History : IEnumerable<JToken>
    {
        List<UndoLevel> undoLevels = new List<UndoLevel>();
        class UndoLevel
        {
            public List<JToken> commands = new List<JToken>();
        }
        UndoLevel current {
            get { return undoLevels[undoLevels.Count - 1]; }
        }
        public void StoreUndoData(JToken pData) {
            if (undoLevels.Count == 0)
                BeginNewUndoLevel();

            current.commands.Add(pData);
        }
        public void BeginNewUndoLevel() {
            undoLevels.Add(new UndoLevel());
            //Console.WriteLine("undo levels" + undoLevels.Count);
            //Console.WriteLine("commands " + this.Count());
        }
        public void PopUndoLevel() {
            if (undoLevels.Count == 0) {
                Console.WriteLine("no more undo levels");
                return;
            }
            undoLevels.RemoveAt(undoLevels.Count - 1);
        }


        public IEnumerator<JToken> GetEnumerator() {
            foreach (UndoLevel l in undoLevels) {
                foreach (JToken d in l.commands) {
                    yield return d;
                }
            }
        }
        public void Clear() {
            undoLevels.Clear();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}
