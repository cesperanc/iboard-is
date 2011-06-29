using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Configuration;
using iBoard.Classes.Data;
using System.Collections.ObjectModel;
using System.Windows;
using System.ServiceModel;
using System.ComponentModel;

namespace iBoard.Classes.Data.Twitter {

    /// <summary>
    /// Get a TwitterAccount from an account
    /// </summary>
    public class TwitterAccount : Account, INotifyPropertyChanged {
        private Uri _imageUrl = new Uri("Assets/Icons/moodledefaultavatar.png", UriKind.Relative);

        public TwitterAccount(Account account)
            : base() {
                if(account.Type.Equals(Account.TWITTERTYPE)) {
                    // the local part
                    this.CloneFrom(account);

                    // the remote part
                    
                    BackgroundWorker bgWorker = new BackgroundWorker();
                    
                    bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args) {
                        try {
                            args.Result = null;
                            Twitterizer.TwitterResponse<Twitterizer.TwitterUser> showUserResponse = Twitterizer.TwitterUser.Show(TwitterTimeline.getTwitterAuthToken(account), account.getOption("username"));
                            if(showUserResponse.Result == Twitterizer.RequestResult.Success) {
                                args.Result = showUserResponse.ResponseObject.ProfileImageLocation;
                            }
                        } catch(Exception ex) {
                            args.Result = "Unable to get the Twitter account: " + ex.Message;
                            args.Cancel = true;
                        }
                    });

                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object wsender, RunWorkerCompletedEventArgs args) {
                        if(!args.Cancelled && args.Error == null) {
                            String url = args.Result as String;

                            if(url != null && !url.Equals("")) {
                                this.ImageUrl = new Uri(url);
                            }
                        }
                    });

                    bgWorker.RunWorkerAsync();
                    
                } else {
                    throw new InvalidCastException("Unable to create a TwitterAccount from something other than a Twitter account");
                }
        }
        /// <summary>
        /// Get the moodle user account image
        /// </summary>
        public Uri ImageUrl {
            get {
                return this._imageUrl;
            }
            set {
                this._imageUrl = value;

                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ImageUrl"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
