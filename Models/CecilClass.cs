using System.Linq;
using Mono.Cecil;
using System.Xml;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using System;
using System.Text.RegularExpressions;

namespace Connect.AssemblyAnalyzer.Models
{
    public class CecilClass : BaseCodeConstruct
    {

        public TypeDefinition ThisClass { get; set; } = null;
        public List<BaseCodeConstruct> Constructors { get; set; } = new List<BaseCodeConstruct>();
        public List<BaseCodeConstruct> Methods { get; set; } = new List<BaseCodeConstruct>();
        public List<BaseCodeConstruct> ExtensionMethods { get; set; } = new List<BaseCodeConstruct>();
        public List<BaseCodeConstruct> Fields { get; set; } = new List<BaseCodeConstruct>();
        public List<BaseCodeConstruct> Properties { get; set; } = new List<BaseCodeConstruct>();
        public List<BaseCodeConstruct> Events { get; set; } = new List<BaseCodeConstruct>();

        public List<CecilClass> SubTypes { get; set; } = new List<CecilClass>();


        public CecilClass(AssemblyReader assemblyReader, TypeDefinition cClass)
        {
            ThisClass = cClass;
            try
            {
                Decompiled = assemblyReader.Decompiler.DecompileTypeAsString(cClass.GetFullTypeName()).ToDictionary();
            }
            catch (System.Exception)
            {
                Decompiled = "".ToDictionary();
            }
            Declaration = GetDeclaration();
            ParseTypeNameFromDeclaration(cClass.Name);

            foreach (MethodDefinition m in cClass.Methods.Where(p1 => !p1.Name.Contains("__") & p1.IsConstructor & !p1.IsGetter & !p1.IsSetter))
            {
                BaseCodeConstruct mm = new BaseCodeConstruct();
                mm.Name = m.Name;
                mm.FullName = m.FullName;
                CodeBlock cb = assemblyReader.GetMethod(m);
                mm.CodeBlocks.Add(cb);
                mm.Decompiled = assemblyReader.Decompiler.DecompileAsString(m).ToDictionary();
                mm.Declaration = mm.GetDeclaration();
                mm = Common.ParseDeprecation(m, mm);
                mm.ParseMethodNameFromDeclaration();
                Constructors.Add(mm);
            }

            foreach (MethodDefinition m in cClass.Methods.Where(p1 => !p1.Name.Contains("__") & !p1.IsConstructor & !p1.IsGetter & !p1.IsSetter))
            {
                BaseCodeConstruct mm = new BaseCodeConstruct();
                mm.Name = m.Name;
                mm.FullName = m.FullName;
                CodeBlock cb = assemblyReader.GetMethod(m);
                mm.CodeBlocks.Add(cb);
                mm.Decompiled = assemblyReader.Decompiler.DecompileAsString(m).ToDictionary();
                mm.Declaration = mm.GetDeclaration();
                mm = Common.ParseDeprecation(m, mm);
                mm.ParseMethodNameFromDeclaration();
                Methods.Add(mm);
            }

            foreach (FieldDefinition f in cClass.Fields.Where(p1 => !p1.Name.Contains("__")))
            {
                BaseCodeConstruct fm = new BaseCodeConstruct();
                fm.Name = f.Name;
                fm.FullName = f.FullName;
                fm.Declaration = f.FullName;
                fm.Decompiled = assemblyReader.Decompiler.DecompileAsString(f).ToDictionary();
                fm.Declaration = fm.GetDeclaration();
                fm = Common.ParseDeprecation(f, fm);
                Fields.Add(fm);
            }

            foreach (PropertyDefinition p in cClass.Properties.Where(p1 => !p1.Name.Contains("__")))
            {
                BaseCodeConstruct pm = new BaseCodeConstruct();
                pm.Name = p.Name;
                pm.FullName = p.FullName;
                if (p.GetMethod != null)
                {
                    CodeBlock cb = assemblyReader.GetMethod(p.GetMethod);
                    if (cb.Body != "")
                    {
                        pm.CodeBlocks.Add(cb);
                    }
                }
                if (p.SetMethod != null)
                {
                    CodeBlock cb = assemblyReader.GetMethod(p.SetMethod);
                    if (cb.Body != "")
                    {
                        pm.CodeBlocks.Add(cb);
                    }
                }
                try
                {
                    pm.Decompiled = assemblyReader.Decompiler.DecompileAsString(p).ToDictionary();
                }
                catch
                {
                    pm.Decompiled = "".ToDictionary();
                }
                pm.Declaration = pm.GetDeclaration();
                pm = Common.ParseDeprecation(p, pm);
                Properties.Add(pm);
            }

            foreach (EventDefinition e in cClass.Events.Where(p1 => !p1.Name.Contains("__")))
            {
                BaseCodeConstruct em = new BaseCodeConstruct();
                em.Name = e.Name;
                em.FullName = e.FullName;
                em.Decompiled = assemblyReader.Decompiler.DecompileAsString(e).ToDictionary();
                em.Declaration = em.GetDeclaration();
                em = Common.ParseDeprecation(e, em);
                Events.Add(em);
            }

            CodeBlocks = assemblyReader.FindClassCodeBlocks(ThisClass.Namespace, ThisClass.Name);

            var a = Common.ParseDeprecation(cClass, this);
            IsDeprecated = a.IsDeprecated;
            DeprecationMessage = a.DeprecationMessage;

            foreach (TypeDefinition t in cClass.NestedTypes.Where(t1 => !t1.Name.Contains("__")))
            {
                SubTypes.Add(new CecilClass(assemblyReader, t));
            }

        }

