namespace MediaDeck.Composition.Startup;

public sealed record RuntimePathOptions(string? BaseDirectory);

public static class RuntimeArgumentsParser {
	public static RuntimePathOptions Parse(string[] args) {
		string? baseDirectory = null;

		for (var i = 0; i < args.Length; i++) {
			var current = args[i];
			if (!TryReadKeyValue(current, args, i, out var key, out var value, out var consumedNext)) {
				continue;
			}

			switch (key.ToLowerInvariant()) {
				case "base":
					baseDirectory = NormalizePath(value);
					break;
			}

			if (consumedNext) {
				i++;
			}
		}

		return new RuntimePathOptions(baseDirectory);
	}

	private static string NormalizePath(string value) {
		return Path.GetFullPath(value.Trim());
	}

	private static bool TryReadKeyValue(string currentArg, string[] args, int index, out string key, out string value, out bool consumedNext) {
		key = string.Empty;
		value = string.Empty;
		consumedNext = false;

		if (currentArg.StartsWith("--", StringComparison.Ordinal)) {
			var body = currentArg[2..];
			var separatorIndex = body.IndexOf('=');
			if (separatorIndex >= 0) {
				key = body[..separatorIndex];
				value = body[(separatorIndex + 1)..];
				return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value);
			}

			if (index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1])) {
				key = body;
				value = args[index + 1];
				consumedNext = true;
				return !string.IsNullOrWhiteSpace(key);
			}
		}

		if (currentArg.StartsWith('/')) {
			var body = currentArg[1..];
			var separatorIndex = body.IndexOf(':');
			if (separatorIndex >= 0) {
				key = body[..separatorIndex];
				value = body[(separatorIndex + 1)..];
				return !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value);
			}
		}

		return false;
	}
}