// Licensed to the .NET Foundation under one or more agreements.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Binding;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Internal.Common.Utils;
using Microsoft.Tools.Common;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tools.Stack.Model;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;

namespace Microsoft.Diagnostics.Tools.Stack
{
    internal static class ReportCommandHandler
    {
        delegate Task<int> ReportDelegate(CancellationToken ct, IConsole console, int processId, string name, TimeSpan duration, FileInfo nettrace, OutputFormat outputFormat, IEnumerable<FrameRenderFlags> hideFrame);

        /// <summary>
        /// Reports a stack trace
        /// </summary>
        /// <param name="ct">The cancellation token</param>
        /// <param name="console"></param>
        /// <param name="processId">The process to report the stack from.</param>
        /// <param name="name">The name of process to report the stack from.</param>
        /// <param name="duration">The duration of to trace the target for. </param>
        /// <returns></returns>
        private static async Task<int> Report(CancellationToken ct, IConsole console, int processId, string name, TimeSpan duration, FileInfo nettrace, OutputFormat outputFormat, IEnumerable<FrameRenderFlags> frameFlags)
        {
            string tempNetTraceFilename = "";
            string tempEtlxFilename = "";

            try
            {
                // This will either be the file name passed, or a temporary one (which must be deleted).
                // We must not delete the file if passed.
                string netTraceFilename;

                if (nettrace != null)
                {
                    netTraceFilename = nettrace.FullName;
                }
                else
                {
                    tempNetTraceFilename = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".nettrace");
                    netTraceFilename = tempNetTraceFilename;

                    // Either processName or processId has to be specified.
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (processId != 0)
                        {
                            Console.WriteLine("Can only specify either --name or --process-id option.");
                            return -1;
                        }

                        processId = CommandUtils.FindProcessIdWithName(name);
                        if (processId < 0)
                        {
                            return -1;
                        }
                    }

                    if (processId < 0)
                    {
                        console.Error.WriteLine("Process ID should not be negative.");
                        return -1;
                    }
                    else if (processId == 0)
                    {
                        console.Error.WriteLine("--process-id is required");
                        return -1;
                    }


                    var client = new DiagnosticsClient(processId);
                    var providers = new List<EventPipeProvider>()
                    {
                        new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational)
                    };

                    // collect a *short* trace with stack samples
                    // the hidden '--duration' flag can increase the time of this trace in case 10ms
                    // is too short in a given environment, e.g., resource constrained systems
                    // N.B. - This trace INCLUDES rundown.  For sufficiently large applications, it may take non-trivial time to collect
                    //        the symbol data in rundown.
                    using (EventPipeSession session = client.StartEventPipeSession(providers))
                    using (FileStream fs = File.OpenWrite(tempNetTraceFilename))
                    {
                        Task copyTask = session.EventStream.CopyToAsync(fs);
                        await Task.Delay(duration);
                        session.Stop();

                        // check if rundown is taking more than 5 seconds and add comment to report
                        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                        Task completedTask = await Task.WhenAny(copyTask, timeoutTask);
                        if (completedTask == timeoutTask)
                        {
                            console.Out.WriteLine(
                                $"# Sufficiently large applications can cause this command to take non-trivial amounts of time");
                        }

                        await copyTask;
                    }
                }

                // using the generated trace file, symbolicate and compute stacks.
                tempEtlxFilename = TraceLog.CreateFromEventPipeDataFile(netTraceFilename);
                using (var symbolReader = new SymbolReader(System.IO.TextWriter.Null) { SymbolPath = SymbolPath.MicrosoftSymbolServerPath })
                using (var eventLog = new TraceLog(tempEtlxFilename))
                {
                    var stackSource = new MutableTraceEventStackSource(eventLog)
                    {
                        OnlyManagedCodeStacks = true
                    };

                    var computer = new SampleProfilerThreadTimeComputer(eventLog, symbolReader);
                    computer.GenerateThreadTimeStacks(stackSource);
                    var stacksForThread = new Dictionary<int, List<List<IStackFrame>>>();

                    stackSource.ForEach((sample) =>
                    {
                        var stackIndex = sample.StackIndex;
                        var stack = new List<IStackFrame>();
                        
                        string frameName = stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false);
                        while (!frameName.StartsWith("Thread ("))
                        {
                            stack.Add(new TraceEventStackFrame(frameName));
                            stackIndex = stackSource.GetCallerIndex(stackIndex);
                            frameName = stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false);
                        }

                        // long form for: int.Parse(threadFrame["Thread (".Length..^1)])
                        // Thread id is in the frame name as "Thread (<ID>)"
                        string template = "Thread (";
                        string threadFrame = stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false);
                        int threadId = int.Parse(threadFrame.Substring(template.Length, threadFrame.Length - (template.Length + 1)));

