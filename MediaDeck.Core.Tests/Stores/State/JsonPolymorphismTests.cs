using System.Text.Json;
using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Files.Filter.FilterItemObjects;
using MediaDeck.Core.Models.Files.SearchConditions;
using MediaDeck.Stores.SerializerContext;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.State;

public class JsonPolymorphismTests {
	private readonly IServiceProvider _serviceProvider;

	public JsonPolymorphismTests() {
		var services = new ServiceCollection();
		// 必要に応じて依存関係を登録
		services.AddTransient<StateModel>();
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
		var condition = new TagSearchCondition();
		// TagSearchCondition のプロパティ設定が必要ならここで行う
		
		// Act & Assert
		this.VerifyRoundTrip<ISearchCondition, TagSearchCondition>(condition, "tag");
	}

	[Fact]
	public void TagFilterItemObject_RoundTrip_Works() {
		// Arrange
		var filter = new TagFilterItemObject();
		
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
			json = JsonSerializer.Serialize(dto, StateJsonSerializerContext.Default.ISearchConditionForJson);
		} else if (original is IFilterItemObject fio) {
			var dto = IFilterItemObjectForJson.CreateJson(fio);
			json = JsonSerializer.Serialize(dto, StateJsonSerializerContext.Default.IFilterItemObjectForJson);
		} else {
			throw new ArgumentException("Unsupported type");
		}

		// JSON に識別子が含まれているか確認
		json.ShouldContain($"\"___Type\":\"{expectedTypeId}\"");

		// 2. 復元
		object? restoredDto;
		if (typeof(TInterface) == typeof(ISearchCondition)) {
			restoredDto = JsonSerializer.Deserialize(json, StateJsonSerializerContext.Default.ISearchConditionForJson);
			var restored = ISearchConditionForJson.CreateModel((ISearchConditionForJson)restoredDto!, this._serviceProvider);
			restored.ShouldBeOfType<TConcrete>();
		} else {
			restoredDto = JsonSerializer.Deserialize(json, StateJsonSerializerContext.Default.IFilterItemObjectForJson);
			var restored = IFilterItemObjectForJson.CreateModel((IFilterItemObjectForJson)restoredDto!, this._serviceProvider);
			restored.ShouldBeOfType<TConcrete>();
		}
	}
}
