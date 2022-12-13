namespace Dots;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		BindingContext = vm;
		InitializeComponent();
	}
}

