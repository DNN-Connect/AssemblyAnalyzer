using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

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
    public string FullName { get; set; } = "";
    public bool IsPrivate { get; set; } = false;
    public bool IsAbstract { get; set; } = false;
    public bool IsStatic { get; set; } = false;
    public bool IsGetter { get; set; } = false;
    public bool IsSetter { get; set; } = false;

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

    public void ParseMethodNameFromDeclaration()
    {
      // for methods we might have overloads so the name must use the full method incl parameters
      var newName = Declaration.Trim();
      newName = newName.TrimEnd(';');
      Match m = Regex.Match(newName, "[^\\s]+\\(.*\\)");
      if (m.Success)
      {
        Name = m.Value;
      }
    }

    public virtual void WriteToDoc(ref XmlNode parent)
    {
      Common.AddAttribute(ref parent, "name", Name);
      Common.AddAttribute(ref parent, "IsGetter", this.IsGetter.ToString());
      Common.AddAttribute(ref parent, "IsSetter", this.IsSetter.ToString());
      Common.AddAttribute(ref parent, "IsAbstract", this.IsAbstract.ToString());
      Common.AddAttribute(ref parent, "IsPrivate", this.IsPrivate.ToString());
      Common.AddAttribute(ref parent, "IsStatic", this.IsStatic.ToString());
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
      foreach (CodeBlock cb in CodeBlocks)
      {
        if (cb.Body.Trim().Length > 0)
        {
          cb.WriteToDoc(ref parent);
        }
      }
    }

  }
}
