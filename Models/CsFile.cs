using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Connect.AssemblyAnalyzer.Models
{
  public class CsFile
  {
    private const string _commentDblSlash = "//";
    private const string _commentSingleQuote = "'";
    private const string _commentSlashStar = "/*";
    private const string _commentStarSlash = "*/";

    public Chunk MainChunk { get; set; } = null;
    public Dictionary<int, string> Contents { get; set; } = new Dictionary<int, string>();
    public int FileLineCount { get; set; } = 0;
    public int FileCommentLineCount { get; set; } = 0;
    public int FileEmptyLineCount { get; set; } = 0;
    public int TotalLines { get; set; } = 0;

    private string _fileName = "";
    public string Filename
    {
      get { return _fileName; }
    }

    public CsFile()
    {
    }
    public CsFile(string path)
    {
      _fileName = path;
      bool inComment = false;
      using (StreamReader sr = File.OpenText(path))
      {
        while (sr.Peek() != -1)
        {
          string line = sr.ReadLine();
          if (line != null)
          {
            TotalLines++;
            Contents.Add(TotalLines, line);

            line = line.Trim();
            if (line == "")
            {
              FileEmptyLineCount++;
            }
            else if (line.StartsWith(_commentSlashStar))
            {
              FileCommentLineCount++;
              // we're only in comment block if it was not closed
              // on same line
              if (line.IndexOf(_commentStarSlash) == -1)
              {
                inComment = true;
              }
            }
            else if (inComment)
            {
              FileCommentLineCount++;
              // check if comment block is closed in line
              if (line.IndexOf(_commentStarSlash) != -1)
              {
                inComment = false;
              }
            }
            else if (line.StartsWith(_commentDblSlash))
            {
              FileCommentLineCount++;
            }
            else if (line.StartsWith(_commentSingleQuote))
            {
              FileCommentLineCount++;
            }
            FileLineCount++;
          }
        }
        sr.Close();
      }
      MainChunk = new Chunk(null, Contents, "", "", "");
    }

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
