using NMF.Expressions;
using NMF.Models.Tests.Railway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainBenchmark
{
    public interface IRepairEngine
    {
        string Name { get; }

        void Read(RailwayContainer model);

        IEnumerable<Tuple<string, Action>> Check();

        IEnumerable<Tuple<string, Action>> Recheck();

        void Repair(List<Action> actions);
    }

    public abstract class RepairEngine<T> : IRepairEngine
    {
        private readonly QueryPattern<T> task;

        public abstract string Name { get; }

        public RepairEngine(QueryPattern<T> task)
        {
            this.task = task;
        }

        public void Read(RailwayContainer model)
        {
            Read(task.Query(model));
        }

        protected abstract void Read(IEnumerableExpression<T> graph);

        public abstract ICollection<T> Check();

        public abstract ICollection<T> Recheck();

        IEnumerable<Tuple<string, Action>> IRepairEngine.Check()
        {
            return Check().Select(i => new Tuple<string, Action>(task.SortKey(i), () => task.Repair(i)));
        }

        public abstract void Repair(List<Action> actions);

        IEnumerable<Tuple<string, Action>> IRepairEngine.Recheck()
        {
            return Recheck().Select(i => new Tuple<string, Action>(task.SortKey(i), () => task.Repair(i)));
        }
    }
}
