using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickMail.Models;
using QuickMail.ViewModels;

namespace QuickMail.Views;

public partial class ComposeWindow : Window
{
    private readonly ComposeViewModel _vm;

    public ComposeWindow(ComposeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.StatusText) && !string.IsNullOrEmpty(vm.StatusText))
                AccessibilityHelper.Announce(this, vm.StatusText);
        };
        Loaded  += (_, _) => ToBox.Focus();
        Closing += OnWindowClosing;
    }

    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // If the message was sent, or nothing was edited, let the window close freely.
        if (_vm.IsSent || !_vm.IsDirty)
            return;

        // Prevent the window from closing until the user decides.
        e.Cancel = true;

        var result = MessageBox.Show(
            this,
            "Do you want to save this message as a draft before closing?",
            "Save Draft?",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
            return; // stay open

        if (result == MessageBoxResult.No)
        {
            // Synchronous path — still inside the original Close() call stack.
            // Setting e.Cancel = false lets that Close() proceed normally
            // without a nested Close() call, which would crash WPF.
            Closing -= OnWindowClosing;
            e.Cancel = false;
            return;
        }

        // result == Yes: save the draft first
        await _vm.SaveDraftCommand.ExecuteAsync(null);
        // Only close if the save succeeded (status won't say "failed")
        if (_vm.StatusText.Contains("failed", System.StringComparison.OrdinalIgnoreCase))
            return;

        // After an await the original Close() has already returned (e.Cancel was true),
        // so we need a fresh Close() here to actually close the window.
        Closing -= OnWindowClosing;
        Close();
    }

    // Delete key removes selected attachment from the compose list.
    private void AttachmentList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && AttachmentList.SelectedItem is AttachmentModel a)
        {
            _vm.RemoveAttachmentCommand.Execute(a);
            e.Handled = true;
        }
    }

    // Drag-and-drop: accept file drops anywhere on the compose window.
    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            foreach (var f in files)
                _vm.AddAttachmentFromPath(f);
    }

    // Alt+U → Subject field; Alt+M → From combo; Ctrl+V with files → add attachments; Escape → cancel.
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+V: if the clipboard contains files, paste them as attachments
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && Clipboard.ContainsFileDropList())
        {
            foreach (string? f in Clipboard.GetFileDropList())
            {
                if (f != null) _vm.AddAttachmentFromPath(f);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            _vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.SystemKey == Key.U && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
        {
            SubjectBox.Focus();
            SubjectBox.SelectAll();
            e.Handled = true;
        }
        else if (e.SystemKey == Key.M && (Keyboard.Modifiers & ModifierKeys.Alt) != 0)
        {
            FromCombo.Focus();
            FromCombo.IsDropDownOpen = true;
            e.Handled = true;
        }
    }
}
