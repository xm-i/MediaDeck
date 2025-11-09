using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Sort;

using Windows.Graphics;

namespace MediaDeck.Views.Sort;
[AddTransient]
public sealed partial class SortManagerWindow : Window {
	public SortManagerWindow(SortManagerViewModel sortManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = sortManagerViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new SizeInt32(1000, 700));
	}

	public SortManagerViewModel ViewModel {
		get;
	}
}
