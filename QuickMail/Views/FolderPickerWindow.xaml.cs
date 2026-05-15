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
    private MailFolderModel? _initialFolder;

    public MailFolderModel? SelectedFolder { get; private set; }
    public AccountModel? SelectedAccount { get; private set; }

    public FolderPickerWindow(IEnumerable<AccountModel> accounts, IReadOnlyDictionary<Guid, List<MailFolderModel>> cachedFolders, IEnumerable<MailFolderModel>? virtualFolders = null, string title = "Go to Folder", MailFolderModel? initialFolder = null)
    {
        _initialFolder = initialFolder;

        InitializeComponent();
        Title = title;

        var roots = new List<FolderTreeNode>();

        // "All Mail" group header with virtual sub-folder children at the top of the tree
        var virtualList = virtualFolders?.ToList();
        if (virtualList is { Count: > 0 })
        {
            var allMailGroup = new FolderTreeNode { IsHeader = true, Label = "All Mail", IsExpanded = true };
            foreach (var vf in virtualList)
                allMailGroup.Children.Add(new FolderTreeNode { Folder = vf, Label = vf.DisplayName });
            roots.Add(allMailGroup);
        }

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

        Loaded += (_, _) =>
        {
            if (_initialFolder == null)
            {
                FolderTreeView.Focus();
                return;
            }

            // Ensure ancestors of the target node are expanded so their
            // item containers get generated before we try to select.
            FindAndExpandPath(roots, _initialFolder);

            // Defer selection until after WPF has generated the new containers.
            Dispatcher.InvokeAsync(() =>
            {
                var node = FindNode(roots, _initialFolder);
                if (node != null)
                    SelectTreeViewNode(FolderTreeView, node);
                else
                    FolderTreeView.Focus();
            }, System.Windows.Threading.DispatcherPriority.Input);
        };
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

    // Finds a node whose Folder matches the target, searching all nodes regardless of expansion.
    private static FolderTreeNode? FindNode(IEnumerable<FolderTreeNode> nodes, MailFolderModel target)
    {
        foreach (var node in nodes)
        {
            if (node.Folder != null && FoldersMatch(node.Folder, target))
                return node;
            var found = FindNode(node.Children, target);
            if (found != null) return found;
        }
        return null;
    }

    // Expands all ancestor nodes along the path to the target so their
    // children's item containers are generated before SelectTreeViewNode runs.
    private static bool FindAndExpandPath(IList<FolderTreeNode> nodes, MailFolderModel target)
    {
        foreach (var node in nodes)
        {
            if (node.Folder != null && FoldersMatch(node.Folder, target))
                return true;
            if (FindAndExpandPath(node.Children, target))
            {
                node.IsExpanded = true;
                return true;
            }
        }
        return false;
    }

    private static bool FoldersMatch(MailFolderModel a, MailFolderModel b) =>
        a.FullName.Equals(b.FullName, StringComparison.OrdinalIgnoreCase) &&
        (a.AccountId == b.AccountId || a.AccountId == Guid.Empty || b.AccountId == Guid.Empty);

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
