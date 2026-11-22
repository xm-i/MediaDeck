using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// ファイルパスフィルター作成ViewModel
/// </summary>
public class FilePathFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="FilePathFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public FilePathFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.FilePath = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeInclude>?>().AddTo(this.CompositeDisposable);

		this.SearchTypeList = [
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Include"), SearchTypeInclude.Include),
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Exclude"), SearchTypeInclude.Exclude)
		];
		this.SearchType.Value = this.SearchTypeList.First();

		this.AddFilterCommand = this.FilePath
			.CombineLatest(this.SearchType, (path, st) => !string.IsNullOrWhiteSpace(path) && st is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			if (this.SearchType.Value is not { } searchType || string.IsNullOrWhiteSpace(this.FilePath.Value)) {
				return;
			}
			var filter = new FilePathFilterItemObject {
				Text = this.FilePath.Value.Trim(),
				SearchType = searchType.Value
			};
			target.Value?.AddFilter(filter);
			this.FilePath.Value = string.Empty;
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.FilePath;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_FilePath_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_FilePath_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE8A5";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_FilePath_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// ファイルパスに含まれる検索文字列を取得します。
	/// </summary>
	public BindableReactiveProperty<string> FilePath {
		get;
	}

	/// <summary>
	/// 検索条件として指定の文字列を含むものを検索するか、含まないものを検索するかを選択します。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeInclude>?> SearchType {
		get;
	}

	/// <summary>
	/// 含む/含まないの選択候補を取得します。
	/// </summary>
	public IReadOnlyList<DisplayObject<SearchTypeInclude>> SearchTypeList {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}
}