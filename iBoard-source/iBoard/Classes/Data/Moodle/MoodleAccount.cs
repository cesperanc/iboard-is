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

namespace iBoard.Classes.Data.Moodle {

    /// <summary>
    /// Get a MoodleAccount from an account
    /// </summary>
    public class MoodleAccount : Account, INotifyPropertyChanged {
        private String _city = "";
        private String _country = "";
        private String _description = "";
        private String _fullName = "";
        private int _moodleId = -1;
        private Uri _imageUrl = new Uri("Assets/Icons/moodledefaultavatar.png", UriKind.Relative);
        private Uri _profileLink = null;
        private Uri _webPageUrl = null;

        public MoodleAccount(Account account)
            : base() {
                if(account.Type.Equals(Account.MOODLETYPE)) {
                    // the local part
                    this.CloneFrom(account);
                    
                    // the remote part
                    BackgroundWorker bgWorker = new BackgroundWorker();

                    bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args) {
                        try {
                            UedWs.UEDWSPortTypeClient client = new UedWs.UEDWSPortTypeClient();
                            client.Endpoint.Address = new EndpointAddress(account.getOption(Properties.Settings.Default.MoodleServiceUrlSettingName));

                            UedWs.UedCredentials credentials = new UedWs.UedCredentials();
                            credentials.username = account.getOption(Properties.Settings.Default.MoodleServiceUsernameSettingName);
                            credentials.autorizationKey = account.getOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName);

                            // let's get the remote moodle user account 
                            UedWs.UedUser user = client.getMyUserPublicProfile(credentials);

                            args.Result = user;
                        } catch(Exception ex) {
                            args.Result = "Unable to get the Moodle account: " + ex.Message;
                            args.Cancel = true;
                        }
                    });

                    bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object wsender, RunWorkerCompletedEventArgs args) {
                        
                        if(!args.Cancelled && args.Error == null) {
                            UedWs.UedUser user = args.Result as UedWs.UedUser;
                            if(user != null) {

                                this.MoodleId = user.id;
                                if(!user.city.Equals("") && user.city != null)
                                    this.City = user.city;
                                if(!user.country.Equals("") && user.country != null)
                                    this.Country = user.country;
                                if(!user.description.Equals("") && user.description != null)
                                    this.Description = user.description;
                                if(!user.fullName.Equals("") && user.fullName != null)
                                    this.Fullname = user.fullName;
                                if(!user.imageUrl.Equals("") && user.imageUrl != null)
                                    this.ImageUrl = new Uri(user.imageUrl);
                                if(!user.profileLink.Equals("") && user.profileLink != null)
                                    this.ProfileLink = new Uri(user.profileLink);
                                if(!user.webPageUrl.Equals("") && user.webPageUrl != null)
                                    this.WebPageUrl = new Uri(user.webPageUrl);
                            }
                        }
                    });

                    bgWorker.RunWorkerAsync();
                } else {
                    throw new InvalidCastException("Unable to create a MoodleAccount from something other than a Moodle account");
                }
        }

        /// <summary>
        /// Get the user account city
        /// </summary>
        public String City {
            get {
                return this._city;
            }
            set {
                this._city = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("City"));
                }
            }
        }

        /// <summary>
        /// Get the user account country
        /// </summary>
        public String Country {
            get {
                return this._country;
            }
            set {
                this._country = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Country"));
                }
            }
        }

        /// <summary>
        /// Get the user account description
        /// </summary>
        public String Description {
            get {
                return this._description;
            }
            set {
                this._description = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Description"));
                }
            }
        }

        /// <summary>
        /// Get the user fullname 
        /// </summary>
        public String Fullname {
            get {
                return this._fullName;
            }
            set {
                this._fullName = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Fullname"));
                }
            }
        }

        /// <summary>
        /// Get the moodle user account identifier
        /// </summary>
        public int MoodleId {
            get {
                return this._moodleId;
            }
            set {
                this._moodleId = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("MoodleId"));
                }
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

        /// <summary>
        /// Get the moodle user account profile link
        /// </summary>
        public Uri ProfileLink {
            get {
                return this._profileLink;
            }
            set {
                this._profileLink = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ProfileLink"));
                }
            }
        }

        /// <summary>
        /// Get the moodle user account Web page url
        /// </summary>
        public Uri WebPageUrl {
            get {
                return this._webPageUrl;
            }
            set {
                this._webPageUrl = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("WebPageUrl"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
