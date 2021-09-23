using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Xml;

namespace Connect.AssemblyAnalyzer
{
    [Cmdlet("Analyze", "Assembly")]
    public class AnalyzeAssembly : PSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, HelpMessage = "Path to the assembly you want to decompile")]
        [Alias("APath")]
        [ValidateNotNullOrEmpty]
        public string Assembly { get; set; }

        [Parameter(Position = 1, Mandatory = true, HelpMessage = "Path to the original code of the assembly")]
        [ValidateNotNullOrEmpty]
        public string CodePath { get; set; }

        [Parameter(Position = 2, Mandatory = true, HelpMessage = "Output path where to write the XML file")]
        [ValidateNotNullOrEmpty]
        public string OutPath { get; set; }

        [Parameter(Position = 3, Mandatory = false, HelpMessage = "Last foldername before code during build")]
        public string ProjectCodePathIdentifier { get; set; } = "";

        protected override void ProcessRecord()
        {

            if (!System.IO.File.Exists(Assembly))
            {
                WriteWarning("Can't find assembly " + Assembly);
                return;
            }

            if (!System.IO.File.Exists(Assembly.Substring(0, Assembly.Length - 4) + ".pdb"))
            {
                WriteWarning("Can't find pdb file for " + Assembly);
                return;
            }

            WriteVerbose("Processing ... ");
            WriteVerbose("Assembly : " + Assembly);
            WriteVerbose("Code     : " + CodePath);
            WriteVerbose("Output   : " + OutPath);

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            XmlNode root = doc.CreateElement("root");
            Common.AddAttribute(ref root, "file", System.IO.Path.GetFileName(Assembly));
            doc.AppendChild(root);

            DateTime startTime = DateTime.Now;

            //try
            //{
            AssemblyReader assem = new AssemblyReader(Assembly, CodePath, ProjectCodePathIdentifier);
            assem.WriteToDoc(ref root);
            TimeSpan timeTaken = DateTime.Now.Subtract(startTime);
            Common.AddAttribute(ref root, "generated", startTime.ToString("u"));
            Common.AddAttribute(ref root, "generationTime", timeTaken.TotalSeconds.ToString());
            string outFile = string.Format("{0}\\{1}_{2}.xml", OutPath, System.IO.Path.GetFileNameWithoutExtension(Assembly), assem.Assembly.Name.Version.ToVersionString());
            doc.Save(outFile);
            //}
            //catch (Exception ex)
            //{
            //    WriteVerbose(ex.Message);
            //    WriteVerbose(ex.StackTrace);
            //}
        }
    }
}
