namespace MiMFa.Compiler.DaRQ
{
    public class Assembler : MiMFa.Compiler.JavaScript.Assembler
    {
        // DaRQ-specific assembler inherits JavaScript assembler behavior.
        public new Compiler Compiler { get; set; }

        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler == null ? Compiler : compiler as Compiler);
            return true;
        }
    }
}
