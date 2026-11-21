using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Composition.Stores.State.Model.Objects;
using MediaDeck.Composition.Tables;
using MediaDeck.Core.Models.Files.Filter;
using MediaDeck.Core.Models.NotificationDispatcher;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace MediaDeck.Core.Tests.Models.Files.Filter;

/// <summary>
/// <see cref="FilterExtensions"/> の単体テスト。
/// </summary>
public class FilterExtensionsTests {
	/// <summary>
	/// フィルタ条件がない場合に全件が返されることを検証します。
	/// </summary>
	[Fact]
	public void Where_WithNoFilters_ReturnsUnfilteredQuery() {
		using var dbFactory = new SqliteTestDbFactory();
		using var db = dbFactory.CreateDbContext();
		db.MediaItems.Add(new MediaItem { FilePath = "a.jpg", MediaType = MediaType.Image, DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
		db.MediaItems.Add(new MediaItem { FilePath = "b.jpg", MediaType = MediaType.Image, DirectoryPath = "dir", Description = "", IsUnderFolderGroup = false });
		db.SaveChanges();

		var services = new ServiceCollection();
		services.AddTransient<SortObject>(sp => new SortObject(sp));
		services.AddTransient<SortItemObject>();
		services.AddSingleton<SearchDefinitionsConfigModel>();
		services.AddSingleton<SearchStateModel>();
		services.AddSingleton<ViewerStateModel>();
		services.AddSingleton<TabStateModel>();
		services.AddSingleton<MediaDeck.Composition.Interfaces.IStringProvider, StubStringProvider>();
		var sp = services.BuildServiceProvider();

		var tabState = sp.GetRequiredService<TabStateModel>();
		var searchDef = sp.GetRequiredService<SearchDefinitionsConfigModel>();
		var dispatcher = new SearchConditionNotificationDispatcher();
		var filterSelector = new FilterSelector(tabState, searchDef, dispatcher);

		var result = db.MediaItems.AsQueryable().Where(filterSelector);

		result.Count().ShouldBe(2);
	}
}