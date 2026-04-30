using MediaDeck.Composition.Enum;

namespace MediaDeck.Composition.Interfaces.MediaItemTypes.ViewModels;

/// <summary>
/// 一括サムネイル再生成ウィンドウに表示する、メディアタイプ毎の生成設定 ViewModel。
/// 各 MediaType ごとに固有の設定 (動画なら時間、PDF ならページ番号 等) を持ち、
/// 与えられたアイテムに対してサムネイルを生成・保存する責務を持つ。
/// </summary>
public interface IBulkThumbnailConfigViewModel {
	/// <summary>
	/// この設定が対象とするメディアタイプ。
	/// </summary>
	public MediaType MediaType {
		get;
	}

	/// <summary>
	/// 現在の設定値で指定アイテムのサムネイルを再生成し、永続化する。
	/// </summary>
	/// <param name="target">対象メディアアイテム ViewModel。</param>
	/// <param name="cancellationToken">処理中断トークン。</param>
	public Task ApplyToAsync(IMediaItemViewModel target, CancellationToken cancellationToken);
}