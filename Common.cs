using Connect.AssemblyAnalyzer.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace Connect.AssemblyAnalyzer
{
    public static class Common
    {

        #region Deprecation

        public static bool IsDeprecated(this MethodDefinition method)
        {
            if (method.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in method.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public static string DeprecationMessage(this MethodDefinition method)
        {
            if (method.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in method.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            return (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return "";
        }

        public static BaseCodeConstruct ParseDeprecation(MethodDefinition method, BaseCodeConstruct member)
        {
            if (method.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in method.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        member.IsDeprecated = true;
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            member.DeprecationMessage = (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return member;
        }

        public static BaseCodeConstruct ParseDeprecation(PropertyDefinition prop, BaseCodeConstruct member)
        {
            if (prop.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in prop.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        member.IsDeprecated = true;
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            member.DeprecationMessage = (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return member;
        }

        public static BaseCodeConstruct ParseDeprecation(TypeDefinition t, BaseCodeConstruct member)
        {
            if (t.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in t.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        member.IsDeprecated = true;
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            member.DeprecationMessage = (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return member;
        }

        public static BaseCodeConstruct ParseDeprecation(FieldDefinition field, BaseCodeConstruct member)
        {
            if (field.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in field.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        member.IsDeprecated = true;
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            member.DeprecationMessage = (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return member;
        }

        public static BaseCodeConstruct ParseDeprecation(EventDefinition evt, BaseCodeConstruct member)
        {
            if (evt.HasCustomAttributes)
            {
                foreach (CustomAttribute ca in evt.CustomAttributes)
                {
                    if (ca.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        member.IsDeprecated = true;
                        if (ca.ConstructorArguments.Count > 0)
                        {
                            member.DeprecationMessage = (string)ca.ConstructorArguments[0].Value;
                        }
                    }
                }
            }
            return member;
        }
        #endregion

        #region Decompilation
        public static DecompilerSettings DecompOptions()
        {
            var opts = new DecompilerSettings(); // { FullDecompilation = false };
            opts.UseDebugSymbols = true;
            opts.UsingDeclarations = true;
            opts.CSharpFormattingOptions.AlignElseInIfStatements = true;
            opts.CSharpFormattingOptions.AlignEmbeddedStatements = true;
            opts.CSharpFormattingOptions.ClassBraceStyle = ICSharpCode.Decompiler.CSharp.OutputVisitor.BraceStyle.EndOfLine;
            opts.CSharpFormattingOptions.IndentBlocks = true;
            opts.CSharpFormattingOptions.IndentMethodBody = true;
            return opts;
        }

        public static Dictionary<int, string> ToDictionary(this string input)
        {
            int i = 1;
            Dictionary<int, string> res = new Dictionary<int, string>();
            foreach (string line in input.Split(Environment.NewLine.ToCharArray()))
            {
                var line1 = line.Trim(Environment.NewLine.ToCharArray());
                res.Add(i, line1);
                i += 1;
            }
            return res;
        }
        #endregion

        #region  XML Stuff

        public static string GetChildElementValue(this XmlNode node, string childPath)
        {
            XmlNode child = node.SelectSingleNode(childPath);
            if (child == null)
            {
                return "";
            }
            else
            {
                return child.InnerText.Trim();
            }
        }


        public static XmlNode AddElement(ref XmlNode node, string elementName)
        {
            return AddElement(ref node, elementName, "", false);
        }


        public static XmlNode AddElement(ref XmlNode node, string elementName, string elementValue)
        {
            return AddElement(ref node, elementName, elementValue, false);
        }


        public static XmlNode AddElement(ref XmlNode node, string elementName, string elementValue, bool useCData)
        {
            XmlNode newElement = node.OwnerDocument.CreateElement(elementName);
            if (useCData)
            {
                XmlCDataSection cData = node.OwnerDocument.CreateCDataSection(elementValue.Replace("]]>", ""));
                newElement.AppendChild(cData);
            }
            else
            {
                newElement.InnerText = elementValue;
            }
            node.AppendChild(newElement);
            return newElement;
        }


        public static void AddAttribute(ref XmlNode node, string attributeName, string attributeValue)
        {
            XmlAttribute newAttribute = node.OwnerDocument.CreateAttribute(attributeName);
            newAttribute.InnerText = attributeValue;
            node.Attributes.Append(newAttribute);
        }
        #endregion

        #region String Extensions
        public static string ToVersionString(this Version v)
        {
            return string.Format("{0:00}.{1:00}.{2:00}", v.Major, v.Minor, v.Build);
        }


        public static string CutoffComments(this string line)
        {
            if (line.Contains("//"))
                line = line.Substring(0, line.IndexOf("//"));
            return line;
        }


        public static string StripGenerics(this string input)
        {
            return Regex.Replace(input, "``?(\\d+)", "");
        }

        public static string StripNamespaces(Match input)
        {
            string res = input.Groups[1].Value;
            if (res.Contains("."))
                res = res.Substring(res.LastIndexOf(".") + 1);
            return res;
        }
        #endregion

        #region IO
        public static Dictionary<int, string> ReadFile(string filePath)
        {

            Dictionary<int, string> contents = new Dictionary<int, string>();
            int lineNr = 1;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(filePath))
            {
                while (!sr.EndOfStream)
                {
                    contents.Add(lineNr, sr.ReadLine());
                    lineNr += 1;
                }
            }
            return contents;

        }
        #endregion

        #region Dependencies
        public static void WriteToDoc(this AssemblyNameReference dependency, ref XmlNode doc)
        {
            var depNode = AddElement(ref doc, "dependency");
            AddAttribute(ref depNode, "version", dependency.Version.ToString());
            AddAttribute(ref depNode, "versionnorm", dependency.Version.ToVersionString());
            depNode.InnerText = dependency.FullName;
        }
        #endregion
    }
}
