using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.Mail;
using iBoard;
using iBoard.Classes.Configuration;
using iBoard.Classes.Data;
using System.Data;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using Microsoft.Expression.Interactivity.Core;
using iBoard.Classes.Data.Email;


namespace iBoard
{
    /// <summary>
    /// Interaction logic for MailControl.xaml
    /// </summary>
    public partial class MailControl : UserControl
    {
        bool connected;
        SmtpClient SmtpServer;
        pop3 popClient = new pop3();
        DataGridTextColumn col1 = new DataGridTextColumn();
        DataGridTextColumn col2 = new DataGridTextColumn();
        DataGridTextColumn col3 = new DataGridTextColumn();
        DataGridTextColumn col4 = new DataGridTextColumn();
        DataGridTextColumn col5 = new DataGridTextColumn();
        public string aux;
        int _accountId;

        public MailControl(string accountName, int accountId)
        {
            InitializeComponent();
            _accountId = accountId;
            dataGrid1.Columns.Add(col1);
            dataGrid1.Columns.Add(col2);
            dataGrid1.Columns.Add(col3);
            dataGrid1.Columns.Add(col4);
            dataGrid1.Columns.Add(col5);

            col1.Header = ("MAIL NUMBER");
            col2.Header = ("FROM");
            col3.Header = ("SUBJECT");
            col4.Header = ("MESSAGE");
            col5.Header = ("DATE");

            col1.Visibility = System.Windows.Visibility.Hidden;
            col4.Visibility = System.Windows.Visibility.Hidden;

            Account account = ConfigurationManager.GetUserAccount(accountId);

            if(account != null) {
                txtfrom.Text = (account.getOption("email"));
            } else {
                MessageBox.Show("A conta não existe");
            }
        }



        private void sendemail_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                string smtpserver;
                string port;
                //string email;
                string password;
                bool ssl;

                Account account = ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId);
                smtpserver = (account.getOption("smtpserver"));
                port = (account.getOption("smtpport"));
                txtfrom.Text = (account.getOption("email"));
                password = (ConfigurationManager.AuthenticatedUser.Decrypt(account.getOption("password")));
                ssl = Convert.ToBoolean((account.getOption("ssl")));

                try
                {

                    SmtpServer = new SmtpClient();

                    SmtpServer.Host = smtpserver;
                    SmtpServer.Port = int.Parse(port);
                    SmtpServer.UseDefaultCredentials = false;
                    SmtpServer.Credentials = new System.Net.NetworkCredential(txtfrom.Text, password);
                    SmtpServer.EnableSsl = true;
                    connected = true;
                    Console.WriteLine("Connected to smtp server");

                }

                catch (Exception exp)
                {

                    Console.WriteLine("Error SMTP client: " + exp.ToString());
                }


                if (connected == true)
                {

                    MailMessage mail = new MailMessage();

                    mail.From = new MailAddress(txtfrom.Text);
                    mail.To.Add(textBox2.Text);
                    mail.Subject = (textBox3.Text);
                    mail.Body = (textBox4.Text);
                    try
                    {
                        SmtpServer.Send(mail);
                    	MessageBox.Show("Mail sended with success", "Information");
                    }
                    catch (Exception erro)
                    {

                        MessageBox.Show("Error SMTP Client. Verifique as configurações da sua conta" + erro, "SMTP Error");
                    }


                    
                    //txtfrom.Text = "";
                    textBox2.Text = "";
                    textBox3.Text = "";
                    textBox4.Text = "";
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

                textBox2.Text = "";
                textBox3.Text = "";
                textBox4.Text = "";
            }
        }

       
        

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            /**
            Duration duration = new Duration(TimeSpan.FromSeconds(20));
            DoubleAnimation doubleanimation = new DoubleAnimation(200.0, duration);
            progressBar1.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
            
             **/

            try
            {
                string popserver;
                string port;
                string email;
                string password;
                bool ssl;

                Account account = ConfigurationManager.GetUserAccount(ConfigurationManager.AuthenticatedUser.ID, _accountId);
                popserver = (account.getOption("popserver"));
                port = (account.getOption("popport"));
                email = (account.getOption("email"));
                password = ConfigurationManager.AuthenticatedUser.Decrypt(account.getOption("password"));
                ssl = Convert.ToBoolean((account.getOption("ssl")));

                popClient.connect(email, password, popserver, port, ssl);

                if (popClient.isConnected())
                {
                    popStatus.Content = "Status: Connected";
                    connected = true;
                }
                else
                {
                    popStatus.Content = "Status: Disconnected";
                }


                if (connected == true)
                {

                    Console.WriteLine("Getting mail");

                    dataGrid1.Items.Clear();
                  
                    List<pop3.emailInfo> emails = new List<pop3.emailInfo>();
                    emails = popClient.getEmail();

                    int countMail = 1;


                    foreach (pop3.emailInfo item in emails)
                    {
                       
                        dataGrid1.Items.Add(new MyData(
                        
                            item.messageID,
                            item.from,
                            item.subject,
                            item.text,
                            item.mailDate
                        ));



                        countMail++;


                    }

                     col1.Binding = new Binding("idMessage");

                    col2.Binding = new Binding("fromSender");


                    col3.Binding = new Binding("subjectSender");


                    col4.Binding = new Binding("textSender");

                    col5.Binding = new Binding("mailDate");

                    col4.Visibility = System.Windows.Visibility.Hidden;
					col5.SortDirection = System.ComponentModel.ListSortDirection.Descending;

                    Console.WriteLine("Done");


                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error POP client: " + ex.ToString());
            }
        }

        



        private void btnMailBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
			ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, Mail.Name, true);
        }

        private void dataGrid1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            DataGrid dg = e.OriginalSource as DataGrid;
            MyData data = dg.SelectedItem as MyData;
            //MessageBox.Show(data.idMessage);
            
            StringBuilder mailInfo = new StringBuilder();
            mailInfo.AppendLine("From: "+data.fromSender);
            mailInfo.AppendLine("Subject: "+data.subjectSender);
            mailInfo.AppendLine("Date: "+data.mailDate);

            txtMailInfo.Text = mailInfo.ToString();

            txtMail.Text = data.textSender;
            ExtendedVisualStateManager.GoToElementState(this.LayoutRoot as FrameworkElement, MailDetail.Name, true);
        }

    }
}
