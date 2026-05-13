using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        SearchBox.Focus();
    }

    // ── Keyboard handling ─────────────────────────────────────────────────────────

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

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Route Up/Down from the search box to the list.
        if (e.Key == Key.Down)
        {
            MoveSelection(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            MoveSelection(-1);
            e.Handled = true;
        }
    }

    private void CommandList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RunSelected();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private void MoveSelection(int delta)
    {
        var count = CommandList.Items.Count;
        if (count == 0) return;

        var index = CommandList.SelectedIndex + delta;
        index = Math.Clamp(index, 0, count - 1);
        CommandList.SelectedIndex = index;
        CommandList.ScrollIntoView(CommandList.SelectedItem);
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
