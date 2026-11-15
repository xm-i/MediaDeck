using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MediaDeck.Views.Panes.RepositoryPanes;

/// <summary>
/// アルバムリポジトリペインのビュー。
/// </summary>
public sealed partial class AlbumRepository {
	/// <summary>
	/// <see cref="AlbumRepository"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public AlbumRepository() {
		this.InitializeComponent();
	}

	private void TreeViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		vm.SetRepositoryConditionCommand.Execute(Unit.Default);
	}

	private async void CreateAlbum_Click(object sender, RoutedEventArgs e) {
		await this.CreateAlbumAsync();
	}

	private void AlbumArea_ContextRequested(UIElement sender, ContextRequestedEventArgs args) {
		if (this.ViewModel is not { IsEmpty.Value: false }) {
			return;
		}
		if (args.OriginalSource is DependencyObject source && IsInsideTreeViewItemOrCheckBox(source, sender)) {
			return;
		}
		if (this.RootGrid.Resources["EmptyAreaContextMenu"] is not MenuFlyout flyout) {
			return;
		}
		if (args.TryGetPosition(sender, out var point)) {
			flyout.ShowAt(sender, new FlyoutShowOptions { Position = point });
		} else {
			flyout.ShowAt(this.RootGrid);
		}
		args.Handled = true;
	}

	/// <summary>
	/// 指定された要素が <see cref="TreeViewItem"/> または <see cref="CheckBox"/> の内部にあるかどうかを判定します。
	/// </summary>
	/// <param name="source">イベント発生元の要素。</param>
	/// <param name="root">探索を打ち切る親要素。</param>
	/// <returns>項目またはチェックボックスの内部にある場合は <c>true</c>。</returns>
	private static bool IsInsideTreeViewItemOrCheckBox(DependencyObject source, DependencyObject root) {
		var current = source;
		while (current != null && current != root) {
			if (current is TreeViewItem or CheckBox) {
				return true;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return false;
	}

	/// <summary>
	/// 新規アルバム作成ダイアログを表示し、入力されたパスでアルバムを作成します。
	/// </summary>
	private async Task CreateAlbumAsync() {
		if (this.ViewModel is not { } vm || this.XamlRoot is null) {
			return;
		}
		var dialog = Ioc.Default.GetRequiredService<NewAlbumDialog>();
		dialog.XamlRoot = this.XamlRoot;
		dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.FullAlbumPath)) {
			await vm.CreateAlbumAsync(dialog.FullAlbumPath);
		}
	}
}

/// <summary>
/// AlbumRepository の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class AlbumRepositoryUserControl : UserControlBase<AlbumRepositoryViewModel>;