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

        public Compiler(IStage[] stages, Options options = null, ResourceProvider io = null)
        {
            this.Options = options ?? new Options();
            this.ResourceProvider = io ?? new ResourceProvider();
            this.stages = new List<IStage>(stages ?? Array.Empty<IStage>());
        }

        public Output Compile(Input input)
        {
            Input = input;
            object data = input.Content;
            Output = new Output(Input.Source);
            //try
            {
                bool isfirst = string.IsNullOrEmpty(Input.Source);
                string source = System.IO.Path.GetFullPath("DaRQ");
                if (isfirst) OnLog("Compile started", LogStatus.Success);
                else OnLog($"Compiling the {Input.Source.Replace(source, ".\\DaRQ")}", LogStatus.Message);
                foreach (var stage in stages)
                {
                    //string sn = stage.GetType().Name;
                    //OnLog($"{sn} stage is started");
                    data = stage.Transform(data, this);
                    //OnLog($"{sn} stage is ended");
                    OnLog(" .", null);
                }
                Output.Content = data?.ToString() ?? string.Empty;
                if (isfirst) OnLog("Compile finished", LogStatus.Success);
                else OnLog(" ✔️ ", null);
                return Output;
            }
            //catch (Exception e)
            //{
            //    OnLog(" ❌ ", null);
            //    OnLog(e.Message, LogStatus.Error);
            //    return Output.Error(e);
            //}
        }

        public void OnLog(string message = "", LogStatus? status = LogStatus.Info, DateTime? time = null)
        {
            if (Log != null) Log(this, new LogEventArgs(message, status, time));
        }
    } 
}
