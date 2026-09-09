using MiMFa.Compiler.Model;

namespace MiMFa.Compiler.JavaScript
{
    public class Executor : MiMFa.Compiler.Executor.Executor
    {
        protected override string ExecuteCode(Node node)
        {
            // TODO: Should to impliment
            return node?.Token?.Value??"";
        }
    }
}
