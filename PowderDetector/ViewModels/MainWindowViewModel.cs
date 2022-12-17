using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Microsoft.WindowsAPICodePack.Dialogs;
using PowderDetector.Commands;
using PowderDetector.Models;

namespace PowderDetector.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        private ImageProcessor _processor;
        private Mat _image;
        private VectorOfVectorOfPoint _pivot;
        private VectorOfVectorOfPoint _powder;
        private VectorOfVectorOfPoint _filtered;

        public string CurrentVersion => Assembly.GetExecutingAssembly()
            .GetName()
            .Version
            .ToString(3);

        public ICommand ExportToExcel { get; }

        public ICommand About { get; }

        public ObservableEntity<string> MinArea { get; }

        public ObservableEntity<string> MaxArea { get; }

        public ObservableEntity<string> Coefficient { get; }

        public ObservableEntity<string> FontScale { get; }

        public ObservableEntity<int> VerticalOffset { get; }

        public ObservableEntity<int> HorizontalOffset { get; }

        public ObservableEntity<uint> SquireSize { get; }

        public ObservableEntity<string> ImagePath { get; }

        public ObservableEntity<bool> AutomaticFilter { get; }

        public ObservableEntity<BitmapSource> Image { get; }

        public MainWindowViewModel()
        {
            About = new DelegateCommand(OnAbout);
            ExportToExcel = new DelegateCommand(OnExcelExporting, CanExportToExcel);

            MinArea = new ObservableEntity<string>("1");
            MinArea.PropertyChanged += OnImageParameterChanged;

            MaxArea = new ObservableEntity<string>("20");
            MaxArea.PropertyChanged += OnImageParameterChanged;

            Coefficient = new ObservableEntity<string>("5");
            Coefficient.PropertyChanged += OnImageParameterChanged;

            FontScale = new ObservableEntity<string>("0,5");
            FontScale.PropertyChanged += OnImageParameterChanged;

            VerticalOffset = new ObservableEntity<int>(3);
            VerticalOffset.PropertyChanged += OnImageParameterChanged;

            HorizontalOffset = new ObservableEntity<int>();
            HorizontalOffset.PropertyChanged += OnImageParameterChanged;

            SquireSize = new ObservableEntity<uint>(10);

            ImagePath = new ObservableEntity<string>();
            ImagePath.PropertyChanged += OnImagePathChanged;

            AutomaticFilter = new ObservableEntity<bool>(true);
            AutomaticFilter.PropertyChanged += OnAutomaticFilterStateChanged;

            Image = new ObservableEntity<BitmapSource>();
        }

        private void OnAutomaticFilterStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_image == null)
            {
                MinArea.Value = "1";
                MaxArea.Value = "20";
                return;
            }

            var median = _processor.GetAreas(_powder).Where(a => a != 0).Median();
            var coefficient = SquireSize.Value * SquireSize.Value / CvInvoke.ContourArea(_pivot[0]);

            MinArea.Value = (median * coefficient / double.Parse(Coefficient.Value, CultureInfo.CurrentCulture))
                .ToString("#.##", CultureInfo.CurrentCulture);

            MaxArea.Value = (median * coefficient * double.Parse(Coefficient.Value, CultureInfo.CurrentCulture))
                .ToString("#.##", CultureInfo.CurrentCulture);
        }

        private void OnImageParameterChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_image == null)
                return;

            var coefficient = SquireSize.Value * SquireSize.Value / CvInvoke.ContourArea(_pivot[0]);

            if (AutomaticFilter.Value)
                _filtered = _processor.FilterPoints(_powder,
                    double.Parse(Coefficient.Value, CultureInfo.CurrentCulture));
            else
                _filtered = _processor.FilterPoints(_powder,
                    double.Parse(string.IsNullOrEmpty(MinArea.Value) ? "0" : MinArea.Value, CultureInfo.CurrentCulture) / coefficient,
                    double.Parse(string.IsNullOrEmpty(MaxArea.Value) ? "0" : MaxArea.Value, CultureInfo.CurrentCulture) / coefficient);

            var draw = _processor.DrawPoints(
                _image,
                _pivot,
                _filtered,
                double.Parse(FontScale.Value, CultureInfo.CurrentCulture),
                HorizontalOffset.Value,
                VerticalOffset.Value);
            Image.Value = draw.ToImage<Bgr, byte>().ToBitmapSource();
        }

        private void OnImagePathChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImagePath.Value))
            {
                Image.Value = null;
                return;
            }

            _processor = new ImageProcessor(ImagePath.Value);

            _image = _processor.LoadImage();
            var contours = _processor.FindContours(_image);
            _pivot = _processor.FindPivot(contours);
            _powder = _processor.FindPowder(contours, _pivot);

            AutomaticFilter.Value = true;

            OnImageParameterChanged(sender, e);
        }

        private void OnExcelExporting(object parameter)
        {
            var a = SquireSize.Value * SquireSize.Value / CvInvoke.ContourArea(_pivot[0]);
            var p = SquireSize.Value / (CvInvoke.ArcLength(_pivot[0], true) / 4);

            var areas = _processor.GetAreas(_filtered)
                .Select(area => area * a)
                .ToList();

            var perimeters = _processor.GetPerimeters(_filtered)
                .Select(perimeter => perimeter * p)
                .ToList();

            var dimensions = _processor.GetDimensions(_filtered)
                .Select(d => new SizeF(d.Width * (float)p, d.Height * (float)p))
                .ToList();

            string path;

            using (var dialog = new CommonSaveFileDialog("Экспорт в файл Excel")
            {
                AddToMostRecentlyUsedList = true,
                EnsureFileExists = true,
                RestoreDirectory = true,
                ShowHiddenItems = false,
                DefaultFileName = "AnalyzeResults",
                DefaultExtension = ".xlsx",
                Filters = { new CommonFileDialogFilter("Excel file format", "*.xlsx") }
            })
            {
                var dialogResult = dialog.ShowDialog();

                if (dialogResult != CommonFileDialogResult.Ok)
                    return;

                path = dialog.FileName;
            }

            var exporter = new ExcelExporter(path);

            var export = (Action)delegate {

                while (true)
                {
                    try
                    {
                        exporter.ExportData(dimensions, perimeters, areas, Image.Value);

                        var dialogResult = MessageBox.Show(
                            "Экспорт успешно выполнен!\n" +
                            "Хотите открыть экспортированный файл?",
                            "Информация",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (dialogResult == MessageBoxResult.Yes)
                            Process.Start(path);

                        return;
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (ex.InnerException?.InnerException?.GetType() == typeof(IOException))
                        {
                            var result = MessageBox.Show(
                                "Не удалось сохранить файл, так как он открыт в другой программе!\n" +
                                "Повторить попытку?",
                                "Ошибка",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result != MessageBoxResult.Yes)
                                return;
                        }
                        else
                            MessageBox.Show(
                                "При сохранении файла возникала следующая ошибка:\n" +
                                ex.Message,
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "При сохранении файла возникала следующая ошибка:\n" +
                            ex.Message,
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        break;
                    }
                }
            };

            export();
        }

        private bool CanExportToExcel(object parameter)
        {
            return _image != null;
        }

        private void OnAbout(object parameter)
        {
            MessageBox.Show("Приложение для определения размеров частиц.\n" +
                            $"Версия: {CurrentVersion}\n" +
                            $"Разработчик: EasyBartholomew\n" +
                            $"\nGitHub: https://github.com/EasyBartholomew/PowderDetector",
                            "О приложении",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
