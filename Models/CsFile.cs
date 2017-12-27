using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Connect.AssemblyAnalyzer.Models
{
    public class CsFile
    {

        public Chunk MainChunk { get; set; } = null;
        public Dictionary<int, string> Contents { get; set; } = new Dictionary<int, string>();
        private string _fileName = "";
        public string Filename
        {
            get { return _fileName; }
        }


        public CsFile(string path)
        {
            _fileName = path;
            int lineNr = 1;
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    Contents.Add(lineNr, sr.ReadLine());
                    lineNr += 1;
                }
            }
            NrLines = lineNr - 1;

            MainChunk = new Chunk(null, Contents, "", "", "");

        }

        public int NrLines { get; set; }

        public Chunk FindClass(string ns, string className)
        {

            className = Regex.Replace(className, "``?\\d+", "");
            return FindChunkByName(MainChunk, ns, className);

        }

        private Chunk FindChunkByName(Chunk chunk, string ns, string name)
        {

            if (chunk.CurrentNamespace == ns && chunk.ThisClassName.ToLower() == name.ToLower())
                return chunk;
            foreach (Chunk sc in chunk.Chunks)
            {
                Chunk res = FindChunkByName(sc, ns, name);
                if (res != null)
                    return res;
            }
            return null;

        }

        public Chunk LookupMethod(int startLine, int endLine)
        {
            return LookupMethodInternal(MainChunk, startLine, endLine);
        }

        private Chunk LookupMethodInternal(Chunk chunk, int startLine, int endLine)
        {
            if (System.Math.Abs(chunk.LineList.First().Key - startLine) < 2 & System.Math.Abs(chunk.LineList.Last().Key - endLine) < 2)
            {
                return chunk;
            }
            else
            {
                foreach (Chunk c in chunk.Chunks)
                {
                    Chunk res = LookupMethodInternal(c, startLine, endLine);
                    if (res != null)
                        return res;
                }
            }
            return null;
        }

    }
}
