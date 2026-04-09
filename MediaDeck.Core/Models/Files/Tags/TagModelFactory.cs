using MediaDeck.Composition.Interfaces.Files;
using MediaDeck.Database.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace MediaDeck.Core.Models.Files.Tags;

[Inject(InjectServiceLifetime.Singleton, typeof(ITagModelFactory))]
public class TagModelFactory : ITagModelFactory {
	private readonly IServiceProvider _serviceProvider;

	public TagModelFactory(IServiceProvider serviceProvider) {
		this._serviceProvider = serviceProvider;
	}

	public ITagModel Create(Tag tag) {
		var model = this._serviceProvider.GetRequiredService<ITagModel>();
		model.Initialize(tag, this);
		return model;
	}

	public ITagCategoryModel Create(TagCategory tagCategory) {
		var model = this._serviceProvider.GetRequiredService<ITagCategoryModel>();
		model.Initialize(tagCategory);
		return model;
	}

	public ITagAliasModel Create(TagAlias tagAlias) {
		var model = this._serviceProvider.GetRequiredService<ITagAliasModel>();
		model.Initialize(tagAlias);
		return model;
	}
}