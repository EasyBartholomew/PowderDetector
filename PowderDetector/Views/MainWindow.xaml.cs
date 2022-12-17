using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PowderDetector.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetPath(TextBox target, string path)
        {
            target.Text = string.Empty;
            target.Text = path;
        }

        private void SetImagePath(string path) => SetPath(ImagePathTextBox, path);

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OnLoadImageClick(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog("Выберите изображение")
                   {
                       AddToMostRecentlyUsedList = true,
                       AllowNonFileSystemItems = false,
                       AllowPropertyEditing = true,
                       Multiselect = false,
                       DefaultFileName = "Частицы",
                       DefaultExtension = ".jpg",
                       EnsurePathExists = true,
                       EnsureFileExists = true,
                       Filters =
                       {
                           new CommonFileDialogFilter("JPEG Image", "*.jpg;*.jpeg")
                       }
                   })
            {
                var result = dialog.ShowDialog();

                if (result != CommonFileDialogResult.Ok)
                    return;

                SetImagePath(dialog.FileName);
            }
        }

        private void OnImageDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (fileNames?.Length > 0)
                {
                    var binaryPath = fileNames.FirstOrDefault(p => 
                        Path.GetExtension(p).ToLower() == ".jpeg"
                        || Path.GetExtension(p).ToLower() == ".jpg");

                    if (binaryPath != null)
                        SetImagePath(binaryPath);
                    else
                        MessageBox.Show("Вы пытаетесь загрузить недопустимый формат файла!\n" +
                                        "Допустимый формат файла \".jpeg\" или \".jpg\".\n" +
                                        "Попробуйте ещё раз!",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                }

                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
