using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.ViewModels.Tools;

using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Tools;

public sealed partial class DuplicateDetectorContent {
	public DuplicateDetectorContent() {
		this.InitializeComponent();
	}

	public DuplicateDetectorViewModel ViewModel {
		get {
			return (DuplicateDetectorViewModel)this.GetValue(ViewModelProperty);
		}

		set {
			this.SetValue(ViewModelProperty, value);
		}
	}

	public static readonly DependencyProperty ViewModelProperty =
		DependencyProperty.Register(nameof(ViewModel),
			typeof(DuplicateDetectorViewModel),
			typeof(DuplicateDetectorContent),
			new(null));

	private async void OnOpenFileClick(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { DataContext: IMediaItemViewModel fileViewModel }) {
			await fileViewModel.ExecuteFileAsync();
		}
	}

	private void OnShowInExplorerClick(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { DataContext: IMediaItemViewModel fileViewModel }) {
			DuplicateDetectorViewModel.ShowInExplorer(fileViewModel.FilePath);
		}
	}
}