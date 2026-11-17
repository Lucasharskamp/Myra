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
        private MyraXamlDocumentModel? _model = null; 
        private readonly Dictionary<INamedTypeSymbol, List<ISymbol>> _typeMembers = [];


        private static readonly CompletionFilter TypeFilter = new("Types", "T", new ImageElement(KnownMonikers.Class.ToImageId()));
        private static readonly CompletionFilter PropertyFilter = new("Properties", "P", new ImageElement(KnownMonikers.Property.ToImageId()));
        private static readonly CompletionFilter MethodFilter = new("Method", "M", new ImageElement(KnownMonikers.Method.ToImageId()));
        private static readonly CompletionFilter EventFilter = new("Events", "E", new ImageElement(KnownMonikers.Event.ToImageId()));

        /// <summary>
        /// Where inside the Xaml the cursor is currently located.
        /// </summary>
        private enum CursorLocation
        {
            /// <summary>
            /// Cursor is outside any element.
            /// </summary>
            Root,
            /// <summary>
            /// user wrote a left-chevron, making a new element.
            /// </summary>
            StartOfElement,
            /// <summary>
            /// E.g. "widget.Property" area
            /// </summary>
            ElementProperty,
            /// <summary>
            /// user wrote an element name, and is now defining attributes
            /// </summary>
            DefiningAttribute,
            /// <summary>
            /// Cursor is inside the quotations of an attribute value
            /// </summary>
            AttributeValue,
            /// <summary>
            /// Cursor is inside element value. 
            /// </summary>
            ElementValue
        }

        /// <inheritdoc />
        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            if (!IsMyraDocument(triggerLocation.Snapshot))
                return CompletionStartData.DoesNotParticipateInCompletion;

            var span = GetApplicableSpan(triggerLocation);

            if (span is null)
                return CompletionStartData.DoesNotParticipateInCompletion;

            return new CompletionStartData(CompletionParticipation.ProvidesItems, span.Value);
        }

        /// <inheritdoc />
        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return CreateCompletionContextAsync(triggerLocation, applicableToSpan, token);
        }

        /// <inheritdoc />
        public Task<CompletionContext> GetCompletionContextAsync(
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            return CreateCompletionContextAsync(triggerLocation, applicableToSpan, token);
        }
          

        /// <inheritdoc />
        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item,  CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (item.Properties.TryGetProperty<object>(typeof(string), out var description))
            {
                return Task.FromResult(description);
            }

            return Task.FromResult<object>(item.DisplayText);
        }

        private async Task<CompletionContext> CreateCompletionContextAsync(
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var snapshot = triggerLocation.Snapshot;  

            if (_model == null)
            {
                _model = _modelProvider.GetModel(snapshot, true);
                if (_model == null)
                    return new CompletionContext([]); 
                  
            }

            var position = triggerLocation.Position;

            var candidates = GetCandidates(snapshot, applicableToSpan, position, token);

            var items = candidates 
                .Select(c => {
                    var fullName = c.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var insertText = GetInsertText(c);
                    return new CompletionItem(c.Name, this, new ImageElement(GetTypeImage(c).ToImageId()),
                        [GetFilter(c)], GetSummary(c), insertText, fullName, fullName, fullName, [], [.. insertText], default, false, false);
                })
                .ToImmutableArray();

            return new CompletionContext(items);
        }

        static string GetSummary(ISymbol symbol)
        {
            var xml = symbol.GetDocumentationCommentXml();

            if (string.IsNullOrEmpty(xml))
                return String.Empty;

            try
            {
                return XElement.Parse(xml)
                    .Element("summary")?
                    .Value.Trim() ?? String.Empty;
            }
            catch
            {
                // in case of broken XML
                return String.Empty; 
            }
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
            if (symbol is IPropertySymbol or IEventSymbol or IMethodSymbol)
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

            if (symbol is IFieldSymbol)
                return KnownMonikers.Field;

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

        private IEnumerable<ISymbol> GetCandidates(ITextSnapshot snapshot, SnapshotSpan applicableToSpan, int position, CancellationToken token)
        {
            var text = snapshot.GetText();

            var location = GetCursorLocation(text, position, out var currentlyWritten, out string? ownerName);

            // After '<', suggest widget types.
            if (location == CursorLocation.StartOfElement)
            {
                if (String.IsNullOrEmpty(currentlyWritten))
                    yield break;

                var options = _model!.TypeCache.Where(t => t.Value != null && t.Value.Name.StartsWith(currentlyWritten, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var type in options)
                {
                    yield return type.Value!;
                }

                yield break;
            } 

            // Inside an opening tag, suggest attributes or attached properties and events.
            // or when <Widget. has been written, suggesting we need to show properties.
            if (location == CursorLocation.ElementProperty
                || location == CursorLocation.DefiningAttribute)
            {

                if (String.IsNullOrEmpty(ownerName))
                    yield break;

                var finalType = _model!.TypeCache.FirstOrDefault(t => t.Value != null && t.Value.Name.StartsWith(ownerName, StringComparison.OrdinalIgnoreCase)).Value;
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

                yield break;
            }

            if (_model?.CodeBehindType == null)
                yield break;

            var open = text.LastIndexOf('<', Math.Max(0, position - 1)) + 1;
            var element = _model!.FindElementAt(open); 

            // Inside an attribute value, suggest code-behind members.
            // Examples: Foo="B... and x:Name="Ab...
            // However, we only do this if a code-behind type is known to exist
            // and if the element has a known type symbol to work from. 
            if (element == null 
                || element.TypeSymbol == null)
                yield break; 

            var openQuote = text.LastIndexOf('"', Math.Max(0, position - 1));

            if (openQuote >= 0)
            {
                var equals = text.LastIndexOf('=', openQuote);

                if (equals >= 0)
                {
                    var attributeStart = equals - 1;

                    while (attributeStart >= 0 &&
                           (char.IsLetterOrDigit(text[attributeStart]) ||
                            text[attributeStart] == '_' ||
                            text[attributeStart] == ':' ||
                            text[attributeStart] == '.'))
                    {
                        attributeStart--;
                    }

                    var attributeName = text.Substring(
                        attributeStart + 1,
                        equals - attributeStart - 1);

                    // get the text already written within the quotation mark(s)
                    var prefix = text.Substring(openQuote + 1, position - openQuote - 1).Trim();

                    if (attributeName == "x:Name")
                    { 
                        foreach (var candidate in _model.CodeBehindFields)
                        { 
                            if (String.IsNullOrEmpty(prefix) || candidate.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                yield return candidate;
                            }
                        } 

                        foreach (var candidate in _model.CodeBehindProperties)
                        { 
                            if (String.IsNullOrEmpty(prefix) || candidate.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                yield return candidate;
                            }
                        }

                        yield break;
                    }

                    var behindSymbol = element.TypeSymbol.GetMembers().FirstOrDefault(m => String.Equals(attributeName, m.Name));
                    if (behindSymbol == null)
                        yield break;

                    if (behindSymbol.Kind == SymbolKind.Event)
                    {
                        foreach (var candidate in _model.CodeBehindMethods)
                        {
                            if (String.IsNullOrEmpty(prefix) || candidate.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            {
                                yield return candidate;
                            }
                        }
                        yield break;
                    } 

                    if (behindSymbol is IPropertySymbol propertySymbol)
                    {
                        if (propertySymbol.Type.TypeKind == TypeKind.Enum)
                        {
                            var enumOptions = propertySymbol.Type
                                .GetMembers()
                                .OfType<IFieldSymbol>()
                                .Where(f => f.HasConstantValue);

                            foreach (var enumOption in enumOptions)
                            {
                                yield return enumOption;
                            }
                        }
                    }
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
         
        private static CursorLocation GetCursorLocation(string text, int position, out string currentlyWritten, out string? ownerName)
        {
            currentlyWritten = "";
            var open = text.LastIndexOf('<', Math.Max(0, position - 1));
            var close = text.LastIndexOf('>', Math.Max(0, position - 1));
            ownerName = null;

            if (open == -1)
                return CursorLocation.Root;

            if (open <= close || 
                text[open + 1] == '/' ||
                char.IsWhiteSpace(text[open + 1]))
            {
                return CursorLocation.ElementValue;
            }
              
            int length = 0;
            var currentChar = text[open + length];
            int until = Math.Min(position, text.Length);
            var location = CursorLocation.StartOfElement;
            while (open + length < until)
            {
                if (location == CursorLocation.StartOfElement || location == CursorLocation.ElementProperty)
                {
                    if (Char.IsWhiteSpace(currentChar))
                    {
                        ownerName = text.Substring(open + 1, Math.Max(0, length - 1));
                        location = CursorLocation.DefiningAttribute;
                    }
                    else if (currentChar == '.')
                    {
                        ownerName = text.Substring(open + 1, Math.Max(0, length - 1));
                        location = CursorLocation.ElementProperty;
                    }
                }
                else if (location == CursorLocation.DefiningAttribute && currentChar == '"')
                    location = CursorLocation.AttributeValue;
                else if (location == CursorLocation.AttributeValue && currentChar == '"')
                    location = CursorLocation.DefiningAttribute;
                
                if (currentChar == '>')
                    location = CursorLocation.ElementValue;
                else if (currentChar == '<')
                    location = CursorLocation.StartOfElement;

                length++;
                currentChar = text[open + length];
            }
            currentlyWritten = text.Substring(open + 1, Math.Max(0, length - 1));
            return location;
        } 

        private static bool IsNameCharacter(char c) =>
            char.IsLetterOrDigit(c) ||
            c == '_' || c == '-' || c == ':' || c == '.';
    }
}
