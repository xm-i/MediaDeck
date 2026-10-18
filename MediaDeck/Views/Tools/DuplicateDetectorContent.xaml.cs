using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.ViewModels.Tools;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Tools;

/// <summary>
/// 重複ファイル検出コンテンツのビュー。
/// </summary>
public sealed partial class DuplicateDetectorContent : DuplicateDetectorContentUserControl {
	/// <summary>
	/// <see cref="DuplicateDetectorContent"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public DuplicateDetectorContent() {
		this.InitializeComponent();
	}

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

/// <summary>
/// DuplicateDetectorContent の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class DuplicateDetectorContentUserControl : UserControlBase<DuplicateDetectorViewModel>;