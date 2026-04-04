using System;
using System.IO;
using FluentAssertions;
using MediaDeck.Composition.Enum;
using MediaDeck.Composition.Stores.Config.Model;
using MediaDeck.Composition.Stores.Config.Model.Objects;
using MediaDeck.Core.Models.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediaDeck.Core.Tests.Models.Services;

public class FilePathServiceTests
{
    private readonly FilePathService _sut;
    private readonly ConfigModel _config;

    public FilePathServiceTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton<PathConfigModel>();
        services.AddSingleton<ExecutionConfigModel>();
        services.AddTransient<ExtensionObjectModel>();
        services.AddSingleton<ScanConfigModel>();
        services.AddSingleton<ConfigModel>();

        var provider = services.BuildServiceProvider();

        _config = provider.GetRequiredService<ConfigModel>();
        _sut = new FilePathService(_config);
    }

    [Fact]
    public void GetThumbnailRelativeFilePath_ShouldReturnGuidBasedPath()
    {
        // Act
        var result = _sut.GetThumbnailRelativeFilePath();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().EndWith(".jpg");

        // Format should be: [2 chars]\[30 chars].jpg
        // Guid without hyphens ("N") is 32 chars. Total length = 2 + 1 + 30 + 4 = 37
        result.Length.Should().Be(37);
        result.Substring(2, 1).Should().Be(@"\");
    }

    [Fact]
    public void GetThumbnailAbsoluteFilePath_ShouldReturnCombinedPath()
    {
        // Arrange
        var relativePath = @"1a\bcdef0123456789abcdef0123456789.jpg";
        var expectedBasePath = _config.PathConfig.ThumbnailFolderPath.Value;
        var expectedAbsolutePath = Path.Combine(expectedBasePath, relativePath);

        // Act
        var result = _sut.GetThumbnailAbsoluteFilePath(relativePath);

        // Assert
        result.Should().Be(expectedAbsolutePath);
    }

    [Theory]
    [InlineData("test.jpg", true)]
    [InlineData("TEST.JPG", true)] // Case insensitive
    [InlineData("video.mp4", true)]
    [InlineData("document.pdf", true)]
    [InlineData("archive.zip", true)]
    [InlineData("test.txt", false)] // Not in target extensions
    [InlineData("no_extension", false)]
    public void IsTargetFile_ShouldReturnExpectedResult(string path, bool expected)
    {
        // Act
        var result = _sut.IsTargetFile(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("VIDEO.MP4", true)]
    [InlineData("movie.avi", true)]
    [InlineData("image.jpg", false)]
    [InlineData("doc.pdf", false)]
    [InlineData("text.txt", false)]
    public void IsVideoFile_ShouldReturnExpectedResult(string path, bool expected)
    {
        // Act
        var result = _sut.IsVideoFile(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("image.jpg", true)]
    [InlineData("IMAGE.JPG", true)]
    [InlineData("photo.png", true)]
    [InlineData("video.mp4", false)]
    [InlineData("doc.pdf", false)]
    [InlineData("text.txt", false)]
    public void IsImageFile_ShouldReturnExpectedResult(string path, bool expected)
    {
        // Act
        var result = _sut.IsImageFile(path);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("image.jpg", MediaType.Image)]
    [InlineData("IMAGE.JPG", MediaType.Image)]
    [InlineData("video.mp4", MediaType.Video)]
    [InlineData("doc.pdf", MediaType.Pdf)]
    [InlineData("archive.zip", MediaType.Archive)]
    [InlineData("unknown.txt", null)]
    public void GetMediaType_ShouldReturnExpectedResult(string path, MediaType? expected)
    {
        // Act
        var result = _sut.GetMediaType(path);

        // Assert
        result.Should().Be(expected);
    }
}
