using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using XamlX;
using XamlX.Transform;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraDiagnosticHandler : XamlDiagnosticsHandler
    {
        private XamlDiagnosticSeverity MinimumSeverity { get; }

        public List<XamlDiagnostic> Diagnostics { get; } = new();
        public bool HasError { get; private set; }

        public MyraDiagnosticHandler(XamlDiagnosticSeverity minimumSeverity)
        {
            MinimumSeverity = minimumSeverity;
            CodeMappings = HandleCodeMappings;
            HandleDiagnostic = DiagnosticHandlerMethod;
        }

        private readonly Dictionary<Type, string> ExceptionCodes = new()
        {
            { typeof(XamlLoadException), "MYRA001" },
            { typeof(XamlTransformException), "MYRA002" }
        };


        private string HandleCodeMappings(object arg)
        {
            if (arg is Exception ex)
            {
                return ExceptionCodes.TryGetValue(ex.GetType(), out var code) ? code : "";
            }

            return arg.ToString();
        }

        private XamlDiagnosticSeverity DiagnosticHandlerMethod(XamlDiagnostic diagnostic)
        {
            if (diagnostic.Severity < MinimumSeverity)
                return diagnostic.MinSeverity;

            if (diagnostic.Severity >= XamlDiagnosticSeverity.Error)
            {
                HasError = true;
            }

            Diagnostics.Add(diagnostic);
            return diagnostic.Severity;
        }

        public void ResolveDiagnostics(TaskLoggingHelper log, string xamlPath, string category)
        {
            foreach (var diagnostic in Diagnostics)
            {
                switch (diagnostic.Severity)
                {
                    case XamlDiagnosticSeverity.Error:
                    case XamlDiagnosticSeverity.Fatal:
                        log.LogError(subcategory: category,
                            diagnostic.Code,
                            "Myra stylesheet",
                            xamlPath,
                            diagnostic.LineNumber!.Value,
                            diagnostic.LinePosition!.Value,
                            diagnostic.LineNumber!.Value,
                            diagnostic.LinePosition!.Value,
                            diagnostic.Title);
                        break;

                    case XamlDiagnosticSeverity.Warning:
                        log.LogWarning(diagnostic.Title, diagnostic.LineNumber, diagnostic.LinePosition);
                        break;

                    case XamlDiagnosticSeverity.None:
                        log.LogMessage(diagnostic.Title, diagnostic.LineNumber, diagnostic.LinePosition);
                        break;
                }
            }
            Diagnostics.Clear();
            if (HasError)
            {
                throw new TaskCanceledException("Errors found; aborting compilation");
            }
        }
    }
}
