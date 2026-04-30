using MediaDeck.MediaItemTypes.UI.Base.Views;
using MediaDeck.MediaItemTypes.Video.ViewModels;

using Microsoft.UI.Xaml;

namespace MediaDeck.MediaItemTypes.UI.Video.Views;

public sealed partial class VideoBulkThumbnailConfigView : VideoBulkThumbnailConfigViewUserControl {
	private IDisposable? _modeSubscription;

	public VideoBulkThumbnailConfigView() {
		this.InitializeComponent();
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

	protected override void OnViewModelChanged(VideoBulkThumbnailConfigViewModel? oldViewModel, VideoBulkThumbnailConfigViewModel? newViewModel) {
		base.OnViewModelChanged(oldViewModel, newViewModel);
		this._modeSubscription?.Dispose();
		this._modeSubscription = newViewModel?.Mode.Subscribe(_ => {
			this.RaisePropertyChanged(nameof(this.ModeIndex));
			this.RaisePropertyChanged(nameof(this.SecondsVisibility));
			this.RaisePropertyChanged(nameof(this.PercentageVisibility));
		});
	}
}

public class VideoBulkThumbnailConfigViewUserControl : UserControlBase<VideoBulkThumbnailConfigViewModel> {
}