using System.Xml;
using System.Security.Cryptography;
using System.Text;

namespace Connect.AssemblyAnalyzer.Models
{
    public class CodeBlock
    {
        public string FilePath { get; set; } = "";
        public int StartLine { get; set; } = -1;
        public int StartColumn { get; set; } = 0;
        public int EndLine { get; set; } = -1;
        public int EndColumn { get; set; } = 0;
        public string Body { get; set; } = "";
        public Mono.Cecil.MethodDefinition CecilMethod { get; set; } = null;

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
            var node = Common.AddElement(ref cbNode, "location", FilePath);
            Common.AddAttribute(ref node, "sl", StartLine.ToString());
            Common.AddAttribute(ref node, "sc", StartColumn.ToString());
            Common.AddAttribute(ref node, "el", EndLine.ToString());
            Common.AddAttribute(ref node, "ec", EndColumn.ToString());
            node = Common.AddElement(ref cbNode, "body", Body, true);
            Common.AddAttribute(ref node, "hash", Hash());
        }

    }
}
