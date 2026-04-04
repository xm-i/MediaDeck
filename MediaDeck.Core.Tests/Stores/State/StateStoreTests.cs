using System;
using System.IO;
using System.Runtime.CompilerServices;
using MediaDeck.Composition.Constants;
using MediaDeck.Composition.Stores.State.Model;
using MediaDeck.Core.Stores.State;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace MediaDeck.Core.Tests.Stores.State;

public class StateStoreTests {
	[Fact]
	public void Save_ShouldHandleException_WhenFileIsLocked() {
		// Arrange
		var serviceCollection = new ServiceCollection();

		var searchState = RuntimeHelpers.GetUninitializedObject(typeof(SearchStateModel)) as SearchStateModel;
		var folderManagerState = RuntimeHelpers.GetUninitializedObject(typeof(FolderManagerStateModel)) as FolderManagerStateModel;
		var viewerState = RuntimeHelpers.GetUninitializedObject(typeof(ViewerStateModel)) as ViewerStateModel;
		var stateModel = new StateModel(searchState!, folderManagerState!, viewerState!);

		serviceCollection.AddSingleton(stateModel);
		var serviceProvider = serviceCollection.BuildServiceProvider();

		var store = new StateStore(serviceProvider);

		var filePath = FilePathConstants.StateFilePath;
		var dir = Path.GetDirectoryName(filePath);
		if (dir != null && !Directory.Exists(dir)) {
			Directory.CreateDirectory(dir);
		}

		// Acquire an exclusive lock to force an IOException during Save
		using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
			// Act & Assert
			// The method should catch the IOException and not throw it to the caller
			var exception = Record.Exception(() => store.Save());
			exception.ShouldBeNull();
		}
	}
}