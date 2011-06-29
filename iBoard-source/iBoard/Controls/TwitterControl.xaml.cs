using System;
using System.Collections.Generic;
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
using System.Windows.Media.Animation;
using Microsoft.Expression.Interactivity.Core;
using System.Xml.Schema;

using System.Collections;
using System.Collections.Specialized;
using Twitterizer;
using iBoard.Classes.Configuration;
using System.Net;

using System.CodeDom.Compiler;
using iBoard.Classes.Data;

using iBoard.Classes.Timeline;


namespace iBoard
{
	/// <summary>
	/// Interaction logic for TwitterControl.xaml
	/// </summary>
	public partial class TwitterControl : UserControl
	{
        OAuthTokens _authToken;
        int _accountId;
        
        public TwitterControl(string accountName, int accountId)
		{
            /*Label myTextBlock = (Label)this.FindName("txtLoading");
            myTextBlock.Visibility = System.Windows.Visibility.Hidden;*/
            this.InitializeComponent();
            //this.Loaded += new RoutedEventHandler(UserControl_Loaded);

            _accountId = accountId;
            LinkedList<Account> account = ConfigurationManager.GetUserAccounts();
            if (ConfigurationManager.UserAccountExists(ConfigurationManager.AuthenticatedUser.ID, _accountId))
            {
                if (!haveTwitterAcess(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId)))
                {
                    MessageBox.Show("<" + accountName + "> The access has revoked, please grant access again");

                    MessageBox.Show("Uma vez que a conta existe, como proceder? actualizar os valores ou criar uma nova? Depois irpara o ecra do twitter");
                    this.Content = "";
                    //ExtendedVisualStateManager.GoToElementState(this.Parent as FrameworkElement, "AddTwitterUser1", true);
                }
            }
            else
            {
                MessageBox.Show("Or this account <" + accountName + "> don't exists or don't belong to this user <" + ConfigurationManager.AuthenticatedUser.Username + ">");
            }

        }

