using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMF.Models.Tests.Railway;
using NMF.Expressions;

namespace TrainBenchmark
{
    public class BatchRepairEngine<T> : RepairEngine<T>
    {
        private IEnumerableExpression<T> graph;

        public override string Name => "NMF(Batch)";

        public BatchRepairEngine(QueryPattern<T> task) : base(task) { }

        public override ICollection<T> Check()
        {
            return graph.ToList();
        }

        protected override void Read(IEnumerableExpression<T> graph)
        {
            this.graph = graph;
        }

        public override ICollection<T> Recheck()
        {
            return Check();
        }

        public override void Repair(List<Action> actions)
        {
            foreach (var action in actions)
                action();
        }
    }
}
