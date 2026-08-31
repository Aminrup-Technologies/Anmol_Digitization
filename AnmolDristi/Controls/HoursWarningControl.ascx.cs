using System.Web;
using System.Web.UI;

namespace AnmolDristi.Controls
{
    public partial class HoursWarningControl : UserControl
    {
        protected override void OnLoad(System.EventArgs e)
        {
            base.OnLoad(e);
            if (!IsPostBack && string.IsNullOrEmpty(litText.Text))
                Bind(null);
        }

        public void Clear()
        {
            Bind(null);
        }

        public void Bind(string text)
        {
            litText.Text = string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : HttpUtility.HtmlEncode(text);

            bool show = !string.IsNullOrWhiteSpace(text);
            if (show)
            {
                pnlHoursWarning.Attributes.Remove("hidden");
                pnlHoursWarning.Attributes["aria-hidden"] = "false";
            }
            else
            {
                pnlHoursWarning.Attributes["hidden"] = "hidden";
                pnlHoursWarning.Attributes["aria-hidden"] = "true";
            }
        }
    }
}
