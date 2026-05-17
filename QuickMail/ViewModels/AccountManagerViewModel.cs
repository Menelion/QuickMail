using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickMail.Models;
using QuickMail.Services;

namespace QuickMail.ViewModels;

public partial class AccountManagerViewModel : AccountEditorViewModel
{
    private readonly IAccountService _accountService;
    private readonly ICredentialService _credentials;

    [ObservableProperty]
    private ObservableCollection<AccountModel> _accounts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private AccountModel? _selectedAccount;

    public bool IsEditing => SelectedAccount != null;

    public AccountManagerViewModel(IAccountService accountService, ICredentialService credentials, IImapService imap, IOAuthService oauth)
        : base(imap, oauth)
    {
        _accountService = accountService;
        _credentials = credentials;
        Accounts = new ObservableCollection<AccountModel>(accountService.LoadAccounts());
    }

    partial void OnSelectedAccountChanged(AccountModel? value)
    {
        if (value == null) return;
        AccountName = value.AccountName;
        DisplayName = value.DisplayName;
        Username = value.Username;
        AuthType = value.AuthType;
        Password = value.AuthType == AuthType.Password
            ? (_credentials.GetPassword(value.Id) ?? string.Empty)
            : string.Empty;
        ImapHost = value.ImapHost;
        ImapPort = value.ImapPort;
        ImapUseSsl = value.ImapUseSsl;
        ImapAcceptInvalidCert = value.ImapAcceptInvalidCert;
        SmtpHost = value.SmtpHost;
        SmtpPort = value.SmtpPort;
        SmtpUseSsl = value.SmtpUseSsl;
        SmtpAcceptInvalidCert = value.SmtpAcceptInvalidCert;
        StatusText = string.Empty;
    }

    public AddAccountViewModel CreateAddAccountViewModel() => new(ImapService, OAuthService);

    public void CommitNewAccount(AccountModel account, string password)
    {
        if (!string.IsNullOrEmpty(password))
            _credentials.SavePassword(account.Id, password);
        Accounts.Add(account);
        _accountService.SaveAccounts([.. Accounts]);
        SelectedAccount = account;
        StatusText = "Account added.";
    }

    [RelayCommand]
    private void SaveAccount()
    {
        if (SelectedAccount == null) return;

        SelectedAccount.AccountName = AccountName;
        SelectedAccount.DisplayName = DisplayName;
        SelectedAccount.Username = Username;
        SelectedAccount.AuthType = AuthType;
        SelectedAccount.ImapHost = ImapHost;
        SelectedAccount.ImapPort = ImapPort;
        SelectedAccount.ImapUseSsl = ImapUseSsl;
        SelectedAccount.ImapAcceptInvalidCert = ImapAcceptInvalidCert;
        SelectedAccount.SmtpHost = SmtpHost;
        SelectedAccount.SmtpPort = SmtpPort;
        SelectedAccount.SmtpUseSsl = SmtpUseSsl;
        SelectedAccount.SmtpAcceptInvalidCert = SmtpAcceptInvalidCert;

        if (AuthType == AuthType.Password && !string.IsNullOrEmpty(Password))
            _credentials.SavePassword(SelectedAccount.Id, Password);

        _accountService.SaveAccounts([.. Accounts]);
        StatusText = "Account saved.";

        // Force list item refresh
        var idx = Accounts.IndexOf(SelectedAccount);
        if (idx >= 0)
        {
            Accounts.RemoveAt(idx);
            Accounts.Insert(idx, SelectedAccount);
            SelectedAccount = Accounts[idx];
        }
    }

    [RelayCommand]
    private void DeleteAccount()
    {
        if (SelectedAccount == null) return;
        _credentials.DeletePassword(SelectedAccount.Id);
        Accounts.Remove(SelectedAccount);
        SelectedAccount = null;
        _accountService.SaveAccounts([.. Accounts]);
        StatusText = "Account deleted.";
    }

    [RelayCommand]
    private void SetDefault(AccountModel? account)
    {
        if (account == null) return;
        _accountService.SetDefaultAccount(account.Id);
        foreach (var a in Accounts)
            a.IsDefault = a.Id == account.Id;
        // Rebuild the collection so item templates re-evaluate IsDefault bindings.
        var selectedId = SelectedAccount?.Id;
        Accounts = new ObservableCollection<AccountModel>([.. Accounts]);
        SelectedAccount = selectedId.HasValue
            ? Accounts.FirstOrDefault(a => a.Id == selectedId.Value)
            : null;
        StatusText = $"{account.AccountLabel} set as default.";
    }
}
