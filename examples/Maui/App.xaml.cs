using System.Diagnostics.CodeAnalysis;

namespace MauiTestApplication;

/// <inheritdoc/>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
[SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
public partial class App : Application
{
  /// <inheritdoc/>
  public App() => InitializeComponent();

  /// <inheritdoc/>
  protected override Window CreateWindow(IActivationState? activationState)
    => new(new AppShell());
}