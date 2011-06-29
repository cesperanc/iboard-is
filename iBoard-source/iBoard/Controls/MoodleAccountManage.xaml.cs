using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using iBoard.Classes.Configuration;
using Microsoft.Expression.Interactivity.Core;
using System.ServiceModel;
using System.ComponentModel;
using iBoard.Classes.Data;

namespace iBoard.Controls {
    /// <summary>
    /// Interaction logic for MoodleAccountManage.xaml
    /// </summary>
    public partial class MoodleAccountManage : UserControl {
		private int _accountId = -1;
        private MainWindow _parentWindow = null;

        public MoodleAccountManage():this(-1) {}
		
        public MoodleAccountManage(int accountId) {
			this._accountId = accountId;
            InitializeComponent();
            this._init();
        }

        /// <summary>
        /// Init the form
        /// </summary>
        private void _init() {
            // Handler for loading complete
            this.Loaded += (se, args) => {
                try {
                    this._parentWindow = (MainWindow) Window.GetWindow(this);
                    if(this._parentWindow != null) {
                        this._parentWindow.lblAccountAddMoodleLoading.Visibility = System.Windows.Visibility.Hidden;
						btnMoodleAccountFormCancel.Visibility = System.Windows.Visibility.Visible;
						btnMoodleAccountFailed.Visibility = System.Windows.Visibility.Visible;
						btnMoodleAccountAdded.Visibility = System.Windows.Visibility.Visible;
                    }
                } catch(Exception) {
                    // this shouldn't happen
                }
            };

            // Load the account to the form
            lblFormTitle.Content = Properties.Resources.UpdateMoodleAccount;
            txtMoodleUrl.Text = Properties.Settings.Default.DefaultMoodleServiceUrl;
            txtMoodleUsername.Text = "";
            txtMoodlePassword.Password = "";
            lblMoodleUrl.Content = "";
            txtMoodleAccountName.Text = "";
            cbMoodleAccountEnabled.IsChecked = true;
            btnMoodleAccountFormDelete.Visibility = System.Windows.Visibility.Hidden;

            if(this._accountId > -1 && ConfigurationManager.UserAccountExists(this._accountId)) {
                Account account = ConfigurationManager.GetUserAccount(this._accountId);
                if(account != null) {
                    lblFormTitle.Content = Properties.Resources.UpdateMoodleAccount;
                    txtMoodleUrl.Text = account.getOption(Properties.Settings.Default.MoodleServiceUrlSettingName).Replace(Properties.Settings.Default.MoodleModuleUrl, "");
                    txtMoodleUsername.Text = account.getOption(Properties.Settings.Default.MoodleServiceUsernameSettingName);
                    txtMoodlePassword.Password = "";//account.getOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName);
                    txtMoodleAccountName.Text = account.Name;
                    cbMoodleAccountEnabled.IsChecked = account.Enabled;
                    btnMoodleAccountFormDelete.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Event for saving a moodle account
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveMoodleAccount(object sender, System.Windows.RoutedEventArgs e) {
            lblValidating.Visibility = System.Windows.Visibility.Visible;
            MoodleAccountForm.IsEnabled = false;

            BackgroundWorker bgWorker = new BackgroundWorker();
            String serverUrl = txtMoodleUrl.Text + Properties.Settings.Default.MoodleModuleUrl;
            String username = txtMoodleUsername.Text;
            String password = txtMoodlePassword.Password;
            String accountName = txtMoodleAccountName.Text;
            Boolean enabled = cbMoodleAccountEnabled.IsChecked.Value;

            bgWorker.DoWork += (wsender, we) => {
                DoWorkEventArgs args = we as DoWorkEventArgs;
                BackgroundWorker worker = wsender as BackgroundWorker;

                try {
                    Account account = null;
                    UedWs.UEDWSPortTypeClient client = new UedWs.UEDWSPortTypeClient();
                    client.Endpoint.Address = new EndpointAddress(serverUrl);

                    UedWs.UedCredentials credentials = new UedWs.UedCredentials();
                    credentials.username = username;
                    
                    credentials.autorizationKey = "";
                    // if it's an existing account and the password is empty, use the previous authorization key
                    if(this._accountId > -1 && password.Equals("") && ConfigurationManager.UserAccountExists(this._accountId)) {
                         account = ConfigurationManager.GetUserAccount(this._accountId);
                         if(account != null) {
                             credentials.autorizationKey = account.getOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName);
                         }
                    } else {
                        // else, retrieve a new authorization key
                        credentials.autorizationKey = client.getAuthorizationKey(credentials.username, password, "iBoard");
                    }

                    // if the credentials are valid
                    if(!credentials.autorizationKey.Equals("") && client.validateCredentials(credentials)) {
                        if(account != null) {
                            account.Name = accountName;
                            account.Type = Account.MOODLETYPE;
                            account.Enabled = enabled;
                            account.setOption(Properties.Settings.Default.MoodleServiceUrlSettingName, serverUrl);
                            account.setOption(Properties.Settings.Default.MoodleServiceUsernameSettingName, credentials.username);
                            account.setOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName, credentials.autorizationKey);
                            ConfigurationManager.UpdateUserAccount(account);
                        } else {
                            account = new Account(accountName, Account.MOODLETYPE, enabled);
                            account.addOption(Properties.Settings.Default.MoodleServiceUrlSettingName, serverUrl);
                            account.addOption(Properties.Settings.Default.MoodleServiceUsernameSettingName, credentials.username);
                            account.addOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName, credentials.autorizationKey);
                            ConfigurationManager.AddUserAccount(account);
                        }
                        UedWs.UedUser user = client.getMyUserPublicProfile(credentials);
                        
                        args.Result = user;
                    } else {
                        throw new Exception();
                    }
                } catch(Exception ex) {
                    args.Result = ex.Message;
                    args.Cancel = true;
                }
            };

            bgWorker.RunWorkerCompleted += (wsender, we) => {
                RunWorkerCompletedEventArgs args = we as RunWorkerCompletedEventArgs;
                if(args.Cancelled || args.Error != null) {
                    ExtendedVisualStateManager.GoToElementState(AddMoodleAccountGrid as FrameworkElement, MoodleAccountValidationFailedState.Name, true);
                }else{
                    lblMoodleAccountSaved.Content = "The moodle account " + accountName + " was saved:";

                    UedWs.UedUser user = args.Result as UedWs.UedUser;
                    if(user != null) {
                        if(user.imageUrl != null && !user.imageUrl.Equals("")) {
                            imgNewMoodleAccountAvatar.Source = new BitmapImage(new Uri(user.imageUrl));
                        }
                        if(user.fullName != null) {
                            lblNewMoodleAccountName.Content = user.fullName;
                        }
                    }

                    ConfigurationManager.ConfigurationUpdated();
                    ExtendedVisualStateManager.GoToElementState(AddMoodleAccountGrid as FrameworkElement, MoodleAccountValidationOkState.Name, true);
                }
                lblValidating.Visibility = System.Windows.Visibility.Hidden;
                MoodleAccountForm.IsEnabled = true;
            };

            bgWorker.RunWorkerAsync();
        }
		
