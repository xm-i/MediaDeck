using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;
using Microsoft.UI.Xaml;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

/// <summary>
/// ビデオサムネイル選択ビュー。
/// </summary>
public sealed partial class VideoThumbnailPickerView : VideoThumbnailPickerViewUserControl {
	private readonly SerialDisposable _viewModelDisposable = new();

	/// <summary>
	/// <see cref="VideoThumbnailPickerView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public VideoThumbnailPickerView() {
		this.InitializeComponent();
		this.Unloaded += this.VideoThumbnailPickerView_Unloaded;
	}

	/// <summary>
	/// ViewModelが変更された際の処理。古い購読を解除し新しいViewModelとの同期を設定します。
	/// </summary>
	/// <param name="oldViewModel">古いViewModel。</param>
	/// <param name="newViewModel">新しいViewModel。</param>
	protected override void OnViewModelChanged(VideoThumbnailPickerViewModel? oldViewModel, VideoThumbnailPickerViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);

		if (newViewModel == null) {
			this._viewModelDisposable.Disposable = null;
			this.PlayerControl.Stop();
			return;
		}

		var disposables = new CompositeDisposable();
		this._viewModelDisposable.Disposable = disposables;

		// ビデオファイルパスの変更を監視して再生
		newViewModel.VideoFilePath.Subscribe(filePath => {
			if (!string.IsNullOrEmpty(filePath)) {
				_ = this.PlayerControl.OpenAsync(filePath);
			} else {
				this.PlayerControl.Stop();
			}
		}).AddTo(disposables);

		// Player の位置変更を ViewModel に同期
		Observable.FromEvent<EventHandler<TimeSpan>, TimeSpan>(
			h => (s, e) => h(e),
			h => this.PlayerControl.CurTimeChanged += h,
			h => this.PlayerControl.CurTimeChanged -= h
		)
		.Subscribe(time => {
			newViewModel.UpdateTime(time);
		})
		.AddTo(disposables);

		// ViewModel の Time 変更を Player に同期（シーク）
		newViewModel.Time.Subscribe(time => {
			this.PlayerControl.Seek(time);
		}).AddTo(disposables);
	}

	/// <summary>
	/// ビューがアンロードされた際に呼び出され、購読解除と再生停止を行います。
	/// </summary>
	/// <param name="sender">イベントの発生元。</param>
	/// <param name="e">イベントデータ。</param>
	private void VideoThumbnailPickerView_Unloaded(object sender, RoutedEventArgs e) {
		this._viewModelDisposable.Disposable = null;
		this.PlayerControl.Stop();
	}
}

/// <summary>
/// <see cref="VideoThumbnailPickerView"/> のジェネリック基底クラス。
/// </summary>
public class VideoThumbnailPickerViewUserControl : UserControlBase<VideoThumbnailPickerViewModel>;