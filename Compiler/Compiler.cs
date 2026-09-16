using MiMFa.Compiler;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Resource;
using System;
using System.Collections.Generic;

namespace MiMFa.Compiler
{
    public class Compiler
    {
        protected readonly IList<IStage> stages;

        public Core.Version Version { get; } = new Core.Version(1);

        public Options Options { get; }

        public event LogEventHandler Log = null;
        public Input Input { get; set; }
        public Output Output { get; set; }
        public ResourceProvider ResourceProvider { get; set; }

        public Compiler(IStage[] stages, Options options = null, ResourceProvider resourceProvider = null)
        {
            this.stages = new List<IStage>(stages ?? Array.Empty<IStage>());
            this.Options = options ?? new Options();
            this.ResourceProvider = resourceProvider ?? new ResourceProvider();
        }

        public Output Compile(Input input)
        {
            bool isfirst = Input == null;
            Input = input;
            object data = input.Content;
            Output = new Output(Input.Source);
            string sn = null;
            if (isfirst) OnLog("Compile started", LogStatus.Subject);
            try
            {
                string source = System.IO.Path.GetFullPath("DaRQ");
                if (!string.IsNullOrWhiteSpace(Input.Source)) OnLog($"Compiling the {Input.Source.Replace(source, ".\\DaRQ")}", LogStatus.Message);
                foreach (var stage in stages)
                {
                    sn = stage.GetType().Name;
                    if (isfirst) OnLog($"{sn} stage is checking...", LogStatus.Info);
                    data = stage.Transform(data, this);
                    if (isfirst) OnLog($"{sn} stage tasks completed. ✔️ ", LogStatus.Success);
                    else OnLog(" .", null);
                }
                Output.Content = data?.ToString() ?? string.Empty;
                if (!isfirst) OnLog(" ✔️ ", null);
                return Output;
            }
            catch (Exception e)
            {
                if (isfirst) OnLog($"{sn} stage is not completed! ❌ ", LogStatus.Error);
                else OnLog($" ❌ ", null);
                OnLog(e.Message, LogStatus.Error);
                return Output.Error(e);
            }
            finally {
                if (isfirst) OnLog("Compile finished", LogStatus.Subject);
            }
        }

        public void OnLog(string message = "", LogStatus? status = LogStatus.Info, DateTime? time = null)
        {
            if (Log != null) Log(this, new LogEventArgs(message, status, time));
        }
    } 
}
