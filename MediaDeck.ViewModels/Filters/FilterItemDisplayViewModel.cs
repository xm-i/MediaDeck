using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Core.Models.Files.Filter;

namespace MediaDeck.ViewModels.Filters;

/// <summary>
/// 適用済みフィルター項目の1行を表す ViewModel クラス。
/// </summary>
public class FilterItemDisplayViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="FilterItemDisplayViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">フィルター項目モデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	/// <param name="remove">削除コールバック。</param>
	public FilterItemDisplayViewModel(
		IFilterItemObject model,
		IStringProvider stringProvider,
		Action<IFilterItemObject> remove) {
		this.Model = model;
		this.CategoryName = FilterItemDisplayHelper.GetCategoryTitle(model, stringProvider);
		this.CategoryIconGlyph = FilterItemDisplayHelper.GetCategoryIconGlyph(model);
		this.DisplayText = FilterItemDisplayHelper.FormatDisplayText(model, stringProvider);

		this.RemoveCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RemoveCommand.Subscribe(_ => {
			remove(this.Model);
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// 対象のフィルター項目モデルを取得します。
	/// </summary>
	public IFilterItemObject Model {
		get;
	}

	/// <summary>
	/// カテゴリ名を取得します。
	/// </summary>
	public string CategoryName {
		get;
	}

	/// <summary>
	/// カテゴリアイコンのグリフ文字列を取得します。
	/// </summary>
	public string CategoryIconGlyph {
		get;
	}

	/// <summary>
	/// 表示用テキストを取得します。
	/// </summary>
	public string DisplayText {
		get;
	}

	/// <summary>
	/// このフィルター項目を削除するコマンドを取得します。
	/// </summary>
	public ReactiveCommand RemoveCommand {
		get;
	}
}