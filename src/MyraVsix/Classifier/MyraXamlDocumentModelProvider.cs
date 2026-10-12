using Microsoft.VisualStudio.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Myra.Classifier
{
    internal sealed class MyraXamlDocumentModelProvider : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly MyraXamlParser _parser;

        private readonly object _gate = new();

        private MyraXamlDocumentModel? _model;

        private CancellationTokenSource? _parseCancellation;
        private bool _disposed;

        public MyraXamlDocumentModelProvider(ITextBuffer buffer,  MyraXamlParser parser)
        {
            _buffer = buffer;
            _parser = parser;

            _buffer.Changed += OnBufferChanged;

            // Build the initial model.
            ScheduleParse();
        }

        public event EventHandler<MyraXamlDocumentModelChangedEventArgs>? ModelChanged;

        public MyraXamlDocumentModel? GetModel(ITextSnapshot snapshot)
        {
            lock (_gate)
            {
                if (_model?.Snapshot == snapshot)
                    return _model;

                return null;
            }
        }

        private void OnBufferChanged(
            object sender,
            TextContentChangedEventArgs e)
        {
            ScheduleParse();
        }

        private void ScheduleParse()
        {
            CancellationToken token;

            lock (_gate)
            {
                if (_disposed)
                    return;

                _parseCancellation?.Cancel();
                _parseCancellation?.Dispose();

                _parseCancellation =
                    new CancellationTokenSource();

                token = _parseCancellation.Token;
            }

            _ = ParseAsync(token);
        }

        private async Task ParseAsync(CancellationToken token)
        {
            try
            {
                // Debounce rapid typing.
                await Task.Delay(
                    TimeSpan.FromMilliseconds(100),
                    token);

                token.ThrowIfCancellationRequested();

                var snapshot = _buffer.CurrentSnapshot;

                // Parsing must not touch the mutable buffer. The parser
                // works exclusively against this immutable snapshot.
                var model = await Task.Run(
                    () => _parser.Parse(snapshot, token),
                    token);

                token.ThrowIfCancellationRequested();

                // The document may have changed while parsing.
                if (!ReferenceEquals(
                        snapshot,
                        _buffer.CurrentSnapshot))
                {
                    return;
                }

                lock (_gate)
                {
                    if (_disposed)
                        return;

                    // Check again after acquiring the lock.
                    if (!ReferenceEquals(
                            snapshot,
                            _buffer.CurrentSnapshot))
                    {
                        return;
                    }

                    _model = model;
                }

                ModelChanged?.Invoke(
                    this,
                    new MyraXamlDocumentModelChangedEventArgs(
                        snapshot));
            }
            catch (OperationCanceledException)
            {
                // Expected when another edit supersedes this parse.
            }
            catch (Exception)
            {
                // Do not let parser failures take down the VS language
                // service. For now, simply discard the failed model.
                //
                // You can later report parser errors through the error
                // tagger instead.
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;

                _disposed = true;

                _buffer.Changed -= OnBufferChanged;

                _parseCancellation?.Cancel();
                _parseCancellation?.Dispose();
                _parseCancellation = null;

                _model = null;
            }
        }
    }

    internal sealed class MyraXamlDocumentModelChangedEventArgs
        : EventArgs
    {
        public MyraXamlDocumentModelChangedEventArgs(
            ITextSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public ITextSnapshot Snapshot { get; }
    }
}
