using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using PicMove.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace PicMove.ViewModels
{
    public class ShellViewModel : BindableBase
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private string _selectedFolder;

        public string DestinationFolder
        {
            get => _destinationFolder;
            set => SetProperty(ref _destinationFolder, value);
        }

        private DelegateCommand _selectedFolderCommand;

        private ObservableCollection<PicInfo> _images = new ObservableCollection<PicInfo>();
        private ICollectionView _imageListView;


        private string _currentImage;
        private string _lastName;
        private string _firstName;
        private DateTime? _dateTaken = DateTime.Now;
        private string _timePoint;

        public string CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public DateTime? DateTaken
        {
            get => _dateTaken;
            set => SetProperty(ref _dateTaken, value);
        }

        public string TimePoint
        {
            get => _timePoint;
            set => SetProperty(ref _timePoint, value);
        }
        private DelegateCommand _executeTransferCommand;
        private DelegateCommand _selectDestinationFolderCommand;
        private string _destinationFolder;
        private int _count;
        private DelegateCommand _checkAllCommand;
        private DelegateCommand _uncheckAllCommand;

        public DelegateCommand ExecuteTransferCommand =>
            _executeTransferCommand ??= new DelegateCommand(ExecuteTransfer);

        public DelegateCommand SelectDestinationFolderCommand => _selectDestinationFolderCommand ??= new DelegateCommand(SelectDestination);

        private void SelectDestination()
        {
            var ofd = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            var dlg = ofd.ShowDialog();
            if (dlg != CommonFileDialogResult.Ok) return;
            DestinationFolder = ofd.FileName;
        }

        private async void ExecuteTransfer()
        {
            if (string.IsNullOrWhiteSpace(DestinationFolder) || string.IsNullOrWhiteSpace(LastName)
                || string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(TimePoint) || DateTaken == null)
            {
                await _dialogCoordinator.ShowMessageAsync(this, "Error - Missing Info", "Please complete destination info to proceed!");
                return;
            }
            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Please wait", "Copying files");
            progress.SetIndeterminate();

            var datePart = DateTaken?.ToShortDateString().Replace("/", "-");

            var folderName = $"{LastName}_{FirstName}_{datePart}_{TimePoint}";
            var destination = Path.Combine(DestinationFolder, folderName);
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            foreach (var picInfo in _images.Where(s => s.Selected))
            {
                var finalName = Path.Combine(destination, picInfo.FileName);
                File.Copy(picInfo.FullPath, finalName);
            }

            CreateDesktopShortcut(destination);

            await progress.CloseAsync();

            var response = await _dialogCoordinator.ShowMessageAsync(this, "Confirm Delete",
                "Do you want to delete files from source folder?", MessageDialogStyle.AffirmativeAndNegative);
            if (response == MessageDialogResult.Affirmative)
            {
                foreach (var picInfo in _images.Where(s => s.Selected))
                {
                    File.Delete(picInfo.FullPath);
                    _images.Remove(picInfo);
                }
            }
        }

        private static void CreateDesktopShortcut(string destination)
        {
            var shortcut = new WindowsShortcutFactory.WindowsShortcut();
            shortcut.Path = destination;
            var scPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.GetFileName(destination) + ".lnk");
            shortcut.Save(scPath);
        }

        public ShellViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            ImageListView = CollectionViewSource.GetDefaultView(_images);
            ImageListView.CurrentChanged += ImageListView_CurrentChanged;
            ImageListView.CollectionChanged += ImageListView_CollectionChanged;
        }

        private void ImageListView_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Count = _images.Count;
        }



        public DelegateCommand CheckAllCommand => _checkAllCommand ??= new DelegateCommand(CheckAll);
        public DelegateCommand UncheckAllCommand => _uncheckAllCommand ??= new DelegateCommand(UncheckAll);

        private void UncheckAll()
        {
            foreach (var img in _images)
            {
                img.Selected = false;
            }
        }

        private void CheckAll()
        {
            foreach (var img in _images)
            {
                img.Selected = true;
            }
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        private void ImageListView_CurrentChanged(object sender, EventArgs e)
        {
            if (ImageListView.CurrentItem == null) return;

            var source = ImageListView.CurrentItem as PicInfo;
            CurrentImage = source?.FullPath;
        }

        public ICollectionView ImageListView
        {
            get => _imageListView;
            set => SetProperty(ref _imageListView, value);
        }

        public string SelectedFolder
        {
            get => _selectedFolder;
            set => SetProperty(ref _selectedFolder, value);
        }

        public DelegateCommand SelectFolderCommand => _selectedFolderCommand ??= new DelegateCommand(SelectFolder);

        private void SelectFolder()
        {
            var ofd = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            var dlg = ofd.ShowDialog();
            if (dlg == CommonFileDialogResult.Ok)
            {
                SelectedFolder = ofd.FileName;
                LoadImages();
            }
        }

        private void LoadImages()
        {

            var files = Directory.EnumerateFiles(SelectedFolder, "*.jp*g");
            foreach (var file in files)
            {
                var thumb = new BitmapImage();
                thumb.BeginInit();

                thumb.UriSource = new Uri(file);
                thumb.DecodePixelHeight = 100;
                thumb.DecodePixelWidth = 100;
                thumb.EndInit();

                var fileInfo = new FileInfo(file);
                _images.Add(new PicInfo
                {
                    DateCreated = fileInfo.CreationTime,
                    DateModified = fileInfo.LastWriteTime,
                    FullPath = fileInfo.FullName,
                    FileName = fileInfo.Name,
                    Thumbnail = thumb
                });
            }
        }
    }
}
