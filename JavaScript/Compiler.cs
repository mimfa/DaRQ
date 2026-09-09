using MiMFa.Compiler.Resource;

namespace MiMFa.Compiler.JavaScript
{
    public class Compiler : MiMFa.Compiler.Compiler
    {
        public Compiler(Options options = null) : base(new IStage[] {
            new Tokenizer(),
            new Preprocessor(),
            new Parser(),
            new Assembler(),
            new Generator()
        }, options ?? new Options(), new ResourceProvider())
        {
        }
    }
}
