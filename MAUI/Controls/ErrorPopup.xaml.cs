using Mopups.Pages;
using Mopups.Services;

namespace Dots.Controls;

public partial class ErrorPopup : PopupPage
{
	public ErrorPopup(string title, string info)
	{
		InitializeComponent();
		TitleLabel.Text = title;
        InfoLabel.Text = info;
    }

    async void Cancel_Clicked(object sender, EventArgs e)
    {
        await MopupService.Instance.PopAsync();
    }
}