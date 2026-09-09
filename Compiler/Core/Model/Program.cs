namespace MiMFa.Compiler.Model
{
    public class Program : Node
    {
        public Program(string source = null, params Node[] children) : base(new Token(TokenType.PathData, source), NodeType.Program, children: children)
        {
        }
    }
}