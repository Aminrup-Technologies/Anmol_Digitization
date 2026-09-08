namespace AnmolDristi
{
    public partial class WhatsApp_BulkSend
    {
        protected global::System.Web.UI.WebControls.TextBox txtCampaignName;
        protected global::System.Web.UI.WebControls.TextBox txtTemplateName;
        protected global::System.Web.UI.WebControls.DropDownList ddlLanguage;
        protected global::System.Web.UI.WebControls.FileUpload fuExcel;
        protected global::System.Web.UI.WebControls.Button btnUpload;
        protected global::System.Web.UI.WebControls.Button btnClear;
        protected global::System.Web.UI.WebControls.Panel pnlUploadOk;
        protected global::System.Web.UI.WebControls.Label lblUploadOk;
        protected global::System.Web.UI.WebControls.Panel pnlUploadErr;
        protected global::System.Web.UI.WebControls.Label lblUploadErr;
        protected global::System.Web.UI.WebControls.Panel pnlPreview;
        protected global::System.Web.UI.WebControls.GridView gvPreview;
        protected global::System.Web.UI.WebControls.Panel pnlMapping;
        protected global::System.Web.UI.WebControls.GridView gvMapping;
        protected global::System.Web.UI.WebControls.Panel pnlValidation;
        protected global::System.Web.UI.WebControls.Label lblValidCount;
        protected global::System.Web.UI.WebControls.Label lblInvalidCount;
        protected global::System.Web.UI.WebControls.Label lblDuplicateCount;
        protected global::System.Web.UI.WebControls.Repeater rptWarnings;
        protected global::System.Web.UI.WebControls.Button btnValidate;
        protected global::System.Web.UI.WebControls.Button btnSend;
        protected global::System.Web.UI.WebControls.Panel pnlResults;
        protected global::System.Web.UI.WebControls.Panel pnlResultOk;
        protected global::System.Web.UI.WebControls.Label lblResultOk;
        protected global::System.Web.UI.WebControls.Panel pnlResultErr;
        protected global::System.Web.UI.WebControls.Label lblResultErr;
        protected global::System.Web.UI.WebControls.Literal litResponseBody;
        protected global::System.Web.UI.WebControls.Literal litCampaignJson;
    }
}