using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace ARRC.Framework
{
    /// <summary>
    /// Editor Preference에 저장할 변수들을 관리한다.
    /// </summary>
    public enum ARRCPrefPathID { CacheFolder, LabelImageFolder, CityGMLFolder };

    public static class ARRCPreference
    {
        static string prefix = "ARRC_";

        public static Dictionary<ARRCPrefPathID, string> defaultFolder = new Dictionary<ARRCPrefPathID, string>()
        {
            { ARRCPrefPathID.CacheFolder,           Path.GetFullPath(Application.dataPath + "/../ARRC_Cache/") },
            { ARRCPrefPathID.LabelImageFolder,      Path.GetFullPath(Application.dataPath + "/../SampleData/LabelImages") },
            { ARRCPrefPathID.CityGMLFolder,         Path.GetFullPath(Application.dataPath + "/../SampleData/CityGML")},
        };

        public static string LoadPreference(ARRCPrefPathID id)
        {
            string key = prefix + id;
            if (PlayerPrefs.HasKey(key)) 
                return PlayerPrefs.GetString(key);
            else
                return defaultFolder[id];
        }

        public static void SetPreference(ARRCPrefPathID id, string path)
        {
            PlayerPrefs.SetString(prefix + id, path);
        }

        public static void DeletePrefence(ARRCPrefPathID id)
        {
            PlayerPrefs.DeleteKey(prefix + id);
        }

        public static void DeleteAllPrefence()
        {
            foreach(ARRCPrefPathID id in Enum.GetValues(typeof(ARRCPrefPathID)).Cast<ARRCPrefPathID>())
                PlayerPrefs.DeleteKey(prefix + id);
        }

    }

}
