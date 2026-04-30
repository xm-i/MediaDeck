using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

namespace MediaDeck.ViewModels.Thumbnails;

/// <summary>
/// 一括サムネイル再生成における各アイテムの処理状態。
/// </summary>
public enum BulkRegenerationStatus {
	/// <summary>未処理</summary>
	Pending,
	/// <summary>処理中</summary>
	Processing,
	/// <summary>完了</summary>
	Completed,
	/// <summary>失敗</summary>
	Failed
}

/// <summary>
/// 一括サムネイル再生成ウィンドウの 1 アイテム ViewModel。
/// </summary>
public class BulkThumbnailItemViewModel : ViewModelBase {
	public BulkThumbnailItemViewModel(IMediaItemViewModel target) {
		this.Target = target;
		this.Status
			.Select(this.GetStatusDisplayText)
			.Subscribe(x => this.StatusText.Value = x)
			.AddTo(this.CompositeDisposable);
	}

	public IMediaItemViewModel Target {
		get;
	}

	public string FilePath {
		get {
			return this.Target.FilePath;
		}
	}

	public BindableReactiveProperty<BulkRegenerationStatus> Status {
		get;
	} = new(BulkRegenerationStatus.Pending);

	public BindableReactiveProperty<string> StatusText {
		get;
	} = new("未処理");

	public BindableReactiveProperty<string?> ErrorMessage {
		get;
	} = new();

	private string GetStatusDisplayText(BulkRegenerationStatus status) {
		return status switch {
			BulkRegenerationStatus.Pending => "未処理",
			BulkRegenerationStatus.Processing => "処理中",
			BulkRegenerationStatus.Completed => "完了",
			BulkRegenerationStatus.Failed => "失敗",
			_ => "未処理"
		};
	}
}