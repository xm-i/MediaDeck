using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using MediaDeck.FileTypes.Base.ViewModels.Interfaces;
using MediaDeck.ViewModels.Tools;

namespace MediaDeck.Views.Tools;

public sealed partial class DuplicateDetectorContent : UserControl {
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
		DependencyProperty.Register(
			nameof(ViewModel),
			typeof(DuplicateDetectorViewModel),
			typeof(DuplicateDetectorContent),
			new PropertyMetadata(null));

	private async void OnOpenFileClick(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { DataContext: IFileViewModel fileViewModel }) {
			await fileViewModel.ExecuteFileAsync();
		}
	}

	private void OnShowInExplorerClick(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { DataContext: IFileViewModel fileViewModel }) {
			DuplicateDetectorViewModel.ShowInExplorer(fileViewModel.FilePath);
		}
	}
}
