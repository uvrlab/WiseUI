using UnityEngine;
using OpenCvSharp;

namespace ARRC.ARRCTexture
{
    public class OutlineDetectionWarpper
    {
        public static Texture2D GetOutlineTexture(string filepath, bool resize, int maxWidth, Color outputColor, int thickness)
        {
            Scalar sColor = new Scalar(outputColor.b * 255, outputColor.g * 255, outputColor.r * 255, outputColor.a * 255);
            Mat mat_source = TextureIO.LoadImage(filepath, false); //여기서는 이미지 resize없이 로드해야함.
            Mat mat_result = OutlineDetection.ExtractOutline(mat_source, sColor, thickness);

            if (resize)
                Cv2.Resize(mat_result, mat_result, new Size(maxWidth, maxWidth / 2), 0, 0, InterpolationFlags.Nearest); //색상 변경 방지를 위해 blending 보간 금지.

            return TextureIO.ConvertMattoTexture2D(mat_result);
        }

    }
}

