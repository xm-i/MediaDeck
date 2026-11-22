using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Primitives;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// 座標フィルター作成ViewModel
/// </summary>
public class LocationFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="LocationFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public LocationFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.HasLocation = new BindableReactiveProperty<DisplayObject<bool>?>().AddTo(this.CompositeDisposable);

		this.HasLocationList = [
			new DisplayObject<bool>(this._stringProvider.GetString("FilterCreator_Location_Contains_True"), true),
			new DisplayObject<bool>(this._stringProvider.GetString("FilterCreator_Location_Contains_False"), false)
		];
		this.HasLocation.Value = this.HasLocationList.First();

		this.AddFilterCommand = this.HasLocation
			.Select(x => x is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			if (this.HasLocation.Value is not { } hasLocation) {
				return;
			}
			var filter = new LocationFilterItemObject {
				Contains = hasLocation.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.Location;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_Location_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_Location_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE707";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_Location_Description");
		}
	}

	/// <inheritdoc />
	public BindableReactiveProperty<bool> IsSelected {
		get;
	}

	/// <inheritdoc />
	public ReactiveCommand AddFilterCommand {
		get;
	}

	/// <summary>
	/// 座標情報を持っているか否かの選択状態を取得します。
	/// </summary>
	public BindableReactiveProperty<DisplayObject<bool>?> HasLocation {
		get;
	}

	/// <summary>
	/// 座標情報を持っているか否かの選択候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<DisplayObject<bool>> HasLocationList {
		get;
	}
}