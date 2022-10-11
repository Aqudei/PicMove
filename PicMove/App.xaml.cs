using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using PicMove.Views;
using Prism.Ioc;
using Prism.Unity;

namespace PicMove
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(DialogCoordinator.Instance);
        }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<Shell>();
            return w;
        }
    }
}
