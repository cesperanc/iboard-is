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
using System.Net;

using Twitterizer;
using Twitterizer.Core;
using Twitterizer.Data;
using Twitterizer.Entities;
using Twitterizer.Streaming;
using mshtml;


namespace iBoard.Controls {
	/// <summary>
	/// Interaction logic for TwitterAccountManage.xaml
	/// </summary>
	 public partial class TwitterAccountManage : UserControl {
	
		private int _accountId = -1;
        private MainWindow _parentWindow = null;
		
		
        

        //Twitter
        string _consumerKey = Properties.Settings.Default.TwitterConsumerKey;
        string _consumerSecret = Properties.Settings.Default.TwitterConsumerSecret;
        string accessTokenToken;
        string accessTokenSecret;

        OAuthTokenResponse _requestToken;
		

        public TwitterAccountManage():this(-1) {}
		
        public TwitterAccountManage(int accountId) {
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
                        this._parentWindow.lblAccountAddTwitterLoading.Visibility = System.Windows.Visibility.Hidden;
                    }
                } catch(Exception) {
                    // this shouldn't happen
                }
            };

            // Load the account to the form
            txtTwitterUsername.Text = "";
            txtTwitterPassword.Password = "";
            txtTwitterAccountName.Text = "";
            cbTwitterAccountEnabled.IsChecked = true;
            btnDeleteAddTwitter.Visibility = System.Windows.Visibility.Hidden;
			btnEditTwitterUser.Visibility = System.Windows.Visibility.Hidden;

            if(this._accountId > -1 && ConfigurationManager.UserAccountExists(this._accountId)) {
                Account account = ConfigurationManager.GetUserAccount(this._accountId);
                if(account != null) {
                    lblTwitterPasswordError.Content = "You only can change the account name and account Status, if need new pin, please remove account";
                    txtTwitterUsername.Text = account.getOption("username");
                    txtTwitterPassword.Password = "";
                    txtTwitterAccountName.Text = account.Name;
                    cbTwitterAccountEnabled.IsChecked = account.Enabled;
                    btnDeleteAddTwitter.Visibility = System.Windows.Visibility.Visible;
					btnEditTwitterUser.Visibility = System.Windows.Visibility.Visible;
                    txtTwitterUsername.IsEnabled = false;
                    txtTwitterPassword.IsEnabled = false;
                }
            }
        }
		
		private void btnBackAddTwitter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			if(this._parentWindow!=null){
                this._parentWindow.CancelTwitterAccountEdit();
			}
        }

        private void btnCancelAddTwitterPin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			if(this._parentWindow!=null){
                this._parentWindow.CancelTwitterAccountEdit();
			}
        }

        private void btnAddTwitterUser_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	lblTwitterUsernameError.Content = "";
			lblTwitterPasswordError.Content = "";
			if(txtTwitterUsername.Text =="" )
			{
                lblTwitterUsernameError.Content = "The username cannot be empty";
				return;
			}
            if (txtTwitterUsername.Text.Contains("@"))
            {

                lblTwitterUsernameError.Content = "You must introduce your username, not e-mail";
                return;
            }
            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.TWITTERTYPE);
            foreach (Account account in accounts)
            {
                if (account.getOption("username") == (txtTwitterUsername.Text))
                {
                    lblTwitterUsernameError.Content = "The username already configured for the authenticaded user";
                    return;
                }
            }
			if(txtTwitterPassword.Password.Equals("")) 
			{
                lblTwitterPasswordError.Content = "The password cannot be empty";
				return;
			}
            if (txtTwitterAccountName.Text== "")
            {

                txtTwitterAccountName.Text = txtTwitterUsername.Text;
            }
			this.Cursor = Cursors.Wait;
            getTwitterPin();
			
        }

        private void getTwitterPin()
        {
            // Obtain a request token
            _requestToken = OAuthUtility.GetRequestToken(_consumerKey, _consumerSecret, "oob", new WebProxy());

            // Direct or instruct the user to the following address:
            Uri authorizationUri = OAuthUtility.BuildAuthorizationUri(_requestToken.Token);

            browser.Dispose();
            browser = new WebBrowser();
            //browser.Width = (double)300;
            
            browser.LoadCompleted += new LoadCompletedEventHandler(browser_LoadCompleted);
            TwitterPinOverlay.Children.Add(browser);

			browser.Navigate(authorizationUri);
            
            //For debug uncoment
            browser.Visibility = System.Windows.Visibility.Hidden;
			//ExtendedVisualStateManager.GoToElementState(this.AccountAddTwitterOverlay as FrameworkElement, AddTwitterUser2.Name, true);
        }

		
        private void browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            HTMLDocument html = (mshtml.HTMLDocument)browser.Document;

            if (e.Uri.Query.Length == 0)
            {
                try
                {
                    txtTwitterOauthPin.Text = html.getElementById("oauth_pin").innerText.ToString();
                    txtTwitterOauthPin.Text = txtTwitterOauthPin.Text.Replace(" ", "");
					ExtendedVisualStateManager.GoToElementState(this.AccountAddTwitterOverlay as FrameworkElement, AddTwitterUser2.Name, true);
					this.Cursor = Cursors.Arrow;
                }
                catch (Exception)
                {
                    //MessageBox.Show(e1.Message + "Não foi possivel receber o pin, Verifique os seus dados");
					this.Cursor = Cursors.Arrow;
					 lblTwitterPasswordError.Content = "Wrong password";
                }
            }
            else
            {
                try
                {
                    html.getElementById("username_or_email").setAttribute("value", txtTwitterUsername.Text.ToString());
                    html.getElementById("session[password]").setAttribute("value", txtTwitterPassword.Password.ToString());
                    html.getElementById("allow").click();

                }
                catch (Exception e2)
                {
                    MessageBox.Show(e2.Message + "Não foi possivel preencher o formulário");
                    MessageBox.Show("É possivel que já exista um user autenticado no twitter. Não foi possivel descobrir como fazer logout");
                    if (this._parentWindow != null)
                    {
                        this._parentWindow.CancelTwitterAccountEdit();
                    }
                }

            }
        }

        private void btnAddTwitterPin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	try
			{
				OAuthTokenResponse authToken = OAuthUtility.GetAccessToken(_consumerKey, _consumerSecret, _requestToken.Token, txtTwitterOauthPin.Text);

                accessTokenToken = authToken.Token;
				accessTokenSecret = authToken.TokenSecret;
			}
			catch(Exception e1)
			{
				MessageBox.Show(e1.Message + "Can't build access token");
			}

			try
            {
                if(ConfigurationManager.AuthenticateUser()) {
                    //MessageBox.Show("Utilizador autenticado");
                    if (_accountId > -1 && ConfigurationManager.UserAccountExists(this._accountId))
                    {
                        Account account = ConfigurationManager.GetUserAccount(this._accountId);
                        account.Name = txtTwitterAccountName.Text;
                        account.Enabled = cbTwitterAccountEnabled.IsChecked.Value;
                        
                    }
                    else
                    {
                        Account twitterAccount = new Account();
                        twitterAccount.Type = Account.TWITTERTYPE;
                        twitterAccount.Name = txtTwitterAccountName.Text;
                        twitterAccount.Enabled = cbTwitterAccountEnabled.IsChecked.Value;
                        twitterAccount.addOption("username", txtTwitterUsername.Text);
                        twitterAccount.addOption("accessTokenToken", accessTokenToken);
                        twitterAccount.addOption("accessTokenSecret", ConfigurationManager.AuthenticatedUser.Encrypt(accessTokenSecret));

                        ConfigurationManager.AddUserAccount(twitterAccount);


                        MessageBox.Show("Account added with sucess", "Information");
                    }
					
					if(this._parentWindow!=null){
						this._parentWindow.CancelTwitterAccountEdit();
					}

                }
                else
                {
                    MessageBox.Show("No user logged in");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("User Can not be added", "Warning");
            }
        }
		
		
        private void btnDeleteAddTwitter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			
        	if(MessageBox.Show(Properties.Resources.AreYouSureThatYouWantToDeleteTheAccount, Properties.Resources.AccountElimination, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) {
				if(this._accountId > -1 && ConfigurationManager.UserAccountExists(this._accountId)) {
					if(ConfigurationManager.DeleteUserAccount(this._accountId)){
						MessageBox.Show(Properties.Resources.TheAccountWasDeletedAsRequested, Properties.Resources.AccountDeleted, MessageBoxButton.OK, MessageBoxImage.Information);
						
						if(this._parentWindow!=null){
							this._parentWindow.CancelTwitterAccountEdit();
						}
						return;
					}
				}
				MessageBox.Show(Properties.Resources.UnableToDeleteTheAccount, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
        }

        private void btnEditTwitterUser_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	
            if (txtTwitterAccountName.Text== "")
            {
                txtTwitterAccountName.Text = txtTwitterUsername.Text;
            }
			
            Account account = ConfigurationManager.GetUserAccount(this._accountId);
            account.Name = txtTwitterAccountName.Text;
            account.Enabled = cbTwitterAccountEnabled.IsChecked.Value;
			ConfigurationManager.UpdateUserAccount(account);
			MessageBox.Show("User updated");
			if(this._parentWindow!=null){
				this._parentWindow.CancelTwitterAccountEdit();
			}
        }
	}
}