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
using System.IO;
using System.Xml.Schema;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using Microsoft.Expression.Interactivity.Core;


//Twitter
//using System.Windows.Forms;
//using System.Windows.Forms.Integration;
using System.Net;
using System.Web;
using Twitterizer;
using mshtml;
using System.ServiceModel;
using iBoard.Classes.Data;
using iBoard.Classes.Data.Moodle;
using iBoard.Classes.Timeline;

namespace iBoard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		/// <summary>
		/// To old the current account to edit
		/// </summary>
		private int _currentAccountToEdit = -1;

        // instanciate the timelines
        private MoodleTimeline _moodleTimeline = new MoodleTimeline();
        private iBoard.Classes.Data.Twitter.TwitterTimeline _twitterTimeline = new iBoard.Classes.Data.Twitter.TwitterTimeline();
        private iBoard.Classes.Data.Email.EmailTimeline _emailTimeline = new Classes.Data.Email.EmailTimeline();
        
        //Twitter
        string _consumerKey = Properties.Settings.Default.TwitterConsumerKey;
        string _consumerSecret = Properties.Settings.Default.TwitterConsumerSecret;


        public MainWindow(){
            InitializeComponent();
        }

        private void CloseUserManagementOverlayBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, LoginState.Name, true);
        }

        private void addAccountBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, NewUserState.Name, true);
        }

        private void configBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            if(MessageBox.Show(Properties.Resources.ThisOperationWillReplaceTheConfigurationFile, Properties.Resources.DataLossWarning, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) {
                // Import the configuration file
                OpenFileDialog openDialog = new OpenFileDialog();
                openDialog.Filter = Properties.Resources.ConfigurationFile + " |*.xml";
                //openDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                openDialog.Title = Properties.Resources.SelectTheConfigurationFile;
                if(openDialog.ShowDialog() == true) {
                    if(ConfigurationManager.LoadExternalXmlFile(openDialog.FileName)) {
                        MessageBox.Show(Properties.Resources.ConfigurationLoadedSuccessfuly, Properties.Resources.ConfigurationLoaded, MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
                    } else {
                        MessageBox.Show(Properties.Resources.ErrorWhileLoadingTheConfiguration, Properties.Resources.ConfigurationRestoreError, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    }
                }
            }
        }

        /// <summary>
        /// On application start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginWindow_Initialized(object sender, System.EventArgs e) {
            try {
                // if we can authenticate the default user, skip to the application directly
                if(ConfigurationManager.AuthenticateUser()) {
                    GetUnifiedAccounts(this, null);
                    ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, MainAppState.Name, true);
                    txtWelcome.Content = "Welcome, " + ConfigurationManager.AuthenticatedUser.Username;
                } else {
                    ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, LoginState.Name, true);
                    Keyboard.Focus(txtLoginUsername);
                }
            } catch(Exception exception) {
                MessageBox.Show(String.Format(Properties.Resources.ErrorOccurred, exception.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Verify if the username of the new user form is valid
        /// </summary>
        /// <returns>Boolean true if it is, false otherwise</returns>
        private Boolean _isNewUsernameValid() {
            lblNewUserUsernameError.Content = "";
            // username cannot be empty
            if(txtNewUserUsername.Text.Equals("")) {
                lblNewUserUsernameError.Content = "The username cannot be empty";
                return false;
            }

            // the username cannot be duplicated
            LinkedList<User> users = ConfigurationManager.GetUsers();
            if(users != null) {
                foreach(User user in users) {
                    if(txtNewUserUsername.Text.Equals(user.Username)) {
                        lblNewUserUsernameError.Content = user.Username + "'s user account already exists";
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Verify if the password of the new user form is valid
        /// </summary>
        /// <returns>Boolean true if it is, false otherwise</returns>
        private Boolean _isNewUserPasswordValid() {
            lblNewUserPasswordError.Content = "";
            // password cannot be empty
            if(txtNewUserPassword.Password.Equals("")) {
                lblNewUserPasswordError.Content = "The password cannot be empty";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Verify if the confirmation password of the new user form is valid
        /// </summary>
        /// <returns>Boolean true if it is, false otherwise</returns>
        private Boolean _isNewUserConfirmationPasswordValid() {
            lblNewUserPasswordConfirmationError.Content = "";
            // password cannot be empty
            if(!txtNewUserPassword.Password.Equals(txtNewUserPasswordConfirmation.Password)) {
                lblNewUserPasswordConfirmationError.Content = "The passwords don't match";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reset the new user form fields
        /// </summary>
        private void _resetAddNewUserForm() {
            lblNewUserUsernameError.Content = "";
            lblNewUserPasswordError.Content = "";
            lblNewUserPasswordConfirmationError.Content = "";

            txtNewUserUsername.Text = "";
            txtNewUserPassword.Password = "";
            txtNewUserPasswordConfirmation.Password = "";
        }

        private void txtNewUserUsername_LostFocus(object sender, System.Windows.RoutedEventArgs e) {
            this._isNewUsernameValid();
        }

        private void txtNewUserPassword_LostFocus(object sender, System.Windows.RoutedEventArgs e) {
            this._isNewUserPasswordValid();
        }

        private void txtNewUserPasswordConfirmation_LostFocus(object sender, System.Windows.RoutedEventArgs e) {
            this._isNewUserConfirmationPasswordValid();
        }

        private void btnCancelAddNewUser_Click(object sender, System.Windows.RoutedEventArgs e) {
            this._resetAddNewUserForm();
            LoginGrid.IsEnabled = true;
            ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, LoginState.Name, true);
        }

        private void btnAddNewUser_Click(object sender, System.Windows.RoutedEventArgs e) {
            if(!this._isNewUsernameValid())
                return;
            if(!this._isNewUserPasswordValid())
                return;
            if(!this._isNewUserConfirmationPasswordValid())
                return;

            // validations done; let's create a new user account
            User user = new User(txtNewUserUsername.Text, txtNewUserPassword.Password);
            if(ConfigurationManager.AddUser(user)) {
                txtLoginUsername.Text = user.Username;
                pbLoginPassword.Password = "";

                this.btnCancelAddNewUser_Click(sender, e);
            } else {
                MessageBox.Show("Unable to add the user to the system. Please, try again");
            }
        }

        // authenticate the user
        private void btnLogin_Click(object sender, System.Windows.RoutedEventArgs e) {
            if(ConfigurationManager.AuthenticateUser(txtLoginUsername.Text, pbLoginPassword.Password, (Boolean) cbRememberLogin.IsChecked)) {
                GetUnifiedAccounts(this, null);
                ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, MainAppState.Name, true);
                txtWelcome.Content = "Welcome, " + txtLoginUsername.Text;
                txtLoginUsername.Text = "";
                pbLoginPassword.Password = "";
            } else {
                MessageBox.Show("Authentication failed for user " + txtLoginUsername.Text, "Authentication failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Terminate the user session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _logout(object sender, System.Windows.RoutedEventArgs e) {
            if(MessageBox.Show(Properties.Resources.ThisWillTerminateYourSession, Properties.Resources.Logout, MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) == MessageBoxResult.Yes) {
                ConfigurationManager.DeAuthenticatedUser();
                ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, LoginState.Name, true);
                userControlPlace.Content = "";
            }
        }

        private void ShowMainApp(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, MainAppState.Name, true);
        }

        private void btnBannerEditAccounts_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountsManageOperationSelectionState.Name, true);
        }

        private void AccountsAddBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountsManageOperationSelectionState.Name, true);
        }

        private void AccountsAddAccount_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountsAddState.Name, true);
        }


        private void TwitterButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AddTwitterUser1.Name, true);
            EditOrCreateTwitterAccount(this, null);
        }

        

        private void TwitterAccountsElement_Selected(object sender, System.Windows.RoutedEventArgs e){

            TreeViewItem item = e.OriginalSource as TreeViewItem;
            //If don't have childs is an account or isn't configured
			//MessageBox.Show(item.DataContext.GetType().Equals(typeof(Account)).ToString());
            if (item.DataContext != null && item.DataContext.GetType().Equals(typeof(iBoard.Classes.Data.Twitter.TwitterAccount))){
                Account account= item.Header as Account;
                userControlPlace.Content = "";
                //userControlPlace.Content = new TwitterControl(account.Name, account.ID);
                TwitterControl test = new TwitterControl(account.Name, account.ID);

                //MessageBox.Show(userControlPlace.Content.GetType().ToString() + "1");
                userControlPlace.Content = test;
                if (userControlPlace.Content.GetType().Equals(typeof(TwitterControl))){
                    //MessageBox.Show(userControlPlace.Content.GetType().ToString() + "2");
                    //hideLoading();
                }
            }
        }

        private void EmailAccountsElement_Selected(object sender, System.Windows.RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            //If don't have childs is an account
            if (item.DataContext != null && item.DataContext.GetType().Equals(typeof(Account)))
            {
                Account account = item.Header as Account;
                userControlPlace.Content = "";
                userControlPlace.Content = new MailControl(account.Name, account.ID);
            }
        }

        private void MoodleAccountsElement_Selected(object sender, System.Windows.RoutedEventArgs e){
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            //If don't have childs is an account
            if (item.DataContext != null && item.DataContext.GetType().Equals(typeof(MoodleAccount))){
				MoodleAccount account = item.Header as MoodleAccount;
                userControlPlace.Content = "";
                userControlPlace.Content = new MoodleControl(account.ID);
            }
        }

        private void ShowNewMoodleAccountForm(object sender, System.Windows.RoutedEventArgs e){
            ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountManageMoodleState.Name, true);
            EditOrCreateMoodleAccount(this, null);
        }
        
        public void CancelMoodleAccountEdit(){
            this.ShowMainApp(this, null);
            MoodleAccountControl.Content = "";
        }
		
		public void CancelTwitterAccountEdit(){
            this.ShowMainApp(this, null);
            TwitterAccountControl.Content = "";
        }

        private void MoodleAccountsEditSelected(object sender, System.Windows.RoutedEventArgs e){
        	TreeViewItem item = e.OriginalSource as TreeViewItem;
            //If don't have childs is an account
            
            if (item.DataContext != null && item.DataContext.GetType().Equals(typeof(MoodleAccount))){
				MoodleAccount account= item.Header as MoodleAccount;
                this._currentAccountToEdit = account.ID;
                ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountManageMoodleState.Name, true);
                EditOrCreateMoodleAccount(this, null);
            }
        }

        private void ManageAccounts(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AccountsEditState.Name, true);
        }

        private void EditOrCreateMoodleAccount(object sender, EventArgs e) {
            MoodleAccountControl.Content = "";
            lblAccountAddMoodleLoading.Visibility = System.Windows.Visibility.Visible;
            MoodleAccountControl.Content = new Controls.MoodleAccountManage(this._currentAccountToEdit);
            this._currentAccountToEdit = -1;
        }

        private void EditOrCreateTwitterAccount(object sender, EventArgs e) {
            TwitterAccountControl.Content = "";
            lblAccountAddTwitterLoading.Visibility = System.Windows.Visibility.Visible;
            TwitterAccountControl.Content = new Controls.TwitterAccountManage(this._currentAccountToEdit);
            this._currentAccountToEdit = -1;
        }
        private void GetUnifiedAccounts(object sender, System.Windows.RoutedEventArgs e){
            lblControlLoading.Visibility = System.Windows.Visibility.Visible;
            userControlPlace.Content = "";
            userControlPlace.Content = new Controls.TimelineViewer();
        }

        private void TwitterAccountsElementSelected(object sender, System.Windows.RoutedEventArgs e)
        {
        	 TreeViewItem item = e.OriginalSource as TreeViewItem;
            //If don't have childs is an account or isn't configured
			//MessageBox.Show(item.DataContext.GetType().Equals(typeof(Account)).ToString());			
			 if (item.DataContext != null && item.DataContext.GetType().Equals(typeof(iBoard.Classes.Data.Twitter.TwitterAccount))){
                Account account= item.Header as Account;
                this._currentAccountToEdit = account.ID;
                ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, AddTwitterUser1.Name, true);
                EditOrCreateTwitterAccount(this, null);
            }
        }

        private void ValidateMailAccount_ButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            //txtMailAccountName.Text = "";
            //txtMailUsername.Text = "";
            //smtpserver.Text = "";
            //popserver.Text = "";
            //portpop.Text = "";
            //portsmtp.Text = "";
            //txtMailPassword.Password = "";

            if (txtMailAccountName.Text == "" || txtMailUsername.Text=="" || smtpserver.Text=="" || popserver.Text=="" || portsmtp.Text=="" || portpop.Text=="" || txtMailPassword.Password.Equals(""))
            {
                    lblMailUsernameError.Content = "Please fill all fields";
                    return;      

            }

            else
            {
                Account emailAccount = new Account();
                emailAccount.Type = Account.EMAILTYPE;
                emailAccount.Name = txtMailAccountName.Text;
                emailAccount.Enabled = cbEnableAccount.IsChecked.Value;
                emailAccount.addOption("smtpserver", smtpserver.Text);
                emailAccount.addOption("smtpport", portsmtp.Text);
                emailAccount.addOption("popserver", popserver.Text);
                emailAccount.addOption("popport", portpop.Text);
                emailAccount.addOption("username", txtMailAccountName.Text);
                emailAccount.addOption("password", ConfigurationManager.AuthenticatedUser.Encrypt(txtMailPassword.Password));
                emailAccount.addOption("email", txtMailUsername.Text);
                emailAccount.addOption("ssl", cbSsl.IsChecked.Value.ToString());

                ConfigurationManager.AddUserAccount(emailAccount);
                ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, "MainAppState", true);

            }
            
            
            
            //MessageBox.Show("Aqui tens de fazer a magia acontecer,... linha 440");
			// Dá uma vista deolhos de como eu fiz nos dois botões que tenho do twitter
			
			//btnAddTwitterUser_Click - este faço as validações, basicas, se os campos estao ou não preenchidos
			// btnAddTwitterPin_Click aqui movo o ecrã para o inicio
			//Apenas precisas de fazer tudo num
        }

        private void AccountsAddMailBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, "AccountsAddState", true);
        }

        private void MailButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
        	ExtendedVisualStateManager.GoToElementState(this.BaseGrid as FrameworkElement, "AccountAddMail", true);
        }
    }
	
    

}
