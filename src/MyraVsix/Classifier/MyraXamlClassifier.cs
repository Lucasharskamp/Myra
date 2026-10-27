using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Myra.Classifier
{ 
    internal sealed class MyraXamlClassifier : IClassifier
    {
        private readonly IClassificationType _typeClassification;
        private readonly IClassificationType _memberClassification;
        private readonly IClassificationType _featureClassification;
        private readonly IClassificationType _numberClassification;
        private readonly IClassificationType _stringClassification;
        private readonly IClassificationType _errorClassification;
        private readonly IClassificationType _methodClassification;
        private readonly IClassificationType _enumClassification;
        private readonly MyraXamlDocumentModelProvider _modelProvider;

        private static readonly Regex IsNumberRegex = new(@"^[\s\d.,]*$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public MyraXamlClassifier(
            IClassificationTypeRegistryService classificationTypes,
            MyraXamlDocumentModelProvider modelProvider)
        {
            _typeClassification = classificationTypes.GetClassificationType("myraXamlType");
            _memberClassification = classificationTypes.GetClassificationType("myraXamlMember");
            _featureClassification = classificationTypes.GetClassificationType("myraXamlFeature");
            _stringClassification = classificationTypes.GetClassificationType("string");
            _numberClassification = classificationTypes.GetClassificationType("number");
            _methodClassification = classificationTypes.GetClassificationType("method name");
            _enumClassification = classificationTypes.GetClassificationType("enum member name");
            _errorClassification = classificationTypes.GetClassificationType("myraXamlError");
            _modelProvider = modelProvider;

            _modelProvider.ModelChanged += OnModelChanged;
        }

        public event EventHandler<ClassificationChangedEventArgs>? ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();

            var model = _modelProvider.GetModel(span.Snapshot);

            if (model == null)
                return result;

            foreach (var element in model.Elements)
            {
                ClassifyElement(element, span, result);

                foreach (var attribute in element.Attributes)
                {
                    ClassifyAttribute(attribute, span, result);
                }
            }

            return result;
        }

        private void ClassifyElement(MyraXamlElement element, SnapshotSpan requestedSpan,  List<ClassificationSpan> result)
        {
            var nameSpanIntersects = element.NameSpan.IntersectsWith(requestedSpan);
            var closingNameSpanIntersects = element.ClosingNameSpan.HasValue
                && element.ClosingNameSpan.Value.IntersectsWith(requestedSpan);

            if (!nameSpanIntersects && !closingNameSpanIntersects)
                return;

            AssignValue(element, requestedSpan, result);

            // use a red error highlight if the type is not found. Otherwise, use a yellow golden element classification.
            var elementClassificationToUse = element.TypeSymbol != null ? _typeClassification : _errorClassification;
 
            // Check if the element is about the a child property, like <Grid.ColumnsProportions> 
            // If not, the classifications are straightforward.
            if (!element.IsPropertyElement)
            {
                // Normal element:  <Grid> or: <local:MyButton>
                result.Add(new ClassificationSpan(element.NameSpan, elementClassificationToUse));

                // if present, classify the </Grid> or </local:MyButton>
                if (closingNameSpanIntersects)
                {
                    result.Add(new ClassificationSpan(element.ClosingNameSpan!.Value, elementClassificationToUse));
                }
                return;
            }

            var memberClassificationToUse = element.TypeSymbol != null ? _memberClassification : _errorClassification;

            // classify the <'owner'.member> area
            if (element.OwnerNameSpan is SnapshotSpan ownerSpan &&
                ownerSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(ownerSpan, elementClassificationToUse));
            }

            // classify the <owner.'member'> area
            if (element.MemberNameSpan is SnapshotSpan memberSpan &&
                memberSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(memberSpan, memberClassificationToUse));
            }

            // do the same for the closing </owner.member> tag
            if (element.ClosingOwnerNameSpan is SnapshotSpan closingOwnerSpan &&
                    closingOwnerSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(closingOwnerSpan, elementClassificationToUse));
            }

            if (element.ClosingMemberNameSpan is SnapshotSpan closingMemberSpan &&
                    closingMemberSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(closingMemberSpan, memberClassificationToUse));
            }

            return; 
        }

        private void ClassifyAttribute(MyraXamlAttribute attribute, SnapshotSpan requestedSpan, List<ClassificationSpan> result)
        {
            // Resolve Property name, like "Text" in 'Text="Hello World"'
            if (attribute.NameSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(attribute.NameSpan,
                    attribute.IsValid ?
                    attribute.IsFeature ? _featureClassification : _memberClassification
                    : _errorClassification));
            }

            // provide class classification if it is a property reference to another type, like 'Grid.Row'
            if (attribute.OwnerNameSpan is SnapshotSpan ownerSpan && ownerSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(ownerSpan, (attribute.IsOwnerValid ?? true) ? _typeClassification : _errorClassification));
            }

            AssignValue(attribute, requestedSpan, result);  
        }

        private void AssignValue(IMyraValueType item, SnapshotSpan requestedSpan, List<ClassificationSpan> result)
        {
            if (!item.ValueSpan.IntersectsWith(requestedSpan) || item.TypeSymbol == null)
            {
                return;
            }

            if (String.Equals(item.TypeSymbol.Name, "MyraEventHandler", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ClassificationSpan(item.ValueSpan, _methodClassification));
                return;
            }

            if (item.TypeSymbol.TypeKind == TypeKind.Enum)
            {
                result.Add(new ClassificationSpan(item.ValueSpan, _enumClassification));
                return;
            }

            if (item.TypeSymbol.TypeKind == TypeKind.Struct)
            {
                result.Add(new ClassificationSpan(item.ValueSpan,
                                    IsNumberRegex.IsMatch(item.Value) ? _numberClassification : _methodClassification));
                return;
            }

            if (String.Equals(item.TypeSymbol.Name, "String", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new ClassificationSpan(item.ValueSpan, _stringClassification));
                return;
            }
        }

        private void OnModelChanged(object? sender, MyraXamlDocumentModelChangedEventArgs e)
        {
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.Snapshot, 0, e.Snapshot.Length)));
        }
    }
}
