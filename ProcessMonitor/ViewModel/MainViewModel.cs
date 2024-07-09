using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProcessMonitor.Services;

namespace ProcessMonitor.ViewModel;
public partial class MainViewModel : ObservableObject
{
	public ProcessListService ProcessListService { get; set; } = [];

	[ObservableProperty]
	private int updateIntervalSeconds = 1;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotRunning))]
	private bool isRunning = false;
	public bool IsNotRunning => !this.IsRunning;


	[RelayCommand]
	private void Start()
	{
		if (this.IsRunning)
			return;
		this.IsRunning = true;
		this.ProcessListService.Start(this.UpdateIntervalSeconds);
	}

	[RelayCommand]
	private void Stop()
	{
		if (this.IsNotRunning)
			return;
		this.IsRunning = false;
		this.ProcessListService.Stop();
	}
}
