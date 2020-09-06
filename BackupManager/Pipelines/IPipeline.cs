using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupManager.Pipelines
{
    public interface IPipeline
    {
        bool ContinuteWithError { get; }

        Task<bool> Execute(object command, Variables variables, CancellationToken cancellationToken);
    }

    public abstract class PipelineBase<TCommand> : IPipeline
    {
        public virtual bool ContinuteWithError => false;

        public async Task<bool> Execute(object command, Variables variables, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Execute((TCommand)command, variables, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public abstract Task Execute(TCommand command, Variables variables, CancellationToken cancellationToken);
    }
}
