using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMF.Models.Tests.Railway;

namespace TrainBenchmark
{
    public class ImmediateRepairEngine<T> : IncrementalRepairEngine<T>
    {
        public override string Name => "NMF(Immediate)";

        public ImmediateRepairEngine(QueryPattern<T> task) : base(task) { }

        public override void Repair(List<Action> actions)
        {
            foreach (var action in actions)
                action();
        }
    }
}
