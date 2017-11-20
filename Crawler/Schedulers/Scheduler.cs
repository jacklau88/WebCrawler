﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Crawler.Schedulers
{
    public class Scheduler<T> : IScheduler
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly List<T> _stack = new List<T>();

        object IScheduler.Pop()
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                return Pop();
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public virtual T Pop()
        {
            if (_stack.Count == 0)
                return default(T);

            var site = _stack.FirstOrDefault();

            _lock.EnterWriteLock();
            try
            {
                _stack.RemoveAt(0);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            return site;
        }

        void IScheduler.Push(object @object)
        {
            _lock.EnterWriteLock();
            try
            {
                if (@object is T requestObject)
                    _stack.Add(requestObject);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public long Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _stack.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public virtual void Push(T requestSite)
        {
            _stack.Add(requestSite);
        }

        ~Scheduler()
        {
            _lock?.Dispose();
        }
    }
}