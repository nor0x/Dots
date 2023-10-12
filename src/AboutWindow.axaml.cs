using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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

            App.Current.TryGetResource("IconFront", out var front);
            App.Current.TryGetResource("IconBack", out var back);
            _front = ((Image)front).Source;
            _back = ((Image)back).Source;
            IconImage.Source = _front;
        }

        private void OpenSourceButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
        }       
        
        private void SupportButton_Clicked(object? sender, Avalonia.Input.TappedEventArgs e)
        {
        }

        private async void Image_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (!_imageFlipped)
            {
                _imageFlipped = true;
                _canFlipBack = false;
                IconImage.RenderTransform = new MatrixTransform(new Matrix(1, 0, 0, 1, 1, 0));

                IconImage.Source = _back;
                await Task.Delay(300);
                _canFlipBack = true;
            }
        }

        private void Image_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_imageFlipped && _canFlipBack)
            {
                IconImage.RenderTransform = new MatrixTransform(new Matrix(-1, 0, 0, 1, 1, 0));

                IconImage.Source = _front;
                _imageFlipped = false;
                _canFlipBack = false;
            }
        }
    }
}
