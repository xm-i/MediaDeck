using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Tags;

using Windows.Graphics;

namespace MediaDeck.Views.Tags;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class TagManagerWindow {
	public TagManagerWindow(TagManagerViewModel tagManagerViewModel) {
		this.InitializeComponent();
		this.ViewModel = tagManagerViewModel;
		this.ViewModel.LoadCommand.Execute(Unit.Default);
		this.AppWindow.Resize(new(1000, 700));
	}

	public TagManagerViewModel ViewModel {
		get;
	}
}