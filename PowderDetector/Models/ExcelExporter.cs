using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using OfficeOpenXml;


namespace PowderDetector.Models
{
    internal class ExcelExporter
    {
        public string Path { get; }

        public ExcelExporter(string path)
        {
            Path = path;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }


        public void ExportData(IEnumerable<SizeF> size, IEnumerable<double> perimeter, IEnumerable<double> area, BitmapSource image)
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Информация о параметрах частиц");

                sheet.Cells[1, 1].Value = "№";
                sheet.Cells[1, 2].Value = "Dmin [мм]";
                sheet.Cells[1, 3].Value = "Dmax [мм]";
                sheet.Cells[1, 4].Value = "P [мм]";
                sheet.Cells[1, 5].Value = "S [мм²]";

                Stream bmp = new MemoryStream();
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(image));
                enc.Save(bmp);
                bmp.Seek(0, SeekOrigin.Begin);

                var picture = sheet.Drawings.AddPicture("Powder", bmp);

                picture.SetPosition(3, 0, 7, 0);

                var headers = sheet.Rows[1];

                headers.Style.Font.SetFromFont("Times New Roman", 12, true);

                var mSize = size.ToList();
                var mPerimeter = perimeter.ToList();
                var mArea = area.ToList();


                for (var i = 2; i < mSize.Count + 2; i++)
                {
                    var currentSize = mSize[i - 2];

                    sheet.Cells[i, 1].Value = i - 1;
                    sheet.Cells[i, 2].Value = currentSize.Height < currentSize.Width ? currentSize.Height : currentSize.Width;
                    sheet.Cells[i, 3].Value = currentSize.Height > currentSize.Width ? currentSize.Height : currentSize.Width;
                    sheet.Cells[i, 4].Value = mPerimeter[i - 2];
                    sheet.Cells[i, 5].Value = mArea[i - 2];
                }

                var columns = sheet.Columns[1, 5];
                columns.BestFit = true;
                columns.AutoFit();

                package.SaveAs(Path);
            }
        }
    }
}
