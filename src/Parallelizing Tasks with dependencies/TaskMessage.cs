// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

namespace R6Tasks.Parallelizing
{
    public class TaskMessage
    {
        public int Id = -1;
        public R6Task? TaskInfo = null;
    }

    public class AddTask : TaskMessage
    {
        public AddTask(int id, R6Task r6Task)
        {
            Id = id;
            TaskInfo = r6Task;
        }
    }

    public class QueueTask : TaskMessage
    {
        public QueueTask(R6Task r6Task)
        {
            TaskInfo = r6Task;
        }
    } 

    public class ExecuteTask : TaskMessage
    {
        
    }
}
