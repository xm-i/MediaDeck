using System.Windows.Input;

using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Interfaces;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Objects;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Stores.Config;
using MediaDeck.ViewModels.Filters;
using MediaDeck.ViewModels.Filters.FilterItemCreators;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using ObservableCollections;

using R3;

using Shouldly;

namespace MediaDeck.Core.Tests.ViewModels;

/// <summary>
/// <see cref="FilterManagerViewModel"/>、各 <see cref="IFilterCreatorViewModel"/> 実装、および <see cref="FilterItemDisplayHelper"/> の単体テスト。
/// </summary>
public sealed class FilterManagerViewModelTests : IDisposable {
	private readonly SynchronizationContext? _previousSyncContext;
	private readonly InlineSynchronizationContext _syncContext;

	/// <summary>
	/// <see cref="FilterManagerViewModelTests"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public FilterManagerViewModelTests() {
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
	/// 初期状態・条件追加・削除時の選択追従および状態フラグ遷移を検証します。
	/// </summary>
	[Fact]
	public void AddAndRemoveConditions_UpdatesSelectionAndStates() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();

		vm.IsEmpty.Value.ShouldBeTrue();
		vm.ConditionCount.Value.ShouldBe(0);
		vm.CurrentCondition.Value.ShouldBeNull();
		vm.ShowEmptyMessage.Value.ShouldBeTrue();
		vm.ShowSelectConditionMessage.Value.ShouldBeFalse();
		vm.HasCurrentCondition.Value.ShouldBeFalse();

		vm.Add();
		var conditions = (IList<FilteringConditionEditorViewModel>)vm.FilteringConditions;
		vm.IsEmpty.Value.ShouldBeFalse();
		vm.ConditionCount.Value.ShouldBe(1);
		vm.HasCurrentCondition.Value.ShouldBeTrue();
		vm.CurrentCondition.Value.ShouldBeSameAs(conditions[0]);

		vm.Add();
		vm.Add();
		var first = conditions[0];
		var second = conditions[1];
		var third = conditions[2];
		vm.CurrentCondition.Value.ShouldBeSameAs(third);

		// 末尾選択中に末尾を削除 -> 前項目
		vm.Remove(third);
		vm.CurrentCondition.Value.ShouldBeSameAs(second);

		// 非選択の先頭を削除 -> 選択維持
		vm.Remove(first);
		vm.CurrentCondition.Value.ShouldBeSameAs(second);

		// 選択を明示的に外した場合 -> ShowSelectConditionMessage
		vm.CurrentCondition.Value = null;
		vm.ShowSelectConditionMessage.Value.ShouldBeTrue();
		vm.ShowEmptyMessage.Value.ShouldBeFalse();

