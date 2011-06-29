using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using iBoard.Classes.Data;

namespace iBoard.Classes.Timeline {

    public class Frame : INotifyPropertyChanged {

        private String _title = "";
        private String _description = "";
        private Uri _url = null;
        private DateTime _date = DateTime.Now;
        private Account _account = null;
        private Int64 _frameId = -1;
        private Uri _iconUrl = new Uri("/iBoard;component/Assets/Icons/moodledefaultavatar.png", UriKind.Relative);

        /// <summary>
        /// Instanciates a new Frame object
        /// </summary>
        /// <param name="account">With the source Account</param>
        /// <param name="title">String with the title</param>
        /// <param name="description">String with the description</param>
        public Frame(Account account, String title, String description) {
            this.FrameAccount = account;
            this.Title = title;
            this.Description = description;

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

        /// <summary>
        /// Instanciates a new Frame object
        /// </summary>
        /// <param name="account">With the source Account</param>
        /// <param name="title">String with the title</param>
        /// <param name="description">String with the description</param>
        /// <param name="date">Datetime with the date of the frame</param>
        public Frame(Account account, String title, String description, DateTime date) 
            : this(account, title, description) {
                this.Date = date;
        }

        /// <summary>
        /// Instanciates a new Frame object
        /// </summary>
        /// <param name="account">With the source Account</param>
        /// <param name="title">String with the title</param>
        /// <param name="description">String with the description</param>
        /// <param name="date">Datetime with the date of the frame</param>
        /// <param name="url">Uri with the URL of the event</param>
        public Frame(Account account, String title, String description, DateTime date, Uri url)
            : this(account, title, description, date) {
                this.Url = url;
        }

        /// <summary>
        /// Instanciates a new Frame object
        /// </summary>
        /// <param name="account">With the source Account</param>
        /// <param name="title">String with the title</param>
        /// <param name="description">String with the description</param>
        /// <param name="date">Datetime with the date of the frame</param>
        /// <param name="url">Uri with the URL of the event</param>
        /// <param name="id">Integer with the frame identification</param>
        public Frame(Account account, String title, String description, DateTime date, Uri url, Int64 id)
            : this(account, title, description, date, url) {
                this.ID = id;
        }

        /// <summary>
        /// Get or set the frame title
        /// </summary>
        public String Title {
            get {
                return this._title;
            }
            set {
                this._title = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Title"));
                }
            }
        }

        /// <summary>
        /// Get or set the frame Description
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
        /// Get or set the frame Url
        /// </summary>
        public Uri Url {
            get {
                return this._url;
            }
            set {
                this._url = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Url"));
                }
            }
        }

        /// <summary>
        /// Get or set the frame Date
        /// </summary>
        public DateTime Date {
            get {
                return this._date;
            }
            set {
                this._date = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Date"));
                }
            }
        }

        /// <summary>
        /// Get or set the frame identifier
        /// </summary>
        public Int64 ID {
            get {
                return this._frameId;
            }
            set {
                this._frameId = value;
                if(this.PropertyChanged != null) {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ID"));
                }
            }
        }

        /// <summary>
        /// Get or set the frame source Account
        /// </summary>
        public Account FrameAccount {
            get {
                return this._account;
            }
            set {
                this._account = value;
                if (this.PropertyChanged != null){
                    this.PropertyChanged(this, new PropertyChangedEventArgs("FrameAccount"));
                }
            }
        }



        /// <summary>
        /// Get or set the frame image uri
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
