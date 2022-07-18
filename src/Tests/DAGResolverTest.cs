// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace R6Tasks.Parallelizing.Tests
{
    public class DAGResolverTest : MonoBehaviour
    {
        private R6Parallelizer _resolver;
        
        private IEnumerator Start()
        {
            _resolver = new R6Parallelizer();
            _resolver.OnTaskCompleted += ResolverOnOnTaskCompleted;

            _resolver.AddTask(1, action(1, 6000), new[] {4, 5});
            _resolver.AddTask(2, action(2, 2000), new[] {5});
            _resolver.AddTask(3, action(3, 8000), new[] {6, 5});
            _resolver.AddTask(4, action(4, 5000), new[] {6});
            _resolver.AddTask(5, action(5, 4500), new[] {7, 8});
            _resolver.AddTask(6, action(6, 1000), new[] {7});
            _resolver.AddTask(7, action(7, 9000));
            _resolver.AddTask(8, action(8, 7000));

            _resolver.Resolve();

            yield return new WaitForSeconds(60f);
        }
        
        private Func<int, int, Action> action = (id, delay) => () => 
        {
            Debug.Log($"Starting operation {id} in Thread Id {Thread.CurrentThread.ManagedThreadId}...");
            Thread.Sleep(delay);
        };

        private void ResolverOnOnTaskCompleted(R6Task op)
        {
            Debug.Log($"Operation {op.Id} completed with time => {op.Start.Value}::{op.End.Value} in Thread Id {Thread.CurrentThread.ManagedThreadId}");
        }

        private void OnDestroy()
        {
            _resolver.Dispose();
        }
    }
}
