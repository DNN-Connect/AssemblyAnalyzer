using System.Xml;

namespace Connect.AssemblyAnalyzer.Models
{
  public class CecilEvent : BaseCodeConstruct
  {
    public BaseCodeConstruct Add { get; set; }
    public BaseCodeConstruct Invoke { get; set; }
    public BaseCodeConstruct Remove { get; set; }

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
      var adder = Common.AddElement(ref parent, "add");
      if (this.Add != null)
      {
        this.Add.WriteToDoc(ref adder);
      }
      var invoker = Common.AddElement(ref parent, "invoke");
      if (this.Invoke != null)
      {
        this.Invoke.WriteToDoc(ref invoker);
      }
      var remover = Common.AddElement(ref parent, "remove");
      if (this.Remove != null)
      {
        this.Remove.WriteToDoc(ref remover);
      }
    }
  }
}