		/// <summary>
		/// Return the form state
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void ReturnToMoodleForm(object sender, System.Windows.RoutedEventArgs e){
            ExtendedVisualStateManager.GoToElementState(AddMoodleAccountGrid as FrameworkElement, MoodleAccountFormState.Name, true);
        }
		
		/// <summary>
		/// Validate the Moodle URL entered on the form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void NewMoodleAccountUrlCheck(object sender, System.Windows.RoutedEventArgs e){
        	try{
        		UedWs.UEDWSPortTypeClient client = new UedWs.UEDWSPortTypeClient();
                client.Endpoint.Address = new EndpointAddress(txtMoodleUrl.Text + Properties.Settings.Default.MoodleModuleUrl);
				lblMoodleUrl.Content = "Connected to a moodle service with the version "+client.getVersion();
			}catch(Exception ex){
				lblMoodleUrl.Content = String.Format(Properties.Resources.ErrorOccurred, ex.Message);
			}
        }
		
		/// <summary>
		/// Event to cancel the form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void CancelMoodleAccountEdit(object sender, System.Windows.RoutedEventArgs e) {
        	if(this._parentWindow!=null){
                this._parentWindow.CancelMoodleAccountEdit();
			}
        }
		
		/// <summary>
		/// Event to delete an account
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void DeleteMoodleAccount(object sender, System.Windows.RoutedEventArgs e){
            if(this._accountId > -1 && ConfigurationManager.UserAccountExists(this._accountId)) {
			    if(MessageBox.Show(Properties.Resources.AreYouSureThatYouWantToDeleteTheAccount, Properties.Resources.AccountElimination, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) {
				
					if(ConfigurationManager.DeleteUserAccount(this._accountId)){
						MessageBox.Show(Properties.Resources.TheAccountWasDeletedAsRequested, Properties.Resources.AccountDeleted, MessageBoxButton.OK, MessageBoxImage.Information);
						CancelMoodleAccountEdit(this, null);
						return;
					}
				    MessageBox.Show(Properties.Resources.UnableToDeleteTheAccount, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			    }
            }  
        }
    }
}
