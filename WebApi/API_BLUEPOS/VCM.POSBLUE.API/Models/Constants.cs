using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VCM.POSBLUE.API.Models
{
    public class Constants
    {
        public static string LoginResponse = "LoginResponse";
        public static class MessageType
        {
            public const string LOGIN = "5250";
            public const string LOGOUT = "5300";
            public const string SALE = "5270";
            public const string BALANCE_ENQUIRY = "5230";
            public const string MEMBER_REGISTRATOR = "5120";
            //public const string REFUND = "5330";
            public const string REFUND = "5331";
            public const string TRANS_ENQUIRY = "5210";



        }
        public static class IDType
        {
            public const string OPERATOR = "OPERATOR";
            public const string NRIC = "NRIC";
            public const string CARD = "CARD";
        }

        public static class Action
        {
            public const string LOGIN = "LGN";
            public const string LOGOUT = "LGO";

        }

        public static class Mode
        {
            public const string OPERATOR = "O";

        }
        public static class Transaction
        {
            public const string Sale = "SAL";
            public const string Refund = "REF";
            public const string Void = "VOI";
        }
    }
}