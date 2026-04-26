using FlyleafLib.MediaPlayer;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Views;
using MediaDeck.MediaItemTypes.UI.Base.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

public sealed partial class VideoDetailViewerPreviewControlView : VideoDetailViewerPreviewControlViewUserControl, IDetailViewerPreviewControlView {
	private readonly CompositeDisposable _disposables = [];
	private readonly SymbolIcon _iconPlay = new(Symbol.Play);
	private readonly SymbolIcon _iconPause = new(Symbol.Pause);
	public Player Player {
		get; set;
	}

	/// <summary>
	/// サムネイルの表示状態。
	/// </summary>
	public BindableReactiveProperty<Visibility> ThumbnailVisibility { get; } = new(Visibility.Collapsed);

	public VideoDetailViewerPreviewControlView() {
		this.Player = new();

		this.InitializeComponent();
		this.btnPlayback.Content = this.Player.Status == Status.Paused ? this._iconPlay : this._iconPause;
		this.Player.PropertyChanged += this.Player_PropertyChanged;
		this.rootGrid.DataContext = this;

		this.Unloaded += (s, e) => {
			this.Player.Dispose();
			this._disposables.Dispose();
		};
	}

	protected override void OnViewModelChanged(IDetailViewerViewModel? oldViewModel, IDetailViewerViewModel? newViewModel) {
		newViewModel?.SelectedFile
			.CombineLatest(newViewModel.IsSelected, (file, isSelected) => (file, isSelected))
			.Subscribe(tuple => {
				var (file, isSelected) = tuple;
				if (file != null && file.MediaType == MediaType.Video && isSelected) {
					this.ThumbnailVisibility.Value = Visibility.Visible;

					this.Player.Open(file.FilePath);
					this.Player.Pause();
				} else {
					this.Player.Stop();
				}
			}).AddTo(this._disposables);
		base.OnViewModelChanged(oldViewModel, newViewModel);
	}
	private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
		switch (e.PropertyName) {
			case nameof(this.Player.Status):
				this.btnPlayback.Content = this.Player.Status == Status.Paused ? this._iconPlay : this._iconPause;

				if (this.Player.Status == Status.Playing) {
					this.ThumbnailVisibility.Value = Visibility.Collapsed;
				}

				break;
		}
	}

}

public class VideoDetailViewerPreviewControlViewUserControl : UserControlBase<IDetailViewerViewModel> {
}