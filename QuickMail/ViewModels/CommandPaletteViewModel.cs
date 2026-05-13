using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMail.Models;
using QuickMail.Services;

namespace QuickMail.ViewModels;

public partial class CommandPaletteViewModel : ObservableObject
{
    public IReadOnlyList<CommandDefinition> Commands { get; }

    [ObservableProperty]
    private CommandDefinition? _selectedCommand;

    public CommandPaletteViewModel(ICommandRegistry registry)
    {
        Commands        = registry.GetAll();
        SelectedCommand = Commands.Count > 0 ? Commands[0] : null;
    }

    partial void OnSelectedCommandChanged(CommandDefinition? value)
    {
        ExecuteSelectedCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedCommand))]
    private void ExecuteSelected()
    {
        SelectedCommand!.Execute();
    }

    private bool HasSelectedCommand() => SelectedCommand is not null;
}
