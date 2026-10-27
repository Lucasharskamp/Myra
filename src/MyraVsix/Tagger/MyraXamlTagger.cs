using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Myra.Classifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Myra.Tagger
{

    internal sealed class MyraXamlTagger : ITagger<ClassificationTag>
    {
        private readonly ITextBuffer _buffer; 
        private readonly MyraXamlDocumentModelProvider _registry;
        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly object _gate = new();
        private List<TagSpan<ClassificationTag>> _tags = [];

        private CancellationTokenSource? _updateCancellation;
        private readonly CancellationTokenSource _lifetime = new();

        private int _generation;
        private bool _disposed; 

        public MyraXamlTagger(ITextBuffer buffer,
            MyraXamlDocumentModelProvider modelProvider,
            IClassificationTypeRegistryService classificationRegistry)
        {
            _buffer = buffer;
            _registry = modelProvider;
            this._classificationRegistry = classificationRegistry;

            _buffer.Changed += OnBufferChanged;
            _ = UpdateAsync();
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<TagSpan<ClassificationTag>> cached;

            lock (_gate)
            {
                cached = _tags;
            }

            var snapshot = spans[0].Snapshot;

            // Never return spans from a different buffer snapshot.
            if (snapshot != _buffer.CurrentSnapshot)
                yield break;

            foreach (var tag in cached)
            {
                if (tag.Span.Snapshot != snapshot)
                    continue;

                if (spans.Any(span => span.IntersectsWith(tag.Span)))
                    yield return tag;
            }
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _ = UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            if (_disposed)
                return;

            var generation = Interlocked.Increment(ref _generation);

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetime.Token);

            CancellationTokenSource? previous;

            lock (_gate)
            {
                previous = _updateCancellation;
                _updateCancellation = cancellation;
            }

            previous?.Cancel();

            try
            {
                var snapshot = _buffer.CurrentSnapshot;
                  
                var model = await _registry.WaitForModelAsync(snapshot, cancellation.Token);
                if (model == null)
                    return;

                cancellation.Token.ThrowIfCancellationRequested();

                var tags = new List<TagSpan<ClassificationTag>>();
                foreach (var element in model.Elements)
                {
                    CreateTags(tags, element);
                }

                Publish(snapshot, generation, tags);
            }
            catch (OperationCanceledException)
            {
                // A newer edit superseded this calculation.
            }
            finally
            {
                lock (_gate)
                {
                    if (ReferenceEquals(_updateCancellation, cancellation))
                        _updateCancellation = null;
                }

                cancellation.Dispose();
            }
        }

        private void CreateTags(List<TagSpan<ClassificationTag>> tags, MyraXamlElement element)
        {
            if (element.MemberType != null && element.MemberNameSpan.HasValue)
            {
                tags.Add(new TagSpan<ClassificationTag>(
                    element.MemberNameSpan.Value,
                    new ClassificationTag(_classificationRegistry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition))));

                if (element.ClosingMemberNameSpan.HasValue)
                {
                    tags.Add(new TagSpan<ClassificationTag>(
                    element.ClosingMemberNameSpan.Value,
                    new ClassificationTag(_classificationRegistry.GetClassificationType(PredefinedClassificationTypeNames.SymbolDefinition))));
                }
            }

            if (element.Type != null)
            {
                tags.Add(new TagSpan<ClassificationTag>(
                          element.NameSpan,
                          new ClassificationTag(
                              _classificationRegistry.GetClassificationType(
                                  PredefinedClassificationTypeNames.SymbolDefinition))));
                if (element.ClosingNameSpan.HasValue)
                {
                    tags.Add(new TagSpan<ClassificationTag>(
                      element.ClosingNameSpan.Value,
                      new ClassificationTag(
                          _classificationRegistry.GetClassificationType(
                              PredefinedClassificationTypeNames.SymbolDefinition))));
                }
            }

            foreach (var attr in element.Attributes)
            {
                if (attr.OwnerType != null) 
                {
                    tags.Add(new TagSpan<ClassificationTag>(
                    attr.NameSpan,
                    new ClassificationTag(
                        _classificationRegistry.GetClassificationType(
                            PredefinedClassificationTypeNames.SymbolDefinition))));
                }
            } 
        }

        private void Publish(ITextSnapshot snapshot,  int generation, List<TagSpan<ClassificationTag>> tags)
        {
            if (_disposed ||
                generation != Volatile.Read(ref _generation) ||
                snapshot != _buffer.CurrentSnapshot)
            {
                return;
            }

            lock (_gate)
            {
                _tags = tags;
            }

            if (snapshot.Length > 0)
            {
                TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _buffer.Changed -= OnBufferChanged;
            _lifetime.Cancel();

            lock (_gate)
            {
                _updateCancellation?.Cancel();
                _tags.Clear();
            }

            _lifetime.Dispose();
        }
    }
}
