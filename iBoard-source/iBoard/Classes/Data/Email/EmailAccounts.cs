using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Configuration;
using System.Collections.ObjectModel;

namespace iBoard.Classes.Data.Email {

    /// <summary>
    /// This gets the updates from the configuration and update the internal collection accordingly
    /// </summary>
    public class EmailAccounts : ObservableCollection<Account>, IConfigListenner {
        private Boolean _showHidden = false;

        public EmailAccounts()
            : this(false) {
        }

        public EmailAccounts(Boolean showHidden)
            : base() {
                this._showHidden = showHidden;
                ConfigurationManager.AddConfigListenner(this);
                this.ConfigUpdated();
        }

        /// <summary>
        /// Notify the class for an update on the configuration
        /// </summary>
        public void ConfigUpdated() {
            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.EMAILTYPE);
            this.Clear();
            foreach(Account account in accounts) {
                this.Add(account);
            }
        }
    }
}
