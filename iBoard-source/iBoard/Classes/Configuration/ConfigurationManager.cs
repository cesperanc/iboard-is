using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Windows;
using System.Reflection;
using System.Collections;
using iBoard.Classes.Data;

namespace iBoard.Classes.Configuration {

    /// <summary>
    /// Interface to be implemented by the listenners for the configuration update
    /// </summary>
    public interface IConfigListenner {
        /// <summary>
        /// Should be implemented by listenners to react to the configuration update
        /// </summary>
        void ConfigUpdated();
    }

    /// <summary>
    /// Interface to be implemented by the listenners for the user account changes
    /// </summary>
    public interface IiBoardUserAccountChanges {
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserAccountUpdated(Account account);
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserAccountCreated(Account account);
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserAccountDeleted(Account account);
    }

    /// <summary>
    /// Interface to be implemented by the listenners for the user changes
    /// </summary>
    public interface IiBoardUserChanges {
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserUpdated(User user);
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserCreated(User user);
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserDeleted(User user);
    }

    /// <summary>
    /// Interface to be implemented by the listenners for the user changes
    /// </summary>
    public interface IiBoardAuthenticationChanges {
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserLoggedIn(User user);
        /// <summary>
        /// Should be implemented by listenners
        /// </summary>
        void UserLoggedOut(User user);
    }

    /// <summary>
    /// Base configuration manager class
    /// </summary>
    /// 
    /// <seealso cref="http://www.csharpfriends.com/articles/getarticle.aspx?articleid=336#4">
    /// Accessed on 2010-12-01: Code documentation information
    /// </seealso>
    /// <seealso cref="http://msdn.microsoft.com/en-us/library/ff650316.aspx">
    /// Accessed on 2010-12-01: Singleton pattern in C#
    /// </seealso>
    /// <seealso cref="http://sharpertutorials.com/using-xsd-tool-to-generate-classes-from-xml/">
    /// Accessed on 2010-12-01: Class generation from XML and XML class deserialization
    /// </seealso>
    /// <seealso cref="http://msdn.microsoft.com/en-us/library/x6c1kb0s(VS.80).aspx">
    /// Accessed on 2010-12-01: Class generation from XML using the xsd.exe tool with the parameter file
    /// </seealso>
    /// <seealso cref="http://msdn.microsoft.com/en-us/library/xfhwa508.aspx">
    /// Accessed on 2010-12-02: Dictionary reference to store the dynamic options
    /// </seealso>
    /// <seealso cref="http://www.yoda.arachsys.com/csharp/singleton.html">
    /// Accessed on 2010-12-24: Singleton pattern in C#
    /// </seealso>
    public class ConfigurationManager {
        private static ConfigurationManager _instance = null;
        private const String _xs = "xs";
        private const String _xsUri = "http://tempuri.org/Config.xsd";
        private String _xmlFileName = null;
        private XmlDocument _xmlDocument = null;
        private XmlNamespaceManager _namespaceManager = null;
        private List<String> _xmlValidationErrors = new List<String>();
        private List<String> _xmlValidationWarnings = new List<String>();
        private User _authenticatedUser = null;
        private ArrayList _configListenners = new ArrayList();
        private ArrayList _usersListenners = new ArrayList();
        private ArrayList _userAccountsListenners = new ArrayList();
        private ArrayList _loginListenners = new ArrayList();
        static readonly object _padlock = new object();
        static readonly object _savelock = new object();
        static readonly object _updatelock = new object();

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="xmlSchemaFileName">The XML Schema file name and path to validate the XML file against</param>
        /// <see cref="ConfigurationManager.Instance"/>
        private ConfigurationManager() {
            this._xmlFileName = this._initializeConfigXmlFile();
            String xmlSchemaFileName = this._initializeConfigXsdFile();

            if(!File.Exists(this._xmlFileName))
                throw new FileNotFoundException(Properties.Resources.XmlFileNotFound, this._xmlFileName);

            if(!File.Exists(xmlSchemaFileName))
                throw new FileNotFoundException(Properties.Resources.XmlSchemaFileNotFound, xmlSchemaFileName);

            try {
                this._xmlDocument = new XmlDocument();
                this._xmlDocument.Schemas.Add(null, xmlSchemaFileName);

                this._reload();
            } catch(XmlSchemaValidationException schemaValidationException) {
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema, schemaValidationException);
            }
        }

        /// <summary>
        /// Validate event callback to store the error and warning messages (if any)
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void _xmlValidateEvent(object o, ValidationEventArgs e) {
            switch(e.Severity) {
                case XmlSeverityType.Error:
                    this._xmlValidationErrors.Add(e.Message);

                    break;

                case XmlSeverityType.Warning:
                    this._xmlValidationWarnings.Add(e.Message);

                    break;
            }
        }

        /// <summary>
        /// Validates the XML document
        /// </summary>
        private void _validate() {
            this._xmlValidationErrors.Clear();
            this._xmlValidationWarnings.Clear();
            this._xmlDocument.Validate(this._xmlValidateEvent);
        }

