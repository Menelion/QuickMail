using System.Threading.Tasks;
using System.Windows;
using QuickMail.Services;
using QuickMail.ViewModels;
using QuickMail.Views;

namespace QuickMail;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // /debug enables verbose debug logging to the log file.
        if (e.Args.Contains("/debug", StringComparer.OrdinalIgnoreCase))
        {
            LogService.DebugMode = true;
            LogService.Log("Debug mode enabled.");
        }

        // Install global exception handlers BEFORE anything else so an exception
        // in startup wiring or any background task is captured in the log instead
        // of disappearing with the process. (review §1.2)
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            var accountService    = new AccountService();
            var credentialService = new CredentialService();
            var oauthService      = new OAuthService();
            var configService     = new ConfigService();
            var imapService       = new ImapService(oauthService, configService);
            var smtpService       = new SmtpService(oauthService);

            var localStore = new LocalStoreService();
            localStore.Initialize();

            var contactService = new ContactService();
            var syncService = new SyncService(imapService, localStore, configService);

            Views.AccessibilityHelper.Configure(configService.Load());

            var commandRegistry = new CommandRegistry();
            commandRegistry.ApplyUserOverrides(configService.Load().CustomHotkeys);

            var viewService = new ViewService();

            var mainVm = new MainViewModel(
                imapService, accountService, credentialService, localStore, oauthService, syncService, configService, commandRegistry, viewService);
            mainVm.LoadAccountList();

            var mainWindow = new MainWindow(mainVm, smtpService, accountService, credentialService, imapService, oauthService, commandRegistry, contactService, configService, localStore, viewService);
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // Log the exception chain before WER kills the process so the cause
            // survives in %APPDATA%\QuickMail\quickmail.log.
            for (var cur = ex; cur != null; cur = cur.InnerException)
                LogService.Log("Startup", cur);
            throw;
        }
    }

    private static void OnDispatcherUnhandledException(
        object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        for (var cur = e.Exception; cur != null; cur = cur.InnerException)
            LogService.Log("Dispatcher", cur);

        // Keep the process alive so the user isn't left staring at a vanished window.
        // The log captures the cause; the next user action will either succeed or
        // fault again, by which point we want it diagnosed rather than swallowed.
        try
        {
            MessageBox.Show(
                $"An unexpected error occurred and was logged.\n\n{e.Exception.GetType().Name}: {e.Exception.Message}",
                "QuickMail",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* MessageBox itself can fail in extreme cases — swallow. */ }

        e.Handled = true;
    }

    private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Non-recoverable: the runtime is tearing down. Just log every frame we can.
        if (e.ExceptionObject is Exception ex)
            for (var cur = ex; cur != null; cur = cur.InnerException)
                LogService.Log("AppDomain", cur);
        else
            LogService.Log($"AppDomain: non-Exception unhandled object: {e.ExceptionObject}");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        for (var cur = e.Exception as Exception; cur != null; cur = cur.InnerException)
            LogService.Log("UnobservedTask", cur);

        // Mark as observed so the GC finaliser doesn't crash the process on .NET <4.5
        // semantics or on a future hardening change.
        e.SetObserved();
    }
}
