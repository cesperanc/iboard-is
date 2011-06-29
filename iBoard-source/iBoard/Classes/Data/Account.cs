using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iBoard.Classes.Data {
    
    public class Account {
        private int _id = -1;
        private String _name="";
        private String _type=Account.MOODLETYPE;
        private Boolean _enabled=false;
        private DateTime _lastUpdate = DateTime.MinValue;
        private System.Collections.Generic.Dictionary<string, String> _options;
        public const String MOODLETYPE = "moodle";
        public const String TWITTERTYPE = "twitter";
        public const String EMAILTYPE = "email";

        public DateTime LastUpdate {
            get {
                return this._lastUpdate;
            }
            set {
                this._lastUpdate = value;
            }
        }

        /// <summary>
        /// Insert or update an account option value
        /// </summary>
        public void addOption(String key, String value) {
            this.setOption(key, value);
        }

        /// <summary>
        /// Insert or update an account option value
        /// </summary>
        public void setOption(String key, String value){
            if(this._options.ContainsKey(key)) {
                this._options[key] = value;
            } else {
                this._options.Add(key, value);
            }
        }

        /// <summary>
        /// Get an account option value
        /// </summary>
        public String getOption(String key) {
            if(this._options.ContainsKey(key)) {
                return this._options[key];
            }
            return null;
        }

        /// <summary>
        /// Remove an option from the account
        /// </summary>
        public Boolean removeOption(String key) {
            if(this._options.ContainsKey(key)) {
                return this._options.Remove(key);
            }
            return false;
        }

        /// <summary>
        /// Get all the account options names
        /// </summary>
        public System.Collections.Generic.LinkedList<String> GetOptionsNames() {
            System.Collections.Generic.LinkedList<String> options = new System.Collections.Generic.LinkedList<String>();

            foreach(KeyValuePair<String, String> option in this._options) {
                options.AddLast(option.Key);
            }
            return options;
        }

        /// <summary>
        /// Specify if the acount is enabled or not
        /// </summary>
        public Boolean Enabled {
            get {
                return this._enabled;
            }
            set {
                this._enabled = value;
            }
        }

        /// <summary>
        /// The type of the account
        /// </summary>
        public String Type {
            get {
                return this._type;
            }
            set {
                this._type = value;
            }
        }

        /// <summary>
        /// The name of the account
        /// </summary>
        public String Name {
            get {
                return this._name;
            }
            set {
                this._name = value;
            }
        }

        /// <summary>
        /// The account identifier
        /// </summary>
        public int ID {
            get {
                return this._id;
            }
            set {
                this._id = value;
            }
        }

        /// <summary>
        /// Instanciates a new Account instance
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <param name="name">Name of the account</param>
        /// <param name="type">Type of the account</param>
        /// <param name="enabled">State of the account</param>
        /// <param name="lastUpdate">Last update from this account</param>
        public Account(int id, string name, String type, bool enabled, DateTime lastUpdate)
            : this(name, type, enabled, lastUpdate) {
            this._id = id;
        }

        /// <summary>
        /// Instanciates a new Account instance
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <param name="name">Name of the account</param>
        /// <param name="type">Type of the account</param>
        /// <param name="enabled">State of the account</param>
        public Account(int id, string name, String type, bool enabled)
            : this(name, type, enabled) {
            this._id = id;
        }

        /// <summary>
        /// Instanciates a new Account instance
        /// </summary>
        /// <param name="name">Name of the account</param>
        /// <param name="type">Type of the account</param>
        /// <param name="enabled">State of the account</param>
        /// <param name="lastUpdate">Last update from this account</param>
        public Account(string name, String type, bool enabled, DateTime lastUpdate) 
            : this(name, type, enabled){
            this._lastUpdate = lastUpdate;
        }

        /// <summary>
        /// Instanciates a new Account instance
        /// </summary>
        /// <param name="name">Name of the account</param>
        /// <param name="type">Type of the account</param>
        /// <param name="enabled">State of the account</param>
        public Account(string name, String type, bool enabled)
            : this() {
            this._name = name;
            this._type = type;
            this._enabled = enabled;
        }

        /// <summary>
        /// Instanciates a new Account instance
        /// </summary>
        public Account() {
            this._id = -1;
            this._name = "";
            this._type = Account.MOODLETYPE;
            this._enabled = false;
            this._options = new System.Collections.Generic.Dictionary<string, string>();
        }

        /// <summary>
        /// Copy the properties from another account
        /// </summary>
        /// <param name="account">Account instance with the data to copy</param>
        public void CloneFrom(Account account) {
            this.Enabled = account.Enabled;
            this.ID = account.ID;
            this.LastUpdate = account.LastUpdate;
            this.Name = account.Name;
            this.Type = account.Type;

            LinkedList<String> optionsNames = account.GetOptionsNames();
            foreach(String optionsName in optionsNames) {
                this.setOption(optionsName, account.getOption(optionsName));
            }
        }

        /// <summary>
        /// Overide the default ToString returning the account name instead
        /// </summary>
        /// <returns>String with the Account name</returns>
        public override string ToString() {
            return this.Name;
        }
    }

}
