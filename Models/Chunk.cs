using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Connect.AssemblyAnalyzer.Models
{
    public class Chunk
    {
        public List<Chunk> Chunks { get; set; } = new List<Chunk>();
        public string CodeBlock { get; set; } = "";
        public string CleanedCodeBlock { get; set; } = "";
        public Dictionary<int, string> LineList { get; set; }
        public string Hash { get; set; } = "";
        public string PrecedingText { get; set; } = "";
        public string CurrentNamespace { get; set; } = "";
        public string ThisClassName { get; set; } = "";
        public string ClassDefinition { get; set; } = "";
        public string ParentClassName { get; set; } = "";
        public bool IsClass { get; set; } = false;
        public bool IsEnum { get; set; } = false;
        public bool IsInterface { get; set; } = false;
        // public List<string> Declarations { get; set; }
        public string Documentation { get; set; } = "";
        public Chunk Parent { get; set; } = null;


        public Chunk(Chunk parent, Dictionary<int, string> lineList, string precedingText, string ns, string parentClass)
        {
            this.Parent = parent;
            this.LineList = lineList;
            this.CodeBlock = string.Join(System.Environment.NewLine, lineList.Values);
            using (MD5 md = MD5.Create())
            {
                this.Hash = System.BitConverter.ToString(md.ComputeHash(Encoding.UTF8.GetBytes(this.CodeBlock))).Replace("-", "").ToLower();
            }
            if (precedingText.Contains(System.Environment.NewLine))
                precedingText = precedingText.Substring(precedingText.LastIndexOf(System.Environment.NewLine) + 2);
            if (precedingText.Contains(";"))
                precedingText = precedingText.Substring(precedingText.LastIndexOf(';') + 1);
            if (precedingText.Contains(")]"))
                precedingText = precedingText.Substring(precedingText.LastIndexOf(")]") + 2);
            this.PrecedingText = precedingText.Trim(' ', ';').Trim();
            this.CurrentNamespace = ns;
            Match m = Regex.Match(this.PrecedingText, "(?i)^namespace (.+)(?-i)");
            if (m.Success)
            {
                CurrentNamespace = m.Groups[1].Value;
            }
            m = Regex.Match(this.PrecedingText, "(?i)([^\\]]*) class (\\w+)(.*)(?-i)");
            if (m.Success)
            {
                ClassDefinition = m.Groups[1].Value.Trim() + " class " + m.Groups[2].Value + m.Groups[3].Value;
                ThisClassName = m.Groups[2].Value;
                IsClass = true;
            }
            m = Regex.Match(this.PrecedingText, "(?i)([^\\]]*) enum (\\w+)(.*)(?-i)");
            if (m.Success)
            {
                ClassDefinition = m.Groups[1].Value.Trim() + " enum " + m.Groups[2].Value + m.Groups[3].Value;
                ThisClassName = m.Groups[2].Value;
                IsEnum = true;
            }
            m = Regex.Match(this.PrecedingText, "(?i)([^\\]]*) interface (\\w+)(.*)(?-i)");
            if (m.Success)
            {
                ClassDefinition = m.Groups[1].Value.Trim() + " interface " + m.Groups[2].Value + m.Groups[3].Value;
                ThisClassName = m.Groups[2].Value;
                IsInterface = true;
            }
            this.ParentClassName = parentClass;
            this.CleanedCodeBlock = string.Join(" ", lineList.Values.Where(l => !(l.Trim().StartsWith("//") | l.Trim().StartsWith("using ") | l.Trim().StartsWith("#"))));
            this.CleanedCodeBlock = Regex.Replace(this.CleanedCodeBlock, "\\s+", " ").Trim();
            this.CleanedCodeBlock = this.PrecedingText + " {" + this.CleanedCodeBlock + "}";

            int lineNr = lineList.First().Key;
            int lastLineNr = lineList.Last().Key;
            //int startLine = 0;
            int level = 0;
            Dictionary<int, string> nextChunkLines = null;
            StringBuilder leadIn = new StringBuilder();
            while (true)
            {
                string line = lineList[lineNr].CutoffComments();
                StringBuilder lineToSave = new StringBuilder();
                if (line.Trim().StartsWith("#"))
                    line = "";
                if (line.Trim().StartsWith("using "))
                    line = "";
                foreach (char c in line)
                {
                    switch (c)
                    {
                        case '{':
                            level += 1;
                            if (level == 1)
                            {
                                nextChunkLines = new Dictionary<int, string>();
                            }
                            else
                            {
                                lineToSave.Append(c);
                            }
                            break;
                        case '}':
                            level -= 1;
                            if (level == 0)
                            {
                                nextChunkLines.Add(lineNr, lineToSave.ToString());
                                Chunks.Add(new Chunk(this, nextChunkLines, leadIn.ToString(), CurrentNamespace, ThisClassName));
                                nextChunkLines = null;
                                leadIn = new StringBuilder();
                            }
                            else
                            {
                                lineToSave.Append(c);
                            }
                            break;
                        default:
                            if (level > 0)
                                lineToSave.Append(c);
                            if (nextChunkLines == null)
                                leadIn.Append(c);
                            break;
                    }
                }
                if (nextChunkLines != null)
                {
                    nextChunkLines.Add(lineNr, lineToSave.ToString());
                }
                if (lineNr == lastLineNr)
                {
                    if (nextChunkLines != null)
                    {
                        Chunks.Add(new Chunk(this, nextChunkLines, leadIn.ToString(), CurrentNamespace, ThisClassName));
                    }
                    break;
                }
                lineNr += 1;
                leadIn.Append(" ");
            }

        }

        private string ReplaceArg(Match input)
        {
            string[] arg = input.Groups[1].Value.Trim().Split(' ');
            return arg[arg.Length - 2];
        }

        public int ChunkCount()
        {
            int res = Chunks.Count;
            foreach (Chunk chunk in Chunks)
            {
                res += chunk.ChunkCount();
            }
            return res;
        }

        public string PreTexts()
        {
            string res = "";
            res = System.Environment.NewLine + PrecedingText;
            if (ParentClassName != "")
            {
                res += " //" + ParentClassName;
            }
            foreach (Chunk chunk in Chunks)
            {
                res += chunk.PreTexts();
            }
            return Regex.Replace(res, "(\\r\\n)+", System.Environment.NewLine);
        }

        public void WriteClasses(TextWriter writer)
        {
            if (IsClass)
            {
                writer.WriteLine(string.Format("{0}.{1}", CurrentNamespace, ThisClassName));
                writer.WriteLine(string.Format("{0}", PrecedingText));
                foreach (Chunk c in Chunks)
                {
                    writer.WriteLine(string.Format("{0}", c.CleanedCodeBlock));
                }
                writer.WriteLine();
            }
            foreach (Chunk chunk in Chunks)
            {
                chunk.WriteClasses(writer);
            }
        }

    }
}
