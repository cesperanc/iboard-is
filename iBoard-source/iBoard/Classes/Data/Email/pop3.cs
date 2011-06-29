// Edgar Clérigo
// pop3 client c#
// using IMailPlusLibrary

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IMailPlusLibrary;
using System.Net.Security;

namespace iBoard.Classes.Data.Email
{
    class pop3
    {
        public struct emailInfo
        {
            public string from;
            public string subject;
            public string text;
            public string messageID;
            public DateTime mailDate;

            public emailInfo(string addFrom, string addSubject, string addText, string addMessageId, DateTime maildate)
            {
                from = addFrom;
                subject = addSubject;
                text = addText;
                messageID = addMessageId;
                mailDate = maildate;
            }

            public void addEmail(string addFrom, string addSubject, string addText, string addMessageId, DateTime maildate )
            {
                from = addFrom;
                subject = addSubject;
                text = addText;
                messageID = addMessageId;
                mailDate = maildate;
            }
            
        }
         // vars
         bool _isConnected = false; // pop3 connection status
         //IMailPlusLibrary.Entities.MailMessage Message; //pop3 mail messages
         IMailPlusLibrary.EmailAccounts account = new IMailPlusLibrary.EmailAccounts(); // pop3 mail accounts
         IMailPlusLibrary.MailClients.POPEmailClient pop = new IMailPlusLibrary.MailClients.POPEmailClient(); //pop3 main
         
         public pop3()
         {
               
         }
         
         public void connect(string username,string password, string server, string port, bool ssl)
         {
             
             pop.UseSSL = ssl;
             pop.PortNumber = Convert.ToInt32(port);
             
             try
             {
                 pop.Connect(server, username, password);
                 if (pop.IsConnected)
                 {
                     _isConnected = true;
                     addEmailAccount(username, password, server, port, ssl);
                     Console.WriteLine("Pop account: " + pop.IsConnected.ToString());
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine("Error pop3 Connect: " + ex.ToString());
             }
             
         }

        public bool isConnected ()
        {
            if (pop.IsConnected)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void addEmailAccount(string username,string password,string server,string port, bool ssl)
        {

            //account.KeepMessagesOnServer = chkLeaveCopy.Checked;
            account.MailClientType = MailClientType.POP3;
            account.Password = password;
            account.PortNumber = port;
            account.Server = server;
            account.UserName = username;
            account.UseSSL = ssl;

        }

        public List<emailInfo> getEmail()
        {
            if (_isConnected)
            {
                List<string> email = new List<string>();
                List<emailInfo> listaEmails = new List<emailInfo>();
                emailInfo item = new emailInfo();
                account.CurrentAccountMails = pop.GetMailList();
                
                foreach (IMailPlusLibrary.Entities.MailMessage message in account.CurrentAccountMails)
                {
                    item.addEmail(message.From, message.Subject, message.TextMessage, message.MessageId, DateTime.Parse(message.Date));
                    listaEmails.Add(item);
                    //email.Add(message.Received);
                    //email.Add(message.From);
                    //email.Add(message.Subject);
                    //email.Add(message.MessageId);
                    //email.Add(message.TextMessage);
                    Console.WriteLine(item.from);
                }
                return listaEmails;
            }
            else
            {
                return null;
            }
           
            
        }


    }
}
