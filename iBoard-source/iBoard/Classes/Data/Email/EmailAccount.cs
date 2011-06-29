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

namespace iBoard.Classes.Data.Email {

    /// <summary>
    /// Get a EmailAccount from an account
    /// </summary>
    public class EmailAccount : Account, INotifyPropertyChanged {
        private Uri _imageUrl = new Uri("Assets/Icons/download.png", UriKind.Relative);

        public EmailAccount(Account account)
            : base() {
                if(account.Type.Equals(Account.EMAILTYPE)) {
                    // the local part
                    this.CloneFrom(account);

                } else {
                    throw new InvalidCastException("Unable to create a EmailAccount from something other than a Email account");
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
