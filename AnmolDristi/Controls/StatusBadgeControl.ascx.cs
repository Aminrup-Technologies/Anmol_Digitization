using System.Web.UI;

namespace AnmolDristi.Controls
{
    public partial class StatusBadgeControl : UserControl
    {
        public string Status { get; set; }
        public string Text { get; set; }
        public bool Large { get; set; }
        public bool Html { get; set; }
        private bool _presentationBound;

        public void Bind(string status)
        {
            Status = status;
            Text = status;
            Html = false;
            Apply();
        }

        public void BindPresentation(string text, string cssClass, bool allowHtml)
        {
            _presentationBound = true;
            Text = text;
            Html = allowHtml;
            Large = true;
            Visible = !string.IsNullOrEmpty(text);
            if (allowHtml)
                spBadge.InnerHtml = text ?? string.Empty;
            else
                spBadge.InnerText = text ?? string.Empty;
            spBadge.Attributes["class"] = cssClass ?? VmsUiStatus.BadgeClass(text);
            spBadge.Attributes["aria-label"] = "Status " + StripTags(text);
        }

        protected override void OnPreRender(System.EventArgs e)
        {
            base.OnPreRender(e);
            if (!_presentationBound && (!string.IsNullOrEmpty(Status) || !string.IsNullOrEmpty(Text)))
                Apply();
        }

        private void Apply()
        {
            string label = string.IsNullOrEmpty(Text) ? Status : Text;
            Visible = !string.IsNullOrEmpty(label);
            if (Html)
                spBadge.InnerHtml = label ?? string.Empty;
            else
                spBadge.InnerText = label ?? string.Empty;

            string css = VmsUiStatus.BadgeClass(Status ?? label);
            if (Large)
                css = "vms-status-badge vms-status-lg " + VmsUiStatus.CssClass(Status ?? label);
            spBadge.Attributes["class"] = css;
            spBadge.Attributes["aria-label"] = "Status " + (label ?? string.Empty);
        }

        private static string StripTags(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", string.Empty);
        }
    }
}
