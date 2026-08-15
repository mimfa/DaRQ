// Converted from src/engine/DaRQ/Compiler/index.ts
using System;
using System.Collections.Generic;

namespace DaRQ.Compiler
{
    public class Compiler
    {
        protected readonly IList<IStage> stages;

        public Version Version { get; } = new Version(1);

        public Options Options { get; }

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
            this.Input = input;
            var data = input.Content;
            var errors = new List<string>();
            try
            {
                int sn = 0;
                foreach (var stage in stages)
                {
                    Console.WriteLine($"Stage {++sn} is started");
                    data = stage.Transform(data, this);
                    Console.WriteLine($"Stage {sn} is ended");
                }
                return this.Output = new Output(data?.ToString() ?? string.Empty, this.Input.Source, errors.ToArray());
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                errors.Add(e.Message);
                return this.Output = new Output(string.Empty, this.Input.Source, errors.ToArray());
            }
        }
    }
}
