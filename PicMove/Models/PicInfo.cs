using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Prism.Mvvm;

namespace PicMove.Models
{
    public class PicInfo : BindableBase
    {
        private bool _selected;
        private BitmapSource _preview;
        public string FileName { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string FullPath { get; set; }
        public BitmapSource Thumbnail { get; set; }

        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }

        public BitmapSource Preview
        {
            get => _preview;
            set => SetProperty(ref _preview, value);
        }
    }
}
