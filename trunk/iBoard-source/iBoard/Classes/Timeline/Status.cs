using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using iBoard.Classes.Data;
using iBoard.Classes.Configuration;

namespace iBoard.Classes.Timeline {

    public class Status : INotifyPropertyChanged {

        private String _description = "";
        private String _accountName = "";
        private int _accountId = -1;
        private Uri _iconUrl = new Uri("/iBoard;component/Assets/Icons/icon.ico", UriKind.Relative);

        /// <summary>
        /// Instanciates a new StatusInfo object
        /// </summary>
        /// <param name="accountId">With the ID of the account</param>
        /// <param name="description">String with the description</param>
        public Status(int accountId, String description) {
            this.ID = accountId;
            this.Description = description;

            if(accountId > -1 && ConfigurationManager.UserAccountExists(accountId)) {
                Account account = ConfigurationManager.GetUserAccount(accountId);
                if(account != null) {
                    this.Name = account.Name;

                    switch(account.Type) {
                        case Account.EMAILTYPE:
                            this.ImageUrl = new Uri("/iBoard;component/Assets/Icons/email.png", UriKind.Relative);
                            break;
                        case Account.MOODLETYPE:
                            this.ImageUrl = new Uri("/iBoard;component/Assets/Icons/moodle.png", UriKind.Relative);
                            break;
                        case Account.TWITTERTYPE:
                            this.ImageUrl = new Uri("/iBoard;component/Assets/Icons/twitter.png", UriKind.Relative);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Get or set the name
        /// </summary>
        public String Name {
            get {
                return this._accountName;
            }
            set {
                this._accountName = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Name"));
                }
            }
        }

        /// <summary>
        /// Get or set the StatusInfo Description
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
        /// Get or set the StatusInfo identifier
        /// </summary>
        public int ID {
            get {
                return this._accountId;
            }
            set {
                this._accountId = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ID"));
                }
            }
        }

        /// <summary>
        /// Get or set the StatusInfo image uri
        /// </summary>
        public Uri ImageUrl {
            get {
                return this._iconUrl;
            }
            set {
                this._iconUrl = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ImageUrl"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
