using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Myra.Classifier
{

    internal sealed class MyraXamlClassifier : IClassifier
    {
        private readonly IClassificationType _elementClassification;
        private readonly IClassificationType _attributeClassification;
        private readonly MyraXamlDocumentModelProvider _modelProvider;

        public MyraXamlClassifier(
            IClassificationType elementClassification,
            IClassificationType attributeClassification,
            MyraXamlDocumentModelProvider modelProvider)
        {
            _elementClassification = elementClassification;
            _attributeClassification = attributeClassification;
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
                ClassifyElement(
                    element,
                    span,
                    result);

                foreach (var attribute in element.Attributes)
                {
                    ClassifyAttribute(
                        attribute,
                        span,
                        result);
                }
            }

            return result;
        }

        private void ClassifyElement(
            MyraXamlElement element,
            SnapshotSpan requestedSpan,
            List<ClassificationSpan> result)
        {

            var nameSpanIntersects = element.NameSpan.IntersectsWith(requestedSpan);
            var closingNameSpanIntersects = element.ClosingNameSpan.HasValue
                && element.ClosingNameSpan.Value.IntersectsWith(requestedSpan);

            if (!nameSpanIntersects && !closingNameSpanIntersects)
                return;

            // A property element consists of:
            //
            //     <Grid.ColumnsProportions>
            //      ^^^^ ^^^^^^^^^^^^^^^^^
            //      type       property
            //
            // The model has already established that this is a real
            // property element, so the classifier only needs to project
            // the two semantic pieces onto their respective colors.

            if (element.IsPropertyElement)
            {
                if (element.OwnerNameSpan is SnapshotSpan ownerSpan &&
                    ownerSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            ownerSpan,
                            _elementClassification));
                }

                if (element.MemberNameSpan is SnapshotSpan memberSpan &&
                    memberSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            memberSpan,
                            _attributeClassification));
                }

                if (element.ClosingOwnerNameSpan is SnapshotSpan closingOwnerSpan &&
                     closingOwnerSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            closingOwnerSpan,
                            _elementClassification));
                }

                if (element.ClosingMemberNameSpan is SnapshotSpan closingMemberSpan &&
                        closingMemberSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            closingMemberSpan,
                            _attributeClassification));
                }

                return;
            }

            // Normal element:
            //
            //     <Grid>
            //      ^^^^
            //
            // or:
            //
            //     <local:MyButton>
            //      ^^^^^^^^^^^^^^
            //
            result.Add(
                new ClassificationSpan(
                    element.NameSpan,
                    _elementClassification));

            if (closingNameSpanIntersects)
            {
                result.Add(new ClassificationSpan(element.ClosingNameSpan!.Value, _elementClassification));
            }
        }

        private void ClassifyAttribute(
            MyraXamlAttribute attribute,
            SnapshotSpan requestedSpan,
            List<ClassificationSpan> result)
        {
            if (!attribute.IsValid)
                return;

            // Normal property:
            //
            //     Text="Hello"
            //     ^^^^
            //
            if (!attribute.IsAttachedProperty)
            {
                if (attribute.NameSpan.IntersectsWith(requestedSpan))
                {
                    result.Add(
                        new ClassificationSpan(
                            attribute.NameSpan,
                            _attributeClassification));
                }

                return;
            }

            // Attached property:
            //
            //     Grid.Column="1"
            //     ^^^^ ^^^^^^
            //
            // The model has already established that Grid.Column is a
            // valid attached property.

            if (attribute.OwnerNameSpan is SnapshotSpan ownerSpan &&
                ownerSpan.IntersectsWith(requestedSpan))
            {
                result.Add(
                    new ClassificationSpan(
                        ownerSpan,
                        _elementClassification));
            }

            if (attribute.MemberNameSpan is SnapshotSpan memberSpan &&
                memberSpan.IntersectsWith(requestedSpan))
            {
                result.Add(new ClassificationSpan(memberSpan, _attributeClassification));
            }
        }

        private void OnModelChanged(object? sender, MyraXamlDocumentModelChangedEventArgs e)
        {
            ClassificationChanged?.Invoke(
                this,
                new ClassificationChangedEventArgs(
                    new SnapshotSpan(e.Snapshot, 0, e.Snapshot.Length)));
        }
    }
}
