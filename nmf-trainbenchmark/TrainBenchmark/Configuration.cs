using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainBenchmark
{
    class Configuration
    {
        [Option('r', "runs", Required = true, HelpText = "The number of runs that the benchmark should run")]
        public int Runs { get; set; }

        [Option('s', "size", Required = true, HelpText = "The size of the input model, needed for serialization")]
        public int Size { get; set; }

        [Option('q', "query", Required = true, HelpText = "The query that should be evaluated")]
        public string Query { get; set; }

        [Option('c', "changeSet", Required = true, HelpText = "A value indicating the change set (fixed or percentile)")]
        public string ChangeSet { get; set; }

        [Option('i', "iterationCount", Required = true, HelpText = "The number of iterations")]
        public int IterationCount { get; set; }

        [Option('t', "executionType", Required = true, HelpText = "How to execute the benchmark. Can be 'Batch', 'Immediate', 'Transaction', 'Parallel'")]
        public ExecutionType ExecutionType { get; set; }

        [Value(0, Required = true)]
        public string Target { get; set; }
    }

    enum ExecutionType
    {
        Batch,
        Immediate,
        Transaction,
        Parallel
    }
}
