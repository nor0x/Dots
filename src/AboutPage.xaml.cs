namespace Dots;

public partial class AboutPage : ContentPage
{
    bool _imageFlipped;
    public AboutPage()
    {
        InitializeComponent();
        VersionSpan.Text = AppInfo.VersionString;
    }

    bool canFlipBack = false;
    private async void IconImage_PointerExited(object sender, PointerEventArgs e)
    {
        //rotate back
        if (_imageFlipped && canFlipBack)
        {
            await IconImage.RotateYTo(90, 150, Easing.CubicInOut);
            IconImage.Source = "iconlogo.png";
            await IconImage.RotateYTo(0, 150, Easing.CubicInOut);
            _imageFlipped = false;
            canFlipBack = false;
        }
    }

    private async void IconImage_PointerEntered(object sender, PointerEventArgs e)
    {
        if (!_imageFlipped)
        {
            _imageFlipped = true;
            canFlipBack = false;
            await IconImage.RotateYTo(90, 150, Easing.CubicInOut);
            IconImage.Source = "iconlogoback.png";
            await IconImage.RotateYTo(180, 150, Easing.CubicInOut);
            await Task.Delay(300);
            canFlipBack = true;
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