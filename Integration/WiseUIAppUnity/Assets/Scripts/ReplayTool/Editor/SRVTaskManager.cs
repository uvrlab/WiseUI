using ARRC.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ARRC.StreamRecorderVisualizer
{
    public class SRVTaskManager : TaskManager
    {
        public void Initialize(string recordingDirectory, EditorWindow wnd)
        {
            ////Get directory name;
            DirectoryInfo dinfo = new DirectoryInfo(recordingDirectory);
            GameObject parentObject = new GameObject(recordingDirectory);
            /*
             * Generate Point Cloud Task 
             */
            string filename_pointcloud = string.Format(@"{0}/{1}/{2}", recordingDirectory, "pinhole_projection", "tsdf-pc.ply");
            AddTask(new ReadPointCloudTask(filename_pointcloud, parentObject.transform));


            ////Needs to exception handling.
            string filename_pvtxt = string.Format(@"{0}/{1}_pv.txt", recordingDirectory, dinfo.Name);
            List<string> lines_pvtxt = System.IO.File.ReadAllLines(filename_pvtxt).ToList();

            ////Get principal points, image size
            var firstLine = lines_pvtxt[0].Split(',').ToList();
            float[] principalPoint = firstLine.GetRange(0, 2).Select(i => float.Parse(i)).ToArray();
            int[] imageSize = firstLine.GetRange(2, 2).Select(i => int.Parse(i)).ToArray();
            //strIntrinsic.ToList().ForEach(i => Debug.Log("intrinsic " + i.ToString()));

            var frameLines = lines_pvtxt.GetRange(1, lines_pvtxt.Count - 1);
            frameLines.ForEach(i=> AddTask(new ReadFrameTask(recordingDirectory, principalPoint[0], principalPoint[1], imageSize[0], imageSize[1], i, parentObject.transform)));
            Debug.Log(frameLines.Count);
        }
    
        public override void Dispose()
        {
            base.Dispose();
        }

        static SRVTaskManager instance;
        public static SRVTaskManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new SRVTaskManager();

                return instance;
            }
        }

        
    }
}
