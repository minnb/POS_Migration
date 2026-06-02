using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.Menu;
using VCM.POSBLUE.Model.Menu;

namespace VCM.POSBLUE.Business.Menu
{
    public interface IMenuBLO
    {
        List<MenuModel> ListMenu();
    }
   public class MenuBLO: IMenuBLO
    {
        private MenuData data { get; set; }
        public MenuBLO()
        {
            data = new MenuData();
        }

        public List<MenuModel> ListMenu()
        {
            return data.ListMenu();
        }
    }
}
