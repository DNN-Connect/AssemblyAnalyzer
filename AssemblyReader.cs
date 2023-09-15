using System.Linq;
using Mono.Cecil;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Connect.AssemblyAnalyzer.Models;
using ICSharpCode.Decompiler.CSharp;
using Newtonsoft.Json;

namespace Connect.AssemblyAnalyzer
{
  public class AssemblyReader
  {

    public AssemblyDefinition Assembly { get; set; } = null;
    public CSharpDecompiler Decompiler { get; set; } = null;
    public Dictionary<string, CsFile> CodeFiles { get; set; } = new Dictionary<string, CsFile>();
    public Dictionary<string, CecilNamespace> Namespaces { get; set; } = new Dictionary<string, CecilNamespace>();
    public string BaseCodePath { get; set; } = "";
    public int LineCount { get; set; } = 0;
    public int CommentLineCount { get; set; } = 0;
    public int EmptyLineCount { get; set; } = 0;
    public int BasePathIndex { get; set; } = -1;
    private List<string> TopCodeFolders { get; set; } = new List<string>();

    public AssemblyReader(string assemblyPath, string codePath)
    {
      BaseCodePath = codePath.EnsureEndsWith(@"\");
      var codeFile = $"{BaseCodePath}allcode.json";
      if (File.Exists(codeFile))
      {
        using (var sr = new StreamReader(codeFile))
        {
          this.CodeFiles = JsonConvert.DeserializeObject<Dictionary<string, CsFile>>(sr.ReadToEnd());
        }
      }
      else
      {
        LoadCsFiles(BaseCodePath);
        using (var sw = new StreamWriter(codeFile))
        {
          sw.WriteLine(JsonConvert.SerializeObject(this.CodeFiles, new JsonSerializerSettings()
          {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
          }));
        }
      }

      // Get the top level folders so we can properly map the build code paths to the source
      foreach (var d in new DirectoryInfo(BaseCodePath).GetDirectories())
      {
        TopCodeFolders.Add(d.Name);
      }

      DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
      resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

      ReaderParameters rparams = new ReaderParameters();
      rparams.ReadSymbols = true;
      rparams.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
      rparams.AssemblyResolver = resolver;
      Assembly = AssemblyDefinition.ReadAssembly(assemblyPath, rparams);
      Decompiler = new CSharpDecompiler(assemblyPath, Common.DecompOptions());

      foreach (TypeDefinition t in Assembly.MainModule.Types)
      {
        if (!Namespaces.ContainsKey(t.Namespace) && t.Namespace != "")
        {
          CecilNamespace cns = new CecilNamespace(this, t.Namespace);
          if (cns.Classes.Count > 0)
          {
            Namespaces.Add(t.Namespace, cns);
          }
        }
      }
    }


    public void WriteToDoc(ref XmlNode doc)
    {
      var assemblyNode = Common.AddElement(ref doc, "assembly");
      var v = Common.AddElement(ref assemblyNode, "version", Assembly.Name.Version.ToString());
      Common.AddAttribute(ref v, "normalized", Assembly.Name.Version.ToVersionString());
      Common.AddElement(ref assemblyNode, "fullName", Assembly.FullName);
      Common.AddElement(ref assemblyNode, "codeLines", LineCount.ToString());
      Common.AddElement(ref assemblyNode, "commentLines", CommentLineCount.ToString());
      Common.AddElement(ref assemblyNode, "emptyLines", EmptyLineCount.ToString());
      var dependencies = Common.AddElement(ref doc, "dependencies");
      foreach (var dep in Assembly.MainModule.AssemblyReferences)
      {
        dep.WriteToDoc(ref dependencies);
      }
      foreach (string ns in Namespaces.Keys.OrderBy(k => k))
      {
        Namespaces[ns].WriteToDoc(ref doc);
      }

    }

    public CsFile GetFile(string filePath)
    {
      if (!CodeFiles.ContainsKey(filePath.ToLowerInvariant()))
      {
        var newFile = new CsFile(filePath);
        CodeFiles.Add(filePath.ToLowerInvariant(), newFile);
        LineCount += newFile.FileLineCount;
        CommentLineCount += newFile.FileCommentLineCount;
        EmptyLineCount += newFile.FileEmptyLineCount;
      }
      return CodeFiles[filePath.ToLowerInvariant()];
    }

    public CodeBlock GetMethod(MethodDefinition method)
    {

      CodeBlock res = new CodeBlock();
      res.CecilMethod = method;

      res.StartLine = int.MaxValue;
      res.EndLine = 0;
      if (method.HasBody)
      {
        var mdbi = method.DebugInformation;
        foreach (Mono.Cecil.Cil.Instruction i in method.Body.Instructions)
        {
          var sp = mdbi.GetSequencePoint(i);
          if (sp != null)
          {
            if (sp.StartLine < res.StartLine)
            {
              res.StartLine = sp.StartLine;
              res.StartColumn = sp.StartColumn;
              if (!string.IsNullOrEmpty(sp.Document.Url))
              {
                if (this.BasePathIndex == -1)
                {
                  var hit = 0;
                  foreach (var seg in sp.Document.Url.Replace("/", "\\").Split('\\'))
                  {
                    if (TopCodeFolders.Contains(seg))
                    {
                      this.BasePathIndex = hit;
                      break;
                    }
                    else
                    {
                      hit = hit + seg.Length + 1;
                    }
                  }
                }
                res.Url = sp.Document.Url.Replace("/", "\\").Substring(this.BasePathIndex);
                res.FilePath = BaseCodePath + res.Url;
              }
            }
            if (sp.EndLine > res.EndLine)
            {
              if (res.FilePath != "" && GetFile(res.FilePath).TotalLines >= sp.EndLine)
              {
                res.EndLine = sp.EndLine;
                res.EndColumn = sp.EndColumn;
              }
            }
          }
        }
      }
      else
      {
        return res;
      }

      if (res.FilePath == null || res.FilePath == "")
        return res;

      if (!CodeFiles.ContainsKey(res.FilePath.ToLowerInvariant()))
      {
        CodeFiles.Add(res.FilePath.ToLowerInvariant(), new CsFile(BaseCodePath + res.FilePath));
      }
      CsFile csFile = CodeFiles[res.FilePath.ToLowerInvariant()];

      res.Body = GetCode(csFile, res.StartLine, res.StartColumn, res.EndLine, res.EndColumn);

      res.References = GetReferences(method);

      return res;

    }

    public List<Reference> GetReferences(MethodDefinition method)
    {
      var res = new List<Reference>();
      if (method.HasBody)
      {
        foreach (var call in method.Body.Instructions.Where(il => il.OpCode == Mono.Cecil.Cil.OpCodes.Call))
        {
          var m = call.Operand as MethodReference;
          res.Add(new Reference()
          {
            Offset = call.Offset,
            FullName = m.FullName
          });
        }
      }
      return res;
    }

    public string GetCode(CsFile csFile, int startLine, int startColumn, int endLine, int endColumn)
    {

      if (startLine > csFile.Contents.Last().Key)
        return "";
      string body = "";
      int line = startLine;
      while (true)
      {
        if (startLine == endLine)
        {
          body = csFile.Contents[line].Substring(startColumn - 1, endColumn - startColumn - 1);
          break;
        }
        else if (line == startLine)
        {
          body = csFile.Contents[line].Substring(startColumn - 1) + System.Environment.NewLine;
        }
        else if (line == endLine)
        {
          body += csFile.Contents[line].Substring(0, endColumn - 1);
          break;
        }
        else
        {
          body += csFile.Contents[line] + System.Environment.NewLine;
        }
        line += 1;
      }
      return body;
    }

    public List<CodeBlock> FindClassCodeBlocks(string ns, string className)
    {
      className = className.StripGenerics();
      List<CodeBlock> res = new List<CodeBlock>();
      foreach (CsFile cs in CodeFiles.Values)
      {
        Chunk c = cs.FindClass(ns, className);
        if (c != null)
        {
          CodeBlock cb = new CodeBlock();
          cb.Body = c.CodeBlock;
          cb.StartLine = c.LineList.First().Key;
          cb.StartColumn = 1;
          cb.EndLine = c.LineList.Last().Key;
          cb.EndColumn = c.LineList[cb.EndLine].Length;
          cb.Url = cs.Filename;
          cb.FilePath = cs.Filename;
          res.Add(cb);
        }
      }
      return res;
    }

    private void LoadCsFiles(string path)
    {
      foreach (string csf in Directory.GetFiles(path, "*.cs"))
      {
        CodeFiles.Add(csf.ToLowerInvariant(), new CsFile(csf));
      }
      foreach (string d in Directory.GetDirectories(path))
      {
        if (!d.EndsWith("\\obj"))
        {
          LoadCsFiles(d);
        }
      }
    }
  }
}
