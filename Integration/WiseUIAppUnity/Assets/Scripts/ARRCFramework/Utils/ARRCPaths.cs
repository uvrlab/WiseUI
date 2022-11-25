using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ARRC.Framework
{
    /// <summary>
    /// editor prefenece에 등록된 path로부터  폴더/파일들의 경로를 반환한다.
    /// </summary>
    /// 
    public static class ARRCPaths
    {

        static string ResultFolder = Path.GetFullPath(Application.dataPath + "/ARRC_Result/");
        static string subfolder_streetview = "StreetView";
        static string subfolder_vworld = "VWorld";

        static string subfolder_building = "Building";

        public static string HistoryFilePath
        {
            get
            {
                return Path.GetFullPath(Application.dataPath + "/../ARRCHistory.xml");
            }
        }

        public static string GetLabelImagePath(string filename)
        {
            string folder = ARRCPreference.LoadPreference(ARRCPrefPathID.LabelImageFolder);
            string path = GetFirstFoundFilePath(folder, filename);
            return path;
        }

        public static string GetResultFolder_Root(string title)
        {
            return GetAssetPath(Path.Combine(ResultFolder, title + "\\"));
        }

        //StreetView Path.
        public static string ResultFolder_StreetView_Color(string title, int zoom)
        {
            string path = Path.Combine(ResultFolder, title);
            path = Path.Combine(path, subfolder_streetview);
            path = Path.Combine(path, "Color");
            path = Path.Combine(path, "Zoom_" + zoom.ToString());

            return path;
        }

        private static string ResultFolder_StreetView_Depth(string title)
        {
            string path = Path.Combine(ResultFolder, title);
            path = Path.Combine(path, subfolder_streetview);
            path = Path.Combine(path, "Depth");

            return path;
        }
        public static string ResultFolder_Building(string title)
        {
            string path = Path.Combine(ResultFolder, title);
            path = Path.Combine(path, subfolder_building);
            return path;
        }

        
        public static string CachFolder_StreetView_Image(int zoom)
        {
            string path = Path.Combine(ARRCPreference.LoadPreference(ARRCPrefPathID.CacheFolder), subfolder_streetview);
            path = Path.Combine(path, "Images");
            path = Path.Combine(path, zoom.ToString());

            return path;
        }

        public static string CachFolder_StreetView_XML
        {
            get
            {
                string path = Path.Combine(ARRCPreference.LoadPreference(ARRCPrefPathID.CacheFolder), subfolder_streetview);
                path = Path.Combine(path, "XML");

                return path;
            }
        }

        public static string CachFolder_VWorld
        {
            get
            {
                string path = Path.Combine(ARRCPreference.LoadPreference(ARRCPrefPathID.CacheFolder), subfolder_vworld);
                return path;
            }
        }
        
        public static string GetColorImagePathFromStreetViewInfo(string titleName, int zoom, string filename)
        {
            string path_image = Path.Combine(ResultFolder_StreetView_Color(titleName, zoom), filename);
            return path_image;
        }
        public static string GetGrayScaleDepthPath(string titleName, string filename_with_ext)
        {
            string path = Path.Combine(ResultFolder_StreetView_Depth(titleName), "GrayScale");
            string path_image = Path.Combine(path, filename_with_ext);
            return path_image;
        }
        public static string GetTurboColorPath(string titleName, string filename_with_ext)
        {
            string path = Path.Combine(ResultFolder_StreetView_Depth(titleName), "TurboColor");
            string path_image = Path.Combine(path, filename_with_ext);
            return path_image;
        }
        public static string GetPseudoDepthPath(string titleName, string filename_with_ext)
        {
            string path = Path.Combine(ResultFolder_StreetView_Depth(titleName), "PseudoDepth");
            string path_image = Path.Combine(path, filename_with_ext);
            return path_image;
        }
        //Utility functions.
        public static string GetAssetPath(string fullpath)
        {
            fullpath.Replace("/", "\\");
            string path = fullpath.Split(new string[] { Path.GetFullPath(Application.dataPath) }, StringSplitOptions.RemoveEmptyEntries).First(); // asset/ 이후의경로만을 취함.
            path = "Assets" + path;
            return path;
        }

        public static string GetFirstFoundFilePath(string directory, string filename_with_ext)
        {
            string[] files = Directory.GetFiles(directory, filename_with_ext, SearchOption.AllDirectories);

            if (files.Length == 0)
                throw new FileNotFoundException(string.Format("The file {0} is not founds in {1} ", filename_with_ext, directory));

            else if (files.Length > 1)
            {
                Debug.LogWarningFormat("{0} files found.", files.Length);
                files.ToList().ForEach(i => Debug.LogWarning(i));
                Debug.LogWarningFormat("It use the file : {0}", files[0]);
            }

            return files[0].Replace("/", "\\");
        }

      
    }
}