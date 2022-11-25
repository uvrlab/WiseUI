using System.Collections.Generic;

namespace ARRC.Framework
{
    public class DownloadTask : Task
    {
        DownloadManager downloadManager;
        int maxDownloadItems;
        public DownloadTask(string CurrentState, DownloadManager downloadManager, int maxDownloadItems = 8) : base(CurrentState)
        {
            taskType = TaskType.Download;
            this.downloadManager = downloadManager;
            this.maxDownloadItems = maxDownloadItems;
        }
        
        public override void Start()
        {
            base.Start();
            downloadManager.Start(maxDownloadItems);
            totalSize = downloadManager.TotalSizeMB;
        }

        public override void Enter()
        {
            bool result = downloadManager.CheckComplete();
            progress = (float)downloadManager.Progress;

            if (result)
                Dispose();
        }

        public override void Dispose()
        {
            downloadManager.Dispose();
            isCompleted = true;
        }
    }
}
