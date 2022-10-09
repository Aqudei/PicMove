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
using Microsoft.WindowsAPICodePack.Dialogs;
using PicMove.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace PicMove.ViewModels
{
    public class ShellViewModel : BindableBase
    {
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
        private DateTime? _dateTaken;
        private DateTime? _timePoint;

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

        public DateTime? TimePoint
        {
            get => _timePoint;
            set => SetProperty(ref _timePoint, value);
        }
        private DelegateCommand _executeTransferCommand;
        private DelegateCommand _selectDestinationFolderCommand;
        private string _destinationFolder;

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

        private void ExecuteTransfer()
        {
            var datePart = DateTaken?.ToShortDateString().Replace("/", "-");
            var timePart = TimePoint?.ToString("t")
                .Replace(":", "-")
                .Replace(" ", "")
                .ToUpper();

            var folderName = $"{LastName}_{FirstName}_{datePart}_{timePart}";
            var destination = Path.Combine(DestinationFolder, folderName);
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            foreach (var picInfo in _images.Where(s => s.Selected))
            {
                var finalName = Path.Combine(destination, Path.GetFileName(picInfo.FileName));
                File.Copy(picInfo.FileName, finalName);
            }
        }

        public ShellViewModel()
        {
            ImageListView = CollectionViewSource.GetDefaultView(_images);
            ImageListView.CurrentChanged += ImageListView_CurrentChanged;
        }

        private void ImageListView_CurrentChanged(object sender, EventArgs e)
        {
            if (ImageListView.CurrentItem != null)
            {
                var source = ImageListView.CurrentItem as PicInfo;
                CurrentImage = source.FileName;
            }
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
                var fileInfo = new FileInfo(file);
                _images.Add(new PicInfo
                {
                    DateCreated = fileInfo.CreationTime,
                    DateModified = fileInfo.LastWriteTime,
                    FileName = fileInfo.FullName
                });
            }
        }
    }
}
