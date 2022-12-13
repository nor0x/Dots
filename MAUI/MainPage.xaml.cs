using System;
using System.Diagnostics;

namespace Dots;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
        Debug.WriteLine("hello world!");
    }
}

