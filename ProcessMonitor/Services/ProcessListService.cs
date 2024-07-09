
using ProcessMonitor.Model;
using ProcessMonitor.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace ProcessMonitor.Services;
public class ProcessListService : ObservableCollection<ProcessInfo>
{
	private readonly IDispatcherTimer timer;
	public bool IsRunning { get; set; } = false;

	public ProcessListService()
	{
		this.timer = Dispatcher.GetForCurrentThread()?.CreateTimer()
			?? throw new Exception("Unable to create internal timer");
		this.timer.Tick += UpdateList;
	}

	public void Start(int updateIntervalSeconds)
	{
		if (this.IsRunning)
			return;

		if (updateIntervalSeconds < 1)
			updateIntervalSeconds = 1;

		this.timer.Interval = TimeSpan.FromSeconds(updateIntervalSeconds);
		this.UpdateList(this, null!);
		this.timer.Start();
		this.IsRunning = true;
		this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.IsRunning)));
	}

	public void Stop()
	{
		if (!this.IsRunning)
			return;
		this.timer.Stop();
		this.ClearItems();
		this.IsRunning = false;
	}
#pragma warning disable CA1416
	private void UpdateList(object? serder, EventArgs eventArgs)
	{
		try
		{
			var activeProcesses = Process
				.GetProcesses()
				.Select(item => new ProcessInfo()
				{
					Id = Convert.ToUInt32(item.Id),
					Name = item.ProcessName,
					UserName = item.GetAssociatedUserName(),
					Status = item.GetProcessStatus(),
					Cpu = item.GetProcessUsingCpu(),
					Memory = item.GetProcessMemoryUsage(),
					Description = item.GetProcessDescription()
                });

			this
				.Where(item => !activeProcesses.Any(c => c.Id == item.Id))
				.ToList()
				.ForEach(item => this.Remove(item));

			activeProcesses
				.Where(item => !this.Any(c => c.Id == item.Id))
				.ToList()
				.ForEach(item =>
				{
					int insertPosition = -1;
					for (int i = 0; i < this.Count; i++)
					{
						if (string.Compare(item.Name, this[i].Name) < 0)
						{
							insertPosition = i;
							break;
						}
					}
					if (insertPosition < 0)
						insertPosition = this.Count;
					this.InsertItem(insertPosition, item);
				});

			foreach (var item in this)
			{
				ProcessInfo activeProcess = activeProcesses.First(a => a.Id == item.Id);
				item.UserName = activeProcess.UserName;
				item.Status = activeProcess.Status;
				item.Cpu = activeProcess.Cpu;
				item.Memory = activeProcess.Memory;
				item.Description = activeProcess.Description;
			}
		}
		catch (Exception er)
		{
			Debug.WriteLine($"Error. ProcessListService.UpdateList: {er.Message}");
		}
	}
}
