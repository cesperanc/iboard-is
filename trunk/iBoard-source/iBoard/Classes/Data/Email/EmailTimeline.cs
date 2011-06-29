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

namespace iBoard.Classes.Data.Email {
    public class EmailTimeline : TimelineListener {
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

            LinkedList<Account> accounts = ConfigurationManager.GetUserAccountsFromType(Account.EMAILTYPE);

            foreach(Account account in accounts) {
                if(account.Enabled && account.ID>-1) {
                    this.GetRecentEmails(account);
                }
            }
        }

        /// <summary>
        /// The recent mails
        /// </summary>
        /// <param name="account">With the account data to use on the query</param>
        private void GetRecentEmails(Account account) {
            TimelineManager.ReportProgress(new Status(account.ID, "Loading account..."));
            
            LinkedList<Frame> frames = new LinkedList<Frame>();
            BackgroundWorker bgWorker = new BackgroundWorker();
            this.workers.AddLast(bgWorker);

            bgWorker.WorkerReportsProgress = true;
            bgWorker.WorkerSupportsCancellation = true;
            bgWorker.DoWork += new DoWorkEventHandler(delegate(object wsender, DoWorkEventArgs args)  {
                
                // Load the remote data
                try {
                    pop3 popClient = new pop3();
                    String popserver = (account.getOption("popserver"));
                    String port = (account.getOption("popport"));
                    String email = (account.getOption("email"));
                    String password = ConfigurationManager.AuthenticatedUser.Decrypt(account.getOption("password"));
                    Boolean ssl = Convert.ToBoolean((account.getOption("ssl")));

                    popClient.connect(email, password, popserver, port, ssl);

                    if(popClient.isConnected()) {
                        List<pop3.emailInfo> emails = popClient.getEmail();
                        foreach(pop3.emailInfo item in emails) {
                            frames.AddLast(new Frame(account, "[" + account.Name + "] From: " + item.from + ": " + item.subject, item.text, item.mailDate, null, account.ID));
                        }
                    }

                } catch {
                    //MessageBox.Show(ex.Message+ ex.StackTrace);
                }
            });

            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(delegate(object wsender, RunWorkerCompletedEventArgs args)  {
                 foreach(Frame frame in frames) {
                     TimelineManager.AddFrame(frame);
                 }
                 TimelineManager.RegisterEvent(account);
                 ConfigurationManager.UpdateUserAccount(account);

                 TimelineManager.ReportProgress(new Status(account.ID, frames.Count + " mails loaded."));
                 this.workers.Remove(bgWorker);
            });
            bgWorker.RunWorkerAsync(account);
        }
    }
}
