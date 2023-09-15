using System.Xml;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace Connect.AssemblyAnalyzer.Models
{
    public class CodeBlock
    {
        public string FilePath { get; set; } = "";
        public string Url { get; set; } = "";
        public int StartLine { get; set; } = -1;
        public int StartColumn { get; set; } = 0;
        public int EndLine { get; set; } = -1;
        public int EndColumn { get; set; } = 0;
        public string Body { get; set; } = "";
        public Mono.Cecil.MethodDefinition CecilMethod { get; set; } = null;
        public List<Reference> References { get; set; } = new List<Reference>();

        public string Hash()
        {
            using (MD5 md = MD5.Create())
            {
                return System.BitConverter.ToString(md.ComputeHash(Encoding.UTF8.GetBytes(Body))).Replace("-", "").ToLower();
            }
        }

        public virtual void WriteToDoc(ref XmlNode parent)
        {
            XmlNode cbNode = Common.AddElement(ref parent, "codeblock");
            var node = Common.AddElement(ref cbNode, "location", Url);
            Common.AddAttribute(ref node, "sl", StartLine.ToString());
            Common.AddAttribute(ref node, "sc", StartColumn.ToString());
            Common.AddAttribute(ref node, "el", EndLine.ToString());
            Common.AddAttribute(ref node, "ec", EndColumn.ToString());
            node = Common.AddElement(ref cbNode, "body", Body, true);
            Common.AddAttribute(ref node, "hash", Hash());
            if (References.Count > 0)
            {
                var refs = Common.AddElement(ref cbNode, "refs");
                foreach (var refr in References)
                {
                    var n = Common.AddElement(ref refs, "ref", refr.FullName, true);
                    Common.AddAttribute(ref n, "os", refr.Offset.ToString());
                }
            }
        }
    }
}
