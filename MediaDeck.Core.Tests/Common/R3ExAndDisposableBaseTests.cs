using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Core.Models.Files;
using MediaDeck.Core.Primitives;
using ObservableCollections;
using R3;
using Shouldly;

namespace MediaDeck.Core.Tests.Common;

/// <summary>
/// <see cref="R3Ex"/> および <see cref="DisposableBase"/> のリソース管理に関するテスト
/// </summary>
public class R3ExAndDisposableBaseTests {
	/// <summary>
	/// ToTwoWayBindableReactiveProperty に渡した CompositeDisposable が破棄された際、生成されたプロパティも破棄されることを検証します。
	/// </summary>
	[Fact]
	public void ToTwoWayBindableReactiveProperty_DisposesProperty_WhenCompositeDisposableDisposed() {
		var disposables = new CompositeDisposable();
		var source = new ReactiveProperty<string>("initial").AddTo(disposables);
		var target = source.ToTwoWayBindableReactiveProperty("initial", disposables);

		disposables.Dispose();

		// Dispose後はプロパティが破棄され、代入時に ObjectDisposedException がスローされること
		Should.Throw<ObjectDisposedException>(() => target.Value = "changed");
		source.Value.ShouldBe("initial");
	}

	/// <summary>
	/// ToTwoWayReactiveProperty に渡した CompositeDisposable が破棄された際、生成されたプロパティも破棄されることを検証します。
	/// </summary>
	[Fact]
	public void ToTwoWayReactiveProperty_DisposesProperty_WhenCompositeDisposableDisposed() {
		var disposables = new CompositeDisposable();
		var source = new ReactiveProperty<int>(1).AddTo(disposables);
		var target = source.ToTwoWayReactiveProperty(x => x.ToString(), int.Parse, "1", disposables);

		disposables.Dispose();

		// Dispose後はプロパティが破棄され、代入時に ObjectDisposedException がスローされること
		Should.Throw<ObjectDisposedException>(() => target.Value = "2");
		source.Value.ShouldBe(1);
	}

#if DEBUG
	private class TestDisposable : DisposableBase {
	}

	/// <summary>
	/// DisposableBase のインスタンスが Dispose された際、CreatedInstances から正しく削除されることを検証します。
	/// </summary>
	[Fact]
	public void DisposableBase_RemovesFromCreatedInstances_OnDispose() {
		var instance = new TestDisposable();

		DisposableBase.CreatedInstances.TryGetValue(typeof(TestDisposable), out var dict).ShouldBeTrue();
		dict!.Count.ShouldBeGreaterThan(0);

		instance.Dispose();

		dict.Count.ShouldBe(0);
	}
#endif

	/// <summary>
	/// DetailSelectorModel が ModelBase を継承し、Dispose 時に ReactiveProperty が破棄されることを検証します。
	/// </summary>
	[Fact]
	public void DetailSelectorModel_InheritsModelBase_AndDisposesPropertiesOnDispose() {
		var mockTagsManager = new Moq.Mock<MediaDeck.Composition.Interfaces.Tags.ITagsManager>();
		var model = new DetailSelectorModel(mockTagsManager.Object);
		model.ShouldBeAssignableTo<ModelBase>();

		model.Dispose();

		// Dispose済み状態であることを検証
		model.DisposeState.ShouldBe(MediaDeck.Composition.Enum.DisposeState.Disposed);
	}

	private sealed class MockDisposableItem(int id) : IDisposable {
		public int Id { get; } = id;
		public bool IsDisposed {
			get; private set;
		}
		public void Dispose() {
			this.IsDisposed = true;
		}
	}

