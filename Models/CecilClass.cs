using System.Linq;
using Mono.Cecil;
using System.Xml;
using System.Collections.Generic;
using ICSharpCode.Decompiler;

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
                CodeBlock cb = assemblyReader.GetMethod(m);
                mm.CodeBlocks.Add(cb);
                mm.Decompiled = assemblyReader.Decompiler.DecompileAsString(m).ToDictionary();
                mm.Declaration = mm.GetDeclaration();
                mm = Common.ParseDeprecation(m, mm);
                Constructors.Add(mm);
            }

            foreach (MethodDefinition m in cClass.Methods.Where(p1 => !p1.Name.Contains("__") & !p1.IsConstructor & !p1.IsGetter & !p1.IsSetter))
            {
                BaseCodeConstruct mm = new BaseCodeConstruct();
                mm.Name = m.Name;
                CodeBlock cb = assemblyReader.GetMethod(m);
                mm.CodeBlocks.Add(cb);
                mm.Decompiled = assemblyReader.Decompiler.DecompileAsString(m).ToDictionary();
                mm.Declaration = mm.GetDeclaration();
                mm = Common.ParseDeprecation(m, mm);
                Methods.Add(mm);
            }

            foreach (FieldDefinition f in cClass.Fields.Where(p1 => !p1.Name.Contains("__")))
            {
                BaseCodeConstruct fm = new BaseCodeConstruct();
                fm.Name = f.Name;
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
