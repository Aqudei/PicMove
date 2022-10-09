using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace PicMove.Models
{
    public class PicInfo : BindableBase
    {
        private bool _selected;
        public string FileName { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value);
        }
    }
}
