using Dots.Helpers;
using System;
using System.Diagnostics;

namespace Dots;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
		UpdateDateSpan.Text = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("hello world!");
    }

    private async void SdkCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var open = DetailsPanel.Width == 0;
        if(open)
        {
            await DetailsPanel.WidthTo(300);
        }
        else
        {
            await DetailsPanel.WidthTo(0);

        }
    }
}

