using FluentAssertions;
using MediaDeck.Core.Models.Repositories;
using System.Runtime.CompilerServices;
using Xunit;

namespace MediaDeck.Core.Tests.Models.Repositories;

/// <summary>
/// <see cref="RepositorySelector"/> のテストクラスです。
/// </summary>
public class RepositorySelectorTest
{
    /// <summary>
    /// コンストラクタがプロパティを正しく初期化することを確認します。
    /// </summary>
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        // FolderRepositoryは依存関係が多くコンストラクタが重いため、RuntimeHelpersを使用して未初期化インスタンスを生成します。
        // RepositorySelectorはインスタンスを保持するだけなので、これで十分です。
        var dummyFolderRepository = RuntimeHelpers.GetUninitializedObject(typeof(FolderRepository)) as FolderRepository;

        // Act
        var selector = new RepositorySelector(dummyFolderRepository!);

        // Assert
        selector.Repositories.Should().NotBeNull();
        selector.Repositories.Should().ContainSingle();
        selector.Repositories.First().Should().BeSameAs(dummyFolderRepository);

        selector.SelectedRepository.Should().NotBeNull();
        selector.SelectedRepository.Value.Should().BeSameAs(dummyFolderRepository);
    }
}
