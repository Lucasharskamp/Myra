using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Myra.Classifier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Myra.Tagger
{ 
    internal sealed class MyraXamlTagger : ITagger<IErrorTag>
    {
        private readonly ITextBuffer _buffer; 
        private readonly MyraXamlDocumentModelProvider _registry;
        private readonly object _gate = new();
        private List<TagSpan<IErrorTag>> _tags = [];

        private CancellationTokenSource? _updateCancellation;
        private readonly CancellationTokenSource _lifetime = new();

        private int _generation;
        private bool _disposed; 

        public MyraXamlTagger(ITextBuffer buffer, MyraXamlDocumentModelProvider modelProvider)
        {
            _buffer = buffer;
            _registry = modelProvider;

            _buffer.Changed += OnBufferChanged;
            _ = UpdateAsync();
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<TagSpan<IErrorTag>> cached;

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

                var tags = new List<TagSpan<IErrorTag>>();
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

        private void CreateTags(List<TagSpan<IErrorTag>> tags, MyraXamlElement element)
        {
            if (!element.IsValid)
            {
                if (element.MemberNameSpan.HasValue)
                {
                    var memberTag = new ErrorTag(MyraShared.ErrorCodeTypeNotExist, String.Format(MyraShared.ErrorHintTypeNotExist, element.Name));
                    tags.Add(new TagSpan<IErrorTag>(element.MemberNameSpan.Value, memberTag));

                    if (element.ClosingMemberNameSpan.HasValue)
                    {
                        tags.Add(new TagSpan<IErrorTag>(element.ClosingMemberNameSpan.Value, memberTag));
                    }
                }

                var tag = new ErrorTag(MyraShared.ErrorCodeTypeNotExist, String.Format(MyraShared.ErrorHintTypeNotExist, element.Name));
                tags.Add(new TagSpan<IErrorTag>(element.NameSpan, tag));

                if (element.ClosingNameSpan.HasValue)
                { 
                    tags.Add(new TagSpan<IErrorTag>(element.ClosingNameSpan.Value, tag));
                }
            }

            foreach (var attr in element.Attributes)
            {
                if (!attr.IsValid) 
                {
                    tags.Add(new TagSpan<IErrorTag>(attr.NameSpan,
                                new ErrorTag(MyraShared.ErrorTypeDoesNotHaveProperty, 
                                String.Format(MyraShared.ErrorHintDoesNotHaveProperty, element.TypeSymbol?.Name ?? "???", attr.Name))));
                }
            } 
        }

        private void Publish(ITextSnapshot snapshot,  int generation, List<TagSpan<IErrorTag>> tags)
        {
            if (_disposed ||  generation != Volatile.Read(ref _generation) || snapshot != _buffer.CurrentSnapshot)
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