        /// <summary>
        /// Reload and revalidates the XML file
        /// </summary>
        private void _reload() {
            lock(_savelock) {
                this._xmlDocument.Load(this._xmlFileName);
                this._validate();
                this._namespaceManager = new XmlNamespaceManager(this._xmlDocument.NameTable);
                this._namespaceManager.AddNamespace(ConfigurationManager._xs, ConfigurationManager._xsUri);
            }
        }

        /// <summary>
        /// Get the application data directory to store the application user files
        /// </summary>
        /// <returns>String with the data directory path</returns>
        private String _getDataDir() {
            String dataDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            dataDir = Path.Combine(dataDir, "iBoard");
            if(!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            return dataDir;
        }

        /// <summary>
        /// Copy the specified stream to the destination filename
        /// </summary>
        /// <param name="streamreader">With to stream to read from</param>
        /// <param name="destinationFilename">With the destination filename</param>
        /// <returns></returns>
        private Boolean _copyStreamToFile(StreamReader streamreader, String destinationFilename) {
            try {
                FileStream filestream;
                StreamWriter streamwriter;
                filestream = new FileStream(destinationFilename, FileMode.Create);
                streamwriter = new StreamWriter(filestream);
                streamwriter.Write(streamreader.ReadToEnd());
                streamwriter.Flush();
                streamwriter.Close();
                streamreader.Close();

                return true;
            } catch {
            }
            return false;
        }

        /// <summary>
        /// Check for the configuration XML and create if it doesn't exist
        /// </summary>
        /// <returns>String with the filename</returns>
        private String _initializeConfigXmlFile() {
            String dataDir = this._getDataDir();
            String fileName = Path.Combine(dataDir, "Config.xml");

            if(!File.Exists(fileName)) {
                if(this._copyStreamToFile(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("iBoard.Assets.Config.Config.xml")), fileName)) {
                    return fileName;
                }
                return null;
            }
            return fileName;
        }

