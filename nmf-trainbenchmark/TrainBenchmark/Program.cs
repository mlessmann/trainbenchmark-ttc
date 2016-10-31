using CommandLine;
using NMF.Expressions.Linq;
using NMF.Models;
using NMF.Models.Repository;
using NMF.Models.Tests.Railway;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainBenchmark
{
    class Program
    {
        private static readonly QueryPattern<ISegment> posLength = new QueryPattern<ISegment>(
            rc => rc.Descendants().OfType<Segment>().Where(seg => seg.Length <= 0),
            segment => segment.Length = -segment.Length + 1,
            seg => string.Format("<segment : {0:0000}>", seg.Id.GetValueOrDefault()));

        private static QueryPattern<Tuple<IRoute, ISensor>> routeSensor = new QueryPattern<Tuple<IRoute, ISensor>>(
            rc => from route in rc.Routes.Concat(rc.Invalids.OfType<Route>())
                  from swP in route.Follows.OfType<SwitchPosition>()
                  where swP.Switch.Sensor != null && !route.DefinedBy.Contains<ISensor>(swP.Switch.Sensor)
                  select new Tuple<IRoute, ISensor>(route, swP.Switch.Sensor),
            match => match.Item1.DefinedBy.Add(match.Item2),
            match => string.Format("<route : {0:0000}, sensor : {1:0000}>",
                     match.Item1.Id.GetValueOrDefault(),
                     match.Item2.Id.GetValueOrDefault()));

        private static QueryPattern<Tuple<IRoute, IRoute>> semaphoreNeighbor = new QueryPattern<Tuple<IRoute, IRoute>>(
            rc => from route1 in rc.Routes.Concat(rc.Invalids.OfType<Route>())
                  from route2 in rc.Routes.Concat(rc.Invalids.OfType<Route>())
                  where route1 != route2 && route2.Entry != route1.Exit
                  from sensor1 in route1.DefinedBy
                  from te1 in sensor1.Elements
                  from te2 in te1.ConnectsTo
                  where te2.Sensor == null || route2.DefinedBy.Contains<ISensor>(te2.Sensor)
                  select new Tuple<IRoute, IRoute>(route2, route1),
            match => match.Item1.Entry = match.Item2.Exit,
            match => string.Format("<semaphore : {0:0000}, route1 : {1:0000}, route2 : {2:0000}>",
                     match.Item2.Exit.Id.GetValueOrDefault(),
                     match.Item2.Id.GetValueOrDefault(),
                     match.Item1.Id.GetValueOrDefault()));

        private static QueryPattern<ISwitch> switchSensor = new QueryPattern<ISwitch>(
            rc => rc.Descendants().OfType<ISwitch>().Where(sw => sw.Sensor == null),
            sw => sw.Sensor = new Sensor(),
            sw => string.Format("<sw : {0:0000}>", sw.Id.GetValueOrDefault()));

        private static QueryPattern<ISwitchPosition> switchSet = new QueryPattern<ISwitchPosition>(
            rc => from route in rc.Routes.Concat(rc.Invalids.OfType<Route>())
                  where route.Entry != null && route.Entry.Signal == Signal.GO
                  from swP in route.Follows
                  where swP.Switch.CurrentPosition != swP.Position
                  select swP,
            swP => swP.Switch.CurrentPosition = swP.Position,
            swP => string.Format("<semaphore : {0:0000}, route : {1:0000}, swP : {2:0000}, sw : {3:0000}>",
                   swP.Route.Entry.Id.GetValueOrDefault(),
                   swP.Route.Id.GetValueOrDefault(),
                   swP.Id.GetValueOrDefault(),
                   swP.Switch.Id.GetValueOrDefault()));

        private static readonly Stopwatch stopwatch = new Stopwatch();
        private static Configuration configuration;

        static void Main(string[] args)
        {
            try
            {
                ParseCommandLineArgs(args);
                var meta = MetaRepository.Instance;

                for (int run = 0; run < configuration.Runs; run++)
                {
                    var rnd = new Random(0);

                    // Read
#if !DEBUG
                    GC.Collect();
#endif
                    stopwatch.Start();
                    var model = LoadModel();
                    IRepairEngine engine = CreateRepairEngine();
                    engine.Read(model);
                    stopwatch.Stop();

                    Emit("read", engine.Name, run, 0, null);

                    // Check
#if !DEBUG
                    GC.Collect();
#endif
                    stopwatch.Restart();
                    var actions = engine.Check();
                    stopwatch.Stop();

                    Emit("check", engine.Name, run, 0, actions.Count());

                    for (int iter = 0; iter < configuration.IterationCount; iter++)
                    {
                        var fixes = FilterFixes(rnd, actions);

                        // Repair
#if !DEBUG
                        GC.Collect();
#endif
                        stopwatch.Restart();
                        engine.Repair(fixes);
                        stopwatch.Stop();

                        Emit("repair", engine.Name, run, iter, null);

                        // ReCheck
#if !DEBUG
                        GC.Collect();
#endif
                        stopwatch.Restart();
                        actions = engine.Recheck();
                        stopwatch.Stop();

                        Emit("recheck", engine.Name, run, iter, actions.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.ExitCode = 1;
            }
        }

        private static List<Action> FilterFixes(Random rnd, IEnumerable<Tuple<string, Action>> actions)
        {
            var actionsSorted = actions.OrderBy(t => t.Item1).Select(t => t.Item2).ToList();
            int numberOfFixes = configuration.ChangeSet == ChangeSet.@fixed ? 10 : actionsSorted.Count / 10;
            var fixes = new List<Action>(numberOfFixes);
            for (int i = 0; i < numberOfFixes && i < actionsSorted.Count; i++)
            {
                int index = rnd.NextInt(actionsSorted.Count);
                fixes.Add(actionsSorted[index]);
                actionsSorted.RemoveAt(index);
            }

            return fixes;
        }

        private static IRepairEngine CreateRepairEngine()
        {
            switch (configuration.Query)
            {
                case "PosLength":
                    return CreateRepairEngine(posLength);
                case "RouteSensor":
                    return CreateRepairEngine(routeSensor);
                case "SemaphoreNeighbor":
                    return CreateRepairEngine(semaphoreNeighbor);
                case "SwitchSensor":
                    return CreateRepairEngine(switchSensor);
                case "SwitchSet":
                    return CreateRepairEngine(switchSet);
                default:
                    throw new ArgumentException("Unknown query: " + configuration.Query);
            }
        }

        private static RepairEngine<T> CreateRepairEngine<T>(QueryPattern<T> query)
        {
            switch (configuration.ExecutionType)
            {
                case ExecutionType.Batch:
                    return new BatchRepairEngine<T>(query);
                case ExecutionType.Immediate:
                    return new ImmediateRepairEngine<T>(query);
                case ExecutionType.Parallel:
                    return new ParallelRepairEngine<T>(query);
                case ExecutionType.Transaction:
                    return new TransactionRepairEngine<T>(query);
                default:
                    throw new InvalidOperationException();
            }
        }

        private static RailwayContainer LoadModel()
        {
            var repository = new ModelRepository();
            var train = repository.Resolve(new Uri(new FileInfo(configuration.Target).FullName));
            return (RailwayContainer)train.Model.RootElements[0];
        }

        private static void ParseCommandLineArgs(string[] args)
        {
            var parseResults = Parser.Default.ParseArguments<Configuration>(args);
            if (parseResults.Errors.Any())
            {
                Environment.Exit(1);
            }
            configuration = parseResults.Value;
        }

        private static void Emit(string phase, string tool, int runIdx, int iteration, int? elements)
        {
            const string format = "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}";

            Console.WriteLine(format, configuration.ChangeSet, runIdx, tool, configuration.Size, configuration.Query, phase, iteration, "time", stopwatch.Elapsed.TotalMilliseconds * 1000000);
            Console.WriteLine(format, configuration.ChangeSet, runIdx, tool, configuration.Size, configuration.Query, phase, iteration, "memory", Environment.WorkingSet);
            if (elements != null)
            {
                Console.WriteLine(format, configuration.ChangeSet, runIdx, tool, configuration.Size, configuration.Query, phase, iteration, "rss", elements.Value);
            }
        }
    }
}
