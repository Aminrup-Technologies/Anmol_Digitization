<%@ Page Title="" Language="C#" MasterPageFile="~/Dristi.Master" AutoEventWireup="true" CodeBehind="Process_Approval.aspx.cs" Inherits="AnmolDristi.Process_Approval" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class="right_col" role="main">
        <div class="container">

            <div class="row">
                <div class="col-md-12 col-sm-12 ">
                    <div class="x_panel">
                        <div class="x_title">
                            <h2>
                                <asp:Label ID="lbl_docname" runat="server" Text="Label"></asp:Label></h2>
                            <div class="clearfix"></div>
                        </div>

                        <div class="row">
                            <div class="col-12">
                                <div class="tab-content ml-1" id="myTabContent">
                                    <div class="x-content">

                                        <div class="col-md-2">
                                            <div class="mb-2">
                                                <asp:Label ID="Label1" runat="server" AssociatedControlID="DDL_Plant" Text="Plant Name" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_DDL_Plant" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_Plant" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                <div class="input-group-sm">
                                                    <asp:DropDownList ID="DDL_Plant" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_Plant_SelectedIndexChanged"></asp:DropDownList>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-2">
                                            <div class="mb-2">
                                                <asp:Label ID="Label2" runat="server" AssociatedControlID="DDL_PlantLine" Text="Select Line" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_DDL_PlantLine" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_PlantLine" Display="Dynamic" InitialValue="0"></asp:RequiredFieldValidator>
                                                <div class="input-group-sm">
                                                    <asp:DropDownList ID="DDL_PlantLine" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_PlantLine_SelectedIndexChanged"></asp:DropDownList>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-2">
                                            <div class="mb-2">
                                                <asp:Label ID="Label3" runat="server" AssociatedControlID="DDL_ProductCategory" Text="Product Category" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_DDL_ProductCategory" runat="server" ErrorMessage="" ForeColor="Red" InitialValue="" ControlToValidate="DDL_ProductCategory" Display="Dynamic"></asp:RequiredFieldValidator>
                                                <div class="input-group-sm">
                                                    <asp:DropDownList ID="DDL_ProductCategory" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductCategory_SelectedIndexChanged"></asp:DropDownList>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-2">
                                            <div class="mb-2">
                                                <asp:Label ID="Label4" runat="server" AssociatedControlID="DDL_ProductBrand" Text="Product Brand" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_DDL_ProductBrand" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_ProductBrand" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                                <div class="input-group-sm">
                                                    <asp:DropDownList ID="DDL_ProductBrand" runat="server" CssClass="form-control form-control-sm rounded" AutoPostBack="true" OnSelectedIndexChanged="DDL_ProductBrand_SelectedIndexChanged"></asp:DropDownList>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-2">
                                            <div class="mb-2">
                                                <asp:Label ID="Label5" runat="server" AssociatedControlID="DDL_BrandSKU" Text="Brand SKU Type" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_DDL_BrandSKU" runat="server" ErrorMessage="" ForeColor="Red" ControlToValidate="DDL_BrandSKU" InitialValue="" Display="Dynamic"></asp:RequiredFieldValidator>
                                                <div class="input-group-sm">
                                                    <asp:DropDownList ID="DDL_BrandSKU" runat="server" CssClass="form-control form-control-sm rounded"></asp:DropDownList>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-3">
                                            <div class="mb-3">
                                                <asp:Label ID="Lbl_Date_From" runat="server" AssociatedControlID="TB_Date_From" Text="Date From :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_Date_From" runat="server" ErrorMessage="Date is required " ControlToValidate="TB_Date_From" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                <asp:RegularExpressionValidator ID="REV_Date_From" runat="server" ControlToValidate="TB_Date_From" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                                <div class="input-group-sm">
                                                    <asp:TextBox ID="TB_Date_From" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>

                                        <div class="col-md-3">
                                            <div class="mb-3">
                                                <asp:Label ID="Lbl_Date_To" runat="server" AssociatedControlID="TB_Date_To" Text="Date To :" ForeColor="Blue" Font-Bold="true" Font-Size="Small"></asp:Label>
                                                <asp:RequiredFieldValidator ID="RFV_Date_To" runat="server" ErrorMessage="Date is required " ControlToValidate="TB_Date_To" InitialValue="" Display="Dynamic" ForeColor="Red"></asp:RequiredFieldValidator>
                                                <asp:RegularExpressionValidator ID="REV_Date_To" runat="server" ControlToValidate="TB_Date_To" ForeColor="Red" ErrorMessage="" Display="Dynamic" ValidationExpression=" "></asp:RegularExpressionValidator>
                                                <div class="input-group-sm">
                                                    <asp:TextBox ID="TB_Date_To" runat="server" CssClass="form-control form-control-sm rounded" Placeholder="" TextMode="Date"></asp:TextBox>
                                                </div>
                                            </div>
                                        </div>

                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <%--Button--%>
            <div class="col-md-6 center-margin">
                <div class="item form-group row">
                    <div class="col-md-6 col-sm-12">
                        <asp:Label ID="lbl_msg" runat="server" Text="Click SUBMIT to view Data!!"></asp:Label>
                    </div>
                    <div class="col-md-6 col-sm-12">
                        <asp:Button ID="ReportbtnCancel" runat="server" Text="Home" CssClass="btn btn-danger btn-sm" CausesValidation="false" PostBackUrl="~/qaqc_qaapprovals.aspx" />
                        <asp:Button ID="ReportbtnSubmit" runat="server" Text="Submit" CssClass="btn btn-success btn-sm" CausesValidation="false" OnClick="ReportbtnSubmit_Click" />
                        <asp:Button ID="ReportbtnReset" runat="server" Text="Reset" CssClass="btn btn-warning btn-sm" CausesValidation="false" OnClick="ReportbtnReset_Click" />
                    </div>
                </div>
            </div>


        <div class="row">
            <div class="col-md-12 col-sm-12 ">
                <div class="x_panel">
                    <div class="x_title">
                        <h2>
                            <asp:Label ID="lbl_viewname" runat="server" Text="Label"></asp:Label>
                            <asp:Button ID="ExportBtn" runat="server" Text="Export" CssClass="btn btn-info btn-sm" CausesValidation="false" />
                        </h2>
                        <div class="clearfix"></div>
                    </div>
                    <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" ShowHeaderWhenEmpty="true" CssClass="table table-striped table-hover table-bordered table-responsive table-sm table-condensed text-wrap" OnRowCommand="GridView1_RowCommand">
                        <Columns>

                            <asp:TemplateField HeaderText="Sl">
                                <ItemTemplate>
                                    <asp:Label ID="lblSl" runat="server" ReadOnly="true" ClientIDMode="Static" Text="Sl:"></asp:Label><strong><%# Container.DataItemIndex + 1 %></strong>
                                    <br />
                                    <asp:Label ID="lblPcrno" runat="server" CssClass="bold-text" ReadOnly="true" ClientIDMode="Static" Font-Bold="true" Text='<%# Eval("PcrNo") %>'></asp:Label>
                                    <br /><asp:Label ID="lbl_rowid" runat="server" Text='<%# Eval("Id") %>' Visible="true" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Plant and Line Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblPlant" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Plant: " + "<strong>" +  Eval("plant_name") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblLine" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Line: " + "<strong>" + Eval("line_name")  + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblCategory" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Category:" + "<strong>" + Eval("category_name") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblBrand" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Brand:" + "<strong>" + Eval("brand_name") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblSku" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "SKU:" + "<strong>" + Eval("SKU_name") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Submission Details">
                                <ItemTemplate>
                                    Date:<asp:Label ID="lbl_SubmittedDate" runat="server" Text='<%# Eval("SubmittedDate", "{0:dd-MM-yyyy}") %>' ForeColor="Brown" Font-Bold="true" /><br />
                                    Time:<asp:Label ID="lbl_SubmittedTime" runat="server" Text='<%# BindSubmittedTime(Eval("SubmittedTime")) %>' ForeColor="Brown" Font-Bold="true" /><br />
                                    Submitter:<asp:Label ID="lbl_EmployeeName" runat="server" Text='<%# Eval("EmployeeName") %>' />[<asp:Label ID="lbl_SubmittedByPNo" runat="server" Text='<%# Eval("SubmittedByEmployeeCode") %>' />]
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Water Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblProcessWater" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Temp.:" + "<strong>" + Eval("ProcessWaterTemp") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblWaterPh" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "PH:" + "<strong>" + Eval("WaterPH")+ "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblWaterHardness" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Hardness:" + "<strong>" + Eval("WaterHardness") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblWaterTest" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Test:" + "<strong>" + Eval("WaterTest")+ "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblTds" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Tds:" + "<strong>" + Eval("TDS")+ "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Maida Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblMaidaBrand" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Brand: " + "<strong>" + Eval("MaidaBrand") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblBatch" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Batch No:" + "<strong>" + Eval("MaidaBatchNo") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblMfg" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Mfg.: " + "<strong>" + Eval("MaidaMfgDate","{0:dd-MM-yyyy}") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <br />
                                    <asp:Label ID="lblAppColor" runat="server" ClientIDMode="Static" Text='<%# "App & Color: " + "<strong>" + (Eval("MaidaAppearanceColor") != null ? (Eval("MaidaAppearanceColor").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblAppComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForMaidaColor") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblFlvTaste" runat="server" ClientIDMode="Static" Text='<%# "Flv & Taste: " + " <strong>" + (Eval("MaidaFlavorAndTaste") != null ? (Eval("MaidaFlavorAndTaste").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblFlvComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentsForMaidaFlavourAndTaste")+ "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblGrittiness" runat="server" ClientIDMode="Static" Text='<%# "Grittiness:  " + " <strong>" + (Eval("MaidaGrittiness") != null ? (Eval("MaidaGrittiness").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblGrittinessComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Comment:" + "<strong>" + Eval("CommentForGrittiness") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Broken Biscuit Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblBBAppColor" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "App & Color: "+" <strong>" + (Eval("BBAppearanceColor") != null ? (Eval("BBAppearanceColor").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblBBComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:"+ "<strong>" + Eval("CommentForBBColor") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblBBFlvTaste" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Flv & Taste: "+" <strong>" + (Eval("BBMouthFeel") != null ? (Eval("BBMouthFeel").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblBBFlvComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Comment:" + "<strong>" + Eval("CommentForBBMouthFeel") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblMouthfeel" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Mouthfeel: " + " <strong>" + (Eval("BBFlavorAndTaste") != null ? (Eval("BBFlavorAndTaste").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblMouthfeelComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:"+ "<strong>" + Eval("CommentForBBFlavorAndTaste") + "</strong>"%>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="HVO Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblHvoSmell" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Smell: " + " <strong>" + (Eval("HvoSmell") != null ? (Eval("HvoSmell").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblHvoSmellComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Comment:"+ "<strong>" + Eval("CommentForHvoSmell") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblHvoTaste" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Taste: " + " <strong>"+ (Eval("HvoTaste") != null ? (Eval("HvoTaste").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblHvoTasteComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Comment:"+ "<strong>"+ Eval("CommentForHvoTaste") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblHvoTemp" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Temp:"+ "<strong>" + Eval("HvoTemp") + "</strong>"%>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="SMP Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblSmpSmell" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Smell: " + "<strong>" + (Eval("SmpSmell") != null ? (Eval("SmpSmell").ToString() == "1" ? "Ok" : "Not Ok") : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblSmpSmellComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>"+ Eval("CommentForSmpSmell")+ "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblSmpTaste" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Taste: " + "<strong>" + (Eval("SmpTaste") != null ? (Eval("SmpTaste").ToString() == "1" ? "Ok" : "Not Ok") : "N/A")  + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblSmpTasteComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForSmpTaste") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblSmpColor" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Color: " + "<strong>" + (Eval("SmpColor") != null ? (Eval("SmpColor").ToString() == "1" ? "Ok" : "Not Ok") : "N/A")  + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblSmpColorComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:"+ "<strong>" + Eval("CommentForSmpColor")+ "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Syrup Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblSyrupTemp" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Temp:" + "<strong>" + Eval("SyrupTemp") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblSyrupColor" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Color: " + " <strong>" + (Eval("SyrupColor") != null ? (Eval("HvoTaste").ToString() == "1" ? "Ok" : "Not Ok") : "N/A")  + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblSyrupColorComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForSyrupColor") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblPh" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "PH:" + "<strong>" + Eval("SyrupPH") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText=" Filter Seive Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblInvert" runat="server" ClientIDMode="Static" Text='<%# "Invert Syrup Bucket Filter Seive:" + "<strong>" + (Eval("InvertSyrpBucketFilter") != null ? (Eval("InvertSyrpBucketFilter").ToString() == "1" ? "Ok" : (Eval("InvertSyrpBucketFilter").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblInvertComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForISBF") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblSugar" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Sugar Sol Bucket Filter Seive:" + "<strong>" + (Eval("SugarSolBucketFilter") != null ? (Eval("SugarSolBucketFilter").ToString() == "1" ? "Ok" : (Eval("SugarSolBucketFilter").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A")  + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblSugarComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForSSBF") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Sheet Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblCreamer" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Creamer Bucket Filter Sheet:" + "<strong>" + (Eval("CreamerBucketFilter") != null ? (Eval("CreamerBucketFilter").ToString() == "1" ? "Ok" : (Eval("CreamerBucketFilter").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblCreamerComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForCBF") + "</strong>"%>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblGrinder" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Sugar Grinder Sheet:" + "<strong>" + (Eval("SugarGrindedSheet") != null ? (Eval("SugarGrindedSheet").ToString() == "1" ? "Ok" : (Eval("SugarGrindedSheet").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblGrinderComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForSGS") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Oil System Bucket Filter Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblOil" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Oil System Bucket Filter:" + "<strong>" + (Eval("OilSystemBucketFilter") != null ? (Eval("OilSystemBucketFilter").ToString() == "1" ? "Ok" : (Eval("OilSystemBucketFilter").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblOilComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForOSBF") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Oil Spray Sieve Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblOilSpray" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Oil Spray Seive:" + "<strong>" + (Eval("OilSpray") != null ? (Eval("OilSpray").ToString() == "1" ? "Ok" : (Eval("OilSpray").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblOilSprayComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%#"Comment:" + "<strong>" + Eval("CommentForOilSpray") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Milk Spray Sieve Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblMilk" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Milk Spray Seive:" + "<strong>" + (Eval("MilkSpray") != null ? (Eval("MilkSpray").ToString() == "1" ? "Ok" : (Eval("MilkSpray").ToString() == "0" ? "Not Ok" : "N/A")) : "N/A") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="LblMilkComment" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Comment:" + "<strong>" + Eval("CommentForMilkSpray") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Temp. Details">
                                <ItemTemplate>
                                    <asp:Label ID="lblCold" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Cold Room:" + "<strong>" + Eval("ColdRoomTemp") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblDeep" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "Deep Freeze:" + "<strong>" + Eval("DeepFreezeTemp") + "</strong>" %>'></asp:Label>
                                    <br />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Maida Image">
                                <ItemTemplate>
                                    <asp:Image ID="imgMaida" runat="server" ImageUrl='<%# Eval("MaidaImageUrl") != null ? ResolveUrl(Eval("MaidaImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="Maida Image" Width="100px" Height="100px" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="BB Image">
                                <ItemTemplate>
                                    <asp:Image ID="imgBB" runat="server" ImageUrl='<%# Eval("BBImageUrl") != null ? ResolveUrl(Eval("BBImageUrl").ToString()) : "~/Images/placeholder.jpg" %>' AlternateText="BB Image" Width="100px" Height="100px" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Approvals">
                                <ItemTemplate>
                                    <asp:Label ID="lblApproval1" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "A1:" + "<strong>" + Eval("Approver1EmployeeCode") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblApproval2" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "A2:" + "<strong>" +  Eval("Approver2EmployeeCode") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Label ID="lblApproval3" runat="server" ReadOnly="true" ClientIDMode="Static" Text='<%# "A3:" + "<strong>" + Eval("DottedLineApproverEmployeeCode") + "</strong>" %>'></asp:Label>
                                    <br />
                                    <asp:Button ID="btn_viewdetails" runat="server" Text="Approve" Font-Size="Smaller" CssClass="btn btn-sm btn-warning" CommandName="ViewApprove" CausesValidation="false" CommandArgument="<%# Container.DataItemIndex %>" />
                                    <%--<asp:Button ID="ApproveBtn" runat="server" Text="Approve" CssClass="btn btn-warning btn-sm" CausesValidation="false" CommandArgument='<%# Eval("PcrNo") %>' OnClick="ApproveBtn_Click" />--%>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </div>
            </div>
        </div>
    </div>
    </div>
</asp:Content>
