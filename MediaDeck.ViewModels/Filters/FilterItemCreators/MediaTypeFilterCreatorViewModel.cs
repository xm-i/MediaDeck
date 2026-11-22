using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;

namespace MediaDeck.ViewModels.Filters.FilterItemCreators;

/// <summary>
/// メディアタイプフィルター作成ViewModel
/// </summary>
public class MediaTypeFilterCreatorViewModel : ViewModelBase, IFilterCreatorViewModel {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="MediaTypeFilterCreatorViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="target">追加先のフィルター条件編集 ViewModel。</param>
	/// <param name="stringProvider">文字列リソースプロバイダー。</param>
	public MediaTypeFilterCreatorViewModel(ReactiveProperty<FilteringConditionEditorViewModel?> target, IStringProvider stringProvider) {
		this._stringProvider = stringProvider;
		this.IsSelected = new BindableReactiveProperty<bool>(false).AddTo(this.CompositeDisposable);
		this.MediaType = new BindableReactiveProperty<MediaTypeOption?>().AddTo(this.CompositeDisposable);

		this.MediaTypeList = [
			new MediaTypeOption(this._stringProvider.GetString("FilterCreator_MediaType_Image"), false, "\uE8B9"),
			new MediaTypeOption(this._stringProvider.GetString("FilterCreator_MediaType_Video"), true, "\uE714")
		];
		this.MediaType.Value = this.MediaTypeList.First();

		this.AddFilterCommand = this.MediaType
			.Select(x => x is not null)
			.ToReactiveCommand()
			.AddTo(this.CompositeDisposable);
		this.AddFilterCommand.Subscribe(_ => {
			if (this.MediaType.Value is not { } mediaType) {
				return;
			}
			var filter = new MediaTypeFilterItemObject {
				IsVideo = mediaType.Value
			};
			target.Value?.AddFilter(filter);
		}).AddTo(this.CompositeDisposable);
	}

	/// <inheritdoc />
	public FilterCreatorKind Kind {
		get {
			return FilterCreatorKind.MediaType;
		}
	}

	/// <inheritdoc />
	public string Title {
		get {
			return this._stringProvider.GetString("FilterCreator_MediaType_Title");
		}
	}

	/// <inheritdoc />
	public string ShortTitle {
		get {
			return this._stringProvider.GetString("FilterCreator_MediaType_ShortTitle");
		}
	}

	/// <inheritdoc />
	public string IconGlyph {
		get {
			return "\uE8B9";
		}
	}

	/// <inheritdoc />
	public string Description {
		get {
			return this._stringProvider.GetString("FilterCreator_MediaType_Description");
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
	/// 選択中のメディアタイプを取得します。
	/// </summary>
	public BindableReactiveProperty<MediaTypeOption?> MediaType {
		get;
	}

	/// <summary>
	/// メディアタイプ候補一覧を取得します。
	/// </summary>
	public IReadOnlyList<MediaTypeOption> MediaTypeList {
		get;
	}
}