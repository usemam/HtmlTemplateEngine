using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Razor;
using Microsoft.CSharp;

namespace HtmlTemplateEngine
{
    public class HtmlTemplateEngine : ITemplateEngine
    {
        public const string DefaultHtmlTemplateSuffix = "";

        private const string NamespaceName = "TemplateEngine";

        private static readonly Dictionary<string, Type> typeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim();

        private static readonly string[] referencedAssemblies = BuildReferenceList().ToArray();
        private static readonly RazorTemplateEngine razorEngine = CreateRazorEngine();

        public HtmlTemplateEngine(ITemplateContentReader contentReader) : this(contentReader, DefaultHtmlTemplateSuffix)
        {
            ContentReader = contentReader;
        }

        public HtmlTemplateEngine(ITemplateContentReader contentReader, string htmlTemplateSuffix)
        {
            Invariant.IsNotNull(contentReader, "contentReader");

            ContentReader = contentReader;
            HtmlTemplateSuffix = htmlTemplateSuffix;
        }

        protected ITemplateContentReader ContentReader { get; private set; }

        protected string HtmlTemplateSuffix { get; private set; }

        public virtual string Execute(string templateName, object model = null)
        {
            Invariant.IsNotBlank(templateName, "templateName");

            var template = (HtmlTemplate) CreateTemplateInstance(templateName);

            template.RenderPartialImpl = Execute;

            template.SetModel(WrapModel(model));
            template.Execute();

            return template.Body;
        }

        protected virtual Assembly GenerateAssembly(string templateName, string templateContent)
        {
            var assemblyName = NamespaceName + "." + Guid.NewGuid().ToString("N") + ".dll";

            var templateResult = razorEngine.GenerateCode(new StringReader(templateContent), templateName, NamespaceName, templateName + ".cs");

            if (templateResult.ParserErrors.Any())
            {
                var parseExceptionMessage = string.Join(Environment.NewLine + Environment.NewLine, templateResult.ParserErrors.Select(e => e.Location + ":" + Environment.NewLine + e.Message).ToArray());

                throw new InvalidOperationException(parseExceptionMessage);
            }

            using (var codeProvider = new CSharpCodeProvider())
            {
                var compilerParameter = new CompilerParameters(referencedAssemblies, assemblyName, false)
                                            {
                                                GenerateInMemory = true,
                                                CompilerOptions = "/optimize"
                                            };

                var compilerResults = codeProvider.CompileAssemblyFromDom(compilerParameter, templateResult.GeneratedCode);

                if (compilerResults.Errors.HasErrors)
                {
                    var compileExceptionMessage = string.Join(Environment.NewLine + Environment.NewLine, compilerResults.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning).Select(e => e.FileName + ":" + Environment.NewLine + e.ErrorText).ToArray());

                    throw new InvalidOperationException(compileExceptionMessage);
                }

                return compilerResults.CompiledAssembly;
            }
        }

        protected virtual dynamic WrapModel(object model)
        {
            if (model == null)
            {
                return null;
            }

            if (model is IDynamicMetaObjectProvider)
            {
                return model;
            }

            var propertyMap  = model.GetType()
                                    .GetProperties()
                                    .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                                    .ToDictionary(property => property.Name, property => property.GetValue(model, null));

            return new TemplateModelWrapper(propertyMap);
        }

        private static RazorTemplateEngine CreateRazorEngine()
        {
            var host = new RazorEngineHost(new CSharpRazorCodeLanguage())
                           {
                               DefaultBaseClass = typeof(HtmlTemplate).FullName,
                               DefaultNamespace = NamespaceName
                           };

            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Collections");
            host.NamespaceImports.Add("System.Collections.Generic");
            host.NamespaceImports.Add("System.Dynamic");
            host.NamespaceImports.Add("System.Linq");

            return new RazorTemplateEngine(host);
        }

        private static IEnumerable<string> BuildReferenceList()
        {
            string currentAssemblyLocation = typeof(HtmlTemplateEngine).Assembly.CodeBase.Replace("file:///", string.Empty).Replace("/", "\\");

            return new List<string>
                       {
                           "mscorlib.dll",
                           "system.dll",
                           "system.core.dll",
                           "microsoft.csharp.dll",
                           currentAssemblyLocation
                       };
        }

        private ITemplate CreateTemplateInstance(string templateName)
        {
            return (ITemplate) Activator.CreateInstance(GetTemplateType(templateName));
        }

        private Type GetTemplateType(string templateName)
        {
            Type templateType;

            syncLock.EnterUpgradeableReadLock();

            try
            {
                if (!typeMapping.TryGetValue(templateName, out templateType))
                {
                    syncLock.EnterWriteLock();

                    try
                    {
                        templateType = GenerateTemplateType(templateName);
                        typeMapping.Add(templateName, templateType);
                    }
                    finally
                    {
                        syncLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                syncLock.ExitUpgradeableReadLock();
            }

            return templateType;
        }

        private Type GenerateTemplateType(string templateName)
        {
            var template = new
                               {
                                   Suffix = HtmlTemplateSuffix,
                                   TemplateName = templateName + HtmlTemplateSuffix,
                                   Content = ContentReader.Read(templateName, HtmlTemplateSuffix),
                               };


            var assembly = GenerateAssembly(template.TemplateName, template.Content);

            return assembly.GetType(NamespaceName + "." + template.TemplateName, true, false);
        }
    }
}