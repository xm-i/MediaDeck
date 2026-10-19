using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.MediaItemTypes.Models;

namespace MediaDeck.Core.Models.Files;

/// <summary>
/// 詳細セレクタの親モデル。プロパティ詳細とタグ詳細の調停を行います。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class DetailSelectorModel : ModelBase {
	/// <summary>
	/// プロパティ詳細モデル
	/// </summary>
	public PropertiesDetailModel PropertiesModel {
		get;
	}

	/// <summary>
	/// タグ詳細モデル
	/// </summary>
	public TagsDetailModel TagsModel {
		get;
	}

	/// <summary>
	/// TargetFilesの中身の変更を通知するためのSubject
	/// </summary>
	public Subject<Unit> ContentChanged {
		get;
	} = new();

	/// <summary>
	/// <see cref="DetailSelectorModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="propertiesModel">プロパティ詳細モデル</param>
	/// <param name="tagsModel">タグ詳細モデル</param>
	public DetailSelectorModel(PropertiesDetailModel propertiesModel, TagsDetailModel tagsModel) {
		this.PropertiesModel = propertiesModel.AddTo(this.CompositeDisposable);
		this.TagsModel = tagsModel.AddTo(this.CompositeDisposable);
		this.ContentChanged.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// TargetFilesから各種状態を算出して子モデルを一括更新します。
	/// </summary>
	/// <param name="files">対象ファイル群</param>
	public void Refresh(IMediaItemModel[] files) {
		this.PropertiesModel.Refresh(files);
		this.TagsModel.Refresh(files);
	}
}