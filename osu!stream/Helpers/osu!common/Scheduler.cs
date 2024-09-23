﻿using System;
using System.Diagnostics;
using System.Threading;

namespace osum.Helpers
{
    //marshals delegates to run from the scheduler's basethread in a threadsafe manner
    public class Scheduler
    {
        private readonly Queue<VoidDelegate> schedulerQueue = new Queue<VoidDelegate>();
        private readonly pList<ScheduledDelegate> timedQueue = new pList<ScheduledDelegate>(null, true);
        private readonly int mainThreadID;
        private readonly Stopwatch timer = new Stopwatch();

        //we assume that basethread calls the constructor
        public Scheduler()
        {
            mainThreadID = Thread.CurrentThread.ManagedThreadId;
            timer.Start();
        }

        //run scheduled events
        public void Update()
        {
            VoidDelegate[] runnable;

            lock (schedulerQueue)
            {
                long currentTime = timer.ElapsedMilliseconds;

                while (timedQueue.Count > 0 && timedQueue[0].ExecuteTime <= currentTime)
                {
                    schedulerQueue.Enqueue(timedQueue[0].Task);
                    timedQueue.RemoveAt(0);
                }

                int c = schedulerQueue.Count;
                if (c == 0) return;

                runnable = new VoidDelegate[c];
                schedulerQueue.CopyTo(runnable, 0);
                schedulerQueue.Clear();
            }

            foreach (VoidDelegate v in runnable)
            {
                VoidDelegate mi = v;
                mi.Invoke();
            }
        }

        public ScheduledDelegate Add(VoidDelegate d, int timeUntilRun)
        {
            lock (schedulerQueue)
            {
                ScheduledDelegate del = new ScheduledDelegate(d, timer.ElapsedMilliseconds + timeUntilRun);
                timedQueue.Add(del);
                return del;
            }
        }

        /// <summary>
        /// Cancel a timed (delayed) delegate before it has been run.
        /// </summary>
        /// <returns>true if the delegate was successfully cancelled.</returns>
        public bool Cancel(ScheduledDelegate d)
        {
            lock (schedulerQueue)
                return timedQueue.Remove(d);
        }

        public void Add(VoidDelegate d, bool forceDelayed = false)
        {
            try
            {
                if (!forceDelayed && Thread.CurrentThread.ManagedThreadId == mainThreadID)
                {
                    //We are on the main thread already - don't need to schedule.
                    d.Invoke();
                    return;
                }

                lock (schedulerQueue)
                    schedulerQueue.Enqueue(d);
            }
            catch
            {
            }
        }
    }

    public struct ScheduledDelegate : IComparable<ScheduledDelegate>
    {
        public ScheduledDelegate(VoidDelegate task, long time)
        {
            Task = task;
            ExecuteTime = time;
        }

        public VoidDelegate Task;
        public long ExecuteTime;

        #region IComparable<ScheduledDelegate> Members

        public int CompareTo(ScheduledDelegate other)
        {
            return ExecuteTime.CompareTo(other.ExecuteTime);
        }

        #endregion
    }
}