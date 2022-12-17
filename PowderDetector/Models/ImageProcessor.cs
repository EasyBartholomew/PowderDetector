using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using OpenTK;

namespace PowderDetector.Models
{
    internal class ImageProcessor
    {
        public string Path { get; }

        public ImageProcessor(string path)
        {
            Path = path;
        }

        public Mat LoadImage()
        {
            return CvInvoke.Imread(Path, LoadImageType.Color);
        }

        public VectorOfVectorOfPoint FindContours(Mat image)
        {
            var gray = new Mat();

            CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);

            var minBound = new Gray(0);
            var maxBound = new Gray(150);

            var mask = gray.ToImage<Gray, byte>().InRange(minBound, maxBound);

            var kernel = new Matrix<byte>(new byte[] { 1 });

            mask = mask.MorphologyEx(MorphOp.Open,
                kernel,
                Point.Empty,
                5,
                BorderType.Default,
                new MCvScalar());

            mask = mask.MorphologyEx(MorphOp.Close,
                kernel,
                Point.Empty,
                5,
                BorderType.Default,
                new MCvScalar());

            var contours = new VectorOfVectorOfPoint();
            var hierarchy = new Mat();

            CvInvoke.FindContours(mask, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            return contours;
        }

        public VectorOfVectorOfPoint FindPivot(VectorOfVectorOfPoint source)
        {
            var result = new VectorOfVectorOfPoint();
            var maxIndex = 0;

            for (var i = 1; i < source.Size; i++)
            {
                if (CvInvoke.ContourArea(source[maxIndex]) < CvInvoke.ContourArea(source[i]))
                    maxIndex = i;
            }

            result.Push(source[maxIndex]);

            return result;
        }

        public VectorOfVectorOfPoint FindPowder(VectorOfVectorOfPoint source, VectorOfVectorOfPoint pivot)
        {
            var pivotArea = CvInvoke.ContourArea(pivot[0]);
            var result = new VectorOfVectorOfPoint();

            for (var i = 0; i < source.Size; i++)
                if (Math.Abs(pivotArea - CvInvoke.ContourArea(source[i])) > 0.000001)
                    result.Push(source[i]);

            return result;
        }

        public VectorOfVectorOfPoint FilterPoints(VectorOfVectorOfPoint source, double minValue, double maxValue)
        {
            var result = new VectorOfVectorOfPoint();

            for (var i = 0; i < source.Size; i++)
            {
                var area = CvInvoke.ContourArea(source[i]);

                if (minValue < area && area < maxValue)
                    result.Push(source[i]);
            }

            return result;
        }

        public VectorOfVectorOfPoint FilterPoints(VectorOfVectorOfPoint source, double coefficient = 2.5)
        {
            var result = new VectorOfVectorOfPoint();
            var list = new List<double>();

            for (var i = 0; i < source.Size; i++)
            {
                var area = CvInvoke.ContourArea(source[i]);
                list.Add(area);
            }

            var median = list.Where(a => a > 5).Median();

            var lowerBound = median / coefficient;
            var upperBound = median * coefficient;

            return FilterPoints(source, lowerBound, upperBound);
        }

        public Mat DrawPoints(Mat image, VectorOfVectorOfPoint pivot, VectorOfVectorOfPoint powder,
            double fontScale = 0.3, int xTextOffset = 3, int yTextOffset = 3)
        {
            var result = image.Clone();

            CvInvoke.DrawContours(result, pivot, -1, new MCvScalar(0, 255, 0), 4);
            CvInvoke.DrawContours(result, powder, -1, new MCvScalar(255, 0, 0), 2);

            for (var i = 0; i < powder.Size; i++)
            {
                var rect = CvInvoke.BoundingRectangle(powder[i]);
                CvInvoke.PutText(result,
                    (i + 1).ToString(),
                    new Point(rect.X - xTextOffset, rect.Y - yTextOffset),
                    FontFace.HersheySimplex,
                    fontScale,
                    new MCvScalar(0, 0, 255), 1,
                    LineType.AntiAlias);
            }

            return result;
        }

        public List<double> GetAreas(VectorOfVectorOfPoint powder)
        {
            var list = new List<double>();

            for (var i = 0; i < powder.Size; i++)
                list.Add(CvInvoke.ContourArea(powder[i]));

            return list;
        }

        public List<double> GetPerimeters(VectorOfVectorOfPoint powder)
        {
            var list = new List<double>();

            for (var i = 0; i < powder.Size; i++)
                list.Add(CvInvoke.ArcLength(powder[i], true));

            return list;
        }

        public List<SizeF> GetDimensions(VectorOfVectorOfPoint powder)
        {
            var list = new List<SizeF>();

            for (var i = 0; i < powder.Size; i++)
            {
                var rect = CvInvoke.MinAreaRect(powder[i]);

                list.Add(rect.Size);
            }

            return list;
        }
    }
}
