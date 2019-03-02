using System;

namespace Prgfx.Fusion
{
    class Program
    {
        static void Main(string[] args)
        {
            // basic setup
            var sourceBuilder = new System.Text.StringBuilder();
            
            // load all *.fusion files embedded in this assembly
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var includedResources = assembly.GetManifestResourceNames();
            foreach (var resourceName in includedResources) {
                if (!resourceName.EndsWith(".fusion")) {
                    continue;
                }
                var stream = assembly.GetManifestResourceStream(resourceName);
                sourceBuilder.Append((new System.IO.StreamReader(stream)).ReadToEnd());
            }
            var inp = @"            
            prototype(Test:Button) < prototype(Neos.Fusion:Tag) {
                tagName = 'button'
                content = 'click me'
                attributes.class = 'btn'
            }
            renderer = Neos.Fusion:Loop {
                items = Neos.Fusion:RawArray {
                    1 = 'foo'
                    2 = 'bar'
                    3 = 'baz'
                }
                itemRenderer = Test:Button {
                    content = ${item}
                    attributes {
                        foo = true
                        class = 'test'
                    }
                }
            }
            ";
            sourceBuilder.Append(inp);

            var parser = new Parser();
            parser.SetDslFactory(new DslFactory());
            var ast = parser.Parse(sourceBuilder.ToString());
            var runtime = new Runtime(ast);
            // Console.WriteLine(ast);
            var outp = runtime.Evaluate("renderer");

            Console.WriteLine(outp);
        }
    }
}
