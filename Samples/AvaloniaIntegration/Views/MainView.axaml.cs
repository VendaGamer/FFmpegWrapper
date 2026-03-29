namespace AvaloniaIntegration.Views;

using Avalonia;
using Avalonia.Controls;
using ViewModels;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        DataContext = new MainViewModel(
            TopLevel.GetTopLevel(this)?.StorageProvider 
            ?? throw new Exception("Could not get Top Level") 
        );
    }
}