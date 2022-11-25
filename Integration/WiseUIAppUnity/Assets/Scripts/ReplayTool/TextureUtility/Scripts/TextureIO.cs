using System.IO;
using UnityEngine;
using OpenCvSharp;

namespace ARRC.ARRCTexture
{
    public static class TextureIO
    {
        public static Texture2D ConvertRenderTexturetoTexture2D(RenderTexture texture, TextureFormat textureFormat)
        {
            
            RenderTexture backup = RenderTexture.active;
            RenderTexture.active = texture;
            Texture2D texture2d = new Texture2D(texture.width, texture.height, textureFormat, false);
            texture2d.ReadPixels(new UnityEngine.Rect(0, 0, texture.width, texture.height), 0, 0, false);
            texture2d.Apply();
            RenderTexture.active = backup;

            return texture2d;
        }

        public static void WriteTexture(RenderTexture texture, string filename, TextureFormat textureFormat)
        {
            WriteTexture(ConvertRenderTexturetoTexture2D(texture, textureFormat), filename);
        }

        public static void WriteTexture(Texture2D texture, string filename)
        {
            float startTime = Time.realtimeSinceStartup;

            byte[] bytes = texture.EncodeToPNG();
            //DestroyImmediate(texture);

            string assetPath = Application.dataPath;
            //string assetDir = Path.GetDirectoryName(assetPath);
            //string assetName = Path.GetFileNameWithoutExtension(assetPath) + "_equirectangular.png";
            //string assetName = filename;
            string newAsset = Path.Combine(assetPath, filename);
            File.WriteAllBytes(newAsset, bytes);

            // Import the new texture.
            //AssetDatabase.ImportAsset(newAsset);
            //AssetDatabase.Refresh();
            //Debug.Log("Equirectangular map saved to " + newAsset);
            //Debug.Log("Time to write file: " + (Time.realtimeSinceStartup - startTime) + " sec");

        }
     
        public static Color TurboColorMap(float x)
        {
             Vector4 kRedVec4 = new Vector4(0.13572138f, 4.61539260f, -42.66032258f, 132.13108234f);
             Vector4 kGreenVec4 = new Vector4(0.09140261f, 2.19418839f, 4.84296658f, -14.18503333f);
             Vector4 kBlueVec4 = new Vector4(0.10667330f, 12.64194608f, -60.58204836f, 110.36276771f);
             Vector2 kRedVec2 = new Vector2(-152.94239396f, 59.28637943f);
             Vector2 kGreenVec2 = new Vector2(4.27729857f, 2.82956604f);
             Vector2 kBlueVec2 = new Vector2(-89.90310912f, 27.34824973f);

            x = Mathf.Clamp(x, 0, 1);
            
            Vector4 v4 = new Vector4(1.0f, x, x * x, x * x * x);
            Vector2 v2 = new Vector2(v4[2], v4[3]) * v4[2];// v4.zw * v4.z;
            
            return
            new Color(
            Vector4.Dot(v4, kRedVec4) + Vector2.Dot(v2, kRedVec2),
            Vector4.Dot(v4, kGreenVec4) + Vector2.Dot(v2, kGreenVec2),
            Vector4.Dot(v4, kBlueVec4) + Vector2.Dot(v2, kBlueVec2), 1);
        }

        public static Color PseudoDepthMap(float dist, float maxDist)
        {
            float maxPerChannel = Mathf.Pow(maxDist, 1 / 3f);
            float b = dist % maxPerChannel;
            float g = (dist / maxPerChannel) % maxPerChannel;
            float r = (dist / maxPerChannel / maxPerChannel) % maxPerChannel;

            return new Color(r / maxPerChannel, g / maxPerChannel, b / maxPerChannel, 1.0f);

        }

