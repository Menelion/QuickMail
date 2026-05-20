using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QuickMail.ViewModels;

namespace QuickMail.Views;

public partial class ViewManagerWindow : Window
{
    private readonly ViewManagerViewModel _vm;

    /// <summary>True when opened via Save View… (create-new flow) rather than Manage Views…</summary>
    private readonly bool _createMode;

    /// <summary>Set to true after a successful Save so that OnClosing knows not to delete.</summary>
    private bool _savedInCreateMode;

    public ViewManagerWindow(ViewManagerViewModel vm, bool createMode = false)
    {
        _vm         = vm;
        _createMode = createMode;
        InitializeComponent();
        DataContext = vm;

        vm.FolderConflictDetected += OnFolderConflict;
        vm.SetHotkeyRequested     += OnSetHotkeyRequested;

        if (createMode)
            Loaded += OnLoadedCreateMode;
    }

    // ── Create-mode setup ─────────────────────────────────────────────────────────

    private void OnLoadedCreateMode(object sender, RoutedEventArgs e)
    {
        // Retitle the window.
        Title = "Save View";

        // Collapse the saved-views list and splitter so the editing panel fills the dialog.
        ListColumn.Width     = new System.Windows.GridLength(0);
        SplitterColumn.Width = new System.Windows.GridLength(0);

        // Swap the bottom button: show "Save View", change "Close" to "Cancel".
        SaveViewAndCloseButton.Visibility = Visibility.Visible;
        CloseButton.Content = "Cancel";

        // Create a new view from the current app state and select it.
        _vm.SaveAsNewCommand.Execute(null);

        // Land focus in the name field with the text selected so the user can type immediately.
        NameBox.Focus();
        NameBox.SelectAll();
    }

    // ── Save View button (create mode only) ───────────────────────────────────────

    private void SaveViewAndCloseButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
        _savedInCreateMode = true;
        Close();
    }

    /// <summary>
    /// If the user dismisses the dialog in create mode without saving (Escape, ✕, Cancel),
    /// remove the view that was auto-created so we don't leave a half-named phantom entry.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_createMode && !_savedInCreateMode && _vm.SelectedView != null)
            _vm.DeleteCommand.Execute(null);
    }

    // ── Folder conflict prompt ────────────────────────────────────────────────────

    private void OnFolderConflict(object? sender, FolderConflictEventArgs e)
    {
        var existing = string.Join(", ", e.ExistingFolders);
        var msg = $"The current folder ({e.NewFolder}) is not in this view's folder list ({existing}).\n\n" +
                  $"Yes = replace the existing folder(s) with {e.NewFolder}\n" +
                  $"No  = add {e.NewFolder} alongside the existing folder(s)\n" +
                  $"Cancel = do nothing";

        var result = MessageBox.Show(msg, "Folder Changed",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        var resolution = result switch
        {
            MessageBoxResult.Yes    => FolderConflictResolution.Replace,
            MessageBoxResult.No     => FolderConflictResolution.Add,
            _                       => FolderConflictResolution.Cancel,
        };

        _vm.ResolveConflict(resolution);
    }

    // ── Hotkey capture ────────────────────────────────────────────────────────────

    private void OnSetHotkeyRequested(object? sender, EventArgs e)
    {
        var dlg = new KeyCaptureDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;

        // Check for conflicts across the entire registry (same approach as SettingsDialog).
        var key       = dlg.CapturedKey;
        var modifiers = dlg.CapturedModifiers;
        var gesture   = Helpers.GestureHelper.Format(key, modifiers);

        // Check all existing views for the same hotkey (exclude the one being edited).
        var conflict = _vm.SavedViews.FirstOrDefault(v =>
            v != _vm.SelectedView &&
            !string.IsNullOrEmpty(v.Hotkey) &&
            string.Equals(v.Hotkey, gesture, StringComparison.OrdinalIgnoreCase));

        if (conflict != null)
        {
            var msg = $"The shortcut {gesture} is already assigned to view \"{conflict.Name}\".\n\n" +
                      "Reassign it to this view? The other view will lose its shortcut.";
            if (MessageBox.Show(msg, "Shortcut Conflict",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            // Clear the conflicting view's hotkey.
            conflict.Hotkey = null;
        }

        _vm.ApplyHotkey(key, modifiers);
    }

    // ── Close ─────────────────────────────────────────────────────────────────────

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