		// 最後の1件を削除 -> 空状態
		vm.CurrentCondition.Value = second;
		vm.Remove(second);
		vm.CurrentCondition.Value.ShouldBeNull();
		vm.IsEmpty.Value.ShouldBeTrue();
		vm.ShowEmptyMessage.Value.ShouldBeTrue();
	}

	/// <summary>
	/// カテゴリチップ切替時に <see cref="IFilterCreatorViewModel.IsSelected"/> と選択種別が排他的に同期されることを検証します。
	/// </summary>
	[Fact]
	public void SelectFilterCreatorCommand_UpdatesIsSelectedAndKindExclusively() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();

		vm.SelectedFilterCreatorViewModel.Value.ShouldBeSameAs(vm.TagFilterCreator);
		vm.SelectedFilterCreatorKind.Value.ShouldBe(FilterCreatorKind.Tag);
		vm.TagFilterCreator.IsSelected.Value.ShouldBeTrue();
		vm.RateFilterCreator.IsSelected.Value.ShouldBeFalse();

		vm.SelectFilterCreatorCommand.Execute(vm.ResolutionFilterCreator);

		vm.SelectedFilterCreatorViewModel.Value.ShouldBeSameAs(vm.ResolutionFilterCreator);
		vm.SelectedFilterCreatorKind.Value.ShouldBe(FilterCreatorKind.Resolution);
		vm.ResolutionFilterCreator.IsSelected.Value.ShouldBeTrue();
		vm.TagFilterCreator.IsSelected.Value.ShouldBeFalse();
		vm.FilterCreatorViewModels.Count(x => x.IsSelected.Value).ShouldBe(1);

		// null を渡しても状態が変わらないこと
		vm.SelectFilterCreator(null);
		vm.SelectedFilterCreatorKind.Value.ShouldBe(FilterCreatorKind.Resolution);
	}

	/// <summary>
	/// <see cref="TagFilterCreatorViewModel"/> のタグサジェスト、バリデーション、追加、および入力クリアを検証します。
	/// </summary>
	[Fact]
	public void TagFilterCreator_SuggestsAndAddsAndClearsInput() {
		using var env = new TestEnvironment();
		env.Tags.Add(CreateTagMock("Landscape", 10, "風景"));
		env.Tags.Add(CreateTagMock("Portrait", 30));
		env.Tags.Add(CreateTagMock("Sunset", 20));

		using var vm = env.CreateViewModel();
		vm.Add();
		var creator = vm.TagFilterCreator;

		// 空文字時は使用回数降順
		creator.TagSuggestions.Value.Select(x => x.TagName).ShouldBe(["Portrait", "Sunset", "Landscape"]);

		// 空白のみでは追加不可
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();
		creator.TagName.Value = "   ";
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();

		// TagName の変更のみではサジェストが上書きされず（候補ハイライト保護）、明示更新で別名一致に絞り込まれること
		creator.TagName.Value = "風景";
		creator.TagSuggestions.Value.Select(x => x.TagName).ShouldBe(["Portrait", "Sunset", "Landscape"]);

		creator.RefreshTagSuggestions("風景");
		creator.TagSuggestions.Value.Count.ShouldBe(1);
		creator.TagSuggestions.Value[0].TagName.ShouldBe("Landscape");
		creator.TagSuggestions.Value[0].HasAliasText.ShouldBeTrue();
		creator.TagSuggestions.Value[0].AliasText.ShouldBe("風景");

		// 除外指定で追加し、入力欄とサジェストが初期状態へ戻ること
		creator.TagName.Value = "Landscape";
		creator.SearchType.Value = creator.SearchTypeList.First(x => x.Value == SearchTypeInclude.Exclude);
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeTrue();
		((ICommand)creator.AddFilterCommand).Execute(null);

		creator.TagName.Value.ShouldBe(string.Empty);
		creator.TagSuggestions.Value.Select(x => x.TagName).ShouldBe(["Portrait", "Sunset", "Landscape"]);
		var added = vm.CurrentCondition.Value!.Model.FilterItemObjects.ShouldHaveSingleItem().ShouldBeOfType<TagFilterItemObject>();
		added.TagName.ShouldBe("Landscape");
		added.SearchType.ShouldBe(SearchTypeInclude.Exclude);

		// タグ一覧の動的変更へサジェストが追従すること
		env.Tags.Add(CreateTagMock("Night", 50));
		creator.TagSuggestions.Value[0].TagName.ShouldBe("Night");
	}

	/// <summary>
	/// <see cref="FilePathFilterCreatorViewModel"/> の入力検証、追加、およびクリア動作を検証します。
	/// </summary>
	[Fact]
	public void FilePathFilterCreator_ValidatesAddsAndClearsInput() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		vm.Add();
		var creator = vm.FilePathFilterCreator;

		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();
		creator.FilePath.Value = "  ";
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();

		creator.FilePath.Value = "2026\\Vacation";
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeTrue();
		((ICommand)creator.AddFilterCommand).Execute(null);

		creator.FilePath.Value.ShouldBe(string.Empty);
		var added = vm.CurrentCondition.Value!.Model.FilterItemObjects.ShouldHaveSingleItem().ShouldBeOfType<FilePathFilterItemObject>();
		added.Text.ShouldBe("2026\\Vacation");
		added.SearchType.ShouldBe(SearchTypeInclude.Include);
	}

	/// <summary>
	/// <see cref="RateFilterCreatorViewModel"/> の RatingControl 同期、0クリア、およびフィルター追加を検証します。
	/// </summary>
	[Fact]
	public void RateFilterCreator_SyncsRatingControlClearAndAddsFilter() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		vm.Add();
		var creator = vm.RateFilterCreator;

		creator.Rate.Value.ShouldBe(3);
		creator.RatingControlValue.Value.ShouldBe(3.0);

		creator.UpdateRateFromRatingControl(4.2);
		creator.Rate.Value.ShouldBe(4);
		creator.RatingControlValue.Value.ShouldBe(4.0);
		creator.RateCaption.Value.ShouldBe("FilterCreator_Rate_Stars:4");

		creator.UpdateRateFromRatingControl(10);
		creator.Rate.Value.ShouldBe(5);

		creator.UpdateRateFromRatingControl(-1);
		creator.Rate.Value.ShouldBe(0);
		creator.RatingControlValue.Value.ShouldBe(-1.0);
		creator.RateCaption.Value.ShouldBe("FilterCreator_Rate_Unrated");

		creator.UpdateRateFromRatingControl(2);
		creator.ClearRateCommand.Execute(Unit.Default);
		creator.Rate.Value.ShouldBe(0);
		creator.RatingControlValue.Value.ShouldBe(-1.0);

		creator.UpdateRateFromRatingControl(5);
		creator.SearchType.Value = creator.SearchTypeList.First(x => x.Value == SearchTypeComparison.Equal);
		((ICommand)creator.AddFilterCommand).Execute(null);

		var added = vm.CurrentCondition.Value!.Model.FilterItemObjects.ShouldHaveSingleItem().ShouldBeOfType<RateFilterItemObject>();
		added.Rate.ShouldBe(5);
		added.SearchType.ShouldBe(SearchTypeComparison.Equal);
	}

	/// <summary>
	/// <see cref="ResolutionFilterCreatorViewModel"/> の片方指定、両方指定、プリセット、クリア、および不正値ガードを検証します。
	/// </summary>
	[Fact]
	public void ResolutionFilterCreator_SupportsPartialDimensionsPresetsAndValidation() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		vm.Add();
		var creator = vm.ResolutionFilterCreator;

		// 初期状態（両方 NaN）は追加不可
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();

		// 負数・Infinity は無効
		creator.WidthValue.Value = -10;
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();
		creator.WidthValue.Value = double.PositiveInfinity;
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();

		// 幅のみ指定
		creator.WidthValue.Value = 1920;
		creator.HeightValue.Value = double.NaN;
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeTrue();
		((ICommand)creator.AddFilterCommand).Execute(null);

		var widthOnly = vm.CurrentCondition.Value!.Model.FilterItemObjects[0].ShouldBeOfType<ResolutionFilterItemObject>();
		widthOnly.Width.ShouldBe(1920);
		widthOnly.Height.ShouldBeNull();
		widthOnly.Resolution.ShouldBeNull();

		// 高さのみ指定
		creator.WidthValue.Value = double.NaN;
		creator.HeightValue.Value = 1080;
		((ICommand)creator.AddFilterCommand).Execute(null);

		var heightOnly = vm.CurrentCondition.Value.Model.FilterItemObjects[1].ShouldBeOfType<ResolutionFilterItemObject>();
		heightOnly.Width.ShouldBeNull();
		heightOnly.Height.ShouldBe(1080);
		heightOnly.Resolution.ShouldBeNull();

		// プリセット（4K）適用と両方指定
		creator.ApplyPresetCommand.Execute("4K");
		creator.WidthValue.Value.ShouldBe(3840);
		creator.HeightValue.Value.ShouldBe(2160);
		((ICommand)creator.AddFilterCommand).Execute(null);

		var both = vm.CurrentCondition.Value.Model.FilterItemObjects[2].ShouldBeOfType<ResolutionFilterItemObject>();
		both.Resolution.ShouldBe(new ComparableSize(3840, 2160));

		// クリアで初期状態に戻る
		creator.ClearResolutionCommand.Execute(Unit.Default);
		double.IsNaN(creator.WidthValue.Value).ShouldBeTrue();
		double.IsNaN(creator.HeightValue.Value).ShouldBeTrue();
		((ICommand)creator.AddFilterCommand).CanExecute(null).ShouldBeFalse();
	}

	/// <summary>
	/// メディアタイプ・座標・フォルダグループ・存在フィルター作成VMが期待するフィルターを追加することを検証します。
	/// </summary>
	[Fact]
	public void OtherFilterCreators_AddExpectedFilterObjects() {
		using var env = new TestEnvironment();
		using var vm = env.CreateViewModel();
		vm.Add();
		var condition = vm.CurrentCondition.Value!;

		vm.MediaTypeFilterCreator.MediaType.Value = vm.MediaTypeFilterCreator.MediaTypeList[1];
		((ICommand)vm.MediaTypeFilterCreator.AddFilterCommand).Execute(null);
		condition.Model.FilterItemObjects[0].ShouldBeOfType<MediaTypeFilterItemObject>().IsVideo.ShouldBeTrue();

		vm.LocationFilterCreator.HasLocation.Value = vm.LocationFilterCreator.HasLocationList[1];
		((ICommand)vm.LocationFilterCreator.AddFilterCommand).Execute(null);
		condition.Model.FilterItemObjects[1].ShouldBeOfType<LocationFilterItemObject>().Contains.ShouldBe(false);

		vm.FolderGroupFilterCreator.SearchType.Value = vm.FolderGroupFilterCreator.SearchTypeList[1];
		((ICommand)vm.FolderGroupFilterCreator.AddFilterCommand).Execute(null);
		condition.Model.FilterItemObjects[2].ShouldBeOfType<FolderGroupFilterItemObject>().SearchType.ShouldBe(SearchTypeInclude.Exclude);

		vm.ExistsFilterCreator.Exists.Value = vm.ExistsFilterCreator.ExistsList[1];
		((ICommand)vm.ExistsFilterCreator.AddFilterCommand).Execute(null);
		condition.Model.FilterItemObjects[3].ShouldBeOfType<ExistsFilterItemObject>().Exists.ShouldBeFalse();

		condition.FilterItemCount.Value.ShouldBe(4);
		condition.HasFilterItems.Value.ShouldBeTrue();
		condition.IsEmpty.Value.ShouldBeFalse();
		condition.SummaryText.Value.ShouldBe("FilterItem_MediaType_Video Common_AndMoreCount:3");

		// FilterItemDisplayViewModel の RemoveCommand で削除できること
		var filterItems = (IList<FilterItemDisplayViewModel>)condition.FilterItems;
		((ICommand)filterItems[0].RemoveCommand).Execute(null);
		condition.FilterItemCount.Value.ShouldBe(3);
	}

	/// <summary>
	/// <see cref="FilterItemDisplayHelper"/> が各フィルター種別の表示名・カテゴリ・アイコン・要約・詳細ツールチップを正しく生成することを検証します。
	/// </summary>
	[Fact]
	public void FilterItemDisplayHelper_FormatsAllTypesSummaryAndTooltip() {
		var sp = new FormatAwareStubStringProvider();

		FilterItemDisplayHelper.FormatSummary([], sp).ShouldBe("FilterManager_NoFilterItemsSummary");
		FilterItemDisplayHelper.FormatDetailTooltip([], sp).ShouldBe("FilterManager_NoFilterItemsSummary");

		IFilterItemObject[] items = [
			new TagFilterItemObject { TagName = "TagA", SearchType = SearchTypeInclude.Include },
			new ResolutionFilterItemObject { Resolution = new ComparableSize(1920, 1080), SearchType = SearchTypeComparison.GreaterThanOrEqual }
		];

		FilterItemDisplayHelper.GetCategoryTitle(items[0], sp).ShouldBe("FilterCreator_Tag_ShortTitle");
		FilterItemDisplayHelper.GetCategoryIconGlyph(items[0]).ShouldBe("\uE8EC");
		FilterItemDisplayHelper.GetCategoryTitle(items[1], sp).ShouldBe("FilterCreator_Resolution_ShortTitle");
		FilterItemDisplayHelper.GetCategoryIconGlyph(items[1]).ShouldBe("\uE740");

		FilterItemDisplayHelper.FormatSummary(items, sp).ShouldBe("FilterItem_Tag_Include:TagA Common_AndMoreCount:1");
		FilterItemDisplayHelper.FormatDetailTooltip(items, sp)
			.ShouldBe($"• FilterItem_Tag_Include:TagA{Environment.NewLine}• FilterItem_Resolution_Area_GreaterThanOrEqual:1920x1080");
	}

	private static ITagModel CreateTagMock(string tagName, int usageCount, params string[] aliases) {
		var tagMock = new Mock<ITagModel>();
		tagMock.SetupGet(x => x.TagName).Returns(tagName);
		tagMock.SetupGet(x => x.UsageCount).Returns(new ReactiveProperty<int>(usageCount));
		var aliasMocks = aliases.Select(a => {
			var aliasMock = new Mock<ITagAliasModel>();
			aliasMock.SetupGet(x => x.Alias).Returns(a);
			return aliasMock.Object;
		}).ToList();
		tagMock.SetupGet(x => x.TagAliases).Returns(aliasMocks);
		return tagMock.Object;
	}

	private sealed class TestEnvironment : IDisposable {
		private readonly ServiceProvider _serviceProvider;

		public TestEnvironment() {
			var services = new ServiceCollection();
			services.AddTransient<FilterObject>();
			services.AddTransient<SortObject>(sp => new SortObject(sp));
			services.AddTransient<SortItemObject>();
			services.AddSingleton<IStringProvider, FormatAwareStubStringProvider>();

			this._serviceProvider = services.BuildServiceProvider();
			this.StringProvider = this._serviceProvider.GetRequiredService<IStringProvider>();
			this.SearchDefinitions = new SearchDefinitionsConfigModel(this._serviceProvider, this.StringProvider);

			var mockConfigStore = new Mock<IConfigStore>();
			this.FilterManager = new FilterManager(mockConfigStore.Object, this.SearchDefinitions);

			this.Tags = [];
			this.TagsManagerMock = new Mock<ITagsManager>();
			this.TagsManagerMock.SetupGet(x => x.Tags).Returns(this.Tags);
		}

		public SearchDefinitionsConfigModel SearchDefinitions {
			get;
		}

		public FilterManager FilterManager {
			get;
		}

		public IStringProvider StringProvider {
			get;
		}

		public ObservableList<ITagModel> Tags {
			get;
		}

		public Mock<ITagsManager> TagsManagerMock {
			get;
		}

		public FilterManagerViewModel CreateViewModel() {
			return new FilterManagerViewModel(this.FilterManager, this.StringProvider, this.TagsManagerMock.Object);
		}

		public void Dispose() {
			this.FilterManager.Dispose();
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