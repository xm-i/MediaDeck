using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Core.Stores.Config;
using MediaDeck.Services;
using MediaDeck.ViewModels.Panes.ViewerPanes;
using MediaDeck.Views.Dialogs;
using MediaDeck.Views.Helpers;
using MediaDeck.Views.Thumbnails;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Serilog;
using Windows.System;
using Windows.UI.Core;

namespace MediaDeck.Views.Panes.ViewerPanes;

/// <summary>
/// 各種メディアビューアペインの基底クラス。
/// 選択変更、ダブルタップ実行、コンテキストメニュー、Ctrl+ホイールによるズームなどの共通UI動作を提供します。
/// </summary>
/// <typeparam name="T">ビューアの ViewModel 型。</typeparam>
public abstract class ViewerPaneBase<T> : UserControlBase<T> where T : ViewerPaneViewModelBase {
	private const string OpenWithIconGlyph = "\uE7AC";
	private const string OpenFolderIconGlyph = "\uE838";
	private const string CopyIconGlyph = "\uE8C8";
	private const string AlbumIconGlyph = "\uE8FD";
	private const string CreateAlbumIconGlyph = "\uE710";
	private const string RecreateThumbnailIconGlyph = "\uEB9F";
	private const string UpdateMetadataIconGlyph = "\uE72C";
	private const string RemoveFileIconGlyph = "\uE74D";

	private readonly IWindowService _windowService;
	private readonly IStringProvider _stringProvider;
	private int _contextMenuOpeningVersion;
	private bool _isDialogOpen;

