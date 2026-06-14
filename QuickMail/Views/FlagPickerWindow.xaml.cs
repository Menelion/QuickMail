using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickMail.Models;
using QuickMail.Services;
using QuickMail.ViewModels;

namespace QuickMail.Views;

public partial class FlagPickerWindow : Window
{
    private readonly FlagPickerViewModel _vm;

    public string? ResultFlagId { get; private set; }

    public FlagPickerWindow(IFlagService flagService, bool currentlyFlagged)
    {
        _vm = new FlagPickerViewModel(flagService, currentlyFlagged);
        InitializeComponent();
        DataContext = _vm;

        _vm.FlagSelected += OnFlagSelected;

        Loaded += async (_, _) =>
        {
            await _vm.LoadAsync();
            FlagList.Focus();
        };
    }

    private void OnFlagSelected(FlagDefinition? flag)
    {
        ResultFlagId = flag?.Id.ToString();
        DialogResult = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Enter && FlagList.IsKeyboardFocusWithin)
        {
            _vm.ApplyFlagCommand.Execute(null);
            e.Handled = true;
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _vm.FlagSelected -= OnFlagSelected;
        base.OnClosed(e);
    }
}
