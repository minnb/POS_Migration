namespace TCX.API.Common.Enums
{
    public enum ActionWinpayEnum
    {
        create_account = 1,
        close_account = 101,
        send_otp = 2, //register
        send_otp2 = 22, //update finger
        send_otp3 = 222, //unregister
        cashin = 3,
        cashout = 30,
        payment = 4,
        query_transaction = 5,
        pending = 9,
        commit = 10,
        refund = 11,
        balance=12,
        change_fingerprint = 13,
        auth_fingerprint = 14,
        cashback = 20
    }

    public enum TypeCodeWinpayEnum
    {
        DEPOSIT = 1,
        SALE = 2,
        WITHDRAW = 3
    }

}
