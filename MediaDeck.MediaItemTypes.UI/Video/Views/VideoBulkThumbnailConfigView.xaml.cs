using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;

using Microsoft.UI.Xaml;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

public sealed partial class VideoBulkThumbnailConfigView : VideoBulkThumbnailConfigViewUserControl {
	private readonly CompositeDisposable _vmSubscriptions = new();

	public VideoBulkThumbnailConfigView() {
		this.InitializeComponent();
		this.Loaded += (_, _) => {
			this.SubscribeViewModel(this.ViewModel);
		};
		this.Unloaded += (_, _) => {
			this._vmSubscriptions.Clear();
		};
	}

	public int ModeIndex {
		get {
			return this.ViewModel is null ? 0 : (int)this.ViewModel.Mode.Value;
		}
		set {
			if (this.ViewModel is { } vm) {
				vm.Mode.Value = (VideoBulkThumbnailMode)value;
			}
		}
	}

	public Visibility SecondsVisibility {
		get {
			return this.ViewModel?.Mode.Value == VideoBulkThumbnailMode.Seconds
				? Visibility.Visible
				: Visibility.Collapsed;
		}
	}

	public Visibility PercentageVisibility {
		get {
			return this.ViewModel?.Mode.Value == VideoBulkThumbnailMode.Percentage
				? Visibility.Visible
				: Visibility.Collapsed;
		}
	}

	/// <inheritdoc />
	protected override void OnViewModelChanged(VideoBulkThumbnailConfigViewModel? oldViewModel, VideoBulkThumbnailConfigViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this._vmSubscriptions.Clear();
		if (this.IsLoaded && newViewModel != null) {
			this.SubscribeViewModel(newViewModel);
		}
	}

	/// <summary>
	/// ViewModel のプロパティ変更購読を開始します。
	/// </summary>
	/// <param name="vm">購読対象の ViewModel。</param>
	private void SubscribeViewModel(VideoBulkThumbnailConfigViewModel? vm) {
		this._vmSubscriptions.Clear();
		if (vm is null) {
			return;
		}

		vm.Mode.Subscribe(_ => {
			this.RaisePropertyChanged(nameof(this.ModeIndex));
			this.RaisePropertyChanged(nameof(this.SecondsVisibility));
			this.RaisePropertyChanged(nameof(this.PercentageVisibility));
		}).AddTo(this._vmSubscriptions);
	}
}

public class VideoBulkThumbnailConfigViewUserControl : UserControlBase<VideoBulkThumbnailConfigViewModel> {
}