using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.Helper
{
    public static class  DateHelper
    {
        public static string GetDeadlineText(DateTime? deadline)
        {
            if (!deadline.HasValue)
                return "Not Specified";

            var today = DateTime.Now.Date;
            var daysLeft = (deadline.Value.Date - today).Days;

            if (daysLeft > 1)
                return $"{daysLeft} days left";
            else if (daysLeft == 1)
                return "1 day left";
            else if (daysLeft == 0)
                return "Last day to apply";
            else
                return "Expired";
        }
    }
}