using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
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

            //App.Current.TryGetResource("IconFront", out var front);
            //App.Current.TryGetResource("IconBack", out var back);
            //_front = ((Image)front).Source;
            //_back = ((Image)back).Source;
            //IconImage.Source = _front;
        }

        private void OpenSourceButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
        }       
        
        private void SupportButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
        }
    }
}