        public static float GetRealDepth(Color pseudoDepth, float maxDist)
        {
            float maxPerChannel = Mathf.Pow(maxDist, 1 / 3f);
            float b = pseudoDepth.b * maxPerChannel;
            float g = pseudoDepth.g * maxPerChannel;
            float r = pseudoDepth.r * maxPerChannel;

            float dist = b + g* maxPerChannel + r* maxPerChannel * maxPerChannel;
            return dist;
        }


//#ifdef USE_OPENCVSHARP
        public static Texture2D LoadTexture(string filepath, bool resize = false, int equirectangularWidth = 0)
        {
            Mat mat_source = LoadImage(filepath, resize, equirectangularWidth);
            Texture2D out_tex = ConvertMattoTexture2D(mat_source);
            return out_tex;
        }
        public static Texture2D LoadTextureFloat(string filepath, bool resize = false, int equirectangularWidth = 0)
        {
            Mat mat_source = LoadImage(filepath, resize, equirectangularWidth);
            Texture2D out_tex = ConvertMattoTexture2DFloat(mat_source);
            return out_tex;
        }
        public static Mat LoadImage(string filepath, bool resize = false, int maxWidth = 0)
        {
            if (!File.Exists(filepath))
                throw new FileNotFoundException("The file is not found : " + filepath);

            Mat mat_out = Cv2.ImRead(filepath, ImreadModes.Unchanged);

            if (resize)
                Cv2.Resize(mat_out, mat_out, new Size(maxWidth, maxWidth / 2), 0, 0, InterpolationFlags.Nearest); //색상 변경 방지를 위해 blending 보간 금지.

            return mat_out;
        }

        public static Texture2D ConvertMattoTexture2D(Mat sourceMat) //The type of sourceMat must be BGRA32
        {
            int imgHeight = sourceMat.Height;
            int imgWidth = sourceMat.Width;

            var indexer = sourceMat.GetGenericIndexer<Vec4b>();
            //Create the Color array that will hold the pixels 
            Color[] c = new Color[imgHeight * imgWidth];

            //Get the pixel data from parallel loop
            //Parallel.For(0, imgHeight, i =>
            for (var i = 0; i < imgHeight; i++)
            {
                for (var j = 0; j < imgWidth; j++)
                {
                    var vec = indexer[imgHeight - i - 1, j];
                    var color = new Color
                    {
                        r = vec.Item2 / 255f,
                        g = vec.Item1 / 255f,
                        b = vec.Item0 / 255f,
                        a = 1,
                    };
                    c[j + i * imgWidth] = color;
                }
            }
            //});

            //Parallel.For(0, imgHeight, i =>
            //for (var i = 0; i < imgHeight; i++)
            //{
            //    for (var j = 0; j < imgWidth; j++)
            //    {
            //        Vec3b color = sourceMat.Get<Vec3b>(i, j);

            //        //byte bR = matData[i * imgWidth + j + 0];
            //        //byte bG = matData[i * imgWidth + j + 1];
            //        //byte bB = matData[i * imgWidth + j + 2];

            //        Color color32 = new Color
            //        {
            //            r = color.Item0,
            //            g = color.Item1,
            //            b = color.Item2,
            //            a = 1
            //        };
            //        c[j + i * imgWidth] = color32;
            //    }
            //}//);

            //Create Texture from the result
            Texture2D tex = new Texture2D(imgWidth, imgHeight, TextureFormat.RGBA32, true, true);
            tex.SetPixels(c);
            tex.Apply();

            return tex;
        }
        static Texture2D ConvertMattoTexture2DFloat(Mat sourceMat) //The type of sourceMat must be BGRA32
        {
            int imgHeight = sourceMat.Height;
            int imgWidth = sourceMat.Width;

            var indexer = sourceMat.GetGenericIndexer<Vec4f>();

            //Create the Color array that will hold the pixels 
            Color[] c = new Color[imgHeight * imgWidth];

            //Get the pixel data from parallel loop
            //Parallel.For(0, imgHeight, i =>
            for (var i = 0; i < imgHeight; i++)
            {
                for (var j = 0; j < imgWidth; j++)
                {
                    var vec = indexer[imgHeight - i - 1, j];
                    var color = new Color
                    {
                        r = vec.Item2,
                        g = vec.Item1,
                        b = vec.Item0,
                        a = vec.Item3,
                    };
                    c[j + i * imgWidth] = color;
                }
            }
            //});

            //Create Texture from the result
            Texture2D tex = new Texture2D(imgWidth, imgHeight, TextureFormat.RGBAFloat, true, true);
            tex.SetPixels(c);
            tex.Apply();

            return tex;
        }

