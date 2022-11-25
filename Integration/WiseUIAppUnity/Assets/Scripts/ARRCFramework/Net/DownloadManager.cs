using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ARRC.Framework
{
    public class DownloadManager
    {
        private int maxDownloadItem;

        public long completeSize;
        public bool isComplete;

        private List<DownloadItem> activeItems;
        private List<DownloadItem> items;
        private long totalSize;

        public int Count
        {
            get
            {
                if (items == null) return -1;
                return items.Count;
            }
        }

        public double Progress
        {
            get
            {
                if (activeItems == null || activeItems.Count == 0) return 0;
                double localProgress = activeItems.Sum(i =>
                {
                    if (i.ignoreRequestProgress) return 0;
                    return (double)i.Progress * i.averageSize;
                }) / totalSize;
                double totalProgress = completeSize / (double)totalSize + localProgress;
                return totalProgress;
            }
        }

        public int TotalSizeMB
        {
            get { return Mathf.RoundToInt(totalSize / (float)ARRCDataSize.MB); }
        }

        public void Add(DownloadItem item)
        {
            if (items == null) items = new List<DownloadItem>();
            item.downloadManager = this;

            items.Add(item);
        }

        public void AddRange(List<DownloadItem> newItems)
        {
            if (items == null) items = new List<DownloadItem>();

            foreach(DownloadItem i in newItems)
                i.downloadManager = this;

            items.AddRange(newItems);
        }

        public bool CheckComplete()
        {
            if (isComplete)
                return true;

            foreach (DownloadItem item in activeItems) item.CheckComplete();

            activeItems.RemoveAll(i => i.complete);
            while (activeItems.Count < maxDownloadItem && items.Count > 0)
            {
                if (!StartNextDownload()) break;
            }
            if (activeItems.Count == 0 && items.Count == 0)
            {
                isComplete = true;
                return true;
            }
            else
                return false;
        }

        public void Dispose()
        {
            if (activeItems != null)
            {
                foreach (DownloadItem item in activeItems) item.Dispose();
                activeItems = null;
            }

            items = null;
        }

        public string EscapeURL(string url)
        {
#if UNITY_2018_3_OR_NEWER
            return UnityWebRequest.EscapeURL(url);
#else
            return WWW.EscapeURL(url);
#endif
        }

        public void Start(int _maxDownloadItem)
        {
            if (items == null || items.Count == 0)
            {
                isComplete = true;
                return;
            }
                

            isComplete = false;
            completeSize = 0;
            maxDownloadItem = _maxDownloadItem;

            activeItems = new List<DownloadItem>();

            try
            {
                totalSize = items.Sum(i => i.averageSize);
            }
            catch
            {
                Dispose();
                return;
            }

            CheckComplete();
        }
     
        public bool StartNextDownload()
        {
            if (items == null || items.Count == 0) return false;

            int index = 0;
            DownloadItem dItem = null;
            while (index < items.Count)
            {
                DownloadItem item = items[index];
                if (item.exclusiveLock != null)
                {
                    bool hasAnotherSomeRequest = false;
                    foreach (DownloadItem activeItem in activeItems)
                    {
                        if (item.exclusiveLock == activeItem.exclusiveLock)
                        {
                            hasAnotherSomeRequest = true;
                            break;
                        }
                    }
                    if (!hasAnotherSomeRequest)
                    {
                        dItem = item;
                        break;
                    }
                }
                else
                {
                    dItem = item;
                    break;
                }
                index++;
            }

            if (dItem == null) return false;

            items.RemoveAt(index);
            if (dItem.Exists)
                return true;

            dItem.Start();
            activeItems.Add(dItem);
            return true;
        }

    }
}
