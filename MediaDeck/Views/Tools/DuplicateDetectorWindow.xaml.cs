using Microsoft.UI.Xaml;

using MediaDeck.ViewModels.Tools;

using Windows.Graphics;

namespace MediaDeck.Views.Tools;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class DuplicateDetectorWindow : Window {
	public DuplicateDetectorWindow(DuplicateDetectorViewModel duplicateDetectorViewModel) {
		this.InitializeComponent();
		this.ViewModel = duplicateDetectorViewModel;
		this.AppWindow.Resize(new SizeInt32(900, 600));
	}

	public DuplicateDetectorViewModel ViewModel {
		get;
	}
}