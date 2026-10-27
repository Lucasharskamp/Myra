using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Myra.Classifier
{ 
    internal sealed class MyraXamlClassifier : IClassifier
    {
        private readonly IClassificationType _elementClassification;
        private readonly IClassificationType _memberClassification;
        private readonly IClassificationType _errorClassification; 
        private readonly IClassificationType _featureClassification;
        private readonly MyraXamlDocumentModelProvider _modelProvider;

        public MyraXamlClassifier(
            IClassificationType elementClassification,
            IClassificationType memberClassification,
            IClassificationType featureClassification,
            IClassificationType errorClassification,
            MyraXamlDocumentModelProvider modelProvider)
        {
            _elementClassification = elementClassification;
            _memberClassification = memberClassification;
            _featureClassification = featureClassification;
            _errorClassification = errorClassification;
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

            // use a red error highlight if the type is not found. Otherwise, use a yellow golden element classification.
            var elementClassificationToUse = element.Type != null ? _elementClassification : _errorClassification;
 
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

            var memberClassificationToUse = element.Type != null ? _memberClassification : _errorClassification;

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
                result.Add(new ClassificationSpan(ownerSpan, (attribute.IsOwnerValid ?? true) ? _elementClassification : _errorClassification));
            } 
        }

        private void OnModelChanged(object? sender, MyraXamlDocumentModelChangedEventArgs e)
        {
            ClassificationChanged?.Invoke(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.Snapshot, 0, e.Snapshot.Length)));
        }
    }
}