        public override void WriteToDoc(ref XmlNode parent)
        {
            XmlNode newClass = Common.AddElement(ref parent, "class");
            Common.AddAttribute(ref newClass, "name", Name);
            Common.AddAttribute(ref newClass, "IsAbstract", ThisClass.IsAbstract.ToString());
            Common.AddAttribute(ref newClass, "IsAnsiClass", ThisClass.IsAnsiClass.ToString());
            Common.AddAttribute(ref newClass, "IsArray", ThisClass.IsArray.ToString());
            Common.AddAttribute(ref newClass, "IsAutoClass", ThisClass.IsAutoClass.ToString());
            Common.AddAttribute(ref newClass, "IsAutoLayout", ThisClass.IsAutoLayout.ToString());
            Common.AddAttribute(ref newClass, "IsBeforeFieldInit", ThisClass.IsBeforeFieldInit.ToString());
            Common.AddAttribute(ref newClass, "IsByReference", ThisClass.IsByReference.ToString());
            Common.AddAttribute(ref newClass, "IsClass", ThisClass.IsClass.ToString());
            Common.AddAttribute(ref newClass, "IsDefinition", ThisClass.IsDefinition.ToString());
            Common.AddAttribute(ref newClass, "IsEnum", ThisClass.IsEnum.ToString());
            Common.AddAttribute(ref newClass, "IsExplicitLayout", ThisClass.IsExplicitLayout.ToString());
            Common.AddAttribute(ref newClass, "IsFunctionPointer", ThisClass.IsFunctionPointer.ToString());
            Common.AddAttribute(ref newClass, "IsGenericInstance", ThisClass.IsGenericInstance.ToString());
            Common.AddAttribute(ref newClass, "IsGenericParameter", ThisClass.IsGenericParameter.ToString());
            Common.AddAttribute(ref newClass, "IsImport", ThisClass.IsImport.ToString());
            Common.AddAttribute(ref newClass, "IsInterface", ThisClass.IsInterface.ToString());
            Common.AddAttribute(ref newClass, "IsNested", ThisClass.IsNested.ToString());
            Common.AddAttribute(ref newClass, "IsNestedAssembly", ThisClass.IsNestedAssembly.ToString());
            Common.AddAttribute(ref newClass, "IsNestedPrivate", ThisClass.IsNestedPrivate.ToString());
            Common.AddAttribute(ref newClass, "IsNestedPublic", ThisClass.IsNestedPublic.ToString());
            Common.AddAttribute(ref newClass, "IsNotPublic", ThisClass.IsNotPublic.ToString());
            Common.AddElement(ref newClass, "fullName", ThisClass.FullName);
            if (IsDeprecated)
            {
                Common.AddElement(ref newClass, "deprecation", DeprecationMessage);
            }
            Common.AddElement(ref newClass, "declaration", Declaration, true);
            Common.AddElement(ref newClass, "documentation").InnerXml = GetDocumentation().Trim();

            XmlNode nextGroup = Common.AddElement(ref newClass, "constructors");
            foreach (BaseCodeConstruct bcc in Constructors)
            {
                XmlNode node = Common.AddElement(ref nextGroup, "constructor");
                bcc.WriteToDoc(ref node);
            }

            nextGroup = Common.AddElement(ref newClass, "methods");
            foreach (BaseCodeConstruct bcc in Methods.OrderBy(t => t.Name))
            {
                XmlNode node = Common.AddElement(ref nextGroup, "method");
                bcc.WriteToDoc(ref node);
            }

            nextGroup = Common.AddElement(ref newClass, "fields");
            foreach (BaseCodeConstruct bcc in Fields.OrderBy(t => t.Name))
            {
                XmlNode node = Common.AddElement(ref nextGroup, "field");
                bcc.WriteToDoc(ref node);
            }

            nextGroup = Common.AddElement(ref newClass, "properties");
            foreach (BaseCodeConstruct bcc in Properties.OrderBy(t => t.Name))
            {
                XmlNode node = Common.AddElement(ref nextGroup, "property");
                bcc.WriteToDoc(ref node);
            }

            nextGroup = Common.AddElement(ref newClass, "events");
            foreach (BaseCodeConstruct bcc in Events.OrderBy(t => t.Name))
            {
                XmlNode node = Common.AddElement(ref nextGroup, "event");
                bcc.WriteToDoc(ref node);
            }

        }

    }
}
