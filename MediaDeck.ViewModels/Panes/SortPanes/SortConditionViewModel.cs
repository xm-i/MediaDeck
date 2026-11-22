using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;

namespace MediaDeck.ViewModels.Panes.SortPanes;

/// <summary>
/// ソート条件ViewModel
/// </summary>
public class SortConditionViewModel : ViewModelBase {
	private readonly IStringProvider _stringProvider;

	/// <summary>
	/// <see cref="SortConditionViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="model">ソート条件モデル。</param>
	/// <param name="stringProvider">多言語リソースプロバイダー。</param>
	public SortConditionViewModel(SortObject model, IStringProvider stringProvider) {
		this.Model = model;
		this._stringProvider = stringProvider;
		this.DisplayName = this.Model.DisplayName
			.ToBindableReactiveProperty(this.Model.DisplayName.Value ?? string.Empty)
			.AddTo(this.CompositeDisposable);

		this.ItemCount = new BindableReactiveProperty<int>(0).AddTo(this.CompositeDisposable);
		this.SummaryText = new BindableReactiveProperty<string>(string.Empty).AddTo(this.CompositeDisposable);

		this.Model.SortItemObjects.ObserveChanged()
			.Subscribe(_ => {
				this.UpdateSummary();
			})
			.AddTo(this.CompositeDisposable);

		this.UpdateSummary();
	}

	/// <summary>
	/// モデルを取得します。
	/// </summary>
	public SortObject Model {
		get;
	}

	/// <summary>
	/// 表示名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// ソート項目数を取得します。
	/// </summary>
	public BindableReactiveProperty<int> ItemCount {
		get;
	}

	/// <summary>
	/// ソート条件の概要テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> SummaryText {
		get;
	}

	private void UpdateSummary() {
		this.ItemCount.Value = this.Model.SortItemObjects.Count;
		this.SummaryText.Value = SortDisplayHelper.FormatSummary(this.Model.SortItemObjects, this._stringProvider);
	}
}