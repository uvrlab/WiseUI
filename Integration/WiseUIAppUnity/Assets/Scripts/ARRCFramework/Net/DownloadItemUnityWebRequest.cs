using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Xml;
using System.IO;

namespace ARRC.Framework
{
    public class DownloadItemUnityWebRequest : DownloadItem
    {
        public UnityWebRequest uwr;
        public Dictionary<string, string> headers;

        public DownloadItemUnityWebRequest(string url) : this(UnityWebRequest.Get(url))
        {
            
        }
        public DownloadItemUnityWebRequest(string url, Dictionary<string, string> data) : this(UnityWebRequest.Post(url, data))
        {

        }
        public DownloadItemUnityWebRequest(UnityWebRequest uwr)
        {
            //DownloadManager.Add(this);
            this.uwr = uwr;
        }

        public override float Progress
        {
            get { return uwr.downloadProgress; }
        }

        public override bool Exists
        {
            get { return File.Exists(filename) || File.Exists(errorFilename); }
        }

        public override void CheckComplete()
        {
            if (!uwr.isDone) return;

            if (string.IsNullOrEmpty(uwr.error))
            {
                byte[] bytes = uwr.downloadHandler.data;
                SaveWWWData(bytes);
                DispatchCompete(ref bytes);
            }
            else Debug.LogWarning("Download failed: " + uwr.url + "\n" + uwr.error);

            downloadManager.completeSize += averageSize;
            complete = true;

            Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();

            uwr.Dispose();
            uwr = null;
        }

        public override void Start()
        {
            if (headers != null)
            {
                foreach (var header in headers) uwr.SetRequestHeader(header.Key, header.Value);
            }
#if UNITY_2018_2_OR_NEWER
            uwr.SendWebRequest();
#else
            uwr.Send();
#endif
        }
    }
}
