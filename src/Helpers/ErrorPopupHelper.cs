using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dots.Helpers
{
    /// <summary>
    /// A message sink, not a popup. Failures used to disappear into a commented-out body; now they
    /// are raised to whoever is listening (the view model, which surfaces them in the error banner).
    /// </summary>
    public class ErrorPopupHelper
    {
        /// <summary>(message, detail) - detail is the full exception text or a log path, if any.</summary>
        public event Action<string, string?>? ErrorRaised;

        public Task ShowPopup(Exception ex)
        {
            Debug.WriteLine(ex);
            ErrorRaised?.Invoke(Dots.Services.InstallerExitCodes.Summarize(ex), ex.ToString());
            return Task.CompletedTask;
        }

        public void ShowError(string message, string? detail = null) => ErrorRaised?.Invoke(message, detail);
    }
}
