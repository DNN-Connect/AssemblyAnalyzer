using System.Linq;
using Mono.Cecil;
using System.Xml;
using System.Collections.Generic;

namespace Connect.AssemblyAnalyzer.Models
{
    public class CecilNamespace
    {

        public string NameSpaceString { get; set; } = "";
        public Dictionary<string, CecilClass> Classes { get; set; } = new Dictionary<string, CecilClass>();

        public CecilNamespace(AssemblyReader assemblyReader, string ns)
        {
            NameSpaceString = ns;
            foreach (TypeDefinition t in assemblyReader.Assembly.MainModule.Types.Where(t1 => t1.Namespace == ns & !t1.Name.Contains("__")))
            {
                switch (t.Name)
                {
                    case "<Module>":
                    case "<PrivateImplementationDetails>":
                    case "PrivateImplementationDetails":
                        break;
                    default:
                        Classes.Add(t.Name, new CecilClass(assemblyReader, t));
                        break;
                }
            }

        }

        public void WriteToDoc(ref XmlNode parent)
        {
            XmlNode newNamespace = Common.AddElement(ref parent, "namespace");
            Common.AddAttribute(ref newNamespace, "name", NameSpaceString);
            foreach (string cName in Classes.Keys.OrderBy(c => c))
            {
                Classes[cName].WriteToDoc(ref newNamespace);
            }
        }
    }
}
