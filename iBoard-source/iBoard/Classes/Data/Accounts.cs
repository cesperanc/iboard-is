using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Configuration;
using System.Collections.ObjectModel;

namespace iBoard.Classes.Data {

    /// <summary>
    /// This gets the updates on the configuration and update the internal collection accordingly
    /// </summary>
    public class Accounts : ObservableCollection<Account>, IConfigListenner {
        public Accounts()
            : base() {
                ConfigurationManager.AddConfigListenner(this);
                this.ConfigUpdated();
        }

        /// <summary>
        /// Notify the class for an update on the configuration
        /// </summary>
        public void ConfigUpdated() {
            LinkedList<Account> accounts = ConfigurationManager.GetUserAccounts();
            this.Clear();
            foreach(Account account in accounts) {
                this.Add(account);
            }
        }
    }
}
