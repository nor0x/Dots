using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Dots.Helpers;
using Newtonsoft.Json.Linq;

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
