using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using iBoard.Classes.Configuration;
using System.Collections;
using System.Windows.Threading;
using iBoard.Classes.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Data;
using System.ComponentModel;

namespace iBoard.Classes.Timeline {

    /// <summary>
    /// Interface to be implemented by the listeners to react on timeline events
    /// </summary>
    public interface ITimelineUpdateEventListener {
        /// <summary>
        /// Should be implemented by listeners to react to the timeline update event
        /// </summary>
        void TimelineUpdate();
    }

    /// <summary>
    /// This class manages the timeline events for each source
    /// </summary>
    public class TimelineManager : ObservableCollection<Frame>, IiBoardUserChanges, IiBoardAuthenticationChanges, IiBoardUserAccountChanges {
        public const String UPDATEEVENT = "update";

        private static TimelineManager _instance = null;
        static readonly object _padlock = new object();
        private ArrayList _listeners;
        private DispatcherTimer _timer;

        /// <summary>
        /// Constructor
        /// </summary>
        private TimelineManager() {
            this._listeners = new ArrayList();

            // update timer
            this._timer = new DispatcherTimer();
            this._timer.Interval = TimeSpan.FromSeconds(5*60);
            this._timer.Tick += new EventHandler(delegate(object s, EventArgs a) {
                TimelineManager.Update();
            });
            this._timer.Start();

            ConfigurationManager.AddAuthenticationListenner(this);
            ConfigurationManager.AddUserAccountsListenner(this);
            ConfigurationManager.AddUsersListenner(this);
        }

        /// <summary>
        /// Get a TimelineManager instance
        /// </summary>
        public static TimelineManager Instance {
            get {
                if(_instance == null) {
                    lock(_padlock) {
                        if(_instance == null) {
                            _instance = new TimelineManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get a TimelineManager instance
        /// </summary>
        public static TimelineManager GetInstance() {
            return TimelineManager.Instance;
        }

        /// <summary>
        /// Start the timeline auto update
        /// </summary>
        public static void Start() {
            TimelineManager.Instance._timer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds">With the update interval</param>
        public static void Start(double seconds) {
            DispatcherTimer timer = TimelineManager.Instance._timer;
            if(timer.IsEnabled)
                timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(seconds);
            timer.Start();
        }

        /// <summary>
        /// Stop the timeline auto update
        /// </summary>
        public static void Stop() {
            TimelineManager.Instance._timer.Stop();
        }

        /// <summary>
        /// Get the auto update state
        /// </summary>
        /// <returns>Boolean true if auto update is enabled, false otherwise</returns>
        public static Boolean IsAutoUpdating() {
            return TimelineManager.Instance._timer.IsEnabled;
        }

        /// <summary>
        /// Get the current auto update interval
        /// </summary>
        /// <returns>TimeSpan with the update interval</returns>
        public static TimeSpan UpdateInterval() {
            return TimelineManager.Instance._timer.Interval;
        }

        /// <summary>
        /// Add a frame to the list
        /// </summary>
        /// <param name="frame"></param>
        public static Boolean AddFrame(Frame frame) {
            if(!TimelineManager.FrameExists(frame)) {
                TimelineManager.Instance.Add(frame);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check is a frame exists in the list
        /// </summary>
        /// <param name="frame"></param>
        public static Boolean FrameExists(Frame frame) {
            TimelineManager timeline = TimelineManager.Instance;
            foreach(Frame frm in timeline.Items) {
                if(frame.ID == frm.ID && frame.Date == frm.Date && frame.FrameAccount.ID == frm.FrameAccount.ID && frame.Description == frm.Description && frame.Title == frm.Title) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Verify if a frame of a specific account type, with the specified ID exists
        /// </summary>
        /// <param name="accountType">String with the account type</param>
        /// <param name="frameId">integer with the frame identifier to look for</param>
        /// <returns>Boolean true if such frame exists, false otherwise</returns>
        public static Boolean ContainsFrame(String accountType, int frameId) {
            TimelineManager timeline = TimelineManager.Instance;
            foreach(Frame frame in timeline.Items) {
                if(frame.ID == frameId && frame.FrameAccount.Type.Equals(accountType)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a listenner to the list to be notified on timeline update event
        /// </summary>
        /// <param name="listener">listener to be added</param>
        public static void AddTimelineListenner(ITimelineUpdateEventListener listener) {
            TimelineManager timeline = TimelineManager.Instance;
            if(!timeline._listeners.Contains(listener)) {
                timeline._listeners.Add(listener);
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listener">The listenner to be removed</param>
        public static void RemoveTimelineListenner(ITimelineUpdateEventListener listener) {
            TimelineManager timeline = TimelineManager.Instance;
            if(!timeline._listeners.Contains(listener)) {
                timeline._listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Execute the TimelineUpdate for each listenner
        /// </summary>
        public static void Update() {
            TimelineManager timeline = TimelineManager.Instance;
            foreach(ITimelineUpdateEventListener listener in timeline._listeners) {
                listener.TimelineUpdate();
            }
        }

        /// <summary>
        /// Called by the each module to report status updates
        /// </summary>
        /// <param name="status"></param>
        public static void ReportProgress(Status status) {
            StatusManager.UpdateStatus(status);
        }

        /// <summary>
        /// Register an event on the database
        /// </summary>
        public static void RegisterEvent(Account account, String sEvent) {
            
            BackgroundWorker bgWorker = new BackgroundWorker();

            bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args) {
                if(ConfigurationManager.UserAccountExists(account.ID)) {
                    
                    SqlConnection connection = new SqlConnection();
                    connection.ConnectionString = Properties.Settings.Default.iBoardConnectionString;
                    
                    try {
                        connection.Open();

                        SqlCommand cmd = connection.CreateCommand();
                        cmd.Connection = connection;
                        cmd.CommandText = "INSERT INTO [logs_tbl] (userId, accountId, accountType, event) VALUES (" + ConfigurationManager.AuthenticatedUser.ID + ", " + account.ID + ", '" + account.Type + "', '" + sEvent + "')";
                        cmd.ExecuteNonQuery();
                        
                    } catch(Exception) {
                        //MessageBox.Show(ex.Message + ": "+ex.StackTrace);
                    } finally {
                        if(connection.State == ConnectionState.Open) {
                            connection.Close();
                        }
                    }
                }
            });

            bgWorker.RunWorkerAsync(account);
        }

        /// <summary>
        /// Register an event on the database
        /// </summary>
        /// <param name="account"></param>
        public static void RegisterEvent(Account account) {
            TimelineManager.RegisterEvent(account, TimelineManager.UPDATEEVENT);
        }

        /// <summary>
        /// Get the cached frames from DB
        /// </summary>
        /// <param name="userId">integer with the user identifier</param>
        /// <param name="account">Account instance</param>
        public static LinkedList<Frame> GetCachedFramesFromDB(int userId, Account account) {
            LinkedList<Frame> frames = new LinkedList<Frame>();

            // Load the cached data
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = Properties.Settings.Default.iBoardConnectionString;
            try {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.Connection = connection;

                cmd.Parameters.Clear();

                cmd.Parameters.Add("@accountId", SqlDbType.Int);
                cmd.Parameters["@accountId"].Value = account.ID;
                cmd.Parameters.Add("@userId", SqlDbType.Int);
                cmd.Parameters["@userId"].Value = userId;

                cmd.CommandText = "SELECT date, title, description, url, frameId, iconUrl FROM [frames_cache_tbl] WHERE accountId = @accountId AND userId = @userId ORDER BY date DESC ";

                SqlDataReader reader = cmd.ExecuteReader();
                while(reader.Read()) {
                    Frame frame = new Frame(account, reader.GetString(1), reader.GetString(2), reader.GetDateTime(0), (reader.GetString(3).Equals("") ? null : new Uri(reader.GetString(3))), reader.GetInt64(4));
                    if(account.Type == Account.TWITTERTYPE) {
                        try {
                            frame.ImageUrl = new Uri(reader.GetString(5));
                        } catch(System.UriFormatException) {
                        }
                    }
                    frames.AddLast(frame);
                }
                reader.Close();

            } catch {
                //MessageBox.Show(ex.Message+ ex.StackTrace);
            } finally {
                if(connection.State == ConnectionState.Open) {
                    connection.Close();
                }
            }
            return frames;
        }

        /// <summary>
        /// Get the cached frames from DB
        /// </summary>
        /// <param name="account">Account instance</param>
        public static LinkedList<Frame> GetCachedFramesFromDB(Account account) {
            if(ConfigurationManager.IsAuthenticated) {
                return TimelineManager.GetCachedFramesFromDB(ConfigurationManager.AuthenticatedUser.ID, account);
            }
            return new LinkedList<Frame>();
        }

        /// <summary>
        /// Cache the frames on the database
        /// </summary>
        /// <param name="userId">integer with the user identifier</param>
        /// <param name="account">Account instance</param>
        /// <param name="frames">LinkedList<Frame> instance of frames to cache</param>
        public static void CacheFramesOnDB(int userId, Account account, LinkedList<Frame> frames) {
            BackgroundWorker bgWorker = new BackgroundWorker();

            bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender2, DoWorkEventArgs args2) {
                SqlConnection connection = new SqlConnection();
                connection.ConnectionString = Properties.Settings.Default.iBoardConnectionString;
                try {
                    connection.Open();

                    SqlCommand cmd = connection.CreateCommand();
                    cmd.Connection = connection;
                    cmd.Transaction = connection.BeginTransaction("InsertFrameForAccount" + account.ID);

                    try {
                        foreach(Frame frame in frames) {

                            cmd.Parameters.Clear();

                            cmd.Parameters.Add("@date", SqlDbType.DateTime);
                            cmd.Parameters["@date"].Value = frame.Date;

                            cmd.Parameters.Add("@title", SqlDbType.VarChar);
                            cmd.Parameters["@title"].Value = frame.Title;

                            cmd.Parameters.Add("@description", SqlDbType.Text);
                            cmd.Parameters["@description"].Value = frame.Description;

                            cmd.Parameters.Add("@url", SqlDbType.NVarChar);
                            cmd.Parameters["@url"].Value = ((frame.Url != null) ? frame.Url.ToString() : "");

                            cmd.Parameters.Add("@userId", SqlDbType.Int);
                            cmd.Parameters["@userId"].Value = userId;

                            cmd.Parameters.Add("@accountId", SqlDbType.Int);
                            cmd.Parameters["@accountId"].Value = account.ID;

                            cmd.Parameters.Add("@accountType", SqlDbType.VarChar);
                            cmd.Parameters["@accountType"].Value = account.Type;

                            cmd.Parameters.Add("@frameId", SqlDbType.BigInt);
                            cmd.Parameters["@frameId"].Value = frame.ID;

                            cmd.Parameters.Add("@iconUrl", SqlDbType.NVarChar);
                            cmd.Parameters["@iconUrl"].Value = frame.ImageUrl.ToString();

                            cmd.CommandText = "INSERT INTO [frames_cache_tbl] (date, title, description, url, accountId, frameId, iconUrl, userId, accountType) " +
                                " SELECT @date, @title, @description, @url, @accountId, @frameId, @iconUrl, @userId, @accountType " +
                                " WHERE NOT EXISTS (SELECT * FROM [frames_cache_tbl] WHERE " +
                                   " date = @date AND " +
                                   " title = @title AND " +
                                   " description LIKE @description AND " +
                                   ((frame.Url != null) ? " url = @url AND " : "") +
                                   " accountId = @accountId AND " +
                                   " frameId = @frameId AND " +
                                   " userId = @userId AND " +
                                   " accountType LIKE @accountType AND " +
                                   " iconUrl = @iconUrl) ";
                            cmd.ExecuteNonQuery();
                        }
                        cmd.Transaction.Commit();

                    } catch(Exception ex) {
                        cmd.Transaction.Rollback();
                        throw ex;
                    }
                } catch {
                    //MessageBox.Show(ex.Message + ": " + ex.StackTrace);
                } finally {
                    if(connection.State == ConnectionState.Open) {
                        connection.Close();
                    }
                }

            });
            bgWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Cache the frames on the database
        /// </summary>
        /// <param name="account">Account instance</param>
        /// <param name="frames">LinkedList<Frame> instance of frames to cache</param>
        public static void CacheFramesOnDB(Account account, LinkedList<Frame> frames) {
            if(ConfigurationManager.IsAuthenticated) {
                TimelineManager.CacheFramesOnDB(ConfigurationManager.AuthenticatedUser.ID, account, frames);
            }
        }

        /// <summary>
        /// Remove the cached frames from DB
        /// </summary>
        /// <param name="userId">integer with the user identification</param>
        /// <param name="accountId">integer with the account identification</param>
        public static void RemoveCachedFramesFromDBFromAccount(int userId, int accountId) {
            SqlConnection connection = new SqlConnection();
            connection.ConnectionString = Properties.Settings.Default.iBoardConnectionString;
            try {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.Connection = connection;

                cmd.Parameters.Clear();
                cmd.Parameters.Add("@userId", SqlDbType.Int);
                cmd.Parameters["@userId"].Value = userId;

                cmd.Parameters.Add("@accountId", SqlDbType.Int);
                cmd.Parameters["@accountId"].Value = accountId;

                cmd.CommandText = "DELETE FROM [frames_cache_tbl] WHERE accountId = @accountId AND userId = @userId";
                cmd.ExecuteNonQuery();

            } catch {
                //MessageBox.Show(ex.Message + ex.StackTrace);
            } finally {
                if(connection.State == ConnectionState.Open) {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Remove the cached frames from DB
        /// </summary>
        /// <param name="accountId">integer with the account identification</param>
        public static void RemoveCachedFramesFromDBFromAccount(int accountId) {
            if(ConfigurationManager.IsAuthenticated) {
                TimelineManager.RemoveCachedFramesFromDBFromAccount(ConfigurationManager.AuthenticatedUser.ID, accountId);
            }
        }

        public void UserUpdated(User user) {
            // User updates don't matter for the timeline
        }

        public void UserCreated(User user) {
            // we will not update the timeline, because the user isn't authenticated
        }

        public void UserDeleted(User user) {
            LinkedList<Account> accounts = user.GetAccounts();
            foreach(Account account in accounts) {
                this.UserAccountDeleted(account);
            }
        }

        public void UserLoggedIn(User user) {
            TimelineManager.Update();
        }

        public void UserLoggedOut(User user) {
            TimelineManager.Instance.Clear();
            StatusManager.GetInstance().Clear();
        }

        public void UserAccountUpdated(Account account) {
            // we can't update the timeline were, to avoid loops
        }

        public void UserAccountCreated(Account account) {
            TimelineManager.Update();
        }

        public void UserAccountDeleted(Account account) {
            TimelineManager timeline = TimelineManager.Instance;
            foreach(Frame frame in timeline.Items) {
                if(frame.FrameAccount.ID == account.ID) {
                    TimelineManager.Instance.Remove(frame);
                }
            }

            TimelineManager.RemoveCachedFramesFromDBFromAccount(account.ID);
            StatusManager.RemoveStatus(new Status(account.ID, "Account deleted"));
            TimelineManager.Update();
        }
    }
}
