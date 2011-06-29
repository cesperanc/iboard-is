using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Configuration;
using System.Collections.ObjectModel;
using System.Windows;

namespace iBoard.Classes.Data.Moodle {

    /// <summary>
    /// This gets the updates from the configuration and update the internal collection accordingly
    /// </summary>
    public class MoodleAccounts : ObservableCollection<MoodleAccount>, IConfigListenner {
        private Boolean _showHidden = false;

        public MoodleAccounts()
            : this(false) {
        }

        public MoodleAccounts(Boolean showHidden)
            : base() {
                this._showHidden = showHidden;
                ConfigurationManager.AddConfigListenner(this);
                this.ConfigUpdated();
        }

        /// <summary>
        /// Notify the class for an update on the configuration
        /// </summary>
        public void ConfigUpdated() {
            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.MOODLETYPE);
            
            this.Clear();
            
            foreach(Account account in accounts) {
                if(this._showHidden || (!this._showHidden && account.Enabled)) {
                    this.Add(new MoodleAccount(account));
                }
            }
        }
    }
}
