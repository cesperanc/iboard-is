using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Configuration;
using System.Collections.ObjectModel;

namespace iBoard.Classes.Data.Twitter {

    /// <summary>
    /// This gets the updates from the configuration and update the internal collection accordingly
    /// </summary>
    public class TwitterAccounts : ObservableCollection<Account>, IConfigListenner {
        private Boolean _showHidden = false;

        public TwitterAccounts()
            : this(false) {
        }

        public TwitterAccounts(Boolean showHidden)
            : base() {
                this._showHidden = showHidden;
                ConfigurationManager.AddConfigListenner(this);
                this.ConfigUpdated();
        }

        /// <summary>
        /// Notify the class for an update on the configuration
        /// </summary>
        public void ConfigUpdated() {
            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.TWITTERTYPE);

            this.Clear();
            foreach(Account account in accounts) {
                if(account.Type.Equals(Account.TWITTERTYPE) && (this._showHidden || (!this._showHidden && account.Enabled))) {
                    this.Add(new TwitterAccount(account));
                }
            }
        }
    }
}
