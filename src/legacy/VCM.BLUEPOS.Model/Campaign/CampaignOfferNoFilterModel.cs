using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignOfferNoFilterModel : BaseFilterModel
    {
        public string OfferType { get; set; }
        public string StartDateStr { get; set; }
        public string EndDateStr { get; set; }

        public DateTime? StartDate
        {
            get
            {
                if (!string.IsNullOrEmpty(StartDateStr) && !string.IsNullOrEmpty(StartDateStr))
                {
                    DateTime parsedDate;
                    DateTime.TryParseExact(StartDateStr, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out parsedDate);
                    return parsedDate;
                }
                else
                {
                    return DateTime.Today;
                }
            }
        }

        public DateTime? EndDate
        {
            get
            {
                if (!string.IsNullOrEmpty(EndDateStr) && !string.IsNullOrEmpty(EndDateStr))
                {
                    DateTime parsedDate;
                    DateTime.TryParseExact(EndDateStr, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out parsedDate);
                    return parsedDate;
                }
                else
                {
                    return DateTime.Today;
                }
            }
        }
    }
}
