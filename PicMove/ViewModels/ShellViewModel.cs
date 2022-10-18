using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using PicMove.Models;
using Prism.Commands;
using Prism.Mvvm;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace PicMove.ViewModels
{
    public class ShellViewModel : BindableBase
    {
        private readonly IDialogCoordinator _dialogCoordinator;
        private string _selectedFolder;
        private const string FolderInDesktop = "Need to be imported to Dolphin";

        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        public string DestinationFolder
        {
            get => _destinationFolder;
            set => SetProperty(ref _destinationFolder, value);
        }

        private DelegateCommand _selectedFolderCommand;

        private ObservableCollection<PicInfo> _images = new ObservableCollection<PicInfo>();
        public ObservableCollection<PicInfo> SelectedItems { get; set; } = new ObservableCollection<PicInfo>();
        private ICollectionView _imageListView;


        private BitmapSource _currentImage;
        private string _lastName;
        private string _firstName;
        private DateTime? _dateTaken = DateTime.Now;
        private string _timePoint;

        public BitmapSource CurrentImage
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
        private int _selectedCount;
        private DelegateCommand<IList<object>> _checkSelectedCommand;
        private DelegateCommand<IList<object>> _uncheckSelectedCommand;

        public DelegateCommand ExecuteTransferCommand =>
            _executeTransferCommand ??= new DelegateCommand(ExecuteTransfer);

        private async void ExecuteTransfer() => await Task.Run(DoExecuteTransferAsync);

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

        public DelegateCommand<IList<object>> CheckSelectedCommand => _checkSelectedCommand ??= new DelegateCommand<IList<object>>(CheckSelected);

        public DelegateCommand<IList<object>> UncheckSelectedCommand => _uncheckSelectedCommand ??= new DelegateCommand<IList<object>>(UncheckSelected);

        private void UncheckSelected(IList<object> selectedItems)
        {
            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is PicInfo picInfo)
                {
                    picInfo.Selected = false;
                }
            }
        }

        private void CheckSelected(IList<object> selectedItems)
        {
            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is PicInfo picInfo)
                {
                    picInfo.Selected = true;
                }
            }
        }



        private async Task DoExecuteTransferAsync()
        {
            CurrentImage = null;

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
            var selected = _images.Where(s => s.Selected).ToArray();

            foreach (var picInfo in selected)
            {
                var finalName = Path.Combine(destination, picInfo.FileName);
                File.Copy(picInfo.FullPath, finalName, true);
            }

            CreateDesktopShortcut(destination);

            await progress.CloseAsync();

            var response = await _dialogCoordinator.ShowMessageAsync(this, "Confirm Delete",
                "Do you want to delete files from source folder?", MessageDialogStyle.AffirmativeAndNegative);
            if (response == MessageDialogResult.Affirmative)
            {

                var picInfos = _images.Where(s => s.Selected).ToArray();
                var deleteUs = picInfos.Select(p=>p.FullPath).ToArray();

                foreach (var picInfo in picInfos)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => _images.Remove(picInfo));

                    picInfo.FullPath = null;
                    picInfo.Thumbnail = null;
                }

                foreach (var pic in deleteUs)
                {
                    File.Delete(pic);
                }

                GC.Collect();
            }
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.DestinationFolder = DestinationFolder;
            Properties.Settings.Default.SourceFolder = SelectedFolder;
            Properties.Settings.Default.Save();
        }

        private void LoadSettings()
        {
            SelectedFolder = Properties.Settings.Default.SourceFolder;
            DestinationFolder = Properties.Settings.Default.DestinationFolder;
        }

        private static void CreateDesktopShortcut(string destination)
        {
            var shortcut = new WindowsShortcutFactory.WindowsShortcut();
            shortcut.Path = destination;
            var folderInDesktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), FolderInDesktop);
            if (!Directory.Exists(folderInDesktop))
                Directory.CreateDirectory(folderInDesktop);

            var scPath = Path.Combine(folderInDesktop, Path.GetFileName(destination) + ".lnk");
            shortcut.Save(scPath);
        }

        public ShellViewModel(IDialogCoordinator dialogCoordinator)
        {
            _dialogCoordinator = dialogCoordinator;
            ImageListView = CollectionViewSource.GetDefaultView(_images);
            ImageListView.CurrentChanged += ImageListView_CurrentChanged;
            ImageListView.CollectionChanged += ImageListView_CollectionChanged;

            LoadSettings();


            if (!string.IsNullOrWhiteSpace(SelectedFolder))
                Application.Current.Dispatcher.BeginInvoke(() => Task.Run(LoadImagesAsync));

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedFolder) || e.PropertyName == nameof(DestinationFolder))
                {
                    SaveSettings();
                }
            };
        }

        private void PicInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        private void ImageListView_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Count = _images.Count;
            SelectedCount = _images.Count(p => p.Selected);
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
            CurrentImage = source?.Preview;
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

        private async void SelectFolder()
        {
            var ofd = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            var dlg = ofd.ShowDialog();
            if (dlg == CommonFileDialogResult.Ok)
            {
                SelectedFolder = ofd.FileName;
                await Task.Run(LoadImagesAsync);
            }
        }

        private async Task LoadImagesAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(_images.Clear);

            if (!Directory.Exists(SelectedFolder))
                return;

            var progress = await _dialogCoordinator.ShowProgressAsync(this, "Processing", "Loading images...");
            progress.SetIndeterminate();

            var files = Directory.EnumerateFiles(SelectedFolder, "*.jp*g");
            foreach (var file in files)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var fileInfo = new FileInfo(file);

                    var thumb = new BitmapImage();
                    thumb.BeginInit();

                    thumb.UriSource = new Uri(file);
                    thumb.DecodePixelHeight = 100;
                    thumb.DecodePixelWidth = 100;
                    thumb.CacheOption = BitmapCacheOption.OnLoad;
                    thumb.EndInit();
                    thumb.Freeze();

                    var preview = new BitmapImage();
                    preview.BeginInit();
                    preview.UriSource = new Uri(file);
                    preview.CacheOption = BitmapCacheOption.OnLoad;
                    preview.EndInit();
                    preview.Freeze();

                    var picInfo = new PicInfo
                    {
                        DateCreated = fileInfo.CreationTime,
                        DateModified = fileInfo.LastWriteTime,
                        FullPath = fileInfo.FullName,
                        FileName = fileInfo.Name,
                        Thumbnail = thumb,
                        Preview = preview
                    };
                    picInfo.PropertyChanged += PicInfo_PropertyChanged1;
                    _images.Add(picInfo);
                });


            }

            await progress.CloseAsync();
        }

        private void PicInfo_PropertyChanged1(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Contains("Selected"))
            {
                SelectedCount = _images.Count(p => p.Selected);
            }
        }
    }
}
