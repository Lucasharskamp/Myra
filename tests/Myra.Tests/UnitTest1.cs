using Myra.Xaml.Compiler;
using Xunit;

namespace Myra.Tests
{
    public class Tests
    {
        [Fact]
        public void Test_Basic_Xaml()
        {
            var compiler = new MyraXamlCompiler();
            var parser = new MyraXamlParser(compiler.Configuration);
            var document = parser.Parse("test.xaml",
                """
                <Grid xmlns="https://github.com/MyraUI/Myra"> 
                    <Button>
                        <Label Text="Hello world!" />
                    </Button> 
                </Grid>
                """);
            compiler.Transform(document);
        }
    }
}
