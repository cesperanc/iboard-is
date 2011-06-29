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
    public class Users : ObservableCollection<User>, IConfigListenner {
        public Users()
            : base() {
                ConfigurationManager.AddConfigListenner(this);
        }

        /// <summary>
        /// Notify the class for an update on the configuration
        /// </summary>
        public void ConfigUpdated() {
            LinkedList<User> users = ConfigurationManager.GetUsers();
            this.Clear();
            foreach(User user in users) {
                this.Add(user);
            }
        }
    }
}
