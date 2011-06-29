using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Windows;

namespace iBoard.Classes.Data {
    
    public class User {
        private int _id = -1;
        private String _username = "";
        private String _password = "";
        private System.Collections.Generic.Dictionary<int, Account> _accounts;
        private const String _key = "1b02rd_p@ssphr2se";


        /// <summary>
        /// Add or replace an account
        /// </summary>
        /// <param name="account">Account to insert or update</param>
        public void AddAccount(Account account) {
            this.UpdateAccount(account);
        }

        /// <summary>
        /// Add or replace an account 
        /// </summary>
        /// <param name="account">Account to insert or update</param>
        public void UpdateAccount(Account account) {
            if(this._accounts.ContainsKey(account.ID)) {
                this._accounts[account.ID] = account;
            } else {
                this._accounts.Add(account.ID, account);
            }
        }

        /// <summary>
        /// Get an account 
        /// </summary>
        /// <param name="accountId">with the account identifier</param>
        /// <returns>the Account</returns>
        public Account GetAccount(int accountId) {
            if(this._accounts.ContainsKey(accountId)) {
                return this._accounts[accountId];
            }
            return null;
        }

        /// <summary>
        /// Get all the user accounts
        /// </summary>
        public System.Collections.Generic.LinkedList<Account> GetAccounts() {
            System.Collections.Generic.LinkedList<Account> accounts = new System.Collections.Generic.LinkedList<Account>();

            foreach(KeyValuePair<int, Account> account in this._accounts) {
                accounts.AddLast(account.Value);
            }
            return accounts;
        }

        /// <summary>
        /// Remove an account from the user accounts
        /// </summary>
        public Boolean RemoveAccount(int id) {
            if(this._accounts.ContainsKey(id)) {
                return this._accounts.Remove(id);
            }
            return false;
        }

        /// <summary>
        /// The user password
        /// </summary>
        public String Password {
            get {
                return this._password;
            }
            set {
                this._password = User.HashPassword(value);
            }
        }

        /// <summary>
        /// The user name
        /// </summary>
        public String Username {
            get {
                return this._username;
            }
            set {
                this._username = value;
            }
        }

        /// <summary>
        /// Set the user encrypted password, bypassing the encription
        /// </summary>
        /// <param name="encryptedPassword"></param>
        public void SetEncryptedPassword(String encryptedPassword) {
            this._password = encryptedPassword;
        }

        /// <summary>
        /// Validate the username and password credentials against the internal User object credentials
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The password</param>
        /// <returns></returns>
        public Boolean AuthenticateAgainst(String username, String password) {
            return (username.Equals(this._username) && User.HashPassword(password).Equals(this._password));
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
        /// Instanciates an User object
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="username">The user name</param>
        /// <param name="password">The user password</param>
        public User(int id, String username, String password)
            : this(username, password) {
            this.ID = id;
        }

        /// <summary>
        /// Instanciates an User object
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The user password</param>
        public User(String username, String password)
            : this(){
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Instanciates an User object
        /// </summary>
        public User() {
            this.ID = -1;
            this.Username = "";
            this.Password = "";
            this._accounts = new System.Collections.Generic.Dictionary<int, Account>();
        }

        /// <summary>
        /// Generate an encrypted user password
        /// </summary>
        /// <param name="password"></param>
        /// <returns>String with the encrypted password</returns>
        public static String HashPassword(String password) {
            SHA512 shm = new SHA512Managed();
            return User._byteArrayToHexString(shm.ComputeHash(Encoding.Default.GetBytes(password)));
        }

        /// <summary>
        /// Convert a byte array to a hexadecimal string
        /// </summary>
        /// <param name="byteArray">to convert</param>
        /// <returns>Hexadecimal string</returns>
        private static String _byteArrayToHexString(byte[] byteArray) {
            StringBuilder sb = new StringBuilder();
            foreach(byte b in byteArray)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        /// <summary>
        /// Convert a hexadecimal string to a byte array
        /// </summary>
        /// <param name="hex">hexadecimal string to convert</param>
        /// <returns>converted byte array</returns>
        /// <seealso cref="http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa-in-c">
        /// Accessed on 2010-12-08: Convert a string to and from a byte array
        /// </seealso>
        private static byte[] _hexStringToByteArray(String hex) {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for(int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        /// <summary>
        /// Generate a passphrase based on the user id
        /// </summary>
        /// <param name="userId">user identifier</param>
        /// <returns>String with the passphrase</returns>
        private String _generatePassphrase(int userId) {
            return User._key + "_" + userId;
        }

        /// <summary>
        /// Encrypt a string with a passphrase with a relation to the user
        /// </summary>
        /// <param name="data">Data to be encrypted</param>
        /// <returns>Encrypted string</returns>
        /// <seealso cref="http://www.dotnetspider.com/resources/38735-Encrypting-Decrypting-string.aspx">
        /// Accessed on 2010-12-08: Reference to encrypt and decrypt a string based on a passphrase
        /// </seealso>
        public String Encrypt(String data) {
            if(this._id > -1) {
                String encryptedData = null;
                String passphrase = this._generatePassphrase(this._id);
                UTF8Encoding utf8enc = new UTF8Encoding();
                MD5CryptoServiceProvider Md5HashProvider = new MD5CryptoServiceProvider();
                byte[] TDESKey = Md5HashProvider.ComputeHash(utf8enc.GetBytes(passphrase));
                TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();
                TDESAlgorithm.Key = TDESKey;
                TDESAlgorithm.Mode = CipherMode.ECB;
                TDESAlgorithm.Padding = PaddingMode.PKCS7;
                byte[] DataToEncrypt = utf8enc.GetBytes(data);
                try {
                    ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                    encryptedData = User._byteArrayToHexString(Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length));
                } finally {
                    // Clear the TripleDes and Hashprovider services of any sensitive information
                    TDESAlgorithm.Clear();
                    Md5HashProvider.Clear();
                }
                return encryptedData;
            }
            return null;
        }

        /// <summary>
        /// Decrypt a string with a passphrase with a relation to the user
        /// </summary>
        /// <param name="data">Data to be decrypted</param>
        /// <returns>Decrypted string</returns>
        /// <seealso cref="http://www.dotnetspider.com/resources/38735-Encrypting-Decrypting-string.aspx">
        /// Accessed on 2010-12-08: Reference to encrypt and decrypt a string based on a passphrase
        /// </seealso>
        public String Decrypt(String encryptedData) {
            if(this._id > -1 && encryptedData!=null && !encryptedData.Equals("")) {
                String decryptedData = null;
                String passphrase = this._generatePassphrase(this._id);
                UTF8Encoding utf8enc = new UTF8Encoding();
                MD5CryptoServiceProvider Md5HashProvider = new MD5CryptoServiceProvider();
                byte[] TDESKey = Md5HashProvider.ComputeHash(utf8enc.GetBytes(passphrase));
                TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();
                TDESAlgorithm.Key = TDESKey;
                TDESAlgorithm.Mode = CipherMode.ECB;
                TDESAlgorithm.Padding = PaddingMode.PKCS7;

                byte[] encryptedByteData = User._hexStringToByteArray(encryptedData);

                try {
                    ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                    decryptedData = utf8enc.GetString(Decryptor.TransformFinalBlock(encryptedByteData, 0, encryptedByteData.Length));
                } finally {
                    TDESAlgorithm.Clear();
                    Md5HashProvider.Clear();
                }
                return decryptedData;
            }
            return null;
        }
    }
}