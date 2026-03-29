namespace AvaloniaIntegration.ViewModels;

using Avalonia.Platform.Storage;

using BindingWrapperUtils;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IStorageProvider _storageProvider;

    private static readonly FilePickerOpenOptions s_openOptions = new() {
        AllowMultiple = false,
        Title = "Select Audio file",
        FileTypeFilter = [
            new FilePickerFileType("Audio Files") {
                Patterns = [
                    "*.mp3",
                    "*.wav",
                    "*.flac",
                    "*.aac",
                    "*.ogg",
                    "*.m4a",
                    "*.wma"
                ]
            },
            FilePickerFileTypes.All
        ]
    };
    
    [ObservableProperty]
    public partial string CurrentFile { get; private set; }
    
    public MainViewModel(IStorageProvider provider)
    {
        _storageProvider = provider;
    }

    [RelayCommand]
    public async Task GetFile()
    {
        var files = await _storageProvider.OpenFilePickerAsync(s_openOptions);
        
        if(files.Count is 0)
            return;

        CurrentFile = files[0].Path.AbsolutePath;
    }
}