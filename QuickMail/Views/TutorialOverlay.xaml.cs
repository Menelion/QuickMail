using System.Windows;
using System.Windows.Controls;
using QuickMail.Models;
using QuickMail.ViewModels;

namespace QuickMail.Views;

public partial class TutorialOverlay : UserControl
{
    private TutorialViewModel? _viewModel;

    public TutorialOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void SetViewModel(TutorialViewModel vm)
    {
        _viewModel = vm;
        DataContext = vm;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TutorialViewModel.CurrentStep) && vm.IsActive)
            {
                var step = vm.CurrentStep;
                if (step != null)
                {
                    var text = step.InstructionText + " Press Escape to skip.";
                    AccessibilityHelper.Announce(this, text, interrupt: true, category: AnnouncementCategory.Hint);
                }
            }
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.IsActive == true && _viewModel.CurrentStep != null)
        {
            var text = _viewModel.CurrentStep.InstructionText + " Press Escape to skip.";
            AccessibilityHelper.Announce(this, text, interrupt: true, category: AnnouncementCategory.Hint);
        }
    }
}