	/// <summary>
	/// コレクションから要素が削除された際、生成された要素が自動破棄されることを検証します。
	/// </summary>
	[Fact]
	public void CreateViewWithDisposal_DisposesItem_OnRemove() {
		var list = new ObservableList<int>();
		var view = list.CreateViewWithDisposal(x => new MockDisposableItem(x));

		list.Add(1);
		list.Add(2);

		var item1 = view.First(x => x.Id == 1);
		var item2 = view.First(x => x.Id == 2);

		item1.IsDisposed.ShouldBeFalse();
		item2.IsDisposed.ShouldBeFalse();

		list.Remove(1);

		item1.IsDisposed.ShouldBeTrue();
		item2.IsDisposed.ShouldBeFalse();

		view.Dispose();
	}

	/// <summary>
	/// コレクションが Clear された際、生成されたすべての要素が自動破棄されることを検証します。
	/// </summary>
	[Fact]
	public void CreateViewWithDisposal_DisposesAllItems_OnClear() {
		var list = new ObservableList<int>();
		var createdItems = new List<MockDisposableItem>();
		var view = list.CreateViewWithDisposal(x => {
			var item = new MockDisposableItem(x);
			createdItems.Add(item);
			return item;
		});

		list.Add(1);
		list.Add(2);
		list.Add(3);

		createdItems.Count.ShouldBe(3);
		createdItems.All(x => !x.IsDisposed).ShouldBeTrue();

		list.Clear();

		createdItems.All(x => x.IsDisposed).ShouldBeTrue();

		view.Dispose();
	}

	/// <summary>
	/// 要素が置換された際、置換前の要素が自動破棄されることを検証します。
	/// </summary>
	[Fact]
	public void CreateViewWithDisposal_DisposesOldItem_OnReplace() {
		var list = new ObservableList<int>();
		var view = list.CreateViewWithDisposal(x => new MockDisposableItem(x));

		list.Add(10);
		var oldItem = view.First();
		oldItem.IsDisposed.ShouldBeFalse();

		list[0] = 20;

		oldItem.IsDisposed.ShouldBeTrue();
		var newItem = view.First();
		newItem.Id.ShouldBe(20);
		newItem.IsDisposed.ShouldBeFalse();

		view.Dispose();
	}

	/// <summary>
	/// ビュー自身が破棄された際、現在保持されている要素がすべて破棄されることを検証します。
	/// </summary>
	[Fact]
	public void CreateViewWithDisposal_DisposesRemainingItems_OnViewDispose() {
		var list = new ObservableList<int>();
		var view = list.CreateViewWithDisposal(x => new MockDisposableItem(x));

		list.Add(1);
		list.Add(2);

		var item1 = view.First(x => x.Id == 1);
		var item2 = view.First(x => x.Id == 2);

		view.Dispose();

		item1.IsDisposed.ShouldBeTrue();
		item2.IsDisposed.ShouldBeTrue();
	}

	/// <summary>
	/// ToNotifyCollectionChangedWithDisposal で生成されたリストが破棄された際、親ビューと全要素も破棄されることを検証します。
	/// </summary>
	[Fact]
	public void ToNotifyCollectionChangedWithDisposal_DisposesParentViewAndItems_OnListDispose() {
		var list = new ObservableList<int>();
		var createdItems = new List<MockDisposableItem>();
		var view = list.CreateViewWithDisposal(x => {
			var item = new MockDisposableItem(x);
			createdItems.Add(item);
			return item;
		});

		var notifyList = view.ToNotifyCollectionChangedWithDisposal();

		list.Add(1);
		list.Add(2);

		createdItems.Count.ShouldBe(2);
		createdItems.All(x => !x.IsDisposed).ShouldBeTrue();

		notifyList.Dispose();

		createdItems.All(x => x.IsDisposed).ShouldBeTrue();
	}

	/// <summary>
	/// ValueCountPair の Dispose が内部の IDisposable 値を破棄することを検証します。
	/// </summary>
	[Fact]
	public void ValueCountPair_DisposesInnerValue_WhenDisposed() {
		var inner = new MockDisposableItem(42);
		var pair = new ValueCountPair<MockDisposableItem>(inner, 5);

		inner.IsDisposed.ShouldBeFalse();

		pair.Dispose();

		inner.IsDisposed.ShouldBeTrue();
	}
}