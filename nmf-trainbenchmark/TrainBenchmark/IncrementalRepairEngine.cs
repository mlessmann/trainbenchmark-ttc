using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NMF.Models.Tests.Railway;
using NMF.Expressions;
using System.Collections.Specialized;

namespace TrainBenchmark
{
    public abstract class IncrementalRepairEngine<T> : RepairEngine<T>
    {
        protected INotifyEnumerable<T> graph;
        private HashSet<T> results;
        private List<NotifyCollectionChangedEventArgs> eventArgs = new List<NotifyCollectionChangedEventArgs>();

        public IncrementalRepairEngine(QueryPattern<T> task) : base(task) { }

        protected override void Read(IEnumerableExpression<T> graph)
        {
            this.graph = graph.AsNotifiable();
        }

        public override ICollection<T> Check()
        {
            results = new HashSet<T>(graph);
            graph.CollectionChanged += (obj, e) => eventArgs.Add(e);
            return results;
        }

        public override ICollection<T> Recheck()
        {
            foreach(var e in eventArgs)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    throw new NotImplementedException();

                if (e.OldItems != null)
                {
                    foreach (T item in e.OldItems)
                        results.Remove(item);
                }

                if (e.NewItems != null)
                {
                    foreach (T item in e.NewItems)
                        results.Add(item);
                }
            }

            eventArgs.Clear();
            return results;
        }
    }
}
