﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.Services
{
    public static class ClientService
    {
        private static NextcloudClient.NextcloudClient _client;

        public static async Task<NextcloudClient.NextcloudClient> GetClient()
        {
            if (_client != null)
            {
                return _client;
            }

            if (!string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.ServerAddress) &&
                !string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.Username))
            {
                var vault = new PasswordVault();

                IReadOnlyList<PasswordCredential> credentialList = null;
                try
                {
                    credentialList = vault.FindAllByResource(SettingsService.Default.Value.LocalSettings.ServerAddress);
                }
                catch
                {
                    // ignored
                }

                var credential = credentialList?.FirstOrDefault(item => item.UserName.Equals(SettingsService.Default.Value.LocalSettings.Username));

                if (credential == null)
                {
                    return null;
                }
                
                credential.RetrievePassword();

                try
                {
                    var response = await NextcloudClient.NextcloudClient.GetServerStatus(credential.Resource, SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors);
                    if (response == null)
                    {
                        await ShowServerAddressNotFoundMessage(credential.Resource);
                        return null;
                    }
                }
                catch
                {
                    await ShowServerAddressNotFoundMessage(credential.Resource);
                    return null;
                }

                _client = new NextcloudClient.NextcloudClient(
                    credential.Resource,
                    credential.UserName,
                    credential.Password
                ) {
                    IgnoreServerCertificateErrors =
                        SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors
                };
            }

            SettingsService.Default.Value.LocalSettings.PropertyChanged += async (sender, args) =>
            {
                if (_client != null && args.PropertyName == "IgnoreServerCertificateErrors")
                {
                    _client.IgnoreServerCertificateErrors =
                        SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors;
                }

                if (
                    string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.ServerAddress) ||
                    string.IsNullOrEmpty(SettingsService.Default.Value.LocalSettings.Username)
                    )
                {
                    _client = null;
                    return;
                }

                var vault = new PasswordVault();

                IReadOnlyList<PasswordCredential> credentialList = null;
                try
                {
                    credentialList = vault.FindAllByResource(SettingsService.Default.Value.LocalSettings.ServerAddress);
                }
                catch
                {
                    // ignored
                }

                var credential = credentialList?.FirstOrDefault(item => item.UserName.Equals(SettingsService.Default.Value.LocalSettings.Username));

                if (credential == null)
                {
                    _client = null;
                    return;
                }

                credential.RetrievePassword();

                try
                {
                    var response = await NextcloudClient.NextcloudClient.GetServerStatus(credential.Resource, SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors);
                    if (response == null)
                    {
                        _client = null;
                        await ShowServerAddressNotFoundMessage(credential.Resource);
                        return;
                    }
                }
                catch
                {
                    _client = null;
                    await ShowServerAddressNotFoundMessage(credential.Resource);
                    return;
                }

                _client = new NextcloudClient.NextcloudClient(
                    credential.Resource,
                    credential.UserName,
                    credential.Password
                ) {
                    IgnoreServerCertificateErrors =
                            SettingsService.Default.Value.LocalSettings.IgnoreServerCertificateErrors
                };
            };

            return _client;
        }

        private static async Task ShowServerAddressNotFoundMessage(string serverAddress)
        {
            var navigationService = PrismUnityApplication.Current.Container.Resolve<INavigationService>();
            var resourceLoader = PrismUnityApplication.Current.Container.Resolve<IResourceLoader>();
            var dialogService = PrismUnityApplication.Current.Container.Resolve<DialogService>();

            var dialog = new ContentDialog
            {
                Title = resourceLoader.GetString("AnErrorHasOccurred"),
                Content = new TextBlock
                {
                    Text = string.Format(resourceLoader.GetString("ServerWithGivenAddressIsNotReachable"), serverAddress),
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = resourceLoader.GetString("OK")
            };
            await dialogService.ShowAsync(dialog);
            SettingsService.Default.Value.Reset();
            navigationService.Navigate(PageToken.Login.ToString(), null);
        }

        public static void Reset()
        {
            _client = null;
        }
    }
}
