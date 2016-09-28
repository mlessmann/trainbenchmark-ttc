using NMF.Expressions;
using NMF.Models.Tests.Railway;
using System;
using System.Collections.Generic;

namespace TrainBenchmark
{
    public class QueryPattern<T>
    {
        public Func<RailwayContainer, IEnumerableExpression<T>> Query { get; }

        public Action<T> Repair { get; }

        public Func<T, string> SortKey { get; }

        public QueryPattern(Func<RailwayContainer, IEnumerableExpression<T>> query,
            Action<T> repair, Func<T, string> sortKey)
        {
            Query = query;
            Repair = repair;
            SortKey = sortKey;
        }
    }
}