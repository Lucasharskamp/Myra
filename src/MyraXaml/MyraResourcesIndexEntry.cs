using System;
using System.IO;

namespace Myra.Xaml
{
    class MyraResourcesEntry
    {
        public MyraResourcesEntry(string path, Func<Stream>? open, int size, string systemPath)
        {
            Path = path;
            Open = open;
            Size = size;
            SystemPath = systemPath;
        }

        public string Path { get; }
        public Func<Stream>? Open { get; }
        public int Size { get; }
        public string SystemPath { get; }
    }

    class MyraResourcesIndexEntry
    {
        public string Path { get; set; } = default!;

        public int Offset { get; set; }

        public int Size { get; set; }
    }
}
