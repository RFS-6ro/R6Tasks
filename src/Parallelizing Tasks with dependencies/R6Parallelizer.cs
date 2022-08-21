// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Actress;
using Actress.Tests;
using JetBrains.Annotations;

namespace R6Tasks.Parallelizing
{
    public class R6Parallelizer : IDisposable
    {
        private readonly MailboxProcessor<TaskMessage> _inbox;

        private readonly CancellationTokenSource _cts;
        
        public event Action<R6Task> OnTaskCompleted;

        public R6Parallelizer(CancellationToken? token = null)
        {
            if (token == null)
            {
                _cts = new CancellationTokenSource();
                token = _cts.Token;
            }
            
            _inbox = MailboxProcessor.Start<TaskMessage>(Loop, _cts);

            _inbox.Errors.Subscribe(new ExceptionObserver());
        }

        [PublicAPI]
        public void AddTask(int id, Action task, int[] edges = null)
        {
            R6Task info = new R6Task
            {
                Context = ExecutionContext.Capture(),
                Edges = edges ?? Array.Empty<int>(),
                Id = id,
                Task = task,
                RemainingEdgesCount = Option.OfNone<int>(),
                Start = Option.OfNone<DateTimeOffset>(),
                End = Option.OfNone<DateTimeOffset>()
            };

            _inbox.Post(new AddTask(id, info));
        }

        [PublicAPI]
        public void Resolve()
        {
            _inbox.Post(new ExecuteTask());
        }

        private async Task Loop(MailboxProcessor<TaskMessage> inbox)
        {
            await LoopImpl(inbox, new Dictionary<int, R6Task>(), new Dictionary<int, List<int>>());
        }

        private async Task LoopImpl(MailboxProcessor<TaskMessage> inbox, Dictionary<int, R6Task> tasks, Dictionary<int, List<int>> edges)
        {
            TaskMessage currentMessage = await inbox.Receive();
            
            if (currentMessage is ExecuteTask)
            {
                VerifyOperationsRegistering(tasks);
                VerifyNoCycles(tasks);
                
                Dictionary<int,List<int>> dependenciesFromTo = new Dictionary<int, List<int>>();
                Dictionary<int, R6Task> remainingTasks = new Dictionary<int, R6Task>();

                foreach (KeyValuePair<int, R6Task> task in tasks)
                {
                    R6Task taskCopy = new R6Task(task.Value);
                    taskCopy.RemainingEdgesCount = Option.Some(taskCopy.Edges.Length);
                    
                    foreach (int from in taskCopy.Edges)
                    {
                        if (!dependenciesFromTo.TryGetValue(from, out List<int> dependencies))
                        {
                            dependenciesFromTo.Add(from, new List<int> {taskCopy.Id});
                        }
                        else
                        {
                            dependencies.Insert(0, taskCopy.Id);
                            dependenciesFromTo[from] = dependencies;
                        }
                    }

                    remainingTasks.Add(task.Key, taskCopy);
                }

                void QueueRemaining()
                {
                    remainingTasks
                        .Where((kv) => kv.Value.RemainingEdgesCount.Value is 0)
                        .ToList()
                        .ForEach((kv) => inbox.Post(new QueueTask(kv.Value)));
                }

                QueueRemaining();
                
                await LoopImpl(inbox, remainingTasks, dependenciesFromTo);
            }
            else if (currentMessage is QueueTask queueTaskMessage)
            {
#pragma warning disable 4014
                Task.Run(() =>
#pragma warning restore 4014
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    
                    DateTimeOffset start = DateTimeOffset.Now;
#if DEBUG
                    Debug.Assert(queueTaskMessage.TaskInfo != null, "queueTask.TaskInfo != null");
#endif
                    R6Task task = queueTaskMessage.TaskInfo.Value;

                    if (task.Context == null)
                    {
                        task.Task();
                    }
                    else
                    {
                        void Callback(object x)
                        {
                            ((R6Task) x).Task();
                        }

                        ExecutionContext.Run(task.Context.CreateCopy(), Callback, task);
                    }

                    DateTimeOffset end = DateTimeOffset.Now;

                    task.Start = Option.Some(start);
                    task.End = Option.Some(end);
                    OnTaskCompleted?.Invoke(task);

                    if (edges.TryGetValue(task.Id, out List<int> lstDependencies) && lstDependencies.Count > 0)
                    {
                        List<R6Task> dependentOperations = GetDependentOperations(lstDependencies, tasks);
                        edges.Remove(task.Id);

                        dependentOperations.ForEach((nestedOperation) => inbox.Post(new QueueTask(nestedOperation)));
                    }
                }, 
                cancellationToken: _cts.Token);

                await LoopImpl(inbox, tasks, edges);
            }
            else if (currentMessage is AddTask addTaskMessage)
            {
#if DEBUG
                Debug.Assert(addTaskMessage.TaskInfo != null, "addTask.TaskInfo != null");
#endif
                tasks.Add(addTaskMessage.Id, addTaskMessage.TaskInfo.Value);
                await LoopImpl(inbox, tasks, edges);
            }
        }

        
        
