<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="apps.aspx.cs" Inherits="AnmolDristi.apps" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Anmol Industries | Apps</title>
    <link href="Resources/font-awesome/css/font-awesome.min.css" rel="stylesheet" />
    <link href="Resources/bootstrap/dist/css/bootstrap.min.css" rel="stylesheet" />
    <link href="Resources/nprogress/nprogress.css" rel="stylesheet" />
    <link href="Resources/custom/custom.css" rel="stylesheet" />
    <style type="text/css">
        .thumbnail {
            text-align: center;
            position: relative;
            overflow: hidden;
            border: 1px solid #ddd;
            border-radius: 4px;
            background-color: #fff;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }

            .thumbnail .image {
                position: relative;
            }

                .thumbnail .image img {
                    display: block;
                    margin: 0 auto; /* Centers the image */
                }

            .thumbnail .caption {
                padding: 10px;
                text-align: center; /* Centers the caption text */
                font-weight: bold;
            }

            .thumbnail .mask {
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background-color: rgba(0, 0, 0, 0.5);
                color: #fff;
                opacity: 0;
                transition: opacity 0.3s ease;
                display: flex;
                align-items: center;
                justify-content: center;
            }

                .thumbnail .mask p {
                    margin: 0;
                }

            .thumbnail:hover .mask {
                opacity: 1;
            }

            .thumbnail .tools {
                position: absolute;
                bottom: 10px;
                right: 10px;
            }

                .thumbnail .tools a {
                    color: #fff;
                    margin-left: 10px;
                    font-size: 16px;
                }

                    .thumbnail .tools a:hover {
                        color: #ddd;
                    }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container body">
            <div class="main_container">
                <!-- page content -->
                <div class="right_col" role="main">
                    <div class="">
                        <div class="page-title">
                            <div class="title_left">
                                <h3>Anmol Industries <small>Applications</small> </h3>
                            </div>
                        </div>

                        <div class="clearfix"></div>

                        <div class="row">
                            <div class="col-md-12">
                                <div class="x_panel">
                                    <div class="x_title">
                                        <h2>Application <small>Store </small></h2>
                                        <div class="clearfix"></div>
                                    </div>
                                    <div class="x_content">

                                        <div class="row" >
                                            <div class="col-md-55" id="powerbi_com" runat="server" visible="false">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://app.powerbi.com/groups/b8563467-c3cb-42a4-b70b-da17856af952/reports/566706ea-ea2c-43e9-8711-baa6de43cd41/ReportSection86019e086bd4cb08a3bf?experience=power-bi" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/sales.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Sales Dashboard</p>
                                                        <p>(anmolindustries.com Users)</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55" id="powerbi_net" runat="server" visible="false">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="redirect.aspx" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/sales.png" alt="image" height="100px" />
                                                        </a>
                                                        <%--<div class="mask">
                                                            <p>Your Text</p>
                                                            <div class="tools tools-bottom">
                                                                <a href="https://app.powerbi.com/links/CiVUK9iyz-?ctid=4922e92e-11cc-41a4-8327-402ddaae5277&pbi_source=linkShare"><i class="fa fa-link"></i></a>
                                                            </div>
                                                        </div>--%>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Sales Dashboard</p>
                                                        <p>(anmolbiscuits.net Users)</p>
                                                    </div>
                                                </div>
                                            </div>


                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="login.aspx">
                                                            <img style="width: 70%; display: block;" src="WebData/qaqc.png" alt="image" height="100px" />
                                                        </a>
                                                        <%--<div class="mask">
                                                            <p>Quality Reporting Portal</p>
                                                            <div class="tools tools-bottom">
                                                                <a href="login.aspx"><i class="fa fa-link"></i></a>
                                                            </div>
                                                        </div>--%>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Quality Reporting</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://portal.zinghr.com/2015/pages/authentication/zing.aspx?ccode=anmol" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/anmolaing.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>ZingHR - Anmol Dost</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://apps.powerapps.com/play/e/default-4922e92e-11cc-41a4-8327-402ddaae5277/a/bbf368a6-e6e7-4499-ad3c-162d14f1f549?tenantId=4922e92e-11cc-41a4-8327-402ddaae5277&hint=d15542da-3559-4df1-aec5-01c898c3dae2&source=sharebutton&sourcetime=1734523733202" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/approval.jpg" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>e-Approval</p>
                                                    </div>
                                                </div>
                                            </div>


                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://anmolfiori.anmolindustries.com:44316/sap/bc/ui2/flp?sap-client=700" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/sapfiori.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>SAP Fiori</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://anmolindustries.edurigo.com/authentication/signin" target="_blank">
                                                            <img style="width: 80%; display: block;" src="WebData/anmolgurukul.png" alt="image" height="70%" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Gurukul</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="http://115.112.186.71:8005/login.aspx" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/confbook.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Conference Booking</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="http://115.112.186.71:8009/pages/UI.php" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/itop-logo-external.png" alt="image" height="80%" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>IT Helpdesk</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="http://115.112.186.71:8001/" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/webdatacapture.jpg" alt="image" height="80%" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>CMS Machines</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="http://115.112.186.71:8002/TimeOffice/Login" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/webtos.png" alt="image" height="80%" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>CMS Reports</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <%--<div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask">
                                                            <p>Your Text</p>
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Snow and Ice Incoming for the South</p>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask">
                                                            <p>Your Text</p>
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Snow and Ice Incoming for the South</p>
                                                    </div>
                                                </div>
                                            </div>


                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask no-caption">
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>
                                                            <strong>Image Name</strong>
                                                        </p>
                                                        <p>Snow and Ice Incoming</p>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask no-caption">
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>
                                                            <strong>Image Name</strong>
                                                        </p>
                                                        <p>Snow and Ice Incoming</p>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask no-caption">
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>
                                                            <strong>Image Name</strong>
                                                        </p>
                                                        <p>Snow and Ice Incoming</p>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask no-caption">
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>
                                                            <strong>Image Name</strong>
                                                        </p>
                                                        <p>Snow and Ice Incoming</p>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <img style="width: 100%; display: block;" src="images/media.jpg" alt="image" />
                                                        <div class="mask no-caption">
                                                            <div class="tools tools-bottom">
                                                                <a href="#"><i class="fa fa-link"></i></a>
                                                                <a href="#"><i class="fa fa-pencil"></i></a>
                                                                <a href="#"><i class="fa fa-times"></i></a>
                                                            </div>
                                                        </div>
                                                    </div>
                                                    <div class="caption">
                                                        <p>
                                                            <strong>Image Name</strong>
                                                        </p>
                                                        <p>Snow and Ice Incoming</p>
                                                    </div>
                                                </div>
                                            </div>--%>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <!-- /page content -->
            </div>
        </div>
    </form>
</body>
</html>