        private bool haveTwitterAcess(Account twitterAccount)
        {

            _authToken = new OAuthTokens();
            _authToken.AccessToken = twitterAccount.getOption("accessTokenToken");
            _authToken.AccessTokenSecret = ConfigurationManager.AuthenticatedUser.Decrypt(twitterAccount.getOption("accessTokenSecret"));
            _authToken.ConsumerKey = Properties.Settings.Default.TwitterConsumerKey;
            _authToken.ConsumerSecret = Properties.Settings.Default.TwitterConsumerSecret;

            TwitterResponse<TwitterUser> showUserResponse = TwitterUser.Show(_authToken, twitterAccount.getOption("username"));
            if (showUserResponse.Result == RequestResult.Success)
            {
                return true;
            }
            else
            {
                MessageBox.Show("Erro: " + showUserResponse.ErrorMessage);
                return false;
            }
        }
        private void getUserTimeline(string username)
        {
            getWaitCursor();
            TimelineOptions options = new TimelineOptions();
            options.Count = 50;

            TwitterResponse<TwitterStatusCollection> timeline = TwitterTimeline.HomeTimeline(_authToken, options);

            if (timeline.Result == RequestResult.Success)
            {
                listboxHomeTimeline.Items.Clear();
                try
                {
                    //MessageBox.Show(timeline.ResponseObject);
                    for (int i = 0; i < timeline.ResponseObject.Count; i++)
                    {
                        var result = timeline.ResponseObject[i];
                        ListBoxItem itm = new ListBoxItem();
                        //itm.Name = result.User.ScreenName;
                        itm.Uid = result.User.Id.ToString(); ;
                        itm.Content = result.User.ScreenName + ": " + result.Text + " " + result.CreatedDate;
                                                
                        listboxHomeTimeline.Items.Add(itm);
                    }
                }
                catch (TwitterizerException)
                {
                    MessageBox.Show("Erro");
                }

            }
            else
            {
                MessageBox.Show(timeline.ErrorMessage);
            }
            getDefaultCursor();

        }
        private void getUserFriends(string username)
        {
            getWaitCursor();

            //FollowersOptions options = new FollowersOptions();

            //TwitterResponse<TwitterUserCollection> followers = TwitterFriendship.Friends(_authToken);
            TwitterResponse<TwitterUserCollection> friends = TwitterFriendship.Friends(_authToken);
            if (friends.Result == RequestResult.Success)
            {
                try
                {
                    int friendsNumber = friends.ResponseObject.Count;

                    for (int j = 0; j < friendsNumber; j++)
                    {
                        var result = friends.ResponseObject[j];

                        if (j == 99)
                        {
                            friends = friends.ResponseObject.NextPage();
                            friendsNumber = friends.ResponseObject.Count;
                            //MessageBox.Show(friendsNumber.ToString());
                            j = 0;
                            continue;
                        }
                        ListViewItem friend = new ListViewItem();
                        //follower.Name = result.ScreenName;
                        friend.Uid = result.Id.ToString();
                        //MessageBox.Show(friend.Uid.ToString());
                        // follower.MouseDoubleClick += new MouseButtonEventHandler(ListViewFollower_Selected);

                        StackPanel stack = new StackPanel(); ;
                        stack.Width = 100;
						//stack.Orientation = System.Windows.Controls.Orientation.Vertical;

                        Label lbl = new Label();
                        lbl.Content = result.ScreenName;
                        lbl.Foreground = Brushes.White;
                        lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;


                        BitmapImage avatar = new BitmapImage();
                        avatar.BeginInit();
                        // Set properties.
                        avatar.CacheOption = BitmapCacheOption.OnDemand;
                        avatar.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                        avatar.UriSource = new Uri(result.ProfileImageLocation);
                        avatar.DecodePixelHeight = 48;
                        avatar.DecodePixelWidth = 48;

                        // End initialization.
                        avatar.EndInit();

                        Image img = new Image();
                        img.Source = avatar;
                        img.Width = 48;
                        img.Height = 48;
                        img.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;

                        stack.Children.Add(img);
                        //Console.WriteLine(result.ScreenName + ": img ->" + result.ProfileImageLocation);

                        stack.Children.Add(lbl);
                        friend.Content = stack;
						listviewFollowers.Items.Add(friend);
						

                    }
					//listviewFollowers.scro

                }

                catch (TwitterizerException e1)
                {
                    MessageBox.Show("Erro - " + e1);
                }


            }
            else
            {
                MessageBox.Show(friends.ErrorMessage);
            }

            //Number of followers added
            lblFollowers.Content = "Following " +listviewFollowers.Items.Count.ToString();
            getDefaultCursor();

        }


        private void getUserMentions(string username)
        {
            getWaitCursor();
            TimelineOptions options = new TimelineOptions();
            options.Count = 50;

            TwitterResponse<TwitterStatusCollection> timeline = TwitterTimeline.Mentions(_authToken, options);

            if (timeline.Result == RequestResult.Success)
            {
                listboxMentionsTimeline.Items.Clear();
                try
                {
                    //MessageBox.Show(timeline.ResponseObject);
                    for (int i = 0; i < timeline.ResponseObject.Count; i++)
                    {
                        var result = timeline.ResponseObject[i];
                        ListBoxItem itm = new ListBoxItem();
                        //itm.Name = result.User.ScreenName;
                        itm.Uid = result.Id.ToString();
                        itm.Content = result.User.ScreenName + ": " + result.Text + result.CreatedDate;
                        listboxMentionsTimeline.Items.Add(itm);
                    }
                }
                catch (TwitterizerException)
                {
                    MessageBox.Show("Erro");
                }

            }
            else
            {
                MessageBox.Show(timeline.ErrorMessage);
            }
            getDefaultCursor();

        }

        private void getUserMessages(string username)
        {
            getWaitCursor();
            TwitterResponse<TwitterDirectMessageCollection> timeline = TwitterDirectMessage.DirectMessages(_authToken);

            if (timeline.Result == RequestResult.Success)
            {
                listboxMentionsTimeline.Items.Clear();
                try
                {
                    //MessageBox.Show(timeline.ResponseObject);
                    for (int i = 0; i < timeline.ResponseObject.Count; i++)
                    {
                        var result = timeline.ResponseObject[i];
                        ListBoxItem itm = new ListBoxItem();
                        itm.Name = result.Sender.ScreenName;
                        itm.Content = result.Sender.ScreenName + ": " + result.Text + result.CreatedDate;
                        listboxMessagesTimeline.Items.Add(itm);
                    }
                }
                catch (TwitterizerException)
                {
                    MessageBox.Show("Erro");
                }

            }
            else
            {
                MessageBox.Show(timeline.ErrorMessage);
            }
            getDefaultCursor();

        }

