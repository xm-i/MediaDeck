using MediaDeck.Common.Base;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Models.Repositories;

namespace MediaDeck.ViewModels.Panes.RepositoryPanes;

/// <summary>
/// 各リポジトリペインの基底 ViewModel。
/// リポジトリ種別、表示名、および対応するモデルへの参照を提供します。
/// </summary>
/// <param name="repositoryType">リポジトリ種別。</param>
/// <param name="name">リポジトリの表示名。</param>
/// <param name="model">リポジトリモデル。</param>
public class RepositoryViewModelBase(RepositoryType repositoryType, string name, RepositoryBase model) : ViewModelBase {
	/// <summary>
	/// リポジトリ種別を取得します。
	/// </summary>
	public RepositoryType RepositoryType {
		get;
	} = repositoryType;

	/// <summary>
	/// リポジトリの表示名を取得します。
	/// </summary>
	public string Name {
		get;
	} = name;

	/// <summary>
	/// リポジトリモデルを取得します。
	/// </summary>
	public RepositoryBase Model {
		get;
	} = model;
}