using OpenCvSharp;

namespace ARRC.ARRCTexture
{
    public static class OutlineDetection
    {/*
        static void Main(string[] args)
        {
            string filepath = "./testset/";
            
            string[] fileEntries = Directory.GetFiles(filepath);
            foreach (string fileName in fileEntries)
            {
                Mat mat_src = new Mat(fileName, ImreadModes.Unchanged);
                Mat mat_result = ExtractOutline(mat_src, 5);

                Mat mat_vis_src = new Mat();
                Mat mat_vis_result = new Mat();

                Cv2.Resize(mat_src, mat_vis_src, new Size(1080, 540));
                Cv2.Resize(mat_result, mat_vis_result, new Size(1080, 540));

                Cv2.ImShow("src image", mat_vis_src);
                Cv2.ImShow("result image", mat_vis_result);
                Cv2.ResizeWindow("src image", 1080, 540);
                Cv2.WaitKey(0);
            }
        }
        */
        public static Mat ExtractOutline(Mat mat_src, Scalar outputColor, int thickness)
        {
            var color_building = new OpenCvSharp.Scalar(70, 70, 70);

            //must be included.
            var color_sky = new OpenCvSharp.Scalar(180, 130, 70);
            var color_load = new OpenCvSharp.Scalar(128, 64, 128);
            var color_sidewalk = new OpenCvSharp.Scalar(232, 35, 244);

            //must be excluded.
            var color_tree = new OpenCvSharp.Scalar(35, 142, 107);

            Mat mat_contour_sky = GetContourImage(mat_src, color_sky, thickness, MorphTypes.Erode);
            Mat mat_contour_load = GetContourImage(mat_src, color_load, thickness, MorphTypes.Erode);
            Mat mat_contour_sidewalk = GetContourImage(mat_src, color_sidewalk, thickness, MorphTypes.Erode);

            Mat mat_contour_building = GetContourImage(mat_src, color_building, thickness, MorphTypes.Dilate);

            Mat mat_contour_bitwize_and = new Mat();
            Mat mat_contour_bitwize_or = new Mat();


            Cv2.BitwiseOr(mat_contour_load, mat_contour_sidewalk, mat_contour_bitwize_or);
            Cv2.BitwiseOr(mat_contour_bitwize_or, mat_contour_sky, mat_contour_bitwize_or);

            // sky and building
            Cv2.BitwiseAnd(mat_contour_bitwize_or, mat_contour_building, mat_contour_bitwize_and);


            // load and building

            int an = 1;
            var elementShape = MorphShapes.Ellipse;
            var element = Cv2.GetStructuringElement(
                             elementShape,
                             new Size(an * 2 + 1, an * 2 + 1),
                             new Point(an, an));

            Cv2.MorphologyEx(mat_contour_bitwize_and, mat_contour_bitwize_and, MorphTypes.Open, element); //잡음 제거.

            Mat mat_result = new Mat(mat_contour_bitwize_and.Size(), MatType.CV_8UC4);
            mat_result.SetTo(outputColor, mat_contour_bitwize_and);

            return mat_result;
        }

        public static Mat GetContourImage(Mat srcImage, Scalar extractingColor, int thickness, MorphTypes morphTypes)
        {

            Point[][] contours;
            HierarchyIndex[] hierarchyIndexes;

            Mat mat_mask = new Mat();
            //Mat mat_contour = new Mat(srcImage.Size(), MatType.CV_8UC3);
            Mat mat_contour = new Mat(srcImage.Size(), MatType.CV_8UC1);

            int bound = 0;
            Scalar color_lower = new Scalar(extractingColor.Val0 - bound, extractingColor.Val1 - bound, extractingColor.Val2 - bound);
            Scalar color_uppper = new Scalar(extractingColor.Val0 + bound, extractingColor.Val1 + bound, extractingColor.Val2 + bound);


            Cv2.InRange(srcImage, color_lower, color_uppper, mat_mask); //주의: 0과 255로 구성된 binary 이미지를 생성하지 않는다.
            Cv2.Threshold(mat_mask, mat_mask, 0, 255, ThresholdTypes.Binary); // binary 이미지 생성.

            int an = thickness;
            var elementShape = MorphShapes.Ellipse;
            var element = Cv2.GetStructuringElement(
                             elementShape,
                             new Size(an * 2 + 1, an * 2 + 1),
                             new Point(an, an));

            Cv2.MorphologyEx(mat_mask, mat_mask, morphTypes, element);  //경계의 바깥쪽 가장자리를 뽑기위해.

            Cv2.FindContours(mat_mask, out contours, out hierarchyIndexes, RetrievalModes.External, ContourApproximationModes.ApproxTC89KCOS); //ApproxSimple일 경우 이상한 잡음이 더 자주 생긴다.
            for (int i = 0; i < contours.Length; i++)
            {
                //if(contours[i].Length >= 10) // 일정 크기 이상의 contour만 취급.
                Cv2.DrawContours(mat_contour, contours, i, Scalar.White, thickness * (srcImage.Width / 512), LineTypes.Link8, hierarchyIndexes); //주의: 0과 255로 구성된 binary 이미지를 생성하지 않는다.
            }

            Cv2.Threshold(mat_contour, mat_contour, 254, 255, ThresholdTypes.Binary); // binary 이미지 생성.

            return mat_contour;
        }
    }
}
