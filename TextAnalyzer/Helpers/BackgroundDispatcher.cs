using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TextAnalyzer.Helpers
{
    public class BackgroundDispatcher
    {
        private static object _lockObject = new object();
        private static BackgroundDispatcher? _defaultDispatcher = null;
        public static BackgroundDispatcher DefaultDispatcher
        {
            get
            {
                if (_defaultDispatcher == null)
                {
                    lock (_lockObject)
                    {
                        if (_defaultDispatcher == null)
                        {
                            _defaultDispatcher = new BackgroundDispatcher();
                        }
                    }
                }
                return _defaultDispatcher;
            }
        }

        public static BackgroundDispatcher NewInstance
        {
            get { return new BackgroundDispatcher(); }
        }

        private class TaskWithCancelToken : IDisposable
        {
            public Task Task { get; private set; }
            public CancellationTokenSource CancelTokenSource { get; private set; }

            public TaskWithCancelToken(IWorkItem workItem)
            {
                CancelTokenSource = new CancellationTokenSource();
                Task = new Task(workItem.ExecuteWorkItem, CancelTokenSource.Token);
            }

            public void Dispose()
            {
                try
                {
                    CancelTokenSource?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.Write($"Dispose CancelTokenSource exception: {e.Message}");
                }
            }
        }

        private interface IWorkItem
        {
            void ExecuteWorkItem();
        }

        private class ActionWorkItem : IWorkItem
        {
            private Action _action;
            public ActionWorkItem(Action action)
            {
                _action = action;
            }

            public void ExecuteWorkItem()
            {
                _action();
            }
        }

        private class DelegateWorkItem : IWorkItem
        {
            private Delegate _delegate;
            private object[] _args;
            public DelegateWorkItem(Delegate method, object[] args)
            {
                _delegate = method;
                _args = args;
            }

            public void ExecuteWorkItem()
            {
                _delegate.DynamicInvoke(_args);
            }
        }

        private Queue<TaskWithCancelToken> _pendingTasks = new();
        private TaskWithCancelToken? _workingTask = null;

        public bool IsExecuting { get; set; }

        public Task BeginInvoke(Action callback)
        {
            return QueueWorkItem(new ActionWorkItem(callback));
        }

        public Task BeginInvoke(Delegate method, params Object[] args)
        {
            return QueueWorkItem(new DelegateWorkItem(method, args));
        }

        public void CancelAllTasks()
        {
            try
            {
                _workingTask?.CancelTokenSource.Cancel();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Cancel working task exception: {e.Message}");
            }

            CancelAllPendingTasks();
        }

        public void CancelAllPendingTasks()
        {
            lock (_pendingTasks)
            {
                foreach (var workItem in _pendingTasks)
                {
                    workItem.CancelTokenSource.Cancel();
                    workItem.Dispose();
                }

                _pendingTasks.Clear();
            }
        }

        private void MethodWaitCallback(object? state)
        {
            _workingTask = state as TaskWithCancelToken;
            while (_workingTask != null)
            {
                Task task = _workingTask.Task;
                task?.RunSynchronously();
                _workingTask.Dispose();

                lock (_pendingTasks)
                {
                    if (_pendingTasks.Count > 0)
                    {
                        _workingTask = _pendingTasks.Dequeue();
                    }
                    else
                    {
                        _workingTask = null;
                        IsExecuting = false;
                    }
                }
            }
        }

        private Task QueueWorkItem(IWorkItem workItem)
        {
            var newTask = new TaskWithCancelToken(workItem);

            TaskWithCancelToken? taskToExecute = null;
            lock (_pendingTasks)
            {
                _pendingTasks.Enqueue(newTask);
                if (!IsExecuting)
                {
                    taskToExecute = _pendingTasks.Dequeue();
                    IsExecuting = true;
                }
            }

            if (taskToExecute != null)
            {
                ThreadPool.QueueUserWorkItem(
                    new WaitCallback(MethodWaitCallback), taskToExecute);
            }

            return newTask.Task;
        }
    }
}
