namespace AnmolDristi
{
    public partial class WhatsApp_BulkSend
    {
        protected global::System.Web.UI.WebControls.TextBox txtCampaignName;
        protected global::System.Web.UI.WebControls.TextBox txtTemplateName;
        protected global::System.Web.UI.WebControls.DropDownList ddlLanguage;
        protected global::System.Web.UI.WebControls.DropDownList ddlTemplateHeader;
        protected global::System.Web.UI.WebControls.FileUpload fuExcel;
        protected global::System.Web.UI.WebControls.Button btnUpload;
        protected global::System.Web.UI.WebControls.Button btnClear;
        protected global::System.Web.UI.WebControls.Label lblUploadOk;
        protected global::System.Web.UI.WebControls.Label lblUploadErr;
        protected global::System.Web.UI.WebControls.Panel pnlPreview;
        protected global::System.Web.UI.WebControls.GridView gvPreview;
        protected global::System.Web.UI.WebControls.Panel pnlMapping;
        protected global::System.Web.UI.WebControls.GridView gvMapping;
        protected global::System.Web.UI.WebControls.Panel pnlValidation;
        protected global::System.Web.UI.WebControls.Label lblTotalCount;
        protected global::System.Web.UI.WebControls.Label lblValidCount;
        protected global::System.Web.UI.WebControls.Label lblInvalidCount;
        protected global::System.Web.UI.WebControls.Label lblDuplicateCount;
        protected global::System.Web.UI.WebControls.Label lblWarningsInfo;
        protected global::System.Web.UI.WebControls.Label lblParseInfo;
        protected global::System.Web.UI.WebControls.Repeater rptWarnings;
        protected global::System.Web.UI.WebControls.Button btnValidate;
        protected global::System.Web.UI.WebControls.Button btnSend;

        // Phase 2 — Content & Attachment Configuration
        protected global::System.Web.UI.WebControls.Label lblContentOk;
        protected global::System.Web.UI.WebControls.Label lblContentErr;
        protected global::System.Web.UI.WebControls.RadioButton rbModeNone;
        protected global::System.Web.UI.WebControls.RadioButton rbModeCommon;
        protected global::System.Web.UI.WebControls.RadioButton rbModeIndividual;
        protected global::System.Web.UI.WebControls.RadioButton rbModeWorkbook;
        protected global::System.Web.UI.WebControls.RadioButton rbModeUrl;
        protected global::System.Web.UI.WebControls.Panel pnlCommonContent;
        protected global::System.Web.UI.WebControls.FileUpload fuCommonContent;
        protected global::System.Web.UI.WebControls.Button btnUploadCommon;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl trCommonInfo;
        protected global::System.Web.UI.WebControls.Label lblCommonFileInfo;
        protected global::System.Web.UI.WebControls.Panel pnlIndividualContent;
        protected global::System.Web.UI.WebControls.DropDownList ddlIndividualMappingColumn;
        protected global::System.Web.UI.WebControls.FileUpload fuIndividualFiles;
        protected global::System.Web.UI.WebControls.Button btnUploadIndividual;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl trIndividualInfo;
        protected global::System.Web.UI.WebControls.Label lblIndividualFileInfo;
        protected global::System.Web.UI.WebControls.Panel pnlWorkbookContent;
        protected global::System.Web.UI.WebControls.DropDownList ddlWorkbookMappingColumn;
        protected global::System.Web.UI.WebControls.FileUpload fuWorkbook;
        protected global::System.Web.UI.WebControls.Button btnUploadWorkbook;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl trWorkbookInfo;
        protected global::System.Web.UI.WebControls.Label lblWorkbookFileInfo;
        protected global::System.Web.UI.WebControls.Panel pnlUrlContent;
        protected global::System.Web.UI.WebControls.DropDownList ddlUrlColumn;

        // Phase 2 — Dispatch Plan
        protected global::System.Web.UI.WebControls.Panel pnlDispatchPlan;
        protected global::System.Web.UI.WebControls.Label lblDispatchSummary;
        protected global::System.Web.UI.WebControls.GridView gvDispatchPlan;
        protected global::System.Web.UI.WebControls.Button btnSendDispatch;
        protected global::System.Web.UI.WebControls.Panel pnlDispatchResults;
        protected global::System.Web.UI.WebControls.GridView gvDispatchResults;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl divDispatchOk;
        protected global::System.Web.UI.WebControls.Label lblDispatchOk;
        protected global::System.Web.UI.HtmlControls.HtmlGenericControl divDispatchErr;
        protected global::System.Web.UI.WebControls.Label lblDispatchErr;

        // Phase 1 — Campaign Result
        protected global::System.Web.UI.WebControls.Panel pnlResults;
        protected global::System.Web.UI.WebControls.Panel pnlResultOk;
        protected global::System.Web.UI.WebControls.Label lblResultOk;
        protected global::System.Web.UI.WebControls.Panel pnlResultErr;
        protected global::System.Web.UI.WebControls.Label lblResultErr;
        protected global::System.Web.UI.WebControls.Literal litResponseBody;
        protected global::System.Web.UI.WebControls.Literal litCampaignJson;
    }
}
