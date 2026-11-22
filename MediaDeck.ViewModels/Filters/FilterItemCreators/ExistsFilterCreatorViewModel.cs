using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// 存在フィルター作成ViewModel
/// </summary>
public class ExistsFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="ExistsFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public ExistsFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.Exists = new BindableReactiveProperty<DisplayObject<bool>?>().AddTo(this.CompositeDisposable);

		this.ExistsList = [
			new DisplayObject<bool>(this._stringProvider.GetString("FilterCreator_Exists_True"), true),
			new DisplayObject<bool>(this._stringProvider.GetString("FilterCreator_Exists_False"), false)
		];
		this.Exists.Value = this.ExistsList.First();

		this.AddFilterCommand = this.Exists
			.Select(x => x is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			if (this.Exists.Value is not { } exists) {
				return;
			}
			var filter = new ExistsFilterItemObject {
				Exists = exists.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.Exists;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Exists_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_Exists_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE9D5";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_Exists_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <summary>
	/// ファイルが存在するか否かの選択状態を取得します。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>?> Exists {
		get;
	}

	/// <summary>
	/// ファイルが存在するか否かの選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<DisplayObject<bool>> ExistsList {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}
}