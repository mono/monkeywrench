using System;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.Serialization.Advanced;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace MonkeyWrench.Web.WebServices
{
	public class WsdlGenerator : SchemaImporterExtension
	{
		public override string ImportSchemaType (string name, string ns, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer, CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
		{
			if (ns == "http://monkeywrench.novell.com/") {
				if (name != "ArrayOfString") {
					mainNamespace.Imports.Add (new CodeNamespaceImport ("MonkeyWrench.DataClasses"));
					mainNamespace.Imports.Add (new CodeNamespaceImport ("MonkeyWrench.DataClasses.Logic"));
					return name;
				}
			}

			return base.ImportSchemaType (name, ns, context, schemas, importer, compileUnit, mainNamespace, options, codeProvider);
		}
	}
}