using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method names with underscores improve readability")]
[assembly: SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test methods may use inline arrays for clarity")]