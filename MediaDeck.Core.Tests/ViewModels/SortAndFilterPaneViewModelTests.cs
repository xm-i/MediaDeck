using System.ComponentModel;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Files.Sort;
using MediaDeck.Core.Models.NotificationDispatcher;
using MediaDeck.Core.Stores.State;
using MediaDeck.ViewModels.Panes.FilterPanes;
using MediaDeck.ViewModels.Panes.SortPanes;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// サイドペイン用 <see cref="SortSelectorViewModel"/> および <see cref="FilterSelectorViewModel"/> の単体テスト。
/// </summary>
public sealed class SortAndFilterPaneViewModelTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="SortAndFilterPaneViewModelTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public SortAndFilterPaneViewModelTests() {
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
	/// <see cref="SortSelectorViewModel"/> がソート方向名と選択条件の概要テキストを条件変更・項目変更に追従して更新することを検証します。
	/// </summary>
	[Fact]
	public void SortSelectorViewModel_UpdatesDirectionAndSummary() {
		using var env = new TestEnvironment();
		env.SearchDefinitions.SortConditions.Clear();
		var cond1 = env.SearchDefinitions.AddSortCondition();
		cond1.DisplayName.Value = "Custom 1";
		cond1.AddSortItemObject(SortItemKey.Rate, ListSortDirection.Descending);

		var cond2 = env.SearchDefinitions.AddSortCondition();
		cond2.DisplayName.Value = "Custom 2";

		env.TabState.SearchState.CurrentSortCondition.Value = cond1.Id;
		env.TabState.SearchState.SortDirection.Value = ListSortDirection.Ascending;

		using var selector = new SortSelector(env.TabState, env.SearchDefinitions, new SearchConditionNotificationDispatcher());
		using var vm = new SortSelectorViewModel(selector, new Mock<IStateStore>().Object, env.StringProvider);

		vm.DirectionDisplayName.Value.ShouldBe("Sort_Direction_Ascending");
		vm.CurrentConditionSummary.Value.ShouldBe("SortItemKey_Rate (Sort_Direction_Descending)");

		// ソート方向の反転
		vm.Direction.Value = ListSortDirection.Descending;
		vm.DirectionDisplayName.Value.ShouldBe("Sort_Direction_Descending");

		// 同一条件への項目追加に追従すること
		cond1.AddSortItemObject(SortItemKey.FilePath, ListSortDirection.Ascending);
		vm.CurrentConditionSummary.Value.ShouldBe("SortItemKey_Rate (Sort_Direction_Descending) → SortItemKey_FilePath (Sort_Direction_Ascending)");

		// 別の空条件へ切替
		var sortConditions = (IList<SortConditionViewModel>)vm.SortConditions;
		vm.CurrentCondition.Value = sortConditions[1];
		vm.CurrentConditionSummary.Value.ShouldBe("SortManager_NoSortItemsSummary");

		// 未選択へ切替
		vm.CurrentCondition.Value = null;
		vm.CurrentConditionSummary.Value.ShouldBe(string.Empty);
	}

	/// <summary>
	/// <see cref="FilterSelectorViewModel"/> が空状態、選択件数、クリアコマンド、および各条件の要約・ツールチップを正しく管理することを検証します。
	/// </summary>
	[Fact]
	public void FilterSelectorViewModel_TracksEmptySelectedCountClearAndTooltips() {
		using var env = new TestEnvironment();
		using var selector = new FilterSelector(env.TabState, env.SearchDefinitions, new SearchConditionNotificationDispatcher());
		using var vm = new FilterSelectorViewModel(selector, env.StringProvider);

		vm.UIReadyCommand.Execute(Unit.Default);
		vm.IsEmpty.Value.ShouldBeTrue();
		vm.SelectedCount.Value.ShouldBe(0);
		vm.HasSelectedConditions.Value.ShouldBeFalse();

		var filter1 = env.SearchDefinitions.AddFilteringCondition();
		filter1.DisplayName.Value = "Favorites";
		filter1.FilterItemObjects.Add(new RateFilterItemObject { Rate = 4, SearchType = SearchTypeComparison.GreaterThanOrEqual });

		var filter2 = env.SearchDefinitions.AddFilteringCondition();
		filter2.DisplayName.Value = "Photos";
		filter2.FilterItemObjects.Add(new MediaTypeFilterItemObject { IsVideo = false });
		filter2.FilterItemObjects.Add(new ExistsFilterItemObject { Exists = true });

		var filteringConditions = (IList<FilteringConditionViewModel>)vm.FilteringConditions;
		vm.IsEmpty.Value.ShouldBeFalse();
		filteringConditions.Count.ShouldBe(2);

		var condVm1 = filteringConditions[0];
		var condVm2 = filteringConditions[1];

		condVm1.ItemCount.Value.ShouldBe(1);
		condVm1.SummaryText.Value.ShouldBe("FilterItem_Rate_GreaterThanOrEqual:4");
		condVm1.DetailTooltip.Value.ShouldBe("• FilterItem_Rate_GreaterThanOrEqual:4");

		condVm2.ItemCount.Value.ShouldBe(2);
		condVm2.SummaryText.Value.ShouldBe("FilterItem_MediaType_Image Common_AndMoreCount:1");
		condVm2.DetailTooltip.Value.ShouldBe($"• FilterItem_MediaType_Image{Environment.NewLine}• FilterItem_Exists_True");

		// 複数選択
		vm.ChangeFilteringConditionSelectionCommand.Execute([condVm1, condVm2]);
		vm.SelectedCount.Value.ShouldBe(2);
		vm.HasSelectedConditions.Value.ShouldBeTrue();

		// 全解除コマンド
		vm.ClearSelectionCommand.Execute(Unit.Default);
		vm.SelectedCount.Value.ShouldBe(0);
		vm.HasSelectedConditions.Value.ShouldBeFalse();
		vm.SelectedConditions.Value.ShouldBeEmpty();

		// 条件全削除で空状態へ戻る
		env.SearchDefinitions.FilteringConditions.Clear();
		vm.IsEmpty.Value.ShouldBeTrue();
	}

	private sealed class TestEnvironment : IDisposable {
		private readonly ServiceProvider _serviceProvider;

		public TestEnvironment() {
			var services = new ServiceCollection();
			services.AddTransient<SortObject>(sp => new SortObject(sp));
			services.AddTransient<SortItemObject>();
			services.AddTransient<FilterObject>();
			services.AddTransient<ExtensionObjectModel>();
			services.AddSingleton<SearchStateModel>();
			services.AddSingleton<ViewerStateModel>();
			services.AddTransient<ListViewerColumnStateModel>();
			services.AddSingleton<TabStateModel>();
			services.AddSingleton<IStringProvider, FormatAwareStubStringProvider>();

			this._serviceProvider = services.BuildServiceProvider();
			this.StringProvider = this._serviceProvider.GetRequiredService<IStringProvider>();
			this.SearchDefinitions = new SearchDefinitionsConfigModel(this._serviceProvider, this.StringProvider);
			this.TabState = this._serviceProvider.GetRequiredService<TabStateModel>();
		}

		public SearchDefinitionsConfigModel SearchDefinitions {
			get;
		}

		public TabStateModel TabState {
			get;
		}

		public IStringProvider StringProvider {
			get;
		}

		public void Dispose() {
			this._serviceProvider.Dispose();
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