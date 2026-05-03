using MediaDeck.Common.Base;
using MediaDeck.Common.Extensions;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.ViewModels.Panes.ViewerPanes;

namespace MediaDeck.ViewModels;

/// <summary>
/// ステータスバーの情報を管理するViewModel
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class StatusBarViewModel : ViewModelBase
{
    /// <summary>
    /// メディアライブラリのViewModel
    /// </summary>
    public MediaContentLibraryViewModel MediaContentLibrary { get; }

    /// <summary>
    /// アイテムの表示サイズ
    /// </summary>
    public BindableReactiveProperty<int> ItemSize { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="mediaContentLibrary">メディアライブラリのViewModel</param>
    /// <param name="tabState">タブの状態モデル</param>
    public StatusBarViewModel(MediaContentLibraryViewModel mediaContentLibrary, TabStateModel tabState)
    {
        this.MediaContentLibrary = mediaContentLibrary;

        // StateのItemSizeと双方向に同期するプロパティを作成
        this.ItemSize = tabState.ViewerState.ItemSize
            .ToTwoWayBindableReactiveProperty(tabState.ViewerState.ItemSize.Value, this.CompositeDisposable)
            .AddTo(this.CompositeDisposable);
    }
}
