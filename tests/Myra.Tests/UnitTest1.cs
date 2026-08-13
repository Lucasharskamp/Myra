using Mono.Cecil;
using Myra.Xaml.Compiler;
using Myra.Xaml.Types;
using System.Runtime.CompilerServices;
using XamlX.TypeSystem;
using Xunit;

namespace Myra.Tests
{
    public class Tests
    {
        private readonly ITestOutputHelper output;

        public Tests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Test_Basic_Xaml()
        {
            var compiler = new MyraXamlCompiler();
            var parser = new MyraXamlParser(compiler.Configuration);
            var document = parser.Parse("Myra/Tests/Foo.xaml",
                """
                <Grid xmlns="https://github.com/MyraUI/Myra"> 
                    <Button>
                        <Label Text="Hello world!" />
                    </Button> 
                </Grid>
                """);
            compiler.Transform(document);

            var asm = typeof(Foo).Assembly; 
            var fooAssembly = AssemblyDefinition.ReadAssembly(asm.Location);
            var fooType = fooAssembly.MainModule.GetType(typeof(Foo).FullName);
            var typeDefinition = fooType.Resolve();

            compiler.Compile(document, typeDefinition);
            foreach (var method in typeDefinition.Methods)
            {
                output.WriteLine("");
                output.WriteLine($"{method.ReturnType.FullName} {method.FullName}");

                if (!method.HasBody)
                    continue;

                foreach (var instruction in method.Body.Instructions)
                {
                    output.WriteLine(
                        $"  IL_{instruction.Offset:X4}: {instruction.OpCode,-12} {instruction.Operand}");
                }
            }
        }
    }

    [CompilerGenerated]
    public partial class Foo
    {
        public Foo()
        {

        }
    }
}