	/// <summary>
	/// <see cref="ViewerPaneBase{T}"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	protected ViewerPaneBase() {
		this._windowService = Ioc.Default.GetRequiredService<IWindowService>();
		this._stringProvider = Ioc.Default.GetRequiredService<IStringProvider>();
	}

	/// <summary>
	/// リストコントロールの選択項目変更ハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected virtual void List_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel is not { } vm) {
			return;
		}
		if (sender is ListViewBase listViewBase) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = listViewBase.SelectedItems.Select(x => x as IMediaItemViewModel).Where(x => x is { }).ToArray()!;
			vm.MediaContentLibraryViewModel.SelectedFile.Value = listViewBase.SelectedItem as IMediaItemViewModel;
		} else if (sender is ListBox listBox) {
			vm.MediaContentLibraryViewModel.SelectedFiles.Value = listBox.SelectedItems.Select(x => x as IMediaItemViewModel).Where(x => x is { }).ToArray()!;
			vm.MediaContentLibraryViewModel.SelectedFile.Value = listBox.SelectedItem as IMediaItemViewModel;
		}
	}

	/// <summary>
	/// ファイルアイテムのダブルタップハンドラ。既定のプログラムまたは関連付けで実行します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected async void File_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
		if (sender is not Grid grid) {
			return;
		}
		if (grid.DataContext is not IMediaItemViewModel fileViewModel) {
			return;
		}
		await fileViewModel.ExecuteFileAsync();
	}

	/// <summary>
	/// ファイルアイテムの右タップハンドラ。共通コンテキストメニューを構築して表示します。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected void List_RightTapped(object sender, RightTappedRoutedEventArgs e) {
		if (sender is not FrameworkElement parentControl || this.ViewModel is null) {
			return;
		}

		var targetElement = FindMediaItemElement(e.OriginalSource as DependencyObject, parentControl);
		if (targetElement?.DataContext is not IMediaItemViewModel fileViewModel) {
			return;
		}

		var menuFlyout = this.CreateFileContextMenu(fileViewModel);
		menuFlyout.ShowAt(targetElement, new FlyoutShowOptions {
			Position = e.GetPosition(targetElement),
		});
		e.Handled = true;
	}

	/// <summary>
	/// ビジュアルツリーを親方向へ遡り、<see cref="IMediaItemViewModel"/> を DataContext に持つ要素を検索します。
	/// </summary>
	/// <param name="source">探索開始要素。</param>
	/// <param name="root">探索を打ち切る親要素。</param>
	/// <returns>見つかった要素。存在しない場合は <c>null</c>。</returns>
	private static FrameworkElement? FindMediaItemElement(DependencyObject? source, DependencyObject root) {
		var current = source;
		while (current != null && !ReferenceEquals(current, root)) {
			if (current is FrameworkElement { DataContext: IMediaItemViewModel } fe) {
				return fe;
			}
			current = VisualTreeHelper.GetParent(current);
		}
		return null;
	}

	/// <summary>
	/// メディアアイテム用のコンテキストメニューを構築します。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	/// <returns>構築された <see cref="MenuFlyout"/>。</returns>
	private MenuFlyout CreateFileContextMenu(IMediaItemViewModel targetFile) {
		var menuFlyout = new MenuFlyout();

		this.AddOpenAndLinkSection(menuFlyout, targetFile);
		menuFlyout.Items.Add(new MenuFlyoutSeparator());
		this.AddCollectionSection(menuFlyout, targetFile);
		menuFlyout.Items.Add(new MenuFlyoutSeparator());
		this.AddMaintenanceSection(menuFlyout, targetFile);
		menuFlyout.Items.Add(new MenuFlyoutSeparator());
		this.AddDeleteSection(menuFlyout, targetFile);

		return menuFlyout;
	}

	/// <summary>
	/// 「開く・外部連携」セクションのメニュー項目を追加します。
	/// </summary>
	/// <param name="menuFlyout">追加先のメニュー。</param>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	private void AddOpenAndLinkSection(MenuFlyout menuFlyout, IMediaItemViewModel targetFile) {
		var programs = this.ViewModel!.GetExternalPrograms(targetFile.MediaType);
		foreach (var program in programs) {
			var name = string.IsNullOrWhiteSpace(program.Name.Value)
				? this._stringProvider.GetString("Viewer_ContextOpenWithExternal")
				: this._stringProvider.GetString("Viewer_ContextOpenWithFormat", program.Name.Value);
			var displayText = program.IsDefault.Value
				? $"{name}{this._stringProvider.GetString("Viewer_ContextDefaultSuffix")}"
				: name;
			var capturedProgram = program;
			menuFlyout.Items.Add(CreateMenuItem(
				displayText,
				OpenWithIconGlyph,
				async (_, _) => await this.ExecuteExternalProgramAsync(capturedProgram, targetFile)));
		}

		menuFlyout.Items.Add(CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextOpenFolder"),
			OpenFolderIconGlyph,
			(_, _) => this.ViewModel?.ShowInExplorer(targetFile)));

		menuFlyout.Items.Add(CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextCopyFilePath"),
			CopyIconGlyph,
			(_, _) => this.CopyFilePaths(targetFile)));
	}

	/// <summary>
	/// 「コレクション整理」セクションのメニュー項目を追加します。
	/// </summary>
	/// <param name="menuFlyout">追加先のメニュー。</param>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	private void AddCollectionSection(MenuFlyout menuFlyout, IMediaItemViewModel targetFile) {
		var addRoot = new MenuFlyoutSubItem {
			Text = this._stringProvider.GetString("Viewer_ContextAddToAlbum"),
			Icon = CreateFontIcon(AlbumIconGlyph),
		};

		// 読み込み完了前や取得失敗時にもサブメニューが空にならないよう、新規作成項目を先行配置する
		addRoot.Items.Add(this.CreateNewAlbumMenuItem(targetFile));
		menuFlyout.Items.Add(addRoot);

		var version = ++this._contextMenuOpeningVersion;
		_ = this.PopulateRecentAlbumsAsync(addRoot, targetFile, version);
	}

	/// <summary>
	/// 「メディアメンテナンス」セクションのメニュー項目を追加します。
	/// </summary>
	/// <param name="menuFlyout">追加先のメニュー。</param>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	private void AddMaintenanceSection(MenuFlyout menuFlyout, IMediaItemViewModel targetFile) {
		menuFlyout.Items.Add(CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextRecreateThumbnail"),
			RecreateThumbnailIconGlyph,
			(_, _) => this.OpenThumbnailRegeneration(targetFile)));

		menuFlyout.Items.Add(CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextUpdateMetadata"),
			UpdateMetadataIconGlyph,
			(_, _) => this.UpdateMetadata(targetFile)));
	}

	/// <summary>
	/// 「削除」セクションのメニュー項目を追加します。
	/// </summary>
	/// <param name="menuFlyout">追加先のメニュー。</param>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	private void AddDeleteSection(MenuFlyout menuFlyout, IMediaItemViewModel targetFile) {
		menuFlyout.Items.Add(CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextRemoveFile"),
			RemoveFileIconGlyph,
			async (_, _) => await this.ConfirmAndRemoveFilesAsync(targetFile)));
	}

	/// <summary>
	/// 指定されたテキスト・アイコン・クリックハンドラを持つ <see cref="MenuFlyoutItem"/> を生成します。
	/// </summary>
	/// <param name="text">メニュー表示文字列。</param>
	/// <param name="iconGlyph">アイコングリフ。</param>
	/// <param name="onClick">クリック時のハンドラ。</param>
	/// <returns>生成された <see cref="MenuFlyoutItem"/>。</returns>
	private static MenuFlyoutItem CreateMenuItem(string text, string iconGlyph, RoutedEventHandler onClick) {
		var item = new MenuFlyoutItem {
			Text = text,
			Icon = CreateFontIcon(iconGlyph),
		};
		item.Click += onClick;
		return item;
	}

	/// <summary>
	/// 指定されたグリフを持つ <see cref="FontIcon"/> を生成します。
	/// </summary>
	/// <param name="glyph">フォントグリフ。</param>
	/// <returns>生成された <see cref="FontIcon"/>。</returns>
	private static FontIcon CreateFontIcon(string glyph) {
		return new FontIcon {
			Glyph = glyph,
		};
	}

	/// <summary>
	/// 「アルバムを新規作成」のサブメニュー項目を生成します。
	/// </summary>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	/// <returns>生成された <see cref="MenuFlyoutItem"/>。</returns>
	private MenuFlyoutItem CreateNewAlbumMenuItem(IMediaItemViewModel targetFile) {
		return CreateMenuItem(
			this._stringProvider.GetString("Viewer_ContextCreateNewAlbum"),
			CreateAlbumIconGlyph,
			async (_, _) => await this.CreateAlbumAndAddItemsAsync(targetFile));
	}

	/// <summary>
	/// コンテキストメニューの最近使用したアルバム一覧を非同期に読み込み、サブメニューを構築します。
	/// </summary>
	/// <param name="addRoot">構築対象のルートサブメニュー。</param>
	/// <param name="targetFile">対象のメディアアイテム。</param>
	/// <param name="version">競合検知用のメニュー展開バージョン。</param>
	private async Task PopulateRecentAlbumsAsync(MenuFlyoutSubItem addRoot, IMediaItemViewModel targetFile, int version) {
		try {
			if (this.ViewModel is null) {
				return;
			}

			var paths = await this.ViewModel.GetRecentAlbumPathsAsync(12);
			if (version != this._contextMenuOpeningVersion) {
				return;
			}

			addRoot.Items.Clear();
			foreach (var path in paths) {
				var capturedPath = path;
				addRoot.Items.Add(CreateMenuItem(
					capturedPath,
					AlbumIconGlyph,
					async (_, _) => await this.AddToAlbumAsync(capturedPath, targetFile)));
			}
			if (addRoot.Items.Count > 0) {
				addRoot.Items.Add(new MenuFlyoutSeparator());
			}
			addRoot.Items.Add(this.CreateNewAlbumMenuItem(targetFile));
		} catch (Exception ex) {
			Log.Error(ex, "コンテキストメニューの最近使用したアルバム一覧の読み込みに失敗しました。");
		}
	}

	/// <summary>
	/// 指定された外部プログラムで対象ファイル群を実行します。
	/// </summary>
	/// <param name="program">実行する外部プログラム設定。</param>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private async Task ExecuteExternalProgramAsync(IExecutionProgramObjectModel program, IMediaItemViewModel targetFile) {
		if (this.ViewModel is null) {
			return;
		}
		try {
			var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
			await this.ViewModel.ExecuteFilesAsync(program, targetFiles);
		} catch (Exception ex) {
			Log.Error(ex, "外部プログラムによるファイル実行に失敗しました。");
		}
	}

	/// <summary>
	/// 対象ファイル群のパスをクリップボードへコピーします。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private void CopyFilePaths(IMediaItemViewModel targetFile) {
		if (this.ViewModel is null) {
			return;
		}
		var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
		this.ViewModel.CopyFilePaths(targetFiles);
	}

	/// <summary>
	/// 指定パスのアルバムへ対象ファイル群を追加します。
	/// </summary>
	/// <param name="albumPath">追加先のアルバムパス。</param>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private async Task AddToAlbumAsync(string albumPath, IMediaItemViewModel targetFile) {
		if (this.ViewModel is null) {
			return;
		}
		try {
			var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
			await this.ViewModel.AddToAlbumAsync(albumPath, targetFiles);
		} catch (Exception ex) {
			Log.Error(ex, "アルバムへのアイテム追加に失敗しました。AlbumPath={AlbumPath}", albumPath);
		}
	}

	/// <summary>
	/// 新規アルバム作成ダイアログを表示し、作成されたアルバムへ対象ファイル群を追加します。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private async Task CreateAlbumAndAddItemsAsync(IMediaItemViewModel targetFile) {
		if (this.ViewModel is null || this.XamlRoot is null || this._isDialogOpen) {
			return;
		}
		this._isDialogOpen = true;
		try {
			var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
			var dialog = Ioc.Default.GetRequiredService<NewAlbumDialog>();
			dialog.XamlRoot = this.XamlRoot;
			dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
			var result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(dialog.FullAlbumPath)) {
				return;
			}
			await this.ViewModel.AddToAlbumAsync(dialog.FullAlbumPath, targetFiles);
		} catch (Exception ex) {
			Log.Error(ex, "新規アルバム作成およびアイテム追加に失敗しました。");
		} finally {
			this._isDialogOpen = false;
		}
	}

	/// <summary>
	/// 対象ファイル群のサムネイル再生成ウィンドウを表示します。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private void OpenThumbnailRegeneration(IMediaItemViewModel targetFile) {
		if (this.ViewModel is null) {
			return;
		}
		var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
		if (targetFiles.Length > 1) {
			this._windowService.ActivateCentered<BulkThumbnailRegenerationWindow>(this, bulkWindow => {
				bulkWindow.ViewModel.Initialize(targetFiles);
			});
		} else {
			this._windowService.ActivateCentered<ThumbnailPickerWindow>(this, window => {
				window.ViewModel.FileViewModel.Value = targetFile;
			});
		}
	}

	/// <summary>
	/// 対象ファイル群のメタデータ更新を要求します。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private void UpdateMetadata(IMediaItemViewModel targetFile) {
		if (this.ViewModel is null) {
			return;
		}
		var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
		this.ViewModel.UpdateMetadata(targetFiles);
	}

	/// <summary>
	/// データベースからの削除確認ダイアログを表示し、承認された場合に対象ファイル群を削除します。
	/// </summary>
	/// <param name="targetFile">右クリック対象のメディアアイテム。</param>
	private async Task ConfirmAndRemoveFilesAsync(IMediaItemViewModel targetFile) {
		if (this.ViewModel is null || this.XamlRoot is null || this._isDialogOpen) {
			return;
		}
		this._isDialogOpen = true;
		try {
			var targetFiles = this.ViewModel.ResolveContextMenuTargetFiles(targetFile);
			var message = targetFiles.Length == 1
				? this._stringProvider.GetString("Viewer_ContextRemoveFileSingle")
				: this._stringProvider.GetString("Viewer_ContextRemoveFileMultiple", targetFiles.Length);

			var dialog = new ContentDialog {
				XamlRoot = this.XamlRoot,
				Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
				Title = message,
				PrimaryButtonText = this._stringProvider.GetString("DialogButton_Yes"),
				SecondaryButtonText = this._stringProvider.GetString("DialogButton_No"),
				CloseButtonText = this._stringProvider.GetString("DialogButton_Cancel"),
				DefaultButton = ContentDialogButton.Primary,
			};
			using var disposable = new CompositeDisposable();
			ThemeHelper.BindTheme(dialog, Ioc.Default.GetRequiredService<IConfigStore>(), disposable);

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary) {
				await this.ViewModel.DeleteFilesAndReloadAsync(targetFiles);
			}
		} catch (Exception ex) {
			Log.Error(ex, "ファイルのデータベースからの削除に失敗しました。");
		} finally {
			this._isDialogOpen = false;
		}
	}

	/// <summary>
	/// Ctrl + マウスホイールによるアイテムサイズ変更ハンドラ。
	/// </summary>
	/// <param name="sender">イベント発生元。</param>
	/// <param name="e">イベント引数。</param>
	protected void HandleListPointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		var ctrlKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
		if (!ctrlKeyState.HasFlag(CoreVirtualKeyStates.Down)) {
			return;
		}
		var delta = e.GetCurrentPoint(sender as UIElement).Properties.MouseWheelDelta;
		this.ViewModel?.AdjustItemSize(delta);
		e.Handled = true;
	}
}

/// <summary>
/// WrapViewer の基底クラス。UserControlBase を介して WrapViewerViewModel を提供します。
/// </summary>
public abstract class WrapViewerUserControl : ViewerPaneBase<WrapViewerViewModel>;

/// <summary>
/// ListViewer の基底クラス。UserControlBase を介して ListViewerViewModel を提供します。
/// </summary>
public abstract class ListViewerUserControl : ViewerPaneBase<ListViewerViewModel>;

/// <summary>
/// DetailViewer の基底クラス。UserControlBase を介して DetailViewerViewModel を提供します。
/// </summary>
public abstract class DetailViewerUserControl : ViewerPaneBase<DetailViewerViewModel>;

/// <summary>
/// MapViewer の基底クラス。UserControlBase を介して MapViewerViewModel を提供します。
/// </summary>
public abstract class MapViewerUserControl : ViewerPaneBase<MapViewerViewModel>;