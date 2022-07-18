// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Threading;
using Actress.Tests;

namespace R6Tasks.Parallelizing
{
    public struct R6Task
    {
        public R6Task(R6Task other)
        {
            Context = other.Context;
            Id = other.Id;
            Edges = new int[other.Edges.Length];
            Array.Copy(other.Edges, Edges, other.Edges.Length);
            Task = other.Task;
            RemainingEdgesCount = other.RemainingEdgesCount;
            Start = other.Start;
            End = other.End;
        }

        public ExecutionContext Context { get; set; }

        public int Id { get; set; }
        
        public int[] Edges { get; set; }

        public Action Task { get; set; }
        
        public Option<int> RemainingEdgesCount { get; set; }
        
        public Option<DateTimeOffset> Start { get; set; }
        
        public Option<DateTimeOffset> End { get; set; }
    }
}
