using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Myra.Tagger
{ 

    internal sealed class MyraXamlTagger<T> : ITagger<T> where T : ITag
    {
        private readonly ITextBuffer _buffer;

        private static readonly Regex ElementRegex = new(
        """
        <
        \s*
        (?<closing>/)?
        (?:
            (?<prefix>[A-Za-z_][A-Za-z0-9_.-]*):
        )?
        (?<name>[A-Za-z_][A-Za-z0-9_.-]*)
        """,
        RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        public MyraXamlTagger(ITextBuffer buffer)
        {
            _buffer = buffer;

            _buffer.Changed += OnBufferChanged;
        }

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            var snapshot = _buffer.CurrentSnapshot;

            foreach (var span in spans)
            {

            }

           // yield return new TagSpan<ErrorTag>(spans.First(), new ErrorTag())

            // Actual Myra tag discovery will go here.
            //
            // Example:
            //
            // yield return new TagSpan<MyraXamlTag>(
            //     new SnapshotSpan(snapshot, start, length),
            //     new MyraXamlTag("..."));
        }

        private void OnBufferChanged(object sender,TextContentChangedEventArgs e)
        {
            // For now, invalidate everything.
            TagsChanged?.Invoke(
                this, new SnapshotSpanEventArgs(new SnapshotSpan(e.After, 0, e.After.Length)));
        }
    }
}