        public static Mat ConvertTexture2DtoMat(Texture2D texture, MatType type)
        {
            Mat mat_before = new Mat(texture.height, texture.width, type, texture.GetRawTextureData());
            Mat mat_out = new Mat();
            Cv2.CvtColor(mat_before, mat_out, ColorConversionCodes.BGRA2RGBA);
            Cv2.Flip(mat_out, mat_out, FlipMode.X);

            return mat_out;
        }
        public static Texture2D OverayTexture(Texture2D texture1, float alpha, Texture2D texture2, float beta, bool showWindow, string windowname)
        {
            Mat mat_texture1 = ConvertTexture2DtoMat(texture1, MatType.CV_8UC4);
            Mat mat_texture2 = ConvertTexture2DtoMat(texture2, MatType.CV_8UC4);
            Mat mat_texture1_32FC4 = new Mat();
            Mat mat_texture2_32FC4 = new Mat();
            Mat mat_mask_8UC1 = new Mat();
            Mat mat_mask_8UC4 = new Mat();
            Mat mat_mask_32FC4 = new Mat();

            Mat mat_out_32FC4 = new Mat();
            Mat mat_out_8UC4 = new Mat();

            mat_texture1.ConvertTo(mat_texture1_32FC4, MatType.CV_32FC4);
            mat_texture2.ConvertTo(mat_texture2_32FC4, MatType.CV_32FC4);

            Cv2.CvtColor(mat_texture2, mat_mask_8UC1, ColorConversionCodes.RGBA2GRAY);
            Cv2.Threshold(mat_mask_8UC1, mat_mask_8UC1, 0, 255, ThresholdTypes.Binary); // 0 or 255로 구성된 binary 이미지 생성. 

            Cv2.CvtColor(mat_mask_8UC1, mat_mask_8UC4, ColorConversionCodes.GRAY2RGBA); // 채널을 늘리려면 CvtColor 이용. 
            mat_mask_8UC4.ConvertTo(mat_mask_32FC4, MatType.CV_32FC4, 1 / 255.0); //타입을 바꾸려면 ConvertTo 이용.

            Cv2.Multiply(mat_texture1_32FC4, Scalar.All(1.0) - mat_mask_32FC4, mat_texture1_32FC4);
            Cv2.AddWeighted(mat_texture1_32FC4, alpha, mat_texture2_32FC4, beta, 0, mat_out_32FC4);
            //Cv2.Add(mat_texture1_32FC4, mat_texture2_32FC4, mat_out_32FC4);

            mat_out_32FC4.ConvertTo(mat_out_8UC4, MatType.CV_8UC4);

            if (showWindow)
                Cv2.ImShow(windowname, mat_out_8UC4);

            Texture2D texture_out = ConvertMattoTexture2D(mat_out_8UC4);
            return texture_out;
        }
        public static Texture2D TransparentOverayTexture(Texture2D texture1, float alpha, Texture2D texture2, float beta, bool showWindow = false, string windowname = "")
        {
            Mat mat_texture1 = ConvertTexture2DtoMat(texture1, MatType.CV_8UC4);
            Mat mat_texture2 = ConvertTexture2DtoMat(texture2, MatType.CV_8UC4);

            Mat mat_out = new Mat();
            Cv2.AddWeighted(mat_texture1, alpha, mat_texture2, beta, 0, mat_out);
            //Cv2.Add(mat_texture1, mat_texture2, mat_out);

            if (showWindow)
                Cv2.ImShow(windowname, mat_out);

            Texture2D texture_out = ConvertMattoTexture2D(mat_out);
            return texture_out;
        }

     

    }
//#endif
}
