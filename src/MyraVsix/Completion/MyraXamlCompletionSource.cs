using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Myra.Classifier; 
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Myra.Completion
{
    internal sealed class MyraXamlCompletionSource(ITextView textView, MyraXamlDocumentModelProvider modelProvider) : IAsyncCompletionSource
    { 
        private readonly ITextView _textView = textView;
        private readonly MyraXamlDocumentModelProvider _modelProvider = modelProvider;
        private bool _isLoaded = false;
        private List<INamedTypeSymbol> _types = [];
        private readonly Dictionary<INamedTypeSymbol, List<ISymbol>> _typeMembers = [];


        private static readonly CompletionFilter TypeFilter = new("Types", "T", new ImageElement(KnownMonikers.Class.ToImageId()));
        private static readonly CompletionFilter PropertyFilter = new("Properties", "P", new ImageElement(KnownMonikers.Property.ToImageId()));
        private static readonly CompletionFilter MethodFilter = new("Method", "M", new ImageElement(KnownMonikers.Method.ToImageId()));
        private static readonly CompletionFilter EventFilter = new("Events", "E", new ImageElement(KnownMonikers.Event.ToImageId()));

        public CompletionStartData InitializeCompletion(
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            CancellationToken token)
        {
            if (!IsMyraDocument(triggerLocation.Snapshot))
                return CompletionStartData.DoesNotParticipateInCompletion;

            var span = GetApplicableSpan(triggerLocation);

            if (span is null)
                return CompletionStartData.DoesNotParticipateInCompletion;

            return new CompletionStartData(
                CompletionParticipation.ProvidesItems,
                span.Value);
        }

        public bool TryGetApplicableToSpan(
            char typedChar,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            out SnapshotSpan? applicableToSpanOut)
        {
            applicableToSpanOut = null;

            if (!IsMyraDocument(triggerLocation.Snapshot))
                return false;
             
            var span = GetApplicableSpan(triggerLocation);

            if (span is null)
                return false;

            applicableToSpanOut = span.Value;
            return true;
        }

        private void LoadTypes(Compilation compilation)
        {
            var widgetType = compilation.GetTypeByMetadataName(MyraShared.WidgetAddress);
            if (widgetType == null)
                return;

            var types = GetTypesInNamespace(compilation.GlobalNamespace, widgetType);
            _types = types.OrderBy(t => t.Name).ToList();
            _isLoaded = true;
        }

        static IEnumerable<INamedTypeSymbol> GetTypesInNamespace(INamespaceSymbol ns, INamedTypeSymbol widgetType)
        {
            var isMyraItems = ns.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).StartsWith("Myra");
            foreach (var type in ns.GetTypeMembers())
            { 

                // skip non-public types, abstract types
                // skip types those not derived from Widget AND not part of Myra.Graphics2D.UI
                if (type.DeclaredAccessibility != Accessibility.Public
                    || type.IsAbstract)
                    continue;

                if (!isMyraItems && !widgetType.IsAssignableFrom(type))
                    continue;

                yield return type;
            } 

            foreach (var childNamespace in ns.GetNamespaceMembers())
            {
                foreach (var type in GetTypesInNamespace(childNamespace, widgetType))
                {
                    yield return type;
                }
            }
        }

        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return CreateCompletionContextAsync(triggerLocation, applicableToSpan, token);
        }

        public Task<CompletionContext> GetCompletionContextAsync(
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return CreateCompletionContextAsync(triggerLocation, applicableToSpan, token);
        }

        public Task<object> GetDescriptionAsync(CompletionItem item, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (item.Properties.TryGetProperty<object>(typeof(string), out var description))
            {
                return Task.FromResult(description);
            }

            return Task.FromResult<object>(item.DisplayText);
        }

        public Task<object> GetDescriptionAsync(
            IAsyncCompletionSession session,
            CompletionItem item,
            CancellationToken token)
        {
            return GetDescriptionAsync(item, token);
        }

        private async Task<CompletionContext> CreateCompletionContextAsync(
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var snapshot = triggerLocation.Snapshot;
            var model = _modelProvider.GetModel(snapshot, true);

            if (model is null)
                return new CompletionContext([]);

            if (!_isLoaded)
            {
                LoadTypes(model.Compilation);
            }

            var position = triggerLocation.Position;

            var candidates = GetCandidates(model, snapshot, position, token);

            var items = candidates
                .Select(c => {
                    var fullName = c.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return new CompletionItem(c.Name, this, new ImageElement(GetTypeImage(c).ToImageId()), 
                        [GetFilter(c)], GetSummary(c), GetInsertText(c), fullName, fullName, []); 
                })
                .ToImmutableArray();

            return new CompletionContext(items);
        }

        static string GetSummary(ISymbol symbol)
        {
            var xml = symbol.GetDocumentationCommentXml();

            if (string.IsNullOrEmpty(xml))
                return String.Empty;

            return XElement.Parse(xml)
                .Element("summary")?
                .Value.Trim() ?? String.Empty;
        }

        static CompletionFilter GetFilter(ISymbol symbol)
        {
            if (symbol is IPropertySymbol)
                return PropertyFilter;

            if (symbol is IMethodSymbol)
                return MethodFilter;

            if (symbol is IEventSymbol) 
                return EventFilter;

            return TypeFilter;
        }

        static string GetInsertText(ISymbol symbol)
        {
            if (symbol is IPropertySymbol or IEventSymbol)
                return $"{symbol.Name}=\"\"";

            return symbol.Name + " ";
        }

        static ImageMoniker GetTypeImage(ISymbol symbol)
        {
            if (symbol is IPropertySymbol)
                return KnownMonikers.Property;

            if (symbol is IMethodSymbol)
                return KnownMonikers.Method;

            if (symbol is IEventSymbol)
                return KnownMonikers.Event;

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                return typeSymbol.TypeKind switch
                {
                    TypeKind.Class => KnownMonikers.Class,
                    TypeKind.Struct => KnownMonikers.Structure,
                    TypeKind.Interface => KnownMonikers.Interface,
                    TypeKind.Enum => KnownMonikers.Enumeration,
                    _ => KnownMonikers.Type
                };
            }

            throw new InvalidOperationException();
             
        }

        private IEnumerable<ISymbol> GetCandidates(MyraXamlDocumentModel model,  ITextSnapshot snapshot, int position, CancellationToken token)
        {
            var text = snapshot.GetText();
            var element = model.FindElementAt(position);

            // After '<', suggest widget types.
            if (IsElementNamePosition(text, position, out var currentlyWritten))
            {
                if (String.IsNullOrEmpty(currentlyWritten))
                    yield break;

                var options = _types.Where(t => t.Name.StartsWith(currentlyWritten, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var type in options)
                {
                    yield return type;
                }

                yield break;
            }

            // Inside an opening tag, suggest attributes or attached properties and events.
            // or when <Widget. has been written, suggesting we need to show properties.
            if (IsInsideOpeningTag(text, position, out var ownerName)
                || IsPropertyElementPosition(text, position, out ownerName))
            {

                if (String.IsNullOrEmpty(ownerName))
                    yield break;
                var finalType = _types.FirstOrDefault(t => t.Name.StartsWith(ownerName, StringComparison.OrdinalIgnoreCase));
                if (finalType == null)
                    yield break;

                if (!this._typeMembers.TryGetValue(finalType, out var members))
                {
                    members = finalType
                        .GetMembers()
                        .Where(m => !m.IsStatic
                                    && m.DeclaredAccessibility == Accessibility.Public
                                    && m is IPropertySymbol or IEventSymbol)
                        .OrderBy(m => m.Name)
                        .ToList();
                    _typeMembers.Add(finalType, members);
                }
                token.ThrowIfCancellationRequested();

                foreach (var member in members)
                {
                    yield return member;
                } 
            } 
        }

        private SnapshotSpan? GetApplicableSpan(SnapshotPoint point)
        {
            var text = point.Snapshot.GetText();
            var position = point.Position;

            if (position > 0 && text[position - 1] == '<')
                return new SnapshotSpan(point, 0);

            var start = position;

            while (start > 0 && IsNameCharacter(text[start - 1]))
                start--;

            if (start > 0 &&
                (text[start - 1] == '<' ||
                 text[start - 1] == '.' ||
                 char.IsWhiteSpace(text[start - 1])))
            {
                return new SnapshotSpan(
                    point.Snapshot,
                    Span.FromBounds(start, position));
            }

            return null;
        }

        private bool IsMyraDocument(ITextSnapshot snapshot)
        {
            var filePath = _textView.TextBuffer.GetFileName();

            // The provider is exported for XML content; the actual project-item
            // metadata check should be shared with your existing Myra-file detector.
            return snapshot.TextBuffer.ContentType.IsOfType("XML") &&
                   filePath?.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsInsideOpeningTag(
            string text,
            int position,
            out string ownerName)
        {
            ownerName = "";
            var open = text.LastIndexOf('<', Math.Max(0, position - 1));
            var close = text.LastIndexOf('>', Math.Max(0, position - 1));

            if (open <= close)
                return false; 
 
            var end = open + 1;

            if (end < text.Length && text[end] == '/')
                return false;

            while (end < position && IsNameCharacter(text[end]))
                end++;

            ownerName = text.Substring(open + 1, end - open - 1);
            return !String.IsNullOrEmpty(ownerName);
        }

        private static bool IsElementNamePosition(string text, int position, out string currentlyWritten)
        {
            currentlyWritten = "";
            var open = text.LastIndexOf('<', Math.Max(0, position - 1));
            var close = text.LastIndexOf('>', Math.Max(0, position - 1));

            if(open > close &&
                open + 1 < text.Length &&
                text[open + 1] != '/' &&
                !char.IsWhiteSpace(text[open + 1]))
            {
                int length = 0;
                var currentChar = text[open + length];
                while (currentChar != '\r' && currentChar != '\n'&& currentChar != '/')
                {
                    // we can allow for line breaks in case a user is creating a new element in the middle
                    // however, a space or tab indicates we're in a new element which is already defined
                    if (currentChar == ' ' || currentChar == '\t')
                        return false;

                    length++;
                    currentChar = text[open + length];
                }
                currentlyWritten = text.Substring(open+1, Math.Max(0, length-1));
                return true;
            }
            return false;
        }

        private static bool IsPropertyElementPosition(
            string text,
            int position,
            out string ownerName)
        {
            ownerName = string.Empty;
            var open = text.LastIndexOf('<', Math.Max(0, position - 1));
            var close = text.LastIndexOf('>', Math.Max(0, position - 1));

            if (open <= close)
                return false;

            var dot = text.LastIndexOf('.', position - 1);

            if (dot <= open)
                return false;

            var start = open + 1;

            if (start < text.Length && text[start] == '/')
                return false;

            ownerName = text.Substring(start, dot - start);
            return ownerName.Length > 0;
        }


        private static bool IsNameCharacter(char c) =>
            char.IsLetterOrDigit(c) ||
            c == '_' || c == '-' || c == ':' || c == '.';
    }
}
