using R3;
using R3.JsonConfig.Attributes;
using MediaDeck.Store.Utils;

[assembly: RegisterJsonConfigWrapper(typeof(ReactiveProperty<>), typeof(ReactivePropertyAdapter<>))]
