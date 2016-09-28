using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMF.Models.Tests.Railway;
using NMF.Expressions;

namespace TrainBenchmark
{
    public class ParallelRepairEngine<T> : IncrementalRepairEngine<T>
    {
        private readonly ExecutionEngine parallelEngine = new ParallelArrayExecutionEngine();

        public override string Name => "NMF(Parallel)";

        public ParallelRepairEngine(QueryPattern<T> task) : base(task) { }

        public override void Repair(List<Action> actions)
        {
            var oldEngine = ExecutionEngine.Current;
            ExecutionEngine.Current = parallelEngine;
            ExecutionEngine.Current.BeginTransaction();
            foreach (var action in actions)
                action();
            ExecutionEngine.Current.CommitTransaction();
            ExecutionEngine.Current = oldEngine;
        }
    }
}
