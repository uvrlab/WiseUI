using ARRC.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadPointCloudTask : Task
{
    string pointCloudFilePath;
    Transform parent;
    // Start is called before the first frame update
    public ReadPointCloudTask(string pointCloudFilePath, Transform parent)
    {
        this.pointCloudFilePath = pointCloudFilePath;
        this.parent = parent;
    }
    public override void Enter()
    {
        PointCloudGeneratorWarpper.Instance.BuildCloud(pointCloudFilePath, parent);
        isCompleted = true;
        progress = 1;
    }
}
