using System.Reflection;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Dots.Helpers;
using Newtonsoft.Json.Linq;

#if MACOS
using Foundation;
#endif

namespace Dots
{
    public partial class AboutWindow : Window
    {
        bool _imageFlipped;
        bool _canFlipBack;
        IImage _front;
        IImage _back;
        public AboutWindow()
        {
            InitializeComponent();
#if WINDOWS
            var version = Assembly.GetEntryAssembly().GetName().Version;
            VersionRun.Text = version.ToString(3);
#endif

#if MACOS
            var v = NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleVersion")?.ToString();
            VersionRun.Text = v;
#endif
        }

        private void OpenSourceButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Constants.GithubUrl.OpenUrl();
        }

        private void SupportButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            Constants.SupportURl.OpenUrl();
        }
    }
}
