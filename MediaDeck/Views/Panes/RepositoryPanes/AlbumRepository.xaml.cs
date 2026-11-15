using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Repositories.Objects;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Panes.RepositoryPanes;
using MediaDeck.Views.Dialogs;
using MediaDeck.Views.Helpers;
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
	private bool _isDialogOpen;

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

	private async void RenameAlbumMenuItem_Click(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: AlbumObject album }) {
			await this.RenameAlbumAsync(album);
		}
	}

	private async void DeleteAlbumMenuItem_Click(object sender, RoutedEventArgs e) {
		if (sender is FrameworkElement { Tag: AlbumObject album }) {
			await this.ConfirmAndDeleteAlbumAsync(album);
		}
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
		if (this.ViewModel is not { } vm || this.XamlRoot is null || this._isDialogOpen) {
			return;
		}
		this._isDialogOpen = true;
		try {
			var dialog = Ioc.Default.GetRequiredService<NewAlbumDialog>();
			dialog.XamlRoot = this.XamlRoot;
			dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(dialog.FullAlbumPath)) {
				await vm.CreateAlbumAsync(dialog.FullAlbumPath);
			}
		} finally {
			this._isDialogOpen = false;
		}
	}

	/// <summary>
	/// アルバム名変更ダイアログを表示し、指定されたアルバムをリネームします。
	/// </summary>
	/// <param name="album">リネーム対象のアルバムオブジェクト。</param>
	private async Task RenameAlbumAsync(AlbumObject album) {
		if (this.ViewModel is not { } vm || this.XamlRoot is null || this._isDialogOpen) {
			return;
		}
		this._isDialogOpen = true;
		try {
			var dialog = Ioc.Default.GetRequiredService<RenameAlbumDialog>();
			dialog.XamlRoot = this.XamlRoot;
			dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
			dialog.Initialize(album.AlbumPath, newPath => vm.RenameAlbumAsync(album, newPath));
			await dialog.ShowAsync();
		} finally {
			this._isDialogOpen = false;
		}
	}

	/// <summary>
	/// 削除確認ダイアログを表示し、承認された場合は指定された単アルバムを削除します。
	/// </summary>
	/// <param name="album">削除対象のアルバムオブジェクト。</param>
	private async Task ConfirmAndDeleteAlbumAsync(AlbumObject album) {
		if (this.ViewModel is not { } vm || this.XamlRoot is null || !album.CanDelete || this._isDialogOpen) {
			return;
		}
		this._isDialogOpen = true;
		try {
			var stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
			var dialog = new ContentDialog {
				XamlRoot = this.XamlRoot,
				Title = string.Format(stringProvider.GetString("AlbumRepository_DeleteConfirmation_Title"), album.AlbumPath),
				Content = stringProvider.GetString("AlbumRepository_DeleteConfirmation_Content"),
				PrimaryButtonText = stringProvider.GetString("DialogButton_Delete"),
				CloseButtonText = stringProvider.GetString("DialogButton_Cancel"),
				DefaultButton = ContentDialogButton.Close
			};
			using var disposable = new CompositeDisposable();
			ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);
			if (await dialog.ShowAsync() == ContentDialogResult.Primary) {
				await vm.DeleteAlbumAsync(album);
			}
		} finally {
			this._isDialogOpen = false;
		}
	}
}

/// <summary>
/// AlbumRepository の基底クラス。UserControlBase を介して ViewModel プロパティを提供します。
/// </summary>
public abstract class AlbumRepositoryUserControl : UserControlBase<AlbumRepositoryViewModel>;