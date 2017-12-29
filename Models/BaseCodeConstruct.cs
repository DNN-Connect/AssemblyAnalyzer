using System.Xml;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Connect.AssemblyAnalyzer.Models
{
    public class BaseCodeConstruct
    {
        public string Declaration { get; set; } = "";
        public List<CodeBlock> CodeBlocks { get; set; } = new List<CodeBlock>();
        public bool IsDeprecated { get; set; } = false;
        public string DeprecationMessage { get; set; } = "";
        public Dictionary<int, string> Decompiled { get; set; } = new Dictionary<int, string>();
        public string Name { get; set; } = "";

        public string GetDocumentation()
        {
            string res = "";
            int i = 1;
            while (i <= Decompiled.Count)
            {
                if (Decompiled[i].Trim() == "" | Decompiled[i].Trim().StartsWith("using ") | Decompiled[i].Trim().StartsWith("namespace ") | Decompiled[i].Trim().StartsWith("{"))
                {
                }
                else if (Decompiled[i].Trim().StartsWith("///"))
                {
                    res += Decompiled[i].Trim().Substring(3) + System.Environment.NewLine;
                }
                else
                {
                    break;
                }
                i += 1;
            }
            return res.Trim(System.Environment.NewLine.ToCharArray());
        }

        public string GetDeclaration()
        {
            string res = "";
            int i = 1;
            while (i <= Decompiled.Count)
            {
                if (!(Decompiled[i].Trim() == "" | Decompiled[i].Trim().StartsWith("//") | Decompiled[i].Trim().StartsWith("[") | Decompiled[i].Trim().StartsWith("using ") | Decompiled[i].Trim().StartsWith("namespace ") | Decompiled[i].Trim().StartsWith("{")))
                {
                    res = Decompiled[i].Trim();
                    break;
                }
                i += 1;
            }
            return res.TrimEnd('{').Trim();
        }


        public void ParseTypeNameFromDeclaration(string defaultName)
        {
            Name = Declaration.Trim();
            if (Name.Contains(":"))
                Name = Name.Substring(0, Name.IndexOf(":")).Trim();
            Match m = Regex.Match(Name, "[^\\s]+$");
            if (m.Success)
            {
                Name = m.Value;
            }
            else
            {
                Name = defaultName;
            }
        }

        public virtual void WriteToDoc(ref XmlNode parent)
        {
            Common.AddAttribute(ref parent,"name", Name);
            if (IsDeprecated)
            {
                Common.AddElement(ref parent, "deprecation", DeprecationMessage);
            }
            Common.AddElement(ref parent, "declaration", Declaration, true);
            var doc = Common.AddElement(ref parent, "documentation");
            var docValue = GetDocumentation().Trim();
            try
            {
                doc.InnerXml = docValue;
            }
            catch (System.Exception)
            {
                XmlCDataSection cData = doc.OwnerDocument.CreateCDataSection(docValue.Replace("]]>", ""));
                doc.AppendChild(cData);
            }
            foreach (CodeBlock cb in CodeBlocks)
            {
                cb.WriteToDoc(ref parent);
            }
        }

    }
}
