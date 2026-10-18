using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
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
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="BulkThumbnailItemViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">対象メディアアイテムViewModel</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public BulkThumbnailItemViewModel(IMediaItemViewModel target, IStringProvider stringProvider) {
		this.Target = target;
		this._stringProvider = stringProvider;
		this.Status = new BindableReactiveProperty<BulkRegenerationStatus>(BulkRegenerationStatus.Pending).AddTo(this.CompositeDisposable);
		this.StatusText = new BindableReactiveProperty<string>(this.GetStatusDisplayText(BulkRegenerationStatus.Pending)).AddTo(this.CompositeDisposable);
		this.ErrorMessage = new BindableReactiveProperty<string?>().AddTo(this.CompositeDisposable);
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
	}

	public BindableReactiveProperty<string> StatusText {
		get;
	}

	public BindableReactiveProperty<string?> ErrorMessage {
		get;
	}

	private string GetStatusDisplayText(BulkRegenerationStatus status) {
		return status switch {
			BulkRegenerationStatus.Pending => this._stringProvider.GetString("BulkThumbnail_Status_Pending"),
			BulkRegenerationStatus.Processing => this._stringProvider.GetString("BulkThumbnail_Status_Processing"),
			BulkRegenerationStatus.Completed => this._stringProvider.GetString("BulkThumbnail_Status_Completed"),
			BulkRegenerationStatus.Failed => this._stringProvider.GetString("BulkThumbnail_Status_Failed"),
			_ => this._stringProvider.GetString("BulkThumbnail_Status_Pending")
		};
	}
}