        private void ListViewFollower_Selected(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("ListViewFollower_Selected");
        }

        private void listviewFollowers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListViewItem follower = listviewFollowers.SelectedItem as ListViewItem;
            if (follower != null)
            {
                if (TwitterStates.CurrentState.Name == "TwitterMainPost")
                {
                    string userMention = "";
                    TwitterResponse<TwitterUser> userResponse = TwitterUser.Show(_authToken, decimal.Parse(follower.Uid));
                    if (userResponse.Result == RequestResult.Success)
                    {
                        userMention = "@" + userResponse.ResponseObject.ScreenName + " ";
                    }
                    
                    if (txtTwittPost.Text.Length + userMention.Length <= 140)
                    {
                        txtTwittPost.Text += userMention;
                        updatetxtPostInfo();
                    }
                }
                /*else
                {
                    TwitterResponse<TwitterUser> followerDetail = TwitterUser.Show(_authToken, decimal.Parse(follower.Uid));
                    txtUserDetail.Text = "";

                    txtUserDetail.Text += showUserDetail(followerDetail.ResponseObject).ToString();
                    txtUserDetail.Text += new StringBuilder("Last three twitts: ").AppendLine();

                    txtUserDetail.Text += showUserTimelineLastX(followerDetail.ResponseObject, 3);

                    Canvas.SetTop(boxUserDetail, Mouse.GetPosition(listviewFollowers).Y);

                    btnUserDetailClose.IsEnabled = true;
                    UserDetailGrid.Visibility = System.Windows.Visibility.Visible;
                }*/
                listviewFollowers.SelectedItem = null;
            }

        }

        private StringBuilder showUserDetail(TwitterUser user)
        {
            getWaitCursor();
            StringBuilder userInfo = new StringBuilder();
            userInfo.Append("Name: " + user.Name).AppendLine();
            userInfo.Append("Twitter user since: " + user.CreatedDate).AppendLine();
            //userInfo.Append(user.Website).AppendLine();HJ
            userInfo.Append(user.NumberOfStatuses + " twitts").AppendLine();
            userInfo.Append(user.NumberOfFavorites + " favorites").AppendLine();
            userInfo.Append(user.NumberOfFollowers + " followers").AppendLine();
            userInfo.Append(user.NumberOfFriends + " friends").AppendLine().AppendLine();
            getDefaultCursor();
            return userInfo;
        }
        private StringBuilder showUserTimelineLastX(TwitterUser user, int twittNumber)
        {
            getWaitCursor();
            UserTimelineOptions options = new UserTimelineOptions();
            options.ScreenName = user.ScreenName;
            options.Count = twittNumber;
            TwitterResponse<TwitterStatusCollection> userTimeline = TwitterTimeline.UserTimeline(_authToken, options);

            StringBuilder timelineString = new StringBuilder();
            for (int j = 0; j < userTimeline.ResponseObject.Count; j++)
            {
                var userStatus = userTimeline.ResponseObject[j];
                timelineString.Append(userStatus.CreatedDate).AppendLine();
                timelineString.Append(userStatus.Text).AppendLine();
                timelineString.AppendLine();
            }
            getDefaultCursor();
            return timelineString;

        }
        private void listboxTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            ListBoxItem twitt = list.SelectedItem as ListBoxItem;

            if (twitt != null)
            {
                UserTimelineOptions options = new UserTimelineOptions();
                //options.ScreenName = twitt.Name;
                options.UserId = decimal.Parse(twitt.Uid);
                TwitterResponse<TwitterStatusCollection> userTimeline = TwitterTimeline.UserTimeline(_authToken, options);
                showUserTimeline(userTimeline);
            }
            list.SelectedItem = null;
        }
        private void showUserTimeline(TwitterResponse<TwitterStatusCollection> userTimeline)
        {
            listboxUserTime.Items.Clear();
            for (int j = 0; j < userTimeline.ResponseObject.Count; j++)
            {
                ListBoxItem itm = new ListBoxItem();
                var userTwitt = userTimeline.ResponseObject[j];
                //itm.Name = userTwitt.User.ScreenName;
                itm.Uid = userTwitt.User.Id.ToString();
                itm.Content = userTwitt.User.ScreenName + ": " + userTwitt.Text + " (at " + userTwitt.CreatedDate + ")";
                listboxUserTime.Items.Add(itm);
            }
            imgUser.Source = new BitmapImage(new Uri(@userTimeline.ResponseObject[0].User.ProfileImageLocation));
            txtUserInfo.Text = "";
            txtUserInfo.Text = showUserDetail(userTimeline.ResponseObject[0].User).ToString();

            ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterUserTimeline", true);

        }

        private void btnUserDetailClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.UserDetailGrid.Visibility = System.Windows.Visibility.Hidden;
            btnUserDetailClose.IsEnabled = false;
        }

        private void UserControl_Initialized(object sender, System.EventArgs e)
        {
            ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMain", true);
        }

        private void btnUserTimelineBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMain", true);
        }

        private void getHandCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.IsEnabled)
                this.Cursor = Cursors.Hand;
        }

        private void getHandCursor()
        {
            if (this.IsEnabled)
                this.Cursor = Cursors.Hand;
        }

        private void getWaitCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Cursor = Cursors.Wait;
        }

        private void getWaitCursor()
        {
            this.Cursor = Cursors.Wait;
        }
        private void getDefaultCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (this.IsEnabled)
                this.Cursor = Cursors.Arrow;
        }

        private void getDefaultCursor()
        {
            this.Cursor = Cursors.Arrow;
        }

        private void lblHome_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.IsEnabled)
            {
                getUserTimeline(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMain", true);
            }
        }

        private void lblMentions_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.IsEnabled)
            {
                getUserMentions(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMainMentions", true);
            }
        }

        private void lblMessages_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.IsEnabled)
            {
                getUserMessages(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMainMessages", true);
            }
        }

        private void lblTwitt_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.IsEnabled)
            {
                if(!listviewFollowers.HasItems)
                {
                    getUserFriends(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                }
                
                TwitterResponse<TwitterUser> userTimeline = TwitterUser.Show(_authToken, ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                txtUserLastPosts.Text = "";

                txtUserLastPosts.Text += new StringBuilder("Last ten twitts: ").AppendLine();

                txtUserLastPosts.Text += showUserTimelineLastX(userTimeline.ResponseObject, 10);

                ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, "TwitterMainPost", true);
            }
        }

        private void txtTwittPost_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtTwittPost.Text.Length <= 140)
            {
                updatetxtPostInfo();
            }
            else
            {
                return;
            }
        }

        private void updatetxtPostInfo()
        {

            txtPostInfo.Text = (140 - txtTwittPost.Text.Length).ToString();
        }

        private void btnPostTwitt_Click(object sender, RoutedEventArgs e)
        {
            if (txtTwittPost.Text != "")
            {
                TwitterResponse<TwitterStatus> statusResponse = TwitterStatus.Update(_authToken, txtTwittPost.Text);

                if (statusResponse.Result == RequestResult.Success)
                {
                    txtTwittPost.Text = "";
                    MessageBox.Show("Twitter enviado com sucesso");

                    TwitterResponse<TwitterUser> userTimeline = TwitterUser.Show(_authToken, ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
                    txtUserLastPosts.Text = "";

                    txtUserLastPosts.Text += new StringBuilder("Last ten twitts: ").AppendLine();

                    txtUserLastPosts.Text += showUserTimelineLastX(userTimeline.ResponseObject, 10);

                }
                else
                {
                    MessageBox.Show("Twitter não enviado");
                }
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //_parentWindow = Window.GetWindow(this);
            //MessageBox.Show("Esconder a label de loading");

            getUserTimeline(ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId).getOption("username"));
            /*Label txtLoading = (Label)_parentWindow.FindName("txtLoading");
            txtLoading.Visibility = System.Windows.Visibility.Hidden;*/
        }
    }
}