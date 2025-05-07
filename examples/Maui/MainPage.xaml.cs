using System.Diagnostics.CodeAnalysis;

namespace MauiTestApplication;

/// <inheritdoc/>
[SuppressMessage("Microsoft.Maintainability", "CA1501:AvoidExcessiveInheritance")]
public partial class MainPage : ContentPage
{
  private int _count;

  /// <inheritdoc/>
  public MainPage() => InitializeComponent();

  private void OnCounterClicked(object sender, EventArgs e)
  {
    _count++;
    CounterBtn.Text = $"Clicked {_count} time{(_count > 1 ? 's' : ' ')}";
    SemanticScreenReader.Announce(CounterBtn.Text);
  }
}