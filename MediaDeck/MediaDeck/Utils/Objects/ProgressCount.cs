namespace MediaDeck.Utils.Objects; 
public class ProgressCount {

	public ReactiveProperty<long> Current {
		get;
	} = new(0);

	public ReactiveProperty<long?> Total {
		get;
	} = new(null);

	public ReadOnlyReactiveProperty<double> Progress {
		get;
	}

	public ReactiveProperty<bool> InProgress {
		get;
	} = new(false);

	public ReadOnlyReactiveProperty<bool> IsIndeterminate {
		get;
	}

	public ProgressCount() {
		this.Progress = this.Current.CombineLatest(this.Total, (current, total) => {
			if (total is not {} ltotal || ltotal == 0) {
				return 0;
			}
			return 100d * current / ltotal;
		}).ToReadOnlyReactiveProperty();

		this.IsIndeterminate = this.Total.Select(x => x == null).ToReadOnlyReactiveProperty();

		this.Progress.Where(x => x == 100).Subscribe(_ => {
			this.InProgress.Value = false;
		});
	}
}
