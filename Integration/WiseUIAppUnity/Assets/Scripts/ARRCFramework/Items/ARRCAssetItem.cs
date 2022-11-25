using UnityEngine;

namespace ARRC.Framework
{
    public class ARRCResultItem : ARRCItem
    {
        //folderName이 포함된것이 맘에 안듦.. but coroutinePhase에서 객체별로 mono를 넘기기 어렵기 때문에 필요함.. 200416.

        [SerializeField]
        public string folderName;

        public ARRCResultItem(string folderName)
        {
            this.folderName = folderName;
        }
    }
}
