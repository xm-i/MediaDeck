using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグカテゴリの追加・編集ダイアログ用 ViewModel クラス。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class TagCategoryEditDialogViewModel : ViewModelBase {
	private readonly ITagsManager _tagsManager;
	private readonly IStringProvider _stringProvider;
	private ITagCategoryModel? _targetCategory;

	/// <summary>
	/// <see cref="TagCategoryEditDialogViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagsManager">タグマネージャー</param>
	/// <param name="stringProvider">文字列プロバイダー</param>
	public TagCategoryEditDialogViewModel(ITagsManager tagsManager, IStringProvider stringProvider) {
		this._tagsManager = tagsManager;
		this._stringProvider = stringProvider;

		this.TagCategoryName = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.Detail = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.DialogTitle = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.PrimaryButtonText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.CanSave = this.TagCategoryName
			.Select(x => !string.IsNullOrWhiteSpace(x))
			.ToBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// カテゴリ名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> TagCategoryName {
		get;
	}

	/// <summary>
	/// カテゴリの詳細説明を取得します。
	/// </summary>
	public BindableReactiveProperty<string> Detail {
		get;
	}

	/// <summary>
	/// ダイアログタイトルを取得します。
	/// </summary>
	public BindableReactiveProperty<string> DialogTitle {
		get;
	}

	/// <summary>
	/// プライマリボタンの表示テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> PrimaryButtonText {
		get;
	}

	/// <summary>
	/// 保存が可能かどうかを取得します。
	/// </summary>
	public BindableReactiveProperty<bool> CanSave {
		get;
	}

	/// <summary>
	/// 編集モードかどうかを取得します。
	/// </summary>
	public bool IsEditMode {
		get;
		private set;
	}

	/// <summary>
	/// 作成または更新されたタグカテゴリモデルを取得します。
	/// </summary>
	public ITagCategoryModel? ResultCategory {
		get;
		private set;
	}

	/// <summary>
	/// 新規追加モードとして初期化します。
	/// </summary>
	public void InitializeForCreate() {
		this._targetCategory = null;
		this.ResultCategory = null;
		this.IsEditMode = false;
		this.DialogTitle.Value = this._stringProvider.GetString("TagCategoryEditDialog_CreateTitle");
		this.PrimaryButtonText.Value = this._stringProvider.GetString("TagCategoryEditDialog_CreateButton");
		this.TagCategoryName.Value = string.Empty;
		this.Detail.Value = string.Empty;
	}

	/// <summary>
	/// 既存カテゴリの編集モードとして初期化します。
	/// </summary>
	/// <param name="category">編集対象のタグカテゴリモデル</param>
	public void InitializeForEdit(ITagCategoryModel category) {
		this._targetCategory = category;
		this.ResultCategory = null;
		this.IsEditMode = true;
		this.DialogTitle.Value = this._stringProvider.GetString("TagCategoryEditDialog_EditTitle");
		this.PrimaryButtonText.Value = this._stringProvider.GetString("TagCategoryEditDialog_SaveButton");
		this.TagCategoryName.Value = category.TagCategoryName;
		this.Detail.Value = category.Detail;
	}

	/// <summary>
	/// 入力内容を保存してタグカテゴリを作成または更新します。
	/// </summary>
	/// <returns>保存に成功した場合は true、入力値が不正な場合は false</returns>
	public async Task<bool> SaveAsync() {
		var name = this.TagCategoryName.Value.Trim();
		if (string.IsNullOrWhiteSpace(name)) {
			return false;
		}

		var detail = this.Detail.Value.Trim();
		if (this._targetCategory == null) {
			this.ResultCategory = await this._tagsManager.CreateTagCategoryAsync(name, detail);
		} else {
			await this._tagsManager.UpdateTagCategoryAsync(this._targetCategory, name, detail);
			this.ResultCategory = this._targetCategory;
		}
		return true;
	}
}