        /// <summary>
        /// Check for the configuration Schema file and create if it doesn't exist
        /// </summary>
        /// <returns>String with the filename</returns>
        private String _initializeConfigXsdFile() {
            String dataDir = this._getDataDir();
            String fileName = Path.Combine(dataDir, "Config.xsd");

            if(!File.Exists(fileName)) {
                if(this._copyStreamToFile(new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("iBoard.Assets.Config.Config.xsd")), fileName)) {
                    return fileName;
                }
                return null;
            }
            return fileName;
        }

        /// <summary>
        /// Selects a single node from the XML document using a XPath expression string where {0} will be replaced by prefix:
        /// </summary>
        /// <param name="xPathExpression">XPath expression string</param>
        /// <returns>XmlNode with the matched element or null</returns>
        private static XmlNode _getNode(String xPathExpression) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            return cm._xmlDocument.SelectSingleNode(String.Format(xPathExpression, ConfigurationManager._xs + ":"), cm._namespaceManager);
        }

        /// <summary>
        /// Selects a list of xml nodes from the XML document using a XPath expression string where {0} will be replaced by prefix:
        /// </summary>
        /// <param name="xPathExpression">XPath expression string</param>
        /// <returns>XmlNodeList with the matched nodes or null</returns>
        private static XmlNodeList _getNodes(String xPathExpression) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            return cm._xmlDocument.SelectNodes(String.Format(xPathExpression, ConfigurationManager._xs + ":"), cm._namespaceManager);
        }

        /// <summary>
        /// Get a ConfigurationManager instance
        /// </summary>
        public static ConfigurationManager Instance {
            get {
                if(_instance == null) {
                    lock(_padlock) {
                        if(_instance == null) {
                            _instance = new ConfigurationManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Reload and revalidates the XML file
        /// </summary>
        public static Boolean Reload() {
            ConfigurationManager cm = ConfigurationManager.Instance;
            cm._reload();
            if(!ConfigurationManager.hasValidationErrors()) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates the XML document
        /// </summary>
        public static Boolean Validate() {
            ConfigurationManager cm = ConfigurationManager.Instance;
            cm._validate();
            if(!ConfigurationManager.hasValidationErrors()) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Save the current XmlDocument
        /// </summary>
        /// <exception cref="Exception">With exception thrown by the save method</exception>
        public static Boolean Save() {
            ConfigurationManager cm = ConfigurationManager.Instance;
            ConfigurationManager.Validate();
            if(!ConfigurationManager.hasValidationErrors()) {
                lock(_savelock) {
                    cm._xmlDocument.Save(cm._xmlFileName);
                    ConfigurationManager.ConfigurationUpdated();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Inquiry for validation errors
        /// </summary>
        /// <returns>Boolean true if we have validation errors, false otherwise</returns>
        public static Boolean hasValidationErrors() {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(cm._xmlValidationErrors.Count > 0)
                return true;
            return false;
        }

        /// <summary>
        /// Inquiry for validation warnings
        /// </summary>
        /// <returns>Boolean true if we have validation warnings, false otherwise</returns>
        public static Boolean hasValidationWarnings() {
            ConfigurationManager cm = ConfigurationManager.Instance;

            if(cm._xmlValidationWarnings.Count > 0)
                return true;
            return false;
        }

        /// <summary>
        /// Get the validation errors strings
        /// </summary>
        /// <returns>List<String> with the validation error strings</returns>
        public static List<String> ValidationErrors {
            get {
                ConfigurationManager cm = ConfigurationManager.Instance;
                return new List<string>(cm._xmlValidationErrors);
            }
        }

        /// <summary>
        /// Get the validation warning strings
        /// </summary>
        /// <returns>List<String> with the validation warning strings</returns>
        public static List<String> ValidationWarnings {
            get {
                ConfigurationManager cm = ConfigurationManager.Instance;
                return new List<string>(cm._xmlValidationWarnings);
            }
        }

        /// <summary>
        /// Verify if the specified user identifier exists on the configuration XML
        /// </summary>
        /// <param name="userId">The user identifier to check for</param>
        /// <returns>Boolean true if the user exists, false otherwise</returns>
        public static Boolean UserIdExists(int userId){
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]");
            if(node != null) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Getter and setter for the default user id
        /// </summary>
        /// <returns>The user identifier or -1 when no default user was found</returns>
        public static int DefaultUserId {
            get {
                if(ConfigurationManager.hasValidationErrors())
                    throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

                XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/@defaultUser");
                if(node != null) {
                    return int.Parse(node.Value);
                }

                return -1;
            }

            set {

                if(ConfigurationManager.hasValidationErrors())
                    throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

                if(value > 0 && !ConfigurationManager.UserIdExists(value))
                    throw new Exception(Properties.Resources.UserIdDontExist);

                // If the attribute already exists, let's use it
                XmlAttribute attribute = (XmlAttribute) ConfigurationManager._getNode("/{0}Config/{0}Users/@defaultUser");
                if(attribute != null) {
                    if(value > 0) {
                        String originalValue = attribute.Value;
                        attribute.Value = value.ToString();

                        // Save the changes
                        if(!ConfigurationManager.Save()) {
                            // Revert the changes and throw a validation error
                            attribute.Value = originalValue;

                            throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);
                        }
                    } else {
                        // delete the atribute
                        XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users");
                        if(node != null) {
                            node.Attributes.Remove(attribute);
                            // Save the changes
                            ConfigurationManager.Save();
                        }
                    }
                } else {
                    if(value > 0) {
                        // The attribute doesn't exist, so let's create one
                        XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users");
                        if(node != null) {
                            attribute = node.OwnerDocument.CreateAttribute("defaultUser");
                            attribute.Value = value.ToString();
                            node.Attributes.Append(attribute);

                            // Save the changes
                            if(!ConfigurationManager.Save()) {
                                // Revert the changes and throw a validation error
                                node.Attributes.Remove(attribute);

                                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);
                            }

                        } else {
                            throw new Exception(Properties.Resources.UnableToSelectTheXmlNodeToSet);
                        }
                    } else {
                        // Nothing to change; however, the user was changed internaly, so let's dispatch a configuration updated event
                        ConfigurationManager.ConfigurationUpdated();
                    }
                }
            }
        }

        /// <summary>
        /// Get a specific account from a specific user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <param name="accountId">with the account identifier</param>
        /// <returns></returns>
        public static Account GetUserAccount(int userId, int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);
            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]/{0}Accounts/{0}Account[@id=" + accountId + "]");
            if(node != null) {
                Account account = new Account();

                if(node.Attributes["id"] != null) {
                    account.ID = int.Parse(node.Attributes["id"].Value);
                }

                foreach(XmlNode child in node.ChildNodes) {
                    switch(child.Name) {
                        case "Name":
                            account.Name = child.InnerText;
                            break;

                        case "Type":
                            account.Type = child.InnerText.ToLower();

                            break;

                        case "Enabled":
                            account.Enabled = (child.InnerText.ToLower().Equals("true") ? true : false);
                            break;

                        case "LastUpdate":
                            account.LastUpdate = DateTime.Parse(child.InnerText);
                            break;

                        case "Option":
                            account.setOption(child.Attributes["name"].Value, child.InnerText);
                            break;

                    }
                }

                return account;
            }
            return null;
        }

        /// <summary>
        /// Get a specific account from the authenticated user
        /// </summary>
        /// <param name="accountId">with the account identifier</param>
        /// <returns></returns>
        public static Account GetUserAccount(int accountId) {
            if(!ConfigurationManager.IsAuthenticated)
                return null;

            return ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, accountId);
        }
        /// <summary>
        /// Get all the user accounts from a specific user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <returns>LinkedList with all the user accounts</returns>
        public static LinkedList<Account> GetUserAccounts(int userId) {
            return ConfigurationManager.GetUserAccountsFromType(userId, "");
        }

        /// <summary>
        /// Get all the user accounts from the authenticated user
        /// </summary>
        /// <returns>LinkedList with all the user accounts</returns>
        public static LinkedList<Account> GetUserAccounts() {
            if(!ConfigurationManager.IsAuthenticated)
                return new LinkedList<Account>();

            return ConfigurationManager.GetUserAccounts(ConfigurationManager.AuthenticatedUser.ID);
        }

        /// <summary>
        /// Get all the user accounts from a specific type, from a specific user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <param name="accountType">with the account type</param>
        /// <returns>LinkedList with all the user accounts</returns>
        public static LinkedList<Account> GetUserAccountsFromType(int userId, String accountType) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);
            XmlNodeList nodes = ConfigurationManager._getNodes("/{0}Config/{0}Users/{0}User[@id=" + userId + "]/{0}Accounts/{0}Account" + ((accountType.Equals("")) ? "" : "[{0}Type='" + accountType + "']"));
            LinkedList<Account> accounts = new LinkedList<Account>();
            if(nodes != null && nodes.Count > 0) {
                Account account = null;
                foreach(XmlNode child in nodes) {
                    if(child.Attributes["id"] != null) {
                        account = ConfigurationManager.GetUserAccount(userId, int.Parse(child.Attributes["id"].Value));

                        if(account != null) {
                            accounts.AddLast(account);
                        }
                    }
                }

            }
            return accounts;
        }



        /// <summary>
        /// Get all the user accounts from a specific type, from the authenticated user
        /// </summary>
        /// <param name="accountType">with the account type</param>
        /// <returns>LinkedList with all the user accounts</returns>
        public static LinkedList<Account> GetUserAccountsFromType(String accountType) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return new LinkedList<Account>();

            return ConfigurationManager.GetUserAccountsFromType(ConfigurationManager.AuthenticatedUser.ID, accountType);
        }

        /// <summary>
        /// Get a specific user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <returns>An User object</returns>
        public static User GetUser(int userId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]");
            if(node != null) {
                User user = new User();

                if(node.Attributes["id"] != null) {
                    user.ID = int.Parse(node.Attributes["id"].Value);
                }

                foreach(XmlNode child in node.ChildNodes) {
                    switch(child.Name) {
                        case "Username":
                            user.Username = child.InnerText;
                            break;

                        case "Password":
                            user.SetEncryptedPassword(child.InnerText);
                            break;
                    }
                }

                LinkedList<Account> accounts = ConfigurationManager.GetUserAccounts(userId);

                foreach(Account account in accounts) {
                    user.AddAccount(account);
                }

                return user;
            }

            return null;
        }

        /// <summary>
        /// Get all the users from the XML file
        /// </summary>
        /// <returns>LinkedList with all the user accounts</returns>
        public static LinkedList<User> GetUsers() {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            XmlNodeList nodes = ConfigurationManager._getNodes("/{0}Config/{0}Users/{0}User");
            if(nodes != null && nodes.Count > 0) {
                LinkedList<User> users = new LinkedList<User>();
                User user = null;
                foreach(XmlNode child in nodes) {
                    if(child.Attributes["id"] != null) {
                        user = ConfigurationManager.GetUser(int.Parse(child.Attributes["id"].Value));

                        if(user != null) {
                            users.AddLast(user);
                        }
                    }
                }

                return users;
            }

            return null;
        }

        /// <summary>
        /// Authenticate the user against the user accounts on XML file
        /// </summary>
        /// <param name="username">with the username of the user</param>
        /// <param name="password">with the password of the user</param>
        /// <param name="remember">To remember this user (set this account as default on the XML file)</param>
        /// <returns>boolean true if the authentication was successfuly, false otherwise</returns>
        public static Boolean AuthenticateUser(String username, String password, Boolean remember) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            LinkedList<User> users = ConfigurationManager.GetUsers();
            if(users != null) {
                foreach(User user in users) {
                    if(user.AuthenticateAgainst(username, password)) {
                        ConfigurationManager cm = ConfigurationManager.Instance;
                        cm._authenticatedUser = user;
                        ConfigurationManager.DefaultUserId = ((remember) ? user.ID : -1);
                        ConfigurationManager.UserLoggedIn(user);

                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Authenticate the user against the user accounts on XML file
        /// </summary>
        /// <param name="username">with the username of the user</param>
        /// <param name="password">with the password of the user</param>
        /// <returns>boolean true if the authentication was successfuly, false otherwise</returns>
        public static Boolean AuthenticateUser(String username, String password) {
            return ConfigurationManager.AuthenticateUser(username, password, false);
        }

        /// <summary>
        /// Authenticate the default user in the system
        /// </summary>
        /// <returns>boolean true if the authentication was successfuly, false otherwise</returns>
        public static Boolean AuthenticateUser() {
            int userId = ConfigurationManager.DefaultUserId;
            if(userId < 0)
                return false;

            User user = ConfigurationManager.GetUser(userId);
            if(user == null)
                return false;

            ConfigurationManager cm = ConfigurationManager.Instance;
            cm._authenticatedUser = user;
            ConfigurationManager.DefaultUserId = userId;
            ConfigurationManager.UserLoggedIn(user);

            return true;
        }

        /// <summary>
        /// Get the authenticated user account
        /// </summary>
        /// <returns>The authenticated User instance</returns>
        public static User AuthenticatedUser {
            get {
                if(ConfigurationManager.hasValidationErrors())
                    throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

                ConfigurationManager cm = ConfigurationManager.Instance;
                return cm._authenticatedUser;
            }
        }

        /// <summary>
        /// Is a valid user authenticated
        /// </summary>
        /// <returns>boolean true if it is, false otherwise</returns>
        public static Boolean IsAuthenticated {
            get {
                if(ConfigurationManager.hasValidationErrors())
                    throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

                return (ConfigurationManager.AuthenticatedUser!=null && ConfigurationManager.AuthenticatedUser.ID>0);
            }
        }

        /// <summary>
        /// Logoff an user account
        /// </summary>
        public static void DeAuthenticatedUser() {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);
            
            ConfigurationManager cm = ConfigurationManager.Instance;
            User user = cm._authenticatedUser;
            cm._authenticatedUser = null;
            ConfigurationManager.DefaultUserId = -1;
            if(user != null) {
                ConfigurationManager.UserLoggedOut(user);
            }
        }

        /// <summary>
        /// Verify if a specific account exists for an user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <param name="accountId">with the account identifier</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UserAccountExists(int userId, int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]/{0}Accounts/{0}Account[@id=" + accountId + "]");
            if(node != null) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verify if a specific account exists for the authenticated user
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <param name="accountId">with the account identifier</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UserAccountExists(int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.UserAccountExists(ConfigurationManager.AuthenticatedUser.ID, accountId);
        }

        /// <summary>
        /// Add an account to an user profile
        /// </summary>
        /// <param name="userId">user identifier to add the account to</param>
        /// <param name="account">the account to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        private static Boolean _addUserAccount(int userId, Account account) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            // if we have a valid ID, let's check if it exists
            if(account.ID > -1 && ConfigurationManager.UserAccountExists(userId, account.ID)) {
                return false;
            }

            // it's a new account, so get the first available account ID
            if(account.ID < 0) {
                account.ID = 0;
                while(ConfigurationManager.UserAccountExists(userId, ++account.ID));
            }

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]/{0}Accounts");
            if(node != null) {

                try {
                    XmlNode accountNode = node.OwnerDocument.CreateElement("Account", node.NamespaceURI);
                    XmlAttribute accountAttribute = null;
                    XmlNode accountChildNode = null;

                    // account id
                    accountAttribute = accountNode.OwnerDocument.CreateAttribute("id");
                    accountAttribute.Value = account.ID.ToString();
                    accountNode.Attributes.Append(accountAttribute);

                    // account name
                    accountChildNode = node.OwnerDocument.CreateElement("Name", node.NamespaceURI);
                    accountChildNode.InnerText = account.Name;
                    accountNode.AppendChild(accountChildNode);

                    // account type
                    accountChildNode = node.OwnerDocument.CreateElement("Type", node.NamespaceURI);
                    accountChildNode.InnerText = account.Type;
                    accountNode.AppendChild(accountChildNode);

                    // account state
                    accountChildNode = node.OwnerDocument.CreateElement("Enabled", node.NamespaceURI);
                    accountChildNode.InnerText = account.Enabled ? "true" : "false";
                    accountNode.AppendChild(accountChildNode);

                    // options
                    LinkedList<String> optionsNames = account.GetOptionsNames();
                    foreach(String optionName in optionsNames) {
                        accountChildNode = node.OwnerDocument.CreateElement("Option", node.NamespaceURI);
                        accountChildNode.InnerText = account.getOption(optionName);

                        accountAttribute = accountChildNode.OwnerDocument.CreateAttribute("name");
                        accountAttribute.Value = optionName;
                        accountChildNode.Attributes.Append(accountAttribute);

                        accountNode.AppendChild(accountChildNode);
                    }

                    // last update
                    if(account.LastUpdate != DateTime.MinValue) {
                        accountChildNode = node.OwnerDocument.CreateElement("LastUpdate", node.NamespaceURI);
                        accountChildNode.InnerText = account.LastUpdate.ToString("s");
                        accountNode.AppendChild(accountChildNode);
                    }

                    node.AppendChild(accountNode);

                    // Save the changes
                    if(ConfigurationManager.Save()) {
                        return true;
                    } else {
                        node.RemoveChild(accountNode);
                        accountNode.RemoveAll();
                    }
                } catch {
                }
            }

            return false;
        }

        /// <summary>
        /// Add an account to an user profile
        /// </summary>
        /// <param name="userId">user identifier to add the account to</param>
        /// <param name="account">the account to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean AddUserAccount(int userId, Account account) {
            if(ConfigurationManager._addUserAccount(userId, account)) {
                ConfigurationManager.UserAccountCreated(account);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add an account to the authenticated user profile
        /// </summary>
        /// <param name="account">the account to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean AddUserAccount(Account account) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.AddUserAccount(ConfigurationManager.AuthenticatedUser.ID, account);
        }

        /// <summary>
        /// Remove an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to remote the account from</param>
        /// <param name="account">the account to remove</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        private static Boolean _deleteUserAccount(int userId, int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            // if we have a valid ID, let's check if it exists

            if(accountId > -1 && ConfigurationManager.UserAccountExists(userId, accountId)) {
                XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]/{0}Accounts/{0}Account[@id=" + accountId + "]");
                if(node != null) {
                    try {
                        node.ParentNode.RemoveChild(node);

                        // Save the changes
                        if(ConfigurationManager.Save()) {

                            return true;
                        } else {
                            node.ParentNode.AppendChild(node);
                        }
                    } catch {
                        //MessageBox.Show(ex.Message + ex.StackTrace);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Remove an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to remote the account from</param>
        /// <param name="account">the account to remove</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean DeleteUserAccount(int userId, int accountId) {
            Account account = ConfigurationManager.GetUserAccount(accountId);
            if(ConfigurationManager._deleteUserAccount(userId, accountId)) {
                ConfigurationManager.UserAccountDeleted(account);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an account from the authenticated user profile
        /// </summary>
        /// <param name="account">the account to remove</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean DeleteUserAccount(int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.DeleteUserAccount(ConfigurationManager.AuthenticatedUser.ID, accountId);
        }

        /// <summary>
        /// Update an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to update the account from</param>
        /// <param name="account">the account to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean _updateUserAccount(int userId, Account account) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            // if we have a valid ID and that account exists, delete it
            lock(_updatelock) {
                if(account.ID > -1 && ConfigurationManager.UserAccountExists(userId, account.ID)) {
                    ConfigurationManager._deleteUserAccount(userId, account.ID);
                }

                return ConfigurationManager._addUserAccount(userId, account);
            }
        }

        /// <summary>
        /// Update an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to update the account from</param>
        /// <param name="account">the account to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccount(int userId, Account account) {
            if(ConfigurationManager._updateUserAccount(userId, account)) {
                ConfigurationManager.UserAccountUpdated(account);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update the last update date for an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to update the account from</param>
        /// <param name="accountId">account identifier to update</param>
        /// <param name="lastUpdate">last update date</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccountLastUpdate(int userId, int accountId, DateTime lastUpdate) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            Account account = ConfigurationManager.GetUserAccount(userId, accountId);

            if(account == null)
                return false;

            account.LastUpdate = lastUpdate;

            return ConfigurationManager.UpdateUserAccount(userId, account);
        }

        /// <summary>
        /// Update the last update date for an account of the authenticated user
        /// </summary>
        /// <param name="accountId">account identifier to update</param>
        /// <param name="lastUpdate">last update date</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccountLastUpdate(int accountId, DateTime lastUpdate) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.UpdateUserAccountLastUpdate(ConfigurationManager.AuthenticatedUser.ID, accountId, lastUpdate);
        }

        /// <summary>
        /// Update the last update date to the current datetime for an account from an user profile
        /// </summary>
        /// <param name="userId">user identifier to update the account from</param>
        /// <param name="accountId">account identifier to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccountLastUpdate(int userId, int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            return ConfigurationManager.UpdateUserAccountLastUpdate(userId, accountId, DateTime.Now);
        }

        /// <summary>
        /// Update the last update date to the current datetime for an account of the authenticated user
        /// </summary>
        /// <param name="accountId">account identifier to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccountLastUpdate(int accountId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.UpdateUserAccountLastUpdate(ConfigurationManager.AuthenticatedUser.ID, accountId, DateTime.Now);
        }

        /// <summary>
        /// Update an account on the authenticated user
        /// </summary>«
        /// <param name="account">the account to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUserAccount(Account account) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.UpdateUserAccount(ConfigurationManager.AuthenticatedUser.ID, account);
        }

        /// <summary>
        /// Verify if a specific user exists
        /// </summary>
        /// <param name="userId">with the user identifier</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UserExists(int userId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]");
            if(node != null) {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Add an user to configuration file
        /// </summary>
        /// <param name="user">the user to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        private static Boolean _addUser(User user) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            // if we have a valid ID, let's check if it exists
            if(user.ID > -1 && ConfigurationManager.UserExists(user.ID)) {
                return false;
            }

            // it's a new account, so get the first available account ID
            if(user.ID < 0) {
                user.ID = 0;
                while(ConfigurationManager.UserExists(++user.ID));
            }

            XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users");
            if(node != null) {

                try {
                    XmlNode userNode = node.OwnerDocument.CreateElement("User", node.NamespaceURI);
                    XmlAttribute userAttribute = null;
                    XmlNode userChildNode = null;

                    // account id
                    userAttribute = userNode.OwnerDocument.CreateAttribute("id");
                    userAttribute.Value = user.ID.ToString();
                    userNode.Attributes.Append(userAttribute);

                    // Username
                    userChildNode = node.OwnerDocument.CreateElement("Username", node.NamespaceURI);
                    userChildNode.InnerText = user.Username;
                    userNode.AppendChild(userChildNode);

                    // Password
                    userChildNode = node.OwnerDocument.CreateElement("Password", node.NamespaceURI);
                    userChildNode.InnerText = user.Password;
                    userNode.AppendChild(userChildNode);

                    // Accounts
                    userChildNode = node.OwnerDocument.CreateElement("Accounts", node.NamespaceURI);
                    userNode.AppendChild(userChildNode);

                    node.AppendChild(userNode);

                    // Save the changes
                    if(ConfigurationManager.Save()) {
                        // Add the accounts
                        LinkedList<Account> accounts = user.GetAccounts();
                        foreach(Account account in accounts) {
                            if(!ConfigurationManager.AddUserAccount(user.ID, account)) {
                                return false;
                            }
                        }

                        return true;
                    } else {
                        node.RemoveChild(userNode);
                        userNode.RemoveAll();
                    }
                } catch {
                }
            }

            return false;
        }

        /// <summary>
        /// Add an user to configuration file
        /// </summary>
        /// <param name="user">the user to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean AddUser(User user) {
            if(ConfigurationManager._addUser(user)) {
                ConfigurationManager.UserCreated(user);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove an user from the configuration file
        /// </summary>
        /// <param name="userId">user identifier to add the account to</param>
        /// <param name="account">the account to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        private static Boolean _deleteUser(int userId) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            // if we have a valid ID, let's check if it exists
            if(userId > -1 && ConfigurationManager.UserExists(userId)) {
                XmlNode node = ConfigurationManager._getNode("/{0}Config/{0}Users/{0}User[@id=" + userId + "]");
                if(node != null) {
                    try {
                        if(ConfigurationManager.DefaultUserId == userId) {
                            ConfigurationManager.DefaultUserId = -1;
                        }
                        node.ParentNode.RemoveChild(node);

                        if(ConfigurationManager.Save()) {
                            return true;
                        } else {
                            node.ParentNode.AppendChild(node);
                        }
                    } catch {
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Remove an user from the configuration file
        /// </summary>
        /// <param name="userId">user identifier to add the account to</param>
        /// <param name="account">the account to add</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean DeleteUser(int userId) {
            User user = ConfigurationManager.GetUser(userId);
            if(ConfigurationManager._deleteUser(userId)) {
                ConfigurationManager.UserDeleted(user);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update an user on the configuration XML file
        /// </summary>
        /// <param name="user">the user to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        private static Boolean _updateUser(User user) {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            Boolean restoreDefaultUserId = false;
            Boolean result = false;
            lock(_updatelock) {
                // if we have a valid ID and that user exists, delete it
                if(user.ID > -1 && ConfigurationManager.UserExists(user.ID)) {
                    if(ConfigurationManager.DefaultUserId == user.ID) {
                        ConfigurationManager.DefaultUserId = -1;
                        restoreDefaultUserId = true;
                    }
                    ConfigurationManager._deleteUser(user.ID);
                }

                result = ConfigurationManager._addUser(user);
                if(result && restoreDefaultUserId) {
                    ConfigurationManager.DefaultUserId = user.ID;
                }
            }
            return result;
        }

        /// <summary>
        /// Update an user on the configuration XML file
        /// </summary>
        /// <param name="user">the user to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUser(User user) {
            if(ConfigurationManager._updateUser(user)) {
                ConfigurationManager.UserUpdated(user);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Update the authenticated user on the configuration XML file
        /// </summary>
        /// <param name="user">the user to update</param>
        /// <returns>Boolean true on success, false otherwise</returns>
        public static Boolean UpdateUser() {
            if(ConfigurationManager.hasValidationErrors())
                throw new XmlSchemaValidationException(Properties.Resources.ErrorOnXmlValidationAgainstTheSchema);

            if(!ConfigurationManager.IsAuthenticated)
                return false;

            return ConfigurationManager.UpdateUser(ConfigurationManager.AuthenticatedUser);
        }

        /// <summary>
        /// Load an external configuration file to the application data directory
        /// </summary>
        /// <param name="xmlFilename">With the file path and name to load the configurations from</param>
        /// <returns>Boolean true if the load was successful, false otherwise</returns>
        public static Boolean LoadExternalXmlFile(String xmlFilename) {

            if(!File.Exists(xmlFilename))
                return false;

            ConfigurationManager cm = ConfigurationManager.Instance;

            String destinationXmlFilename = cm._initializeConfigXmlFile();

            if(destinationXmlFilename == null)
                return false;

            String xmlSchemaFileName = cm._initializeConfigXsdFile();

            if(xmlSchemaFileName==null || !File.Exists(xmlSchemaFileName))
                return false;

            XmlDocument doc = new XmlDocument();
            doc.Schemas.Add(null, xmlSchemaFileName);
            doc.Load(xmlFilename);
            cm._xmlValidationErrors.Clear();
            cm._xmlValidationWarnings.Clear();
            doc.Validate(cm._xmlValidateEvent);

            if(!ConfigurationManager.hasValidationErrors()) {
                doc.Save(destinationXmlFilename);
            }

            return ConfigurationManager.Reload();
        }

        /// <summary>
        /// Export the configuration file
        /// </summary>
        /// <param name="xmlFilename">With the file path and name to export the configurations to</param>
        /// <returns>Boolean true if the export was successful, false otherwise</returns>
        public static Boolean ExportXmlFileTo(String xmlFilename) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            if(!ConfigurationManager.Reload())
                return false;

            ConfigurationManager.Validate();
            if(!ConfigurationManager.hasValidationErrors()) {
                try {
                    cm._xmlDocument.Save(xmlFilename);
                    return true;
                } catch {
                }
            }
            return false;
        }

        /// <summary>
        /// Add a listenner to the list to be notified on configuration updates
        /// </summary>
        /// <param name="listenner">listener to be added</param>
        public static void AddConfigListenner(IConfigListenner listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._configListenners.Contains(listenner)) {
                cm._configListenners.Add(listenner);
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listenner">The listenner to be removed</param>
        public static void RemoveConfigListenner(IConfigListenner listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._configListenners.Contains(listenner)) {
                cm._configListenners.Remove(listenner);
            }
        }

        /// <summary>
        /// Execute the ConfigUpdated for each listenner
        /// </summary>
        public static void ConfigurationUpdated() {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IConfigListenner listenner in cm._configListenners) {
                try {
                    listenner.ConfigUpdated();
                } catch {
                }
            }
        }

        /// <summary>
        /// Add a listenner to the list to be notified on users updates
        /// </summary>
        /// <param name="listenner">listener to be added</param>
        public static void AddUsersListenner(IiBoardUserChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._usersListenners.Contains(listenner)) {
                cm._usersListenners.Add(listenner);
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listenner">The listenner to be removed</param>
        public static void RemoveUsersListenner(IiBoardUserChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._usersListenners.Contains(listenner)) {
                cm._usersListenners.Remove(listenner);
            }
        }

        /// <summary>
        /// Execute the UserUpdated for each listenner
        /// </summary>
        public static void UserUpdated(User user) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserChanges listenner in cm._usersListenners) {
                try {
                    listenner.UserUpdated(user);
                } catch {
                }
            }
        }

        /// <summary>
        /// Execute the UserCreated for each listenner
        /// </summary>
        public static void UserCreated(User user) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserChanges listenner in cm._usersListenners) {
                try {
                    listenner.UserCreated(user);
                } catch {
                }
            }
        }

        /// <summary>
        /// Execute the UserDeleted for each listenner
        /// </summary>
        public static void UserDeleted(User user) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserChanges listenner in cm._usersListenners) {
                try {
                    listenner.UserDeleted(user);
                } catch {
                }
            }
        }

        /// <summary>
        /// Add a listenner to the list to be notified on user account updates
        /// </summary>
        /// <param name="listenner">listener to be added</param>
        public static void AddUserAccountsListenner(IiBoardUserAccountChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._userAccountsListenners.Contains(listenner)) {
                cm._userAccountsListenners.Add(listenner);
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listenner">The listenner to be removed</param>
        public static void RemoveUserAccountsListenner(IiBoardUserAccountChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._userAccountsListenners.Contains(listenner)) {
                cm._userAccountsListenners.Remove(listenner);
            }
        }

        /// <summary>
        /// Execute the UserAccountUpdated for each listenner
        /// </summary>
        public static void UserAccountUpdated(Account account) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserAccountChanges listenner in cm._userAccountsListenners) {
                try {
                    listenner.UserAccountUpdated(account);
                } catch {
                }
            }
        }

        /// <summary>
        /// Execute the UserAccountCreated for each listenner
        /// </summary>
        public static void UserAccountCreated(Account account) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserAccountChanges listenner in cm._userAccountsListenners) {
                try {
                    listenner.UserAccountCreated(account);
                } catch {
                }
            }
        }

        /// <summary>
        /// Execute the UserAccountDeleted for each listenner
        /// </summary>
        public static void UserAccountDeleted(Account account) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardUserAccountChanges listenner in cm._userAccountsListenners) {
                try {
                    listenner.UserAccountDeleted(account);
                } catch {
                }
            }
        }

        /// <summary>
        /// Add a listenner to the list to be notified on user login/logout operations
        /// </summary>
        /// <param name="listenner">listener to be added</param>
        public static void AddAuthenticationListenner(IiBoardAuthenticationChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._loginListenners.Contains(listenner)) {
                cm._loginListenners.Add(listenner);
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listenner">The listenner to be removed</param>
        public static void RemoveAuthenticationListenner(IiBoardAuthenticationChanges listenner) {
            ConfigurationManager cm = ConfigurationManager.Instance;
            if(!cm._loginListenners.Contains(listenner)) {
                cm._loginListenners.Remove(listenner);
            }
        }

        /// <summary>
        /// Execute the UserLoggedIn for each listenner
        /// </summary>
        public static void UserLoggedIn(User user) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardAuthenticationChanges listenner in cm._loginListenners) {
                try {
                    listenner.UserLoggedIn(user);
                } catch {
                }
            }
        }

        /// <summary>
        /// Execute the UserLoggedOut for each listenner
        /// </summary>
        public static void UserLoggedOut(User user) {
            ConfigurationManager cm = ConfigurationManager.Instance;

            foreach(IiBoardAuthenticationChanges listenner in cm._loginListenners) {
                try {
                    listenner.UserLoggedOut(user);
                } catch {
                }
            }
        }
        
    }
}
