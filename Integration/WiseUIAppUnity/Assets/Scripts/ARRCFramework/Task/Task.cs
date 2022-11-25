namespace ARRC.Framework
{
    public enum TaskType { Coroutine, Download, }

    public class Task
    {
        public TaskType taskType;
        public string currentState;

        public bool isStarted;
        public bool isCompleted;

        public float progress;
        public int totalSize;
        public float processingTime;
   
        public Task()
        {
            isStarted = false;
            isCompleted = false;

            progress = 0;
            totalSize = 0;
            processingTime = 0;
        }
        public Task(string currentState)
        {
            this.currentState = currentState;
        }
        public virtual void Start()
        {
            isStarted = true;
        }

        public virtual void Enter() { }

        public virtual void Dispose() { }

      
     

    }
}