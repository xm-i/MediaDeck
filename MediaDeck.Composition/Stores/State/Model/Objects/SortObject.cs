using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using R3.JsonConfig.Attributes;

namespace MediaDeck.Composition.Stores.State.Model.Objects;
/// <summary>
/// ソート設定復元用オブジェクト
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDto]
public class SortObject(IServiceProvider serviceProvider) {
	/// <summary>
	/// ID
	/// </summary>
	public Guid Id {
		get;
		set;
	} = Guid.NewGuid();

	/// <summary>
	/// 表示名
	/// </summary>
	public ReactiveProperty<string> DisplayName {
		get;
	} = new();

	/// <summary>
	/// ソート条件クリエイター
	/// </summary>
	public ObservableList<SortItemObject> SortItemObjects {
		get;
	} = [];

	public SortItemObject AddSortItemObject() {
		var sio = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<SortItemObject>();
		this.SortItemObjects.Add(sio);
		return sio;
	}

	public void RemoveSortItemObject(SortItemObject sortItemObject) {
		this.SortItemObjects.Remove(sortItemObject);
	}

	public bool Equals(SortObject? other) {
		if (other is null) {
			return false;
		}

		if (ReferenceEquals(this, other)) {
			return true;
		}

		return Equals(this.DisplayName.Value, other.DisplayName.Value) && this.SortItemObjects.SequenceEqual(other.SortItemObjects);
	}

	public override bool Equals(object? obj) {
		return obj is SortObject rso && this.Equals(rso);
	}

	public override int GetHashCode() {
		unchecked {
			return ((this.DisplayName.Value?.GetHashCode() ?? 0) * 397) ^ this.SortItemObjects.Select(x => x.GetHashCode()).Aggregate((x, y) => x ^ y);
		}
	}
}
