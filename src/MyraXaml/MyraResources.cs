using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Myra.Xaml
{
    sealed class MyraResources : IResourceGroup
    {
        private readonly AssemblyDefinition _asm;
        Dictionary<string, MyraResource> _resources = new Dictionary<string, MyraResource>();
        private EmbeddedResource? _embedded;
        public MyraResources(AssemblyDefinition asm, string projectDir)
        {
            _asm = asm;
            _embedded = ((EmbeddedResource)asm.MainModule.Resources.FirstOrDefault(r =>
                r.ResourceType == ResourceType.Embedded && r.Name == Constants.MyraResourceName));
            if (_embedded == null)
                return;
            using (var stream = _embedded.GetResourceStream())
            {
                var br = new BinaryReader(stream);
                var index = ReadIndex(new MemoryStream(br.ReadBytes(br.ReadInt32())));
                var baseOffset = stream.Position;
                foreach (var e in index)
                {
                    stream.Position = e.Offset + baseOffset;
                    _resources[e.Path] = new MyraResource(this, projectDir, e.Path, br.ReadBytes(e.Size));
                }
            }
        }

        public static List<MyraResourcesIndexEntry> ReadIndex(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var entryCount = reader.ReadInt32();
            var entries = new List<MyraResourcesIndexEntry>(entryCount);

            for (var i = 0; i < entryCount; ++i)
            {
                entries.Add(new MyraResourcesIndexEntry
                {
                    Path = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    Size = reader.ReadInt32()
                });
            }

            return entries;
        }

        public static void WriteIndex(Stream output, List<MyraResourcesIndexEntry> entries)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);

            WriteIndex(writer, entries);
        }

        private static void WriteIndex(BinaryWriter writer, List<MyraResourcesIndexEntry> entries)
        {
            writer.Write(entries.Count);

            foreach (var entry in entries)
            {
                writer.Write(entry.Path ?? string.Empty);
                writer.Write(entry.Offset);
                writer.Write(entry.Size);
            }
        }

        public static void WriteResources(Stream output, List<MyraResourcesEntry> resources)
        {
            var entries = new List<MyraResourcesIndexEntry>();
            var index = new Dictionary<string, (MyraResourcesIndexEntry entry, Func<Stream> open)>();
            var offset = 0;

            foreach (var resource in resources)
            {
                // Try to combine resources with the same system path, if present.
                if (!string.IsNullOrEmpty(resource.SystemPath)
                    && index.TryGetValue(resource.SystemPath!, out var existingResource))
                {
                    entries.Add(new MyraResourcesIndexEntry
                    {
                        Path = resource.Path,
                        Offset = existingResource.entry.Offset,
                        Size = existingResource.entry.Size
                    });
                }
                else
                {
                    var entry = new MyraResourcesIndexEntry
                    {
                        Path = resource.Path,
                        Offset = offset,
                        Size = resource.Size
                    };
                    index[resource.SystemPath ?? offset.ToString()] = (entry, resource.Open!);
                    entries.Add(entry);
                    offset += resource.Size;
                }
            }

            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            writer.Write(0); // index size placeholder, overwritten below

            var posBeforeEntries = output.Position;
            WriteIndex(writer, entries);

            var posAfterEntries = output.Position;
            var indexSize = (int)(posAfterEntries - posBeforeEntries);
            output.Position = 0L;
            writer.Write(indexSize);
            output.Position = posAfterEntries;

            foreach (var pair in index)
            {
                using var resourceStream = pair.Value.open();
                resourceStream.CopyTo(output);
            }
        }

        public void Save()
        {
            if (_embedded != null)
            {
                _asm.MainModule.Resources.Remove(_embedded);
                _embedded = null;
            }

            if (_resources.Count == 0)
                return;

            var output = new MemoryStream();

            WriteResources(
                output,
                _resources.Select(x => new MyraResourcesEntry(
                    path: x.Key,
                    size: x.Value.FileContents.Length,
                    systemPath: x.Value.FilePath,
                    open: () => new MemoryStream(x.Value.FileContents)
                )).ToList());

            output.Position = 0L;
            _embedded = new EmbeddedResource(Constants.MyraResourceName, ManifestResourceAttributes.Public, output);
            _asm.MainModule.Resources.Add(_embedded);
        }

        public string Name => "MyraResources";
        public List<IResource> Resources => _resources.Values.Cast<IResource>().ToList();

        class MyraResource : IResource
        {
            private readonly MyraResources _grp;
            private readonly byte[] _data;

            public MyraResource(MyraResources grp,
                string projectDir,
                string name, byte[] data)
            {
                _grp = grp;
                _data = data;
                Name = name;
                FilePath = Path.Combine(projectDir, name.TrimStart('/'));
                Uri = $"myrares://{grp._asm.Name.Name}/{name.TrimStart('/')}";
            }
            public string Uri { get; }
            public string Name { get; }
            public string FilePath { get; }
            public byte[] FileContents => _data;

            public void Remove() => _grp._resources.Remove(Name);
        }

        static void CopyDebugDocument(MethodDefinition method, MethodDefinition copyFrom)
        {
            if (!copyFrom.DebugInformation.HasSequencePoints)
                return;
            var dbg = method.DebugInformation;

            dbg.Scope = new ScopeDebugInformation(method.Body.Instructions.First(), method.Body.Instructions.First())
            {
                End = new InstructionOffset(),
                Import = new ImportDebugInformation()
            };
            dbg.SequencePoints.Add(new SequencePoint(method.Body.Instructions.First(),
                copyFrom.DebugInformation.SequencePoints.First().Document)
            {
                StartLine = 0xfeefee,
                EndLine = 0xfeefee
            });

        }
    }
}