                        if (stacksForThread.TryGetValue(threadId, out var stacks))
                        {
                            stacks.Add(stack);
                        }
                        else
                        {
                            stacksForThread[threadId] = new List<List<IStackFrame>>() { stack };
                        }
                    });

                    var flags = frameFlags.Aggregate((a, b) => a | b);
                    IRenderer visitor;
                    ParallelStack root;
                    switch (outputFormat)
                    {
                        case OutputFormat.Stacks:
                            // For every thread recorded in our trace, print the first stack
                            foreach (var (threadId, samples) in stacksForThread)
                            {
#if DEBUG
                                console.Out.WriteLine($"Found {samples.Count} stacks for thread 0x{threadId:X}");
#endif
                                console.Out.WriteLine($"Thread (0x{threadId:X}):");
                                foreach (IStackFrame frame in samples[0])
                                {
                                    console.Out.WriteLine($"  {frame.Text}".Replace("UNMANAGED_CODE_TIME", "[Native Frames]"));
                                }
                                console.Out.WriteLine();
                            }
                            break;
                        case OutputFormat.ParallelStacks:
                            root = GetParallelStack(stacksForThread);
                            visitor = new ColorConsoleRenderer(console, limit: 4, flags);
                            console.Out.WriteLine("");
                            foreach (var stack in root.Stacks)
                            {
                                console.Out.Write("________________________________________________");
                                stack.Render(visitor);
                                console.Out.WriteLine("");
                                console.Out.WriteLine("");
                                console.Out.WriteLine("");
                            }
                            break;
                        case OutputFormat.MermaidClassDiagram:
                            root = GetParallelStack(stacksForThread);
                            visitor = new ColorConsoleRenderer(console, limit: 4, flags);
                            MermaidClassDiagramRenderer.Render(root, visitor);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {ex.ToString()}");
                return -1;
            }
            finally
            {
                if (File.Exists(tempNetTraceFilename))
                    File.Delete(tempNetTraceFilename);
                if (File.Exists(tempEtlxFilename))
                    File.Delete(tempEtlxFilename);
            }
            return 0;
        }

        private static ParallelStack GetParallelStack(Dictionary<int, List<List<IStackFrame>>> stacksForThread)
        {
            ParallelStack root = new ParallelStack();

            // For every thread recorded in our trace, print the first stack
            foreach (var (threadId, samples) in stacksForThread)
            {
                // Reverse so first item in the list is the root
                samples[0].Reverse();
                root.AddStack((uint)threadId, samples[0]);
            }

            return root;
        }

        private static void PrintStack(IConsole console, int threadId, StackSourceSample stackSourceSample, StackSource stackSource)
        {
            console.Out.WriteLine($"Thread (0x{threadId:X}):");
            var stackIndex = stackSourceSample.StackIndex;
            while (!stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), verboseName: false).StartsWith("Thread ("))
            {
                console.Out.WriteLine($"  {stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), verboseName: false)}"
                    .Replace("UNMANAGED_CODE_TIME", "[Native Frames]"));
                stackIndex = stackSource.GetCallerIndex(stackIndex);
            }
            console.Out.WriteLine();
        }

        public static Command ReportCommand() =>
            new Command(
                name: "report",
                description: "reports the managed stacks from a running .NET process or nettrace file") 
            {
                // Handler
                HandlerDescriptor.FromDelegate((ReportDelegate)Report).GetCommandHandler(),
                // Options
                ProcessIdOption(),
                NameOption(),
                DurationOption(),
                NetTraceOption(),
                OutputFormatOption(),
                HideFrameOption()
            };

        static Option DurationOption() =>
            new Option(
                alias: "--duration",
                description: @"When specified, will trace for the given timespan and then automatically stop the trace. Provided in the form of dd:hh:mm:ss.")
            {
                Argument = new Argument<TimeSpan>(name: "duration-timespan", getDefaultValue: () => TimeSpan.FromMilliseconds(10)),
                IsHidden = true
            };

        static Option ProcessIdOption() =>
            new Option(
                aliases: new[] { "-p", "--process-id" },
                description: "The process id to report the stack.")
            {
                Argument = new Argument<int>(name: "pid")
            };

        static Option NameOption() =>
            new Option(
                aliases: new[] { "-n", "--name" },
                description: "The name of the process to report the stack.");
        
        static Option NetTraceOption() =>
            new Option(
                aliases: new[] { "-t", "--nettrace" },
                description: "The name of the .nettrace file to report the stack.")
            {
                Argument = new Argument<FileInfo>
                {
                    Description = "The .nettrace file to read the stacks from.",
                    Arity = new ArgumentArity(1, 1),
                }.ExistingOnly()
            };

        static Option OutputFormatOption() =>
            new Option(
                aliases: new[] { "-o", "--output-format" },
                description: "The output format of the stack trace.")
            {
                Argument = new Argument<OutputFormat>(getDefaultValue:() => OutputFormat.Stacks)
            };

        static Option HideFrameOption() =>
            new Option(
                aliases: new[] { "-h", "--frame-flags" },
                description: "Hides part(s) of the frame."){
                Argument = new Argument<IEnumerable<FrameRenderFlags>>(() => new List<FrameRenderFlags>()
                    {
                        FrameRenderFlags.RenderAll
                    })
                {
                    Arity = ArgumentArity.OneOrMore,
                }
            };
    }
}
