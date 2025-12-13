using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Tools;

using Windows.Graphics;

namespace MediaDeck.Views.Tools;
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class BackgroundTasksWindow : Window {
	public BackgroundTasksWindow(BackgroundTasksViewModel backgroundTasksViewModel) {
		this.InitializeComponent();
		this.ViewModel = backgroundTasksViewModel;
		this.AppWindow.Resize(new SizeInt32(400, 200));
	}

	public BackgroundTasksViewModel ViewModel {
		get;
	}
}
