using System.Diagnostics.CodeAnalysis;

namespace MauiTestApplication;

/// <inheritdoc/>
[SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
public partial class AppShell : Shell
{
  /// <inheritdoc/>
  public AppShell() => InitializeComponent();
}