using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iBoard.Classes.Timeline;
using System.Windows;
using iBoard.Classes.Configuration;
using System.ComponentModel;
using System.ServiceModel;
using System.Collections;
using System.Data.SqlClient;
using System.Data;
using System.Collections.ObjectModel;

namespace iBoard.Classes.Data.Moodle {
    public class MoodleTimeline : TimelineListener {
        private static readonly ICollection<Frame> _frames = new ObservableCollection<Frame>();

        LinkedList<BackgroundWorker> workers = new LinkedList<BackgroundWorker>();

        /// <summary>
        /// Instanciates a new TimelineUpdate object
        /// </summary>
        public override void TimelineUpdate() {
            foreach(BackgroundWorker bgWorker in workers) {
                if(bgWorker.IsBusy) {
                    bgWorker.CancelAsync();
                }
                this.workers.Remove(bgWorker);
            }

            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.MOODLETYPE);

            foreach(Account account in accounts) {
                if(account.Enabled) {
                    TimelineManager.ReportProgress(new Status(account.ID, "Loading account..."));
                    this.GetCachedModificationsForAccount(account);
                    this.GetRecentModificationsForAccount(account);
                }
            }
        }

        /// <summary>
        /// Get the cached modifications from DB
        /// </summary>
        /// <param name="account">Account instance</param>
        private void GetCachedModificationsForAccount(Account account) {
            LinkedList<Frame> frames = TimelineManager.GetCachedFramesFromDB(account);
            foreach(Frame frame in frames) {
                this.AddFrame(frame);
            }
        }

        /// <summary>
        /// Cache the moodle frames
        /// </summary>
        /// <param name="account">Account instance</param>
        /// <param name="frames">LinkedList<Frame> instance of frames to store</param>
        private void CacheModificationsForAccount(Account account, LinkedList<Frame> frames) {
            TimelineManager.CacheFramesOnDB(account, frames);
        }

        /// <summary>
        /// The recent modifications for a course
        /// </summary>
        /// <param name="account">With the account data to use on the query</param>
        private void GetRecentModificationsForAccount(Account account) {
            LinkedList<Frame> frames = new LinkedList<Frame>();
            BackgroundWorker bgWorker = new BackgroundWorker();
            this.workers.AddLast(bgWorker);

            bgWorker.WorkerReportsProgress = true;
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args)  {

                // Load the remote data
                try {
                    UedWs.UEDWSPortTypeClient client = new UedWs.UEDWSPortTypeClient();
                    client.Endpoint.Address = new EndpointAddress(account.getOption(Properties.Settings.Default.MoodleServiceUrlSettingName));

                    UedWs.UedCredentials credentials = new UedWs.UedCredentials();
                    credentials.username = account.getOption(Properties.Settings.Default.MoodleServiceUsernameSettingName);
                    credentials.autorizationKey = account.getOption(Properties.Settings.Default.MoodleServiceAutorizationKeySettingName);

                    UedWs.UedDate beginDate = MoodleTimeline.DateTimeToUedDate(account.LastUpdate);

                    if(client.validateCredentials(credentials)) {
                        UedWs.UedCourse[] courses = client.getMyCourses(credentials);

                        int courseCount = 0;
                        bgWorker.ReportProgress(courseCount++);

                        foreach(UedWs.UedCourse course in courses) {
                            UedWs.UedRecentModification[] recentModifications = client.getCourseRecentModifications(credentials, course.id, beginDate);

                            foreach(UedWs.UedRecentModification modification in recentModifications) {
                                frames.AddLast(new Frame(account, "[" + account.Name + "] @" + course.fullName, modification.text + " " + modification.modulename, MoodleTimeline.UedDateToDateTime(modification.date), (modification.url.Equals("") ? null : new Uri(modification.url)), course.id));
                            }
                            bgWorker.ReportProgress(((courseCount++) * 100) / courses.Length);
                        }
                    }

                } catch(Exception) {
                    //MessageBox.Show(ex.Message+ ex.StackTrace);
                }
            });

            bgWorker.ProgressChanged += new ProgressChangedEventHandler(delegate(object s, ProgressChangedEventArgs args) {
                TimelineManager.ReportProgress(new Status(account.ID, "Checking for course events. " + args.ProgressPercentage + "% completed."));
            });

            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object wsender, RunWorkerCompletedEventArgs args)  {
                this.CacheModificationsForAccount(account, frames);

                foreach(Frame frame in frames) {
                    this.AddFrame(frame);
                }
                this.workers.Remove(bgWorker);
                TimelineManager.RegisterEvent(account);
                ConfigurationManager.UpdateUserAccountLastUpdate(account.ID);
                TimelineManager.ReportProgress(new Status(account.ID, frames.Count + " account events loaded."));
            });
            bgWorker.RunWorkerAsync(account);
        }

        public static DateTime UedDateToDateTime(UedWs.UedDate date){
            return new DateTime(int.Parse(date.year), int.Parse(date.month), int.Parse(date.day), int.Parse(date.hour), int.Parse(date.minute), int.Parse(date.second));
        }

        public static UedWs.UedDate DateTimeToUedDate(DateTime date) {
            UedWs.UedDate uedDate = new UedWs.UedDate();
            uedDate.year = date.Year.ToString();
            uedDate.month = date.Month.ToString();
            uedDate.day = date.Day.ToString();
            uedDate.hour = date.Hour.ToString();
            uedDate.minute = date.Minute.ToString();
            uedDate.second = date.Second.ToString();
            return uedDate;
        }

        /// <summary>
        /// Get a collection of moodle frames
        /// </summary>
        /// <returns></returns>
        public ICollection<Frame> GetMoodleFrames() {
            return MoodleTimeline._frames;
        }

        /// <summary>
        /// Adds a frame to the moodle timeline
        /// </summary>
        /// <param name="frame">Frame instance to add</param>
        public override Boolean AddFrame(Frame frame) {
            if(frame.FrameAccount.Type == Account.MOODLETYPE && base.AddFrame(frame)) {
                foreach(Frame frm in MoodleTimeline._frames){
                    // if the frame does exist on the list, return true
                    if(frame.ID == frm.ID && frame.Date == frm.Date && frame.FrameAccount.ID == frm.FrameAccount.ID && frame.Description == frm.Description && frame.Title == frm.Title) {
                        return true;
                    }
                }
                MoodleTimeline._frames.Add(frame);
                return true;
            }
            return false;
        }
    }
}
