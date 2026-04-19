using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Interfaces.Tags;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Core.Models.Tags;
using MediaDeck.Stores.SerializerContext;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ObservableCollections;
using Shouldly;

namespace MediaDeck.Core.Tests.Stores.State;

public class JsonPolymorphismTests {
	private readonly IServiceProvider _serviceProvider;

	private readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions() {
		TypeInfoResolver = StateJsonSerializerContext.Default.WithAddedModifier(global::R3.JsonConfig.ForJsonConverterRegistry.ApplyPolymorphism)
	};

	private readonly Mock<ITagsManager> _tagsManagerMock = new();

	public JsonPolymorphismTests() {
		var services = new ServiceCollection();
		// 必要に応じて依存関係を登録
		services.AddTransient<AppStateModel>();
		services.AddTransient<FolderSearchCondition>();
		services.AddTransient<TagSearchCondition>();
		services.AddTransient<TagFilterItemObject>();
		services.AddTransient<RateFilterItemObject>();
		services.AddTransient<TagModel>();
		services.AddTransient<ITagModel, TagModel>();
		services.AddTransient<TagCategoryModel>();
		services.AddTransient<ITagCategoryModel, TagCategoryModel>();
		services.AddTransient<TagAliasModel>();
		services.AddTransient<ITagAliasModel, TagAliasModel>();
		services.AddSingleton(this._tagsManagerMock.Object);
		this._serviceProvider = services.BuildServiceProvider();
	}

	[Fact]
	public void FolderSearchCondition_RoundTrip_Works() {
		// Arrange
		var condition = new FolderSearchCondition {
			FolderPath = "C:\\Test"
		};

		// Act & Assert
		this.VerifyRoundTrip<ISearchCondition, FolderSearchCondition>(condition, "folder");
	}

	[Fact]
	public void TagSearchCondition_RoundTrip_Works() {
		// Arrange
		var tag = new TagModel {
			TagId = 1,
			TagCategoryId = 1,
			TagCategory = new TagCategoryModel { TagCategoryId = 1, TagCategoryName = "Cat", Detail = "" },
			TagName = "TestTag",
			Detail = "",
			Romaji = "testtag",
			TagAliases = []
		};
		this._tagsManagerMock.Setup(x => x.Tags).Returns(new ObservableList<ITagModel> { tag });

		var condition = new TagSearchCondition(this._tagsManagerMock.Object) {
			TagId = tag.TagId
		};

		// Act & Assert
		this.VerifyRoundTrip<ISearchCondition, TagSearchCondition>(condition, "tag");
	}

	[Fact]
	public void TagFilterItemObject_RoundTrip_Works() {
		// Arrange
		var filter = new TagFilterItemObject {
			TagName = "testTag"
		};

		// Act & Assert
		this.VerifyRoundTrip<IFilterItemObject, TagFilterItemObject>(filter, "tagFilter");
	}

	[Fact]
	public void RateFilterItemObject_RoundTrip_Works() {
		// Arrange
		var filter = new RateFilterItemObject();

		// Act & Assert
		this.VerifyRoundTrip<IFilterItemObject, RateFilterItemObject>(filter, "rate");
	}

	private void VerifyRoundTrip<TInterface, TConcrete>(TInterface original, string expectedTypeId)
		where TConcrete : TInterface {
		var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
			WriteIndented = true
		};

		// 1. DTO への変換 (本来は StateModel 全体で行うが、ここでは個別テスト)
		// ISearchConditionForJson.CreateJson などの静的メソッドが生成されているはず
		// しかし、直接シリアライズ・デシリアライズをテストするのが確実

		string json;
		if (original is ISearchCondition sc) {
			var dto = ISearchConditionForJson.CreateJson(sc);
			json = JsonSerializer.Serialize(dto, this.DefaultOptions);
		} else if (original is IFilterItemObject fio) {
			var dto = IFilterItemObjectForJson.CreateJson(fio);
			json = JsonSerializer.Serialize(dto, this.DefaultOptions);
		} else {
			throw new ArgumentException("Unsupported type");
		}

		// JSON に識別子が含まれているか確認
		json.ShouldContain($"\"___Type\":\"{expectedTypeId}\"");

		// 2. 復元
		object? restoredDto;
		if (typeof(TInterface) == typeof(ISearchCondition)) {
			restoredDto = JsonSerializer.Deserialize<ISearchConditionForJson>(json, this.DefaultOptions);
			var restored = ISearchConditionForJson.CreateModel((ISearchConditionForJson)restoredDto!, this._serviceProvider);
			restored.ShouldBeOfType<TConcrete>();
		} else {
			restoredDto = JsonSerializer.Deserialize<IFilterItemObjectForJson>(json, this.DefaultOptions);
			var restored = IFilterItemObjectForJson.CreateModel((IFilterItemObjectForJson)restoredDto!, this._serviceProvider);
			restored.ShouldBeOfType<TConcrete>();
		}
	}
}