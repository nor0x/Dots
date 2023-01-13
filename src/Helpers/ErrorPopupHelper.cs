using Dots.Controls;
using Microsoft.AppCenter.Crashes;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots.Helpers
{
    public class ErrorPopupHelper
    {
        public async Task ShowPopup(Exception ex)
        {
            Crashes.TrackError(ex);
            await MopupService.Instance.PushAsync(new ErrorPopup(ex.Message, ex.StackTrace));

        }
    }
}
