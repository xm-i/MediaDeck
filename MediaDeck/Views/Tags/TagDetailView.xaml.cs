using MediaDeck.ViewModels.Tags;
using Microsoft.UI.Xaml;
using ObservableCollections;

namespace MediaDeck.Views.Tags;

/// <summary>
/// タグ詳細・編集を表示するユーザーコントロール。
/// </summary>
public sealed partial class TagDetailView {
	/// <summary>
	/// タグカテゴリ一覧の依存関係プロパティ。
	/// </summary>
	public static readonly DependencyProperty CategoriesProperty =
		DependencyProperty.Register(
			nameof(Categories),
			typeof(INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel>),
			typeof(TagDetailView),
			new PropertyMetadata(null));

	/// <summary>
	/// <see cref="TagDetailView"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	public TagDetailView() {
		this.InitializeComponent();
	}

	/// <summary>
	/// タグカテゴリ一覧を取得または設定します。
	/// </summary>
	public INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel>? Categories {
		get {
			return (INotifyCollectionChangedSynchronizedViewList<TagCategoryViewModel>?)this.GetValue(CategoriesProperty);
		}

		set {
			this.SetValue(CategoriesProperty, value);
		}
	}

	/// <summary>
	/// タグの削除が要求されたときに発生するイベント。
	/// </summary>
	public event EventHandler? DeleteTagRequested;

	private void DeleteTagButton_Click(object sender, RoutedEventArgs e) {
		this.DeleteTagRequested?.Invoke(this, EventArgs.Empty);
	}
}

/// <summary>
/// <see cref="TagDetailView"/> の基底ユーザーコントロール。
/// </summary>
public abstract class TagDetailViewUserControl : UserControlBase<TagViewModel>;