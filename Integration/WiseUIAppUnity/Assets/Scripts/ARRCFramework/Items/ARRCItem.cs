using System;

namespace ARRC.Framework
{
    public class ARRCItem
    {
        public string guid;
        public long timestamp;

        public ARRCItem()
        {
            guid = Guid.NewGuid().ToString();
            timestamp = DateTime.Now.Ticks;
        }

    }
}