        private static void VerifyOperationsRegistering(Dictionary<int, R6Task> tasks)
        {
            HashSet<int> notRegisteredTasks = GetNotRegisteredTasks(tasks);
            if (notRegisteredTasks.Count > 0)
            {
#if DEBUG
                StringBuilder builder = new StringBuilder();
                builder.Append("Missing operation: ");
                foreach (int id in notRegisteredTasks)
                {
                    builder.Append("[");
                    builder.Append(id);
                    builder.Append("]");
                }

                throw new InvalidOperationException(builder.ToString());
#else
                throw new InvalidOperationException("Missing operations detected");
#endif
            }
        }

        private static HashSet<int> GetNotRegisteredTasks(Dictionary<int, R6Task> tasks)
        {
            HashSet<int> collection = new HashSet<int>();
            foreach (R6Task task in tasks.Values)
            {
                foreach (int edge in task.Edges)
                {
                    if (!tasks.ContainsKey(edge))
                    {
                        collection.Add(edge);
                    }
                }
            }
            return collection;
        }
        
        
        
        private static void VerifyNoCycles(Dictionary<int, R6Task> operations)
        {
            if (VerifyTopologicalSort(operations) == null)
            {
                throw new InvalidOperationException("Cycle detected");
            }
        }

        private static Option<List<int>> VerifyTopologicalSort(Dictionary<int, R6Task> tasks)
        {
            Dictionary<int, List<int>> tasksToFrom = new Dictionary<int, List<int>>(tasks.Values.Count);
            Dictionary<int, List<int>> tasksFromTo = new Dictionary<int, List<int>>(tasks.Values.Count);

            foreach (R6Task task in tasks.Values)
            {
                tasksToFrom.Add(task.Id, new List<int>(task.Edges));

                foreach (int edge in task.Edges)
                {
                    if (!tasksFromTo.TryGetValue(edge, out _))
                    {
                        tasksFromTo.Add(edge, new List<int>());
                    }
                    tasksFromTo[edge].Add(task.Id);
                }
            }
            
            List<int> partialOrderingIds = new List<int>(tasksToFrom.Count);
            List<int> iterationIds = new List<int>(tasksToFrom.Count);
            return BuildOrdering(tasksToFrom, tasksFromTo, partialOrderingIds, iterationIds);
        }

        private static Option<List<int>> BuildOrdering
        (
            Dictionary<int, List<int>> tasksToFrom, 
            Dictionary<int, List<int>> tasksFromTo, 
            List<int> partialOrderingIds, 
            List<int> iterationIds
        )
        {
            while (true)
            {
                switch (tasksToFrom.Count)
                {
                    case 0:
                        return Option.Some(partialOrderingIds);
                }
                
                iterationIds.Clear();

                foreach (KeyValuePair<int, List<int>> task in tasksToFrom.Where(task => task.Value.Count == 0))
                {
                    iterationIds.Add(task.Key);
                    
                    if (!tasksFromTo.TryGetValue(task.Key, out var taskList))
                    {
                        continue;
                    }

                    foreach (int id in taskList)
                    {
                        _ = tasksToFrom[id].Remove(task.Key);   
                    }
                }
                
                if (iterationIds.Count == 0)
                {
                    return null;
                }

                foreach (var iterationId in iterationIds)
                {
                    _ = tasksToFrom.Remove(iterationId);
                }
                
                partialOrderingIds.AddRange(iterationIds);
            }
        }

        
        
        private static List<R6Task> GetDependentOperations(List<int> dependencies, Dictionary<int, R6Task> tasks)
        {
            if (dependencies.Count == 0) { return new List<R6Task>(0); }
            
            var headId = dependencies[0];
            
            int[] dependenciesTemp = dependencies.ToArray();
            int[] tailTemp = new int[dependenciesTemp.Length - 1];
            Array.Copy(dependenciesTemp, 1, tailTemp, 0, tailTemp.Length);
            var tail = tailTemp.ToList();

            var head = new R6Task(tasks[headId])
            {
                RemainingEdgesCount = TryReduceSome(tasks[headId].RemainingEdgesCount)
            };
            
            tasks[headId] = head;

            List<R6Task> result;

            switch (tasks[headId].RemainingEdgesCount.Value)
            {
                case 0:
                    result = new List<R6Task> { tasks[headId] };
                    result.AddRange(GetDependentOperations(tail, tasks));
                    break;
                default:
                    result = GetDependentOperations(tail, tasks);
                    break;
            }

            return result;
        }

        private static Option<int> TryReduceSome(Option<int> option)
        {
            if (option.IsNone) { return null; }
            
            return Option.Some(option.Value - 1);
        }

        #region Dispose
        
        ~R6Parallelizer()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _inbox?.Dispose();
            }
        }

        private void ReleaseUnmanagedResources() { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        #endregion
        
        private class ExceptionObserver : IObserver<Exception>
        {
            public void OnCompleted()
            {
                
            }

            public void OnError(Exception error)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogException(error);
#endif
            }

            public void OnNext(Exception value)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogException(value);
#endif
            }
        }
    }
}
