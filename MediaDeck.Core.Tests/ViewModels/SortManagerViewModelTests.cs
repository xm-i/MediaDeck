using System.ComponentModel;
using System.Windows.Input;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Services;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Store.Config;
using MediaDeck.ViewModels.Sort;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// <see cref="SortManagerViewModel"/>、および関連する <see cref="SortConditionEditorViewModel"/> / <see cref="SortItemEditorViewModel"/> の単体テスト。
/// </summary>
public sealed class SortManagerViewModelTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="SortManagerViewModelTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SortManagerViewModelTests() {
		this._previousSyncContext = SynchronizationContext.Current;
		this._syncContext = new InlineSynchronizationContext();
		SynchronizationContext.SetSynchronizationContext(this._syncContext);
	}

	/// <summary>
	/// テスト終了時に同期コンテキストを元の状態へ復元します。
	/// </summary>
	public void Dispose() {
		SynchronizationContext.SetSynchronizationContext(this._previousSyncContext);
	}

	/// <summary>
	/// コンストラクタが先頭のソート条件を選択し、全条件で選択肢リストを参照共有することを検証します。
	/// </summary>
	[Fact]
	public void Constructor_SelectsFirstConditionAndSharesCandidateLists() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		var conditions = (IList<SortConditionEditorViewModel>)vm.SortConditions;

		vm.IsEmpty.Value.ShouldBeFalse();
		vm.ConditionCount.Value.ShouldBe(env.SearchDefinitions.SortConditions.Count);
		vm.CurrentCondition.Value.ShouldNotBeNull();
		vm.CurrentCondition.Value.ShouldBeSameAs(conditions[0]);
		vm.HasCurrentCondition.Value.ShouldBeTrue();
		vm.ShowEmptyMessage.Value.ShouldBeFalse();
		vm.ShowSelectConditionMessage.Value.ShouldBeFalse();

		conditions.Count.ShouldBeGreaterThan(1);
		conditions[0].CandidateSortKeys.ShouldBeSameAs(conditions[1].CandidateSortKeys);
		conditions[0].DirectionOptions.ShouldBeSameAs(conditions[1].DirectionOptions);
	}

	/// <summary>
	/// 条件が0件・未選択・選択ありの間の状態フラグが正しく遷移することを検証します。
	/// </summary>
	[Fact]
	public void EmptyAndSelectStates_TransitionCorrectly() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		using var vm = env.CreateViewModel();

		vm.IsEmpty.Value.ShouldBeTrue();
		vm.ConditionCount.Value.ShouldBe(0);
		vm.CurrentCondition.Value.ShouldBeNull();
		vm.HasCurrentCondition.Value.ShouldBeFalse();
		vm.ShowEmptyMessage.Value.ShouldBeTrue();
		vm.ShowSelectConditionMessage.Value.ShouldBeFalse();

		((ICommand)vm.AddSortConditionCommand).Execute(null);

		vm.IsEmpty.Value.ShouldBeFalse();
		vm.ConditionCount.Value.ShouldBe(1);
		vm.CurrentCondition.Value.ShouldNotBeNull();
		vm.HasCurrentCondition.Value.ShouldBeTrue();
		vm.ShowEmptyMessage.Value.ShouldBeFalse();
		vm.ShowSelectConditionMessage.Value.ShouldBeFalse();

		vm.CurrentCondition.Value = null;

		vm.HasCurrentCondition.Value.ShouldBeFalse();
		vm.ShowEmptyMessage.Value.ShouldBeFalse();
		vm.ShowSelectConditionMessage.Value.ShouldBeTrue();
	}

	/// <summary>
	/// 条件削除時に先頭・末尾・最後の1件・非選択項目削除ごとの選択追従が正しく動作することを検証します。
	/// </summary>
	[Fact]
	public void RemoveCondition_SelectsAdjacentConditionCorrectly() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		using var vm = env.CreateViewModel();
		var conditions = (IList<SortConditionEditorViewModel>)vm.SortConditions;

		vm.Add();
		vm.Add();
		vm.Add();
		var first = conditions[0];
		var second = conditions[1];
		var third = conditions[2];

		// 先頭選択中に末尾（非選択）を削除 -> 先頭のまま
		vm.CurrentCondition.Value = first;
		vm.Remove(third);
		vm.CurrentCondition.Value.ShouldBeSameAs(first);

		// 先頭選択中に先頭を削除 -> 元2番目（現0番目）を選択
		vm.Remove(first);
		vm.CurrentCondition.Value.ShouldBeSameAs(second);

		// 追加して末尾を選択し、末尾を削除 -> 前の項目を選択
		vm.Add();
		var fourth = conditions[1];
		vm.CurrentCondition.Value.ShouldBeSameAs(fourth);
		vm.Remove(fourth);
		vm.CurrentCondition.Value.ShouldBeSameAs(second);

		// 最後の1件を削除 -> null
		vm.Remove(second);
		vm.CurrentCondition.Value.ShouldBeNull();
		vm.ShowEmptyMessage.Value.ShouldBeTrue();
	}

	/// <summary>
	/// デフォルト以外のキーと方向でソート項目を追加した際、生成されたエディタ項目に最初から正しい値が設定されることを検証します。
	/// </summary>
	[Fact]
	public void AddSortItemCommand_SetsKeyAndDirectionImmediatelyOnCreation() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		using var vm = env.CreateViewModel();
		vm.Add();
		var condition = vm.CurrentCondition.Value!;
		var sortItems = (IList<SortItemEditorViewModel>)condition.SortItems;

		condition.SelectedSortKey.Value = condition.CandidateSortKeys.First(x => x.Key == SortItemKey.Rate);
		condition.SelectedDirectionOption.Value = condition.DirectionOptions.First(x => x.Direction == ListSortDirection.Descending);
		condition.Direction.Value.ShouldBe(ListSortDirection.Descending);

		((ICommand)condition.AddSortItemCommand).Execute(null);

		condition.SortItemCount.Value.ShouldBe(1);
		condition.HasSortItems.Value.ShouldBeTrue();
		condition.IsEmpty.Value.ShouldBeFalse();
		var itemVm = sortItems[0];
		itemVm.SortItemKey.ShouldBe(SortItemKey.Rate);
		itemVm.Direction.ShouldBe(ListSortDirection.Descending);
		itemVm.Priority.Value.ShouldBe(1);
		itemVm.CanMoveUp.Value.ShouldBeFalse();
		itemVm.CanMoveDown.Value.ShouldBeFalse();
	}

	/// <summary>
	/// ソート項目の上下移動、境界制御、インライン方向反転、削除で優先順位と状態が正しく更新されることを検証します。
	/// </summary>
	[Fact]
	public void SortItemEditor_MoveUpMoveDownToggleDirectionAndRemove_WorkCorrectly() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		using var vm = env.CreateViewModel();
		vm.Add();
		var condition = vm.CurrentCondition.Value!;
		var sortItems = (IList<SortItemEditorViewModel>)condition.SortItems;

		condition.AddSortItemByKeyCommand.Execute(SortItemKey.Rate);
		condition.Direction.Value = ListSortDirection.Descending;
		condition.AddSortItemByKeyCommand.Execute(SortItemKey.FileSize);
		condition.Direction.Value = ListSortDirection.Ascending;
		condition.AddSortItemByKeyCommand.Execute(SortItemKey.FilePath);

		condition.SortItemCount.Value.ShouldBe(3);
		sortItems[0].Priority.Value.ShouldBe(1);
		sortItems[0].CanMoveUp.Value.ShouldBeFalse();
		sortItems[0].CanMoveDown.Value.ShouldBeTrue();
		sortItems[1].Priority.Value.ShouldBe(2);
		sortItems[1].CanMoveUp.Value.ShouldBeTrue();
		sortItems[1].CanMoveDown.Value.ShouldBeTrue();
		sortItems[2].Priority.Value.ShouldBe(3);
		sortItems[2].CanMoveUp.Value.ShouldBeTrue();
		sortItems[2].CanMoveDown.Value.ShouldBeFalse();

		// 境界でのコマンド実行不可確認
		((ICommand)sortItems[0].MoveUpCommand).CanExecute(null).ShouldBeFalse();
		((ICommand)sortItems[2].MoveDownCommand).CanExecute(null).ShouldBeFalse();

		// 2番目を上へ移動 -> FileSize(Desc) が先頭に
		((ICommand)sortItems[1].MoveUpCommand).Execute(null);
		sortItems[0].SortItemKey.ShouldBe(SortItemKey.FileSize);
		sortItems[0].Direction.ShouldBe(ListSortDirection.Descending);
		sortItems[1].SortItemKey.ShouldBe(SortItemKey.Rate);
		sortItems[0].Priority.Value.ShouldBe(1);
		sortItems[1].Priority.Value.ShouldBe(2);

		// 先頭を下へ移動 -> 元の順序に戻る
		((ICommand)sortItems[0].MoveDownCommand).Execute(null);
		sortItems[0].SortItemKey.ShouldBe(SortItemKey.Rate);
		sortItems[1].SortItemKey.ShouldBe(SortItemKey.FileSize);

		// 2番目の方向をインライン反転 -> Descending から Ascending へ
		((ICommand)sortItems[1].ToggleDirectionCommand).Execute(null);
		sortItems[1].SortItemKey.ShouldBe(SortItemKey.FileSize);
		sortItems[1].Direction.ShouldBe(ListSortDirection.Ascending);
		sortItems[1].IsAscending.ShouldBeTrue();
		sortItems[1].Priority.Value.ShouldBe(2);

		// 2番目を削除 -> 残り2件の優先順位再計算
		((ICommand)sortItems[1].RemoveCommand).Execute(null);
		condition.SortItemCount.Value.ShouldBe(2);
		sortItems[0].SortItemKey.ShouldBe(SortItemKey.Rate);
		sortItems[0].Priority.Value.ShouldBe(1);
		sortItems[1].SortItemKey.ShouldBe(SortItemKey.FilePath);
		sortItems[1].Priority.Value.ShouldBe(2);
	}

	/// <summary>
	/// ソート条件のサマリーテキストが0件・1件・3件以上で適切にフォーマットされることを検証します。
	/// </summary>
	[Fact]
	public void SummaryText_ReflectsItemCountAndTruncation() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		using var vm = env.CreateViewModel();
		vm.Add();
		var condition = vm.CurrentCondition.Value!;

		condition.SummaryText.Value.ShouldBe("SortManager_NoSortItemsSummary");

		condition.AddSortItemByKeyCommand.Execute(SortItemKey.Rate);
		condition.SummaryText.Value.ShouldBe("SortItemKey_Rate (Sort_Direction_Ascending)");

		condition.AddSortItemByKeyCommand.Execute(SortItemKey.FileSize);
		condition.SummaryText.Value.ShouldBe("SortItemKey_Rate (Sort_Direction_Ascending) → SortItemKey_FileSize (Sort_Direction_Ascending)");

		condition.AddSortItemByKeyCommand.Execute(SortItemKey.FilePath);
		condition.SummaryText.Value.ShouldBe("SortItemKey_Rate (Sort_Direction_Ascending) → SortItemKey_FileSize (Sort_Direction_Ascending) Common_AndMoreCount:1");
	}

	/// <summary>
	/// 空の <see cref="SortObject"/> に対する <see cref="SortObject.GetHashCode"/> が例外を送出しないことを検証します。
	/// </summary>
	[Fact]
	public void EmptySortObject_GetHashCode_DoesNotThrow() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		var emptySortObject = env.SearchDefinitions.AddSortCondition();

		Should.NotThrow(() => emptySortObject.GetHashCode());
	}

	/// <summary>
	/// <see cref="SortConditionEditorViewModel"/> での方向反転および移動操作が <see cref="SortSelector.OnSortConditionChanged"/> に伝播することを検証します。
	/// </summary>
	[Fact]
	public void SortSelectorIntegration_NotifiesOnMoveAndDirectionToggle() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		var sortObj = env.SearchDefinitions.AddSortCondition();
		sortObj.AddSortItemObject(SortItemKey.Rate, ListSortDirection.Ascending);
		sortObj.AddSortItemObject(SortItemKey.FilePath, ListSortDirection.Ascending);
		env.TabState.SearchState.CurrentSortCondition.Value = sortObj.Id;

		using var selector = new SortSelector(env.TabState, env.SearchDefinitions, new SearchConditionNotificationDispatcher());
		using var vm = env.CreateViewModel();
		var editor = vm.CurrentCondition.Value!;
		var sortItems = (IList<SortItemEditorViewModel>)editor.SortItems;

		var notifyCount = 0;
		using var sub = selector.OnSortConditionChanged.Subscribe(_ => notifyCount++);

		((ICommand)sortItems[0].ToggleDirectionCommand).Execute(null);
		notifyCount.ShouldBe(1);
		sortItems[0].Direction.ShouldBe(ListSortDirection.Descending);

		((ICommand)sortItems[0].MoveDownCommand).Execute(null);
		notifyCount.ShouldBe(2);
		sortObj.SortItemObjects[0].SortItemKey.ShouldBe(SortItemKey.FilePath);
		sortObj.SortItemObjects[1].SortItemKey.ShouldBe(SortItemKey.Rate);
	}

	private sealed class TestEnvironment : IDisposable {
		private readonly StubAppPathProvider _pathProvider = new();
		private readonly ServiceProvider _realServiceProvider;

		public TestEnvironment() {
			var services = new ServiceCollection();
			services.AddTransient<SortObject>(sp => new SortObject(sp));
			services.AddTransient<SortItemObject>();
			services.AddTransient<FilterObject>();
			services.AddTransient<ExtensionObjectModel>();
			services.AddSingleton<SearchStateModel>();
			services.AddSingleton<ViewerStateModel>();
			services.AddSingleton<AppStateModel>();
			services.AddSingleton<DefaultTabStateModel>();
			services.AddTransient<ListViewerColumnStateModel>();
			services.AddSingleton<TabStateModel>();
			services.AddSingleton<SearchDefinitionsConfigModel>();
			services.AddSingleton<LanguageConfigModel>();
			services.AddSingleton<ConfigModel>();
			services.AddSingleton<PathConfigModel>();
			services.AddSingleton<ScanConfigModel>();
			services.AddSingleton<ThumbnailConfigModel>();
			services.AddSingleton<ExecutionConfigModel>();
			services.AddSingleton<EnvironmentConfigModel>();
			services.AddSingleton<SearchConfigModel>();
			services.AddSingleton<FolderManagerConfigModel>();
			services.AddLogging();
			services.AddSingleton<AppNotificationDispatcher>();
			services.AddSingleton<IStringProvider, FormatAwareStubStringProvider>();

			this._realServiceProvider = services.BuildServiceProvider();
			this.SearchDefinitions = this._realServiceProvider.GetRequiredService<SearchDefinitionsConfigModel>();
			this.TabState = this._realServiceProvider.GetRequiredService<TabStateModel>();
			this.StringProvider = this._realServiceProvider.GetRequiredService<IStringProvider>();

			var mockServiceProvider = new Mock<IServiceProvider>();
			var mockScope = new Mock<IServiceScope>();
			var mockScopeFactory = new Mock<IServiceScopeFactory>();

			mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(mockScopeFactory.Object);
			mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
			mockScope.Setup(x => x.ServiceProvider).Returns(this._realServiceProvider);
			mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<ConfigStore>))).Returns(this._realServiceProvider.GetRequiredService<ILogger<ConfigStore>>());
			mockServiceProvider.Setup(x => x.GetService(typeof(AppNotificationDispatcher))).Returns(this._realServiceProvider.GetRequiredService<AppNotificationDispatcher>());
			mockServiceProvider.Setup(x => x.GetService(typeof(AppStateModel))).Returns(this._realServiceProvider.GetRequiredService<AppStateModel>());
			mockServiceProvider.Setup(x => x.GetService(typeof(IAppPathProvider))).Returns(this._pathProvider);

			var configStore = new ConfigStore(mockServiceProvider.Object, this._pathProvider);
			this.SortManager = new SortManager(configStore, this.SearchDefinitions);
		}

		public SearchDefinitionsConfigModel SearchDefinitions {
			get;
		}

		public TabStateModel TabState {
			get;
		}

		public SortManager SortManager {
			get;
		}

		public IStringProvider StringProvider {
			get;
		}

		public SortManagerViewModel CreateViewModel() {
			return new SortManagerViewModel(this.SortManager, this.StringProvider);
		}

		public void Dispose() {
			this.SortManager.Dispose();
			this._realServiceProvider.Dispose();
			this._pathProvider.Cleanup();
		}
	}

	private sealed class FormatAwareStubStringProvider : IStringProvider {
		public string GetString(string key) {
			return key;
		}

		public string GetString(string key, params object?[] args) {
			return args.Length == 0 ? key : $"{key}:{string.Join(",", args)}";
		}
	}

	private sealed class InlineSynchronizationContext : SynchronizationContext {
		public override void Post(SendOrPostCallback d, object? state) {
			d(state);
		}

		public override void Send(SendOrPostCallback d, object? state) {
			d(state);
		}
	}
}