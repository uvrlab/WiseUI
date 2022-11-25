namespace ARRC.Framework
{
    public class ARRCGenerator
    {
        public bool isComplete;
        public float progress;
        public int totalCount;
        public string currentState;
        public float averageTime;

        int k;

    
        public ARRCGenerator()
        {
            InitializeState();
        }
        public void InitializeState()
        {
            isComplete = false;
            progress = 0;
            totalCount = 0;
            currentState = "Ready.";
            averageTime = 0;
            k = 0;
        }
        //public virtual bool CheckComplete() { return IsComplete; }

        public void UpdateProcssingTime(float time)
        {
            k++;
            averageTime = (k - 1) * averageTime / k + time / k;
        }

        public virtual void Dispose()
        {

        }

        
    }
}
