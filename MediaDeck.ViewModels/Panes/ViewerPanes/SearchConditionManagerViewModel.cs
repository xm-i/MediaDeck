using System.Diagnostics.CodeAnalysis;
using MediaDeck.Common.Base;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Notifications;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Models.Files.SearchConditions;

namespace MediaDeck.ViewModels.Panes.ViewerPanes;

/// <summary>
/// 検索条件マネージャの ViewModel。
/// 適用中の検索条件一覧およびサジェスト候補の管理とディスパッチを行います。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SearchConditionManagerViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="SearchConditionManagerViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="mediaContentLibrary">メディアコンテンツライブラリ。</param>
	/// <param name="searchConditionNotificationDispatcher">検索条件通知ディスパッチャ。</param>
	public SearchConditionManagerViewModel(MediaContentLibrary mediaContentLibrary, ISearchConditionNotificationDispatcher searchConditionNotificationDispatcher) {
		this.SearchConditions =
			mediaContentLibrary
				.SearchConditions
				.ToWritableNotifyCollectionChanged(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable),
					(SearchConditionViewModel scvm, ISearchCondition sc, ref bool setValue) => scvm.SearchCondition,
					SynchronizationContextCollectionEventDispatcher.Current)
				.AddTo(this.CompositeDisposable);

		this.SearchConditionCandidates = mediaContentLibrary.SearchConditionCandidates
			.CreateView(x => new SearchConditionViewModel(x).AddTo(this.CompositeDisposable))
			.AddTo(this.CompositeDisposable);
		this.FilteredSearchConditionCandidates = this.SearchConditionCandidates
			.ToNotifyCollectionChanged(SynchronizationContextCollectionEventDispatcher.Current)
			.AddTo(this.CompositeDisposable);
		this.SearchConditionNotificationDispatcher = searchConditionNotificationDispatcher;

		this.RefreshCommand = new ReactiveCommand().AddTo(this.CompositeDisposable);
		this.RefreshCommand.Subscribe(_ => this.Reload()).AddTo(this.CompositeDisposable);
	}

	/// <summary>適用中の検索条件一覧を取得します。</summary>
	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> SearchConditions {
		get;
	}

	/// <summary>検索条件の候補ビューを取得します。</summary>
	public ISynchronizedView<ISearchCondition, SearchConditionViewModel> SearchConditionCandidates {
		get;
	}

	/// <summary>フィルタリングされた検索条件の候補一覧を取得します。</summary>
	public INotifyCollectionChangedSynchronizedViewList<SearchConditionViewModel> FilteredSearchConditionCandidates {
		get;
	}

	/// <summary>検索条件通知ディスパッチャを取得します。</summary>
	public ISearchConditionNotificationDispatcher SearchConditionNotificationDispatcher {
		get;
	}

	/// <summary>
	/// 入力文字列に基づいてサジェスト候補を絞り込みます。
	/// </summary>
	/// <param name="word">フィルタリング単語。</param>
	public void RefreshSearchTokenCandidates(string word) {
		this.SearchConditionCandidates.AttachFilter(x => {
			return x.IsMatchForSuggest(word);
		});
	}

	/// <summary>手動リロードを実行する（Dispatcher 経由で検索を発火する）。</summary>
	public void Reload() {
		this.SearchConditionNotificationDispatcher.ReloadRequested.OnNext(Unit.Default);
	}

	/// <summary>
	/// 単語検索条件を追加します。
	/// </summary>
	/// <param name="word">検索する単語。</param>
	public void AddWordCondition(string word) {
		if (!string.IsNullOrWhiteSpace(word)) {
			this.SearchConditionNotificationDispatcher.AddRequest.OnNext(new WordSearchCondition { Word = word });
		}
	}

	/// <summary>
	/// 指定した検索条件を削除します。
	/// </summary>
	/// <param name="condition">削除対象の検索条件 ViewModel。</param>
	public void RemoveCondition(SearchConditionViewModel condition) {
		this.SearchConditionNotificationDispatcher.RemoveRequest.OnNext(condition.SearchCondition);
	}

	/// <summary>
	/// 指定した検索条件を削除します。
	/// </summary>
	/// <param name="condition">削除対象の検索条件。</param>
	public void RemoveCondition(ISearchCondition condition) {
		this.SearchConditionNotificationDispatcher.RemoveRequest.OnNext(condition);
	}

	/// <summary>
	/// 未確定のプロパティ検索条件スタブを、確定したプロパティ検索条件に置換します。
	/// </summary>
	/// <param name="stub">置換対象の未確定スタブ。</param>
	/// <param name="propertyName">プロパティ名。</param>
	/// <param name="op">比較演算子。</param>
	/// <param name="rawValue">入力値の文字列表現。</param>
	public void ConfigurePropertyCondition(ISearchCondition stub, string propertyName, SearchTypeComparison op, string rawValue) {
		var condition = new PropertySearchCondition {
			PropertyName = propertyName,
			Operator = op,
			Value = rawValue,
			IsConfigured = true,
		};
		this.SearchConditionNotificationDispatcher.UpdateRequest.OnNext(list => {
			var index = list.IndexOf(stub);
			if (index >= 0) {
				list.RemoveAt(index);
				list.Insert(index, condition);
			}
		});
	}

	/// <summary>
	/// 指定されたアイテムが未確定のプロパティ検索条件スタブである場合、対応するプロパティ記述情報を取得します。
	/// </summary>
	/// <param name="item">判定対象のアイテム。</param>
	/// <param name="stub">未確定のプロパティ検索条件スタブ。</param>
	/// <param name="descriptor">プロパティ記述情報。</param>
	/// <returns>未確定のプロパティ検索条件スタブであり、記述子が存在する場合は <c>true</c>。</returns>
	public bool TryGetUnconfiguredPropertyDescriptor(
		object? item,
		[NotNullWhen(true)] out ISearchCondition? stub,
		[NotNullWhen(true)] out MediaItemPropertyDescriptor? descriptor) {
		if (item is SearchConditionViewModel { SearchCondition: PropertySearchCondition { IsConfigured: false } propertyStub }) {
			descriptor = MediaItemPropertyCatalog.Find(propertyStub.PropertyName);
			if (descriptor != null) {
				stub = propertyStub;
				return true;
			}
			this.RemoveCondition(propertyStub);
		}
		stub = null;
		descriptor = null;
		return false;
	}

	/// <summary>
	/// 検索条件の変更を通知します。
	/// </summary>
	public void NotifySearchConditionChanged() {
		this.SearchConditionNotificationDispatcher.SearchConditionChanged.OnNext(Unit.Default);
	}

	/// <summary>
	/// リロードを実行するコマンドを取得します。
	/// </summary>
	public ReactiveCommand RefreshCommand {
		get;
	}
}