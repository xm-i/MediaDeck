using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// フォルダーグループフィルター作成ViewModel
/// </summary>
public class FolderGroupFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="FolderGroupFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public FolderGroupFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeInclude>?>().AddTo(this.CompositeDisposable);

		this.SearchTypeList = [
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_FolderGroup_OptionInclude"), SearchTypeInclude.Include),
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_FolderGroup_OptionExclude"), SearchTypeInclude.Exclude)
		];
		this.SearchType.Value = this.SearchTypeList.First();

		this.AddFilterCommand = this.SearchType
			.Select(x => x is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			if (this.SearchType.Value is not { } searchType) {
				return;
			}
			var filter = new FolderGroupFilterItemObject {
				SearchType = searchType.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.FolderGroup;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_FolderGroup_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_FolderGroup_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uED41";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_FolderGroup_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// フォルダグループ配下/配下外の選択状態を取得します。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<SearchTypeInclude>?> SearchType {
		get;
	}

	/// <summary>
	/// フォルダグループ配下/配下外の選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<DisplayObject<SearchTypeInclude>> SearchTypeList {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}
}