using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickMail.Models;
using QuickMail.Services;

namespace QuickMail.Views;

/// <summary>
/// Modal folder picker backed by a real WPF TreeView so screen readers
/// announce role, level, expanded/collapsed state correctly.
/// </summary>
public partial class FolderPickerWindow : Window
{
    private readonly Dictionary<MailFolderModel, AccountModel> _folderToAccount = new();

    public MailFolderModel? SelectedFolder { get; private set; }
    public AccountModel? SelectedAccount { get; private set; }

    public FolderPickerWindow(IEnumerable<AccountModel> accounts, IReadOnlyDictionary<Guid, List<MailFolderModel>> cachedFolders, MailFolderModel? allMailFolder = null, string title = "Go to Folder")
    {
        InitializeComponent();
        Title = title;

        var roots = new List<FolderTreeNode>();

        // Virtual "All Mail" node at the top of the tree
        if (allMailFolder != null)
            roots.Add(new FolderTreeNode { Folder = allMailFolder, Label = allMailFolder.DisplayName, IsExpanded = true });

        foreach (var account in accounts)
        {
            if (!cachedFolders.TryGetValue(account.Id, out var folders) || folders.Count == 0) continue;

            foreach (var f in folders)
                _folderToAccount[f] = account;

            var accountRoots = FolderTreeBuilder.Build(folders, account);
            foreach (var r in accountRoots)
                roots.Add(r);
        }

        FolderTreeView.ItemsSource = roots;

        Loaded += (_, _) => FolderTreeView.Focus();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e) => Commit();

    private void FolderTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Commit();
        }
    }

    // First-letter navigation for the folder TreeView (TreeView has no built-in TextSearch).
    private void FolderTreeView_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0])) return;

        var allNodes = GetVisibleTreeNodes(FolderTreeView.Items.OfType<FolderTreeNode>()).ToList();
        if (allNodes.Count == 0) return;

        var current = FolderTreeView.SelectedItem as FolderTreeNode;
        var startIdx = current != null ? allNodes.IndexOf(current) : -1;

        for (int i = 1; i <= allNodes.Count; i++)
        {
            var candidate = allNodes[(startIdx + i) % allNodes.Count];
            if (candidate.Label.StartsWith(e.Text, StringComparison.OrdinalIgnoreCase))
            {
                SelectTreeViewNode(FolderTreeView, candidate);
                e.Handled = true;
                return;
            }
        }
    }

    private static IEnumerable<FolderTreeNode> GetVisibleTreeNodes(IEnumerable<FolderTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node.IsExpanded && node.Children.Count > 0)
                foreach (var child in GetVisibleTreeNodes(node.Children))
                    yield return child;
        }
    }

    private static bool SelectTreeViewNode(ItemsControl parent, FolderTreeNode target)
    {
        foreach (var item in parent.Items)
        {
            if (item is not FolderTreeNode node) continue;
            if (parent.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem container) continue;

            if (node == target)
            {
                container.IsSelected = true;
                container.BringIntoView();
                container.Focus();
                return true;
            }
            if (node.IsExpanded && SelectTreeViewNode(container, target))
                return true;
        }
        return false;
    }

    private void Commit()
    {
        if (FolderTreeView.SelectedItem is FolderTreeNode node && node.Folder != null)
        {
            SelectedFolder = node.Folder;
            _folderToAccount.TryGetValue(node.Folder, out var account);
            SelectedAccount = account;
            DialogResult = true;
        }
    }
}
