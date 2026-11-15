using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;
using MediaDeck.Core.Models.Files;

namespace MediaDeck.ViewModels.Panes.DetailPanes;

/// <summary>
/// 詳細ペインのセレクター（親コンテナ）ViewModel。
/// プロパティ詳細とタグ詳細の調停を行います。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class DetailSelectorViewModel : ViewModelBase {
	private readonly DetailSelectorModel _model;

	/// <summary>
	/// プロパティ詳細 ViewModel
	/// </summary>
	public PropertiesDetailViewModel PropertiesViewModel {
		get;
	}

	/// <summary>
	/// タグ詳細 ViewModel
	/// </summary>
	public TagsDetailViewModel TagsViewModel {
		get;
	}

	/// <summary>
	/// 選択対象ファイル群
	/// </summary>
	public BindableReactiveProperty<IMediaItemModel[]?> TargetFiles {
		get;
	} = new();

	/// <summary>
	/// ファイルが選択されているかどうか。
	/// </summary>
	public BindableReactiveProperty<bool> HasSelectedFiles {
		get;
	}

	private IMediaItemModel[] TargetFileModels {
		get {
			return this.TargetFiles.Value ?? Array.Empty<IMediaItemModel>();
		}
	}

	/// <summary>
	/// <see cref="DetailSelectorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">詳細セレクターモデル</param>
	/// <param name="propertiesViewModel">プロパティ詳細 ViewModel</param>
	/// <param name="tagsViewModel">タグ詳細 ViewModel</param>
	public DetailSelectorViewModel(
		DetailSelectorModel model,
		PropertiesDetailViewModel propertiesViewModel,
		TagsDetailViewModel tagsViewModel) {
		this._model = model.AddTo(this.CompositeDisposable);
		this.PropertiesViewModel = propertiesViewModel.AddTo(this.CompositeDisposable);
		this.TagsViewModel = tagsViewModel.AddTo(this.CompositeDisposable);
		this.TargetFiles.AddTo(this.CompositeDisposable);

		this.HasSelectedFiles = this.TargetFiles
			.Select(files => files is { Length: > 0 })
			.ToBindableReactiveProperty(this.TargetFiles.Value is { Length: > 0 })
			.AddTo(this.CompositeDisposable);

		// TargetFiles の変更を子 ViewModel へ同期
		this.TargetFiles.Subscribe(files => {
			this.PropertiesViewModel.TargetFiles.Value = files;
			this.TagsViewModel.TargetFiles.Value = files;
		}).AddTo(this.CompositeDisposable);

		this._model.ContentChanged
			.Subscribe(_ => this._model.Refresh(this.TargetFileModels))
			.AddTo(this.CompositeDisposable);
	}
}