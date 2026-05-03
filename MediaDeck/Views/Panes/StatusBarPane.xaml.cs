using MediaDeck.ViewModels;

namespace MediaDeck.Views.Panes;

/// <summary>
/// ステータスバーを表示するユーザーコントロール
/// </summary>
public sealed partial class StatusBarPane
{
    public StatusBarPane()
    {
        this.InitializeComponent();
    }
}

/// <summary>
/// StatusBarPaneの基底クラス。UserControlBaseを介してViewModelプロパティを提供します。
/// </summary>
public abstract class StatusBarPaneUserControl : UserControlBase<StatusBarViewModel>;
