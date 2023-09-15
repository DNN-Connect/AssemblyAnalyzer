using System.Xml;

namespace Connect.AssemblyAnalyzer.Models
{
  public class CecilProperty : BaseCodeConstruct
  {
    public BaseCodeConstruct Getter { get; set; }
    public BaseCodeConstruct Setter { get; set; }

    public virtual new void WriteToDoc(ref XmlNode parent)
    {
      Common.AddAttribute(ref parent, "name", Name);
      Common.AddElement(ref parent, "fullName", FullName, true);
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
      var getter = Common.AddElement(ref parent, "get");
      if (this.Getter != null)
      {
        this.Getter.WriteToDoc(ref getter);
      }
      var setter = Common.AddElement(ref parent, "set");
      if (this.Setter != null)
      {
        this.Setter.WriteToDoc(ref setter);
      }
    }
  }
}
