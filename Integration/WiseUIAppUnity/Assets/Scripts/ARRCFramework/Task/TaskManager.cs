using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARRC.Framework
{
    public class TaskManager
    {

        List<Task> requiredTasks = new List<Task>();
        private int activeIndex = 0;

        public int ActiveIndex { get { return activeIndex; } }

        public virtual void Update()
        {
            if (!IsEmpty)
            {
                try
                {
                    if (IsReady)
                        requiredTasks[activeIndex].Start();

                    else if (IsRunning)
                        requiredTasks[activeIndex].Enter();

                    else if (IsPass)
                        activeIndex++;

                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Dispose();
                }
            }
        }

        public string StateOfActiveTask { get { return requiredTasks[activeIndex].currentState; } }

        public float ProgressOfActiveTask { get { return requiredTasks[activeIndex].progress; } }

        public float ProcessingTimeOfActiveTask { get { return requiredTasks[activeIndex].processingTime; } }

        public int SizeOfActiveTask { get { return requiredTasks[activeIndex].totalSize; } }

        //public float ProcessingTime{ get; private set; }

        public string StateOf(int idx)
        {
            if (idx < 0 || idx >= requiredTasks.Count)
                throw new IndexOutOfRangeException();

            return requiredTasks[idx].currentState;
        }

        /// <summary>
        /// ///주의 : Task를 Start한 후에만 totalSize가 계산됨.
        /// </summary>
        public float TotalSizeOf(int idx)
        {
            if (idx < 0 || idx >= requiredTasks.Count)
                throw new IndexOutOfRangeException();

            return requiredTasks[idx].totalSize;
        }

        /// <summary>
        ///  progress를 ARRCGenerator에서 공유하기 때문에 사용할 수 없음.
        /// </summary>
        //public float ProgressOf(int idx)
        //{
        //    if (idx < 0 || idx >= requiredTasks.Count)
        //        throw new IndexOutOfRangeException();

        //    return requiredTasks[idx].progress;
        //}

        /// <summary>
        /// ...
        /// </summary>
        public TaskType TaskTypeOf(int idx)
        {
            if (idx < 0 || idx >= requiredTasks.Count)
                throw new IndexOutOfRangeException();

            return requiredTasks[idx].taskType;
        }
        public int TaskCount { get { return requiredTasks.Count; } }

        public virtual void Initialize()
        {
            activeIndex = 0;
            requiredTasks.Clear();
        }

        protected void AddTask(Task newTask)
        {
            requiredTasks.Add(newTask);
        }

        public virtual void Dispose()
        {
            if (IsRunning)
                requiredTasks[activeIndex].Dispose();

            requiredTasks.Clear();
            activeIndex = 0;
        }

        public virtual bool IsEmpty
        {
            get
            {
                return requiredTasks.Count == 0;
            }
        }

        public virtual bool IsReady
        {
            get
            {
                return !IsEmpty && !requiredTasks[activeIndex].isStarted;
            }
        }

        public virtual bool IsRunning
        {
            get
            {
                return !IsEmpty && requiredTasks[activeIndex].isStarted && !requiredTasks[activeIndex].isCompleted;
            }
        }

        public virtual bool IsPass
        {
            get
            {
                return !IsEmpty && requiredTasks[activeIndex].isStarted && requiredTasks[activeIndex].isCompleted && (activeIndex + 1 < requiredTasks.Count);
            }
        }

        public virtual bool IsCompleted
        {
            get
            {
                return !IsEmpty && requiredTasks[activeIndex].isCompleted && activeIndex + 1 == requiredTasks.Count;
            }
        }
    }
}