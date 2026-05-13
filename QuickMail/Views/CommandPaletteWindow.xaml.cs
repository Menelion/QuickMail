using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QuickMail.Services;
using QuickMail.ViewModels;

namespace QuickMail.Views;

public partial class CommandPaletteWindow : Window
{
    private readonly CommandPaletteViewModel _vm;

    public CommandPaletteWindow(ICommandRegistry registry)
    {
        _vm = new CommandPaletteViewModel(registry);
        InitializeComponent();
        DataContext = _vm;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Center below the top edge of the owner window (similar to VS Code palette position).
        if (Owner != null)
        {
            Left = Owner.Left + (Owner.ActualWidth - ActualWidth) / 2;
            Top  = Owner.Top + 60;
        }

        // Select and focus the first item so the screen reader announces it immediately.
        MoveSelection(0);
    }

    // ── Keyboard handling ────────────────────────────────────────────────────────

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            RunSelected();
            e.Handled = true;
        }
    }

    private void CommandList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RunSelected();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    // delta=0 re-focuses the currently selected index (used on load).
    private void MoveSelection(int delta)
    {
        var count = CommandList.Items.Count;
        if (count == 0) return;

        var index = delta == 0
            ? Math.Max(CommandList.SelectedIndex, 0)
            : Math.Clamp(CommandList.SelectedIndex + delta, 0, count - 1);

        CommandList.SelectedIndex = index;
        CommandList.ScrollIntoView(CommandList.SelectedItem);

        // Defer focus until after the layout pass so the container is materialised.
        Dispatcher.InvokeAsync(() =>
        {
            if (CommandList.ItemContainerGenerator.ContainerFromIndex(index) is ListBoxItem item)
                item.Focus();
        }, DispatcherPriority.Background);
    }

    private void RunSelected()
    {
        if (_vm.SelectedCommand != null)
        {
            _vm.ExecuteSelectedCommand.Execute(null);
            DialogResult = true;
        }
    }
}
