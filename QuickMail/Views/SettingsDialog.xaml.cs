using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QuickMail.ViewModels;

namespace QuickMail.Views;

/// <summary>
/// Converts an empty string to "—" (dash) for display purposes.
/// </summary>
public class EmptyStringToDashConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string;
        return string.IsNullOrEmpty(str) ? "—" : str;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsDialog(SettingsViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
        DialogResult = true;
        Close();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
            e.Handled = true;
        }
    }

    private void HotkeyListView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_vm.SelectedHotkey == null)
        {
            return;
        }

        // Allow navigation keys to work normally
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End
            or Key.PageUp or Key.PageDown or Key.Tab)
        {
            return;
        }

        // Require at least one modifier key (Ctrl, Alt, or Shift)
        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            return;
        }

        // Ignore modifier-only key presses
        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift)
        {
            return;
        }

        _vm.SelectedHotkey.SetCustomBinding(e.Key, modifiers);
        e.Handled = true;

        // Update Clear button state
        ClearButton.IsEnabled = _vm.SelectedHotkey.HasCustomBinding;
    }

    private void HotkeyListView_SelectionChanged(object sender, object e)
    {
        ClearButton.IsEnabled = _vm.SelectedHotkey != null && _vm.SelectedHotkey.HasCustomBinding;
    }
}
