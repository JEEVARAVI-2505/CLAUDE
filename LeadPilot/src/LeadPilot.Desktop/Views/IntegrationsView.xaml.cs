using System.Windows.Controls;

namespace LeadPilot.Desktop.Views;

/// <summary>Integrations screen. All behaviour lives in the view-model.</summary>
public partial class IntegrationsView : UserControl
{
    /// <summary>Plain-language list of what data can leave the computer (shown in the view).</summary>
    public static string DisclosureText => LeadPilot.Application.ViewModels.IntegrationsViewModel.Disclosure;

    public IntegrationsView() { InitializeComponent(); }
}
