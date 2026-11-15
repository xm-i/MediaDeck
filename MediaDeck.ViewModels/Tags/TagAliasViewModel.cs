using MediaDeck.Common.Base;
using MediaDeck.Composition.Interfaces.Tags;

namespace MediaDeck.ViewModels.Tags;

/// <summary>
/// タグ別名（エイリアス）の ViewModel クラス。
/// </summary>
public class TagAliasViewModel : ViewModelBase {
	/// <summary>
	/// <see cref="TagAliasViewModel"/> クラスの新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="tagAlias">タグ別名モデル</param>
	/// <param name="parent">親となるタグ ViewModel</param>
	public TagAliasViewModel(ITagAliasModel tagAlias, TagViewModel parent) {
		this.Model = tagAlias;
		this.Parent = parent;
		this.Alias = new BindableReactiveProperty<string>(tagAlias.Alias).AddTo(this.CompositeDisposable);
		this.Ruby = new BindableReactiveProperty<string>(tagAlias.Ruby ?? string.Empty).AddTo(this.CompositeDisposable);

		this.Alias.Skip(1).Subscribe(x => {
			tagAlias.Alias = x;
			parent.RequestSave();
		}).AddTo(this.CompositeDisposable);

		this.Ruby.Skip(1).Subscribe(x => {
			tagAlias.Ruby = string.IsNullOrEmpty(x) ? null : x;
			parent.RequestSave();
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タグ別名モデルを取得します。
	/// </summary>
	public ITagAliasModel Model {
		get;
	}

	/// <summary>
	/// 親となるタグ ViewModel を取得します。
	/// </summary>
	public TagViewModel Parent {
		get;
	}

	/// <summary>
	/// 別名テキストを取得します。
	/// </summary>
	public BindableReactiveProperty<string> Alias {
		get;
	}

	/// <summary>
	/// 読み仮名を取得します。
	/// </summary>
	public BindableReactiveProperty<string> Ruby {
		get;
	}
}