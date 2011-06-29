using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using iBoard.Classes.Configuration;
using System.Collections;
using System.Windows.Threading;
using iBoard.Classes.Data;
using System.Windows;

namespace iBoard.Classes.Timeline {

    /// <summary>
    /// Interface to be implemented by the listeners to react on status events
    /// </summary>
    public interface IStatusUpdateEventListener {
        /// <summary>
        /// Should be implemented by listeners to react to the status update event
        /// </summary>
        void StatusUpdate(Status status);
    }

    /// <summary>
    /// This class manages the status events for each source
    /// </summary>
    public class StatusManager : ObservableCollection<Status> {

        private ArrayList _listeners;
        private static StatusManager _instance = null;
        static readonly object _padlock = new object();
        private static readonly object _statusesLock = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        private StatusManager() {
            this._listeners = new ArrayList();
        }

        /// <summary>
        /// Get a StatusManager instance
        /// </summary>
        public static StatusManager GetInstance() {
            if(_instance == null) {
                lock(_padlock) {
                    if(_instance == null) {
                        _instance = new StatusManager();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Update or insert a specific status
        /// </summary>
        /// <param name="status">Status instance with the status</param>
        public static void UpdateStatus(Status status) {
            StatusManager instance = StatusManager.GetInstance();

            lock(StatusManager._statusesLock) {
                StatusManager.RemoveStatus(status);
                // add the item
                instance.Add(status);
                
                StatusManager.NotifyStatusUpdate(status);
            }
        }

        /// <summary>
        /// Remove a specific status
        /// </summary>
        /// <param name="status">Status instance with the status</param>
        public static void RemoveStatus(Status status) {
            StatusManager instance = StatusManager.GetInstance();
            foreach(Status st in instance.Items) {
                if(st.ID == status.ID) {
                    instance.Remove(st);
                    return;
                }
            }
        }

        /// <summary>
        /// Add a listenner to the list to be notified on status update event
        /// </summary>
        /// <param name="listener">listener to be added</param>
        public static void AddStatusListenner(IStatusUpdateEventListener listener) {
            StatusManager instance = StatusManager.GetInstance();
            if(!instance._listeners.Contains(listener)) {
                lock(StatusManager._statusesLock) {
                    instance._listeners.Add(listener);
                }
            }
        }

        /// <summary>
        /// Remove a listenner from the notification list
        /// </summary>
        /// <param name="listener">The listenner to be removed</param>
        public static void RemoveStatusListenner(IStatusUpdateEventListener listener) {
            StatusManager instance = StatusManager.GetInstance();
            if(!instance._listeners.Contains(listener)) {
                lock(StatusManager._statusesLock) {
                    instance._listeners.Remove(listener);
                }
            }
        }

        /// <summary>
        /// Execute the StatusUpdate for each listenner
        /// </summary>
        public static void NotifyStatusUpdate(Status status) {
            StatusManager instance = StatusManager.GetInstance();
            foreach(IStatusUpdateEventListener listener in instance._listeners) {
                lock(StatusManager._statusesLock) {
                    listener.StatusUpdate(status);
                }
            }
        }
    }
}
