using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iBoard.Classes.Data.Email
{
    class MyData
    {
        private string _idMessage;
        private string _fromSender;
        private string _subjectSender;
        private string _textSender;
        private DateTime _mailDate;

        public MyData(string idMessage, string fromSender, string subjectSender, string textSender, DateTime mailDate)
        {
            this._idMessage = idMessage;
            this._fromSender = fromSender;
            this._subjectSender = subjectSender;
            this._textSender = textSender;
            this._mailDate = mailDate;
        }

        public string idMessage
        {
            get { return _idMessage; }
            set { _idMessage = value; }
        }

        public string fromSender
        {
            get { return _fromSender; }
            set { _fromSender = value; }
        }


        public string subjectSender
        {
            get { return _subjectSender; }
            set { _subjectSender = value; }
        }

        public string textSender
        {
            get { return _textSender; }
            set { _textSender = value; }
        }

        public DateTime mailDate
        {
            get { return _mailDate; }
            set { _mailDate = value; }
        }
    }
}
