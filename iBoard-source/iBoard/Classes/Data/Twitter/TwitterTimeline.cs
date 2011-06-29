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
using Twitterizer;
using System.Windows.Threading;

namespace iBoard.Classes.Data.Twitter {
    public class TwitterTimeline : TimelineListener {
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

            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.TWITTERTYPE);

            foreach(Account account in accounts) {
                if(account.Enabled && account.ID>-1) {
                    this.GetRecentTweetsForAccount(account);
                }
            }
        }

        /// <summary>
        /// The recent modifications for a course
        /// </summary>
        /// <param name="account">With the account data to use on the query</param>
        private void GetRecentTweetsForAccount(Account account) {
            TimelineManager.ReportProgress(new Status(account.ID, "Loading account..."));

            // Load the cached data
            LinkedList<Frame> frames = TimelineManager.GetCachedFramesFromDB(account);
            foreach(Frame frame in frames) {
                this.AddFrame(frame);
            }

            frames.Clear();

            BackgroundWorker bgWorker = new BackgroundWorker();
            this.workers.AddLast(bgWorker);

            bgWorker.WorkerReportsProgress = true;
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args)  {
                
                // Load the remote data
                try {
                    
                    Twitterizer.OAuthTokens authToken = TwitterTimeline.getTwitterAuthToken(account);

                    Twitterizer.TimelineOptions options = new Twitterizer.TimelineOptions();
                    String sinceStatusId = account.getOption("SinceStatusId");
                    if(sinceStatusId != null) {
                        options.SinceStatusId = Convert.ToDecimal(sinceStatusId);
                    } else {
                        options.Count = 10;
                    }

                    Twitterizer.TwitterResponse<TwitterStatusCollection> timeline = Twitterizer.TwitterTimeline.HomeTimeline(authToken, options);

                    if(sinceStatusId != null && timeline.Result == Twitterizer.RequestResult.Success && timeline.ResponseObject.Count<10) {
                        options = new Twitterizer.TimelineOptions();
                        options.Count = 10;
                        timeline = Twitterizer.TwitterTimeline.HomeTimeline(authToken, options);
                    }

                    if(timeline.Result == Twitterizer.RequestResult.Success) {
                        frames.Clear();
                        Frame frame;
                        foreach(Twitterizer.TwitterStatus status in timeline.ResponseObject) {
                            frame = new Frame(account, "["+account.Name+"] @"+ status.User.ScreenName, status.Text, status.CreatedDate, ((status.User.Website != null) ? new Uri(status.User.Website) : null), Convert.ToInt64(status.Id));
                            frame.ImageUrl = new Uri(status.User.ProfileImageLocation);
                            frames.AddLast(frame);
                        }
                        if(timeline.ResponseObject[0] != null) {
                            sinceStatusId = Convert.ToString(timeline.ResponseObject[0].Id);
                        }
                        args.Result = Convert.ToString(sinceStatusId);
                        
                    }

                } catch {
                    //MessageBox.Show(ex.Message+ ex.StackTrace);
                }
            });

            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object wsender, RunWorkerCompletedEventArgs args)  {
                TimelineManager.CacheFramesOnDB(account, frames);
                int totalFramesAdded = 0;
                foreach(Frame frame in frames) {
                    if(TimelineManager.AddFrame(frame)) {
                        totalFramesAdded++;
                    }
                }
                TimelineManager.RegisterEvent(account);

                if(!args.Cancelled && args.Error == null) {
                    String sinceStatusId = args.Result as String;
                    account.setOption("SinceStatusId", sinceStatusId);
                }
                account.LastUpdate = DateTime.Now;
                ConfigurationManager.UpdateUserAccount(account);

                TimelineManager.ReportProgress(new Status(account.ID, totalFramesAdded + " new twitts loaded."));
                this.workers.Remove(bgWorker);
            });
            bgWorker.RunWorkerAsync(account);
        }

        /// <summary>
        /// Get an twitter OAuthToken for authentication
        /// </summary>
        /// <param name="twitterAccount">Account instance to get the token from</param>
        /// <returns>Twitterizer.OAuthTokens with the account token</returns>
        public static Twitterizer.OAuthTokens getTwitterAuthToken(Account twitterAccount) {
            Twitterizer.OAuthTokens authToken = new Twitterizer.OAuthTokens();
            authToken.AccessToken = twitterAccount.getOption("accessTokenToken");
            authToken.AccessTokenSecret = ConfigurationManager.AuthenticatedUser.Decrypt(twitterAccount.getOption("accessTokenSecret"));
            authToken.ConsumerKey = Properties.Settings.Default.TwitterConsumerKey;
            authToken.ConsumerSecret = Properties.Settings.Default.TwitterConsumerSecret;

            return authToken;
        }
    }
}
