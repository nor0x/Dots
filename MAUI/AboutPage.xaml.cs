namespace Dots;

public partial class AboutPage : ContentPage
{
    bool _imageFlipped;
    public AboutPage()
    {
        InitializeComponent();
    }

    private async void IconImage_PointerExited(object sender, PointerEventArgs e)
    {
        //rotate back
        if (_imageFlipped)
        {
            await IconImage.RotateYTo(90, 150, Easing.CubicInOut);
            IconImage.Source = "iconlogo.png";
            await IconImage.RotateYTo(0, 150, Easing.CubicInOut);
            _imageFlipped = false;
        }
    }

    private async void IconImage_PointerEntered(object sender, PointerEventArgs e)
    {
        if (!_imageFlipped)
        {
            //3d flip animation of IconImage
            await IconImage.RotateYTo(90, 150, Easing.CubicInOut);
            IconImage.Source = "iconlogoback.png";
            await IconImage.RotateYTo(180, 150, Easing.CubicInOut);
            _imageFlipped = true;
        }
    }

    private void OpenSourceButton_Clicked(object sender, EventArgs e)
    {
        Browser.Default.OpenAsync("https://github.com/nor0x/dots");
    }

    private void SupportButton_Clicked(object sender, EventArgs e)
    {
        Browser.Default.OpenAsync("https://ko-fi.com/j0hnny");
    }
}