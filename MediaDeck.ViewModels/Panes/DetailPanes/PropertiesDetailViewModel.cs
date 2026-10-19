using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Composition.Tables.Metadata;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

/// <summary>
/// プロパティ詳細ペイン（メタデータ・評価・説明文・共通プロパティ）の ViewModel。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class PropertiesDetailViewModel : ViewModelBase {
	private readonly PropertiesDetailModel _model;

	/// <summary>
	/// 対象ファイル群
	/// </summary>
	public BindableReactiveProperty<IMediaItemModel[]?> TargetFiles {
		get;
	} = new();

	private IMediaItemModel[] TargetFileModels {
		get {
			return this.TargetFiles.Value ?? Array.Empty<IMediaItemModel>();
		}
	}

	/// <summary>
	/// 代表ファイルパス
	/// </summary>
	public BindableReactiveProperty<string> RepresentativeFilePath {
		get;
	}

	/// <summary>
	/// プロパティ一覧
	/// </summary>
	public BindableReactiveProperty<FileProperty[]> Properties {
		get;
	}

	/// <summary>
	/// 評価値
	/// </summary>
	public BindableReactiveProperty<double> Rate {
		get;
	}

	/// <summary>
	/// 説明文
	/// </summary>
	public BindableReactiveProperty<string> Description {
		get;
	}

	/// <summary>
	/// 使用回数
	/// </summary>
	public BindableReactiveProperty<double> UsageCount {
		get;
	}

	/// <summary>
	/// メタデータ
	/// </summary>
	public BindableReactiveProperty<MediaMetadata?> Metadata {
		get;
	}

	/// <summary>
	/// 評価値更新コマンド
	/// </summary>
	public ReactiveCommand<double> UpdateRateCommand {
		get;
	} = new();

	/// <summary>
	/// 説明文更新コマンド
	/// </summary>
	public ReactiveCommand UpdateDescriptionCommand {
		get;
	} = new();

	/// <summary>
	/// <see cref="PropertiesDetailViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">プロパティ詳細モデル</param>
	public PropertiesDetailViewModel(PropertiesDetailModel model) {
		this._model = model.AddTo(this.CompositeDisposable);
		this.TargetFiles.AddTo(this.CompositeDisposable);
		this.UpdateRateCommand.AddTo(this.CompositeDisposable);
		this.UpdateDescriptionCommand.AddTo(this.CompositeDisposable);

		this.RepresentativeFilePath = model.RepresentativeFilePath.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.Properties = model.Properties.ToBindableReactiveProperty([]).AddTo(this.CompositeDisposable);
		this.Rate = model.Rate.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.Description = model.Description.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.UsageCount = model.UsageCount.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.Metadata = model.Metadata.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);

		this.TargetFiles
			.Where(x => x != null)
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels))
			.AddTo(this.CompositeDisposable);

		this.UpdateRateCommand.SubscribeAwait(async (x, ct) => {
			if (!double.IsInteger(x) || this.TargetFiles.Value is null) {
				return;
			}
			await model.UpdateRateAsync(this.TargetFileModels, (int)x);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);

		this.UpdateDescriptionCommand.SubscribeAwait(async (_, ct) => {
			if (this.TargetFileModels.Length == 0) {
				return;
			}
			await model.UpdateDescriptionAsync(this.TargetFileModels.First(), this.Description.Value);
		}, AwaitOperation.Drop).AddTo(this.CompositeDisposable);
	}
}