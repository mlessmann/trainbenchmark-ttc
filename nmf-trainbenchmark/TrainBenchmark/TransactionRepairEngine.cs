using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMF.Models.Tests.Railway;
using NMF.Expressions;

namespace TrainBenchmark
{
    public class TransactionRepairEngine<T> : IncrementalRepairEngine<T>
    {
        public override string Name => "NMF(Transaction)";

        public TransactionRepairEngine(QueryPattern<T> task) : base(task) { }

        public override void Repair(List<Action> actions)
        {
            ExecutionEngine.Current.BeginTransaction();
            foreach (var action in actions)
                action();
            ExecutionEngine.Current.CommitTransaction();
        }
    }
}
