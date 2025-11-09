using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Tags;

using Windows.Graphics;

namespace MediaDeck.Views.Tags;
[AddTransient]
public sealed partial class TagManagerWindow : Window {
	public TagManagerWindow(TagManagerViewModel tagManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = tagManagerViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new SizeInt32(1000, 700));
	}

	public TagManagerViewModel ViewModel {
		get;
	}
}
