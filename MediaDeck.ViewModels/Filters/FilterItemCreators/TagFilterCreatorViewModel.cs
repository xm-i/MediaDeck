using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// タグフィルター作成ViewModel
/// </summary>
public class TagFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;
	private readonly ITagsManager _tagsManager;

	/// <summary>
	/// <see cref="TagFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel</param>
	/// <param name="stringProvider">文字列リソースプロバイダー</param>
	/// <param name="tagsManager">タグマネージャー</param>
	public TagFilterCreatorViewModel(
		ReactiveProperty<FilteringConditionEditorViewModel?> target,
		IStringProvider stringProvider,
		ITagsManager tagsManager) {
		this._stringProvider = stringProvider;
		this._tagsManager = tagsManager;

		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.TagName = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);
		this.SearchType = new BindableReactiveProperty<DisplayObject<SearchTypeInclude>?>().AddTo(this.CompositeDisposable);
		this.TagSuggestions = new BindableReactiveProperty<IReadOnlyList<TagSuggestionItem>>([])
			.AddTo(this.CompositeDisposable);

		this.SearchTypeList = [
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Include"), SearchTypeInclude.Include),
			new DisplayObject<SearchTypeInclude>(this._stringProvider.GetString("FilterCreator_Exclude"), SearchTypeInclude.Exclude)
		];
		this.SearchType.Value = this.SearchTypeList.First();

		this._tagsManager.Tags.ObserveChanged()
			.Subscribe(_ => {
				this.RefreshTagSuggestions();
			})
			.AddTo(this.CompositeDisposable);

		this.AddFilterCommand = this.TagName
			.CombineLatest(this.SearchType, (name, st) => !string.IsNullOrWhiteSpace(name) && st is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);

		this.AddFilterCommand.Subscribe(_ => {
			if (this.SearchType.Value is not { } searchType || string.IsNullOrWhiteSpace(this.TagName.Value)) {
				return;
			}
			var filter = new TagFilterItemObject {
				TagName = this.TagName.Value.Trim(),
				SearchType = searchType.Value
			};
			target.Value?.AddFilter(filter);
			this.TagName.Value = string.Empty;
			this.RefreshTagSuggestions(string.Empty);
		}).AddTo(this.CompositeDisposable);

		this.RefreshTagSuggestions();
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.Tag;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Tag_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_Tag_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE8EC";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_Tag_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// タグ名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> TagName {
		get;
	}

	/// <summary>
	/// 検索条件として指定のタグを含むものを検索するか、含まないものを検索するかを選択します。
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

	/// <summary>
	/// タグ入力欄のサジェスト候補一覧を取得します。
	/// </summary>
	public BindableReactiveProperty<IReadOnlyList<TagSuggestionItem>> TagSuggestions {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// 指定された文字列または現在の入力文字列に基づいてタグのサジェスト候補を更新します。
	/// </summary>
	/// <param name="queryText">検索文字列（省略時は現在の入力テキスト）。</param>
	public void RefreshTagSuggestions(string? queryText = null) {
		var query = (queryText ?? this.TagName.Value)?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(query)) {
			this.TagSuggestions.Value = this._tagsManager.Tags
				.OrderByDescending(t => t.UsageCount.Value)
				.Take(20)
				.Select(t => new TagSuggestionItem(t.TagName, t.UsageCount.Value, null))
				.ToArray();
			return;
		}

		var matches = new List<TagSuggestionItem>();
		foreach (var tag in this._tagsManager.Tags.OrderByDescending(t => t.UsageCount.Value)) {
			if (tag.MatchesFilter(query, out var representativeText)) {
				matches.Add(new TagSuggestionItem(tag.TagName, tag.UsageCount.Value, representativeText));
				if (matches.Count >= 30) {
					break;
				}
			}
		}
		this.TagSuggestions.Value = matches;
	}
}