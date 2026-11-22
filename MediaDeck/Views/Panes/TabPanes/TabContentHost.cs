using MediaDeck.ViewModels;
using Microsoft.UI.Xaml;

namespace MediaDeck.Views.Panes.TabPanes;

/// <summary>
/// <see cref="TabViewModel"/> の遅延ロード状態に応じて <see cref="TabContentPane"/> を生成・表示する軽量ホストコントロール。
/// 未ロードの非アクティブタブではビジュアルツリーの生成を回避します。
/// </summary>
public sealed class TabContentHost : UserControlBase<TabViewModel> {
	private readonly CompositeDisposable _vmSubscriptions = new();
	private bool _isAttached;

	/// <summary>
	/// <see cref="TabContentHost"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TabContentHost() {
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;
		this.Loading += this.TabContentHost_Loading;
		this.Loaded += this.TabContentHost_Loaded;
		this.Unloaded += this.TabContentHost_Unloaded;
	}

	/// <summary>
	/// ビジュアルツリー接続開始（<see cref="FrameworkElement.Loading"/>）時に現在の ViewModel への購読を開始し、
	/// ロード済みタブであれば初回レイアウト前に <see cref="TabContentPane"/> を生成します。
	/// </summary>
	private void TabContentHost_Loading(FrameworkElement sender, object args) {
		this._isAttached = true;
		this.BindViewModel(this.ViewModel);
	}

	/// <summary>
	/// コントロールの読み込み完了時のフォールバック処理。未購読の場合のみ ViewModel への購読を開始します。
	/// </summary>
	private void TabContentHost_Loaded(object sender, RoutedEventArgs e) {
		if (!this._isAttached) {
			this._isAttached = true;
			this.BindViewModel(this.ViewModel);
		}
	}

	/// <summary>
	/// コントロールのアンロード時に ViewModel への購読を解除します。
	/// </summary>
	private void TabContentHost_Unloaded(object sender, RoutedEventArgs e) {
		this._isAttached = false;
		this._vmSubscriptions.Clear();
	}

	/// <inheritdoc />
	protected override void OnViewModelChanged(TabViewModel? oldViewModel, TabViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this._vmSubscriptions.Clear();
		if (this._isAttached || this.IsLoaded) {
			this._isAttached = true;
			this.BindViewModel(newViewModel);
		} else if (this.Content is TabContentPane pane && !ReferenceEquals(pane.ViewModel, newViewModel?.Content.Value)) {
			this.Content = null;
		}
	}

	/// <summary>
	/// 指定された <see cref="TabViewModel"/> のコンテンツ状態変更を購読し、表示内容を同期します。
	/// </summary>
	/// <param name="viewModel">購読対象のタブViewModel。</param>
	private void BindViewModel(TabViewModel? viewModel) {
		this._vmSubscriptions.Clear();
		if (viewModel == null) {
			this.UpdateContent(null);
			return;
		}

		viewModel.Content
			.Subscribe(this.UpdateContent)
			.AddTo(this._vmSubscriptions);
	}

	/// <summary>
	/// ロードされた <see cref="TabContentViewModel"/> に応じて <see cref="TabContentPane"/> を生成または更新します。
	/// </summary>
	/// <param name="content">タブコンテンツViewModel。未ロード時は <c>null</c>。</param>
	private void UpdateContent(TabContentViewModel? content) {
		if (content == null) {
			this.Content = null;
			return;
		}

		if (this.Content is TabContentPane existingPane) {
			if (!ReferenceEquals(existingPane.ViewModel, content)) {
				existingPane.ViewModel = content;
			}
			return;
		}

		this.Content = new TabContentPane {
			ViewModel = content
		};
	}
}