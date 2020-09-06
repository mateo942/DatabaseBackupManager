using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public class PipelineSetup
    {
        private readonly IList<KeyValuePair<Type, IPipelineCommand>> _pipelines = new List<KeyValuePair<Type, IPipelineCommand>>();
        public IDictionary<string, string> Variables { get; private set; }

        internal int index;

        public PipelineSetup AddStage(Type type, IPipelineCommand command)
        {
            _pipelines.Add(new KeyValuePair<Type, IPipelineCommand>(type, command));

            return this;
        }

        public PipelineSetup AddStage<IPipeline>(IPipelineCommand command)
        {
            var t = typeof(IPipeline);

            return AddStage(t, command);
        }

        public PipelineSetup AddVariables(IDictionary<string, string> variables)
        {
            Variables = variables;

            return this;
        }

        internal KeyValuePair<Type, IPipelineCommand>? Next()
        {
            if (index > _pipelines.Count - 1)
                return null;

            var a = _pipelines[index];
            index++;

            return a;
        }
    }

    public class PipelineManger
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PipelineManger> _logger;

        public PipelineManger(IServiceProvider serviceProvider, ILogger<PipelineManger> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task Execute(PipelineSetup pipelineSetup, CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                Variables variables = new Variables();
                variables.AddRange(pipelineSetup.Variables);

                KeyValuePair<Type, IPipelineCommand>? pipeline;
                while ((pipeline = pipelineSetup.Next()) != null)
                {
                    _logger.LogDebug("Running pipeline: {0}", pipeline.Value.Key.Name);

                    var i = (IPipeline)scope.ServiceProvider.GetRequiredService(pipeline.Value.Key);
                    var r = await i.Execute(pipeline.Value.Value, variables, cancellationToken);

                    _logger.LogDebug("Pipeline step: {0} ended, Success: {1}", pipeline.Value.Key.Name, r);

                    if (r == false && i.ContinuteWithError == false)
                        throw new Exception(string.Format("Pipeline: {0} error", pipeline.Value.Key.Name));
                }
            }
        }
    }
}
