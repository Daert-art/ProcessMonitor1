using ProcessMonitor.ViewModel;

namespace ProcessMonitor.View;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel viewModel)
	{
		InitializeComponent();
		this.BindingContext = viewModel;
	}
}

