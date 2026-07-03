using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Data.Notify;
using VCM.BLUEPOS.Model.Notify;

namespace VCM.BLUEPOS.Business.Notify
{
    public interface INotifyBLO
    {
        List<NotifyModel> LoadNotify(string title, string status, out int totalRecord, int skip, int take);
        ViewNotifyModel ViewInfoNotify(string id);
        Tuple<bool, string, NotifyViewClientModel> ViewTop1NotifyClient(string userName);
        Tuple<bool, string, NotifySaveModel> SaveNotify(NotifySaveModel model);
        Tuple<bool, string, NotifyConfirmModel> ConfirmNotify(NotifyConfirmModel model);
        Tuple<bool, string> DeleteNotify(string ID);
    }
    public class NotifyBLO : INotifyBLO
    {
        private NotifyData data { get; set; }
        public NotifyBLO()
        {
            data = new NotifyData();
        }

        public List<NotifyModel> LoadNotify(string title, string status, out int totalRecord, int skip, int take)
        {
            return data.LoadNotify(title, status, out totalRecord, skip, take);
        }

        public ViewNotifyModel ViewInfoNotify(string id)
        {
            return data.ViewInfoNotify(id);
        }

        public Tuple<bool, string, NotifySaveModel> SaveNotify(NotifySaveModel model)
        {
            return data.SaveNotify(model);
        }
        public Tuple<bool, string, NotifyViewClientModel> ViewTop1NotifyClient(string userName)
        {
            return data.ViewTop1NotifyClient(userName);
        }

        public Tuple<bool, string, NotifyConfirmModel> ConfirmNotify(NotifyConfirmModel model)
        {
            return data.ConfirmNotify(model);
        }

        public Tuple<bool, string> DeleteNotify(string ID)
        {
            return data.DeleteNotify(ID);
        }
    }
}
