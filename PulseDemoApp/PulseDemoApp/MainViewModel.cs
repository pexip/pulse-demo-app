using CommunityToolkit.Mvvm.Input;
using PulseDemoApp.Validation;

namespace PulseDemoApp;

public partial class MainViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    private bool joinCardEnabled;

    [ObservableProperty]
    private bool hostPinCardEnabled;

    [ObservableProperty]
    private bool conferenceCardEnabled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private string videoAddress;

    #endregion

    public MainViewModel()
    {
        this.videoAddress = string.Empty;
    }

    #region Commands

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        
    }

    #endregion

    #region Command Validations

    private bool CanRegister()
    {
        return new AliasValidator().ValidateFullyQualifiedAlias(VideoAddress);
    }

    #endregion
}
