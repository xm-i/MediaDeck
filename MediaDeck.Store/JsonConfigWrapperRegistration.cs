using MediaDeck.Store.Utils;
using R3;
using R3.JsonConfig.Attributes;

[assembly: RegisterJsonConfigWrapper(typeof(ReactiveProperty<>), typeof(ReactivePropertyAdapter<>))]