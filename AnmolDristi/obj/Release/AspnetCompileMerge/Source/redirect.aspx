<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="redirect.aspx.cs" Inherits="AnmolDristi.redirect" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Anmol Industries | Sales Reports - Second Domain</title>
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
                                <h3>Anmol Industries <small>Sales Reports</small> </h3>
                            </div>
                        </div>

                        <div class="clearfix"></div>

                        <div class="row">
                            <div class="col-md-12">
                                <div class="x_panel">
                                    <div class="x_title">
                                        <h2>Power BI <small>Reports </small></h2>
                                        <div class="clearfix"></div>
                                    </div>
                                    <div class="x_content">

                                        <div class="row">
                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://app.powerbi.com/groups/b8563467-c3cb-42a4-b70b-da17856af952/reports/4e61a07d-7016-4088-9a07-296bc26f4398?ctid=4922e92e-11cc-41a4-8327-402ddaae5277&pbi_source=linkShare" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/PrimarySales.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Primary Sales Dashboard</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://app.powerbi.com/links/w-L0nji9ws?ctid=4922e92e-11cc-41a4-8327-402ddaae5277&pbi_source=linkShare" target="_blank">
                                                            <img style="width: 60%; display: block;" src="WebData/sales.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Brand Report</p>
                                                    </div>
                                                </div>
                                            </div>


                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://app.powerbi.com/groups/b8563467-c3cb-42a4-b70b-da17856af952/reports/e18253f8-0478-4e93-aab9-d752695fea2d?ctid=4922e92e-11cc-41a4-8327-402ddaae5277&pbi_source=linkShare" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/SecondarySales.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Secondary Sales</p>
                                                    </div>
                                                </div>
                                            </div>

                                            <div class="col-md-55">
                                                <div class="thumbnail">
                                                    <div class="image view view-first">
                                                        <a href="https://app.powerbi.com/groups/b8563467-c3cb-42a4-b70b-da17856af952/reports/baabd841-7c1d-4fa1-9286-e813919d490a?ctid=4922e92e-11cc-41a4-8327-402ddaae5277&pbi_source=linkShare" target="_blank">
                                                            <img style="width: 70%; display: block;" src="WebData/TertiarySales.png" alt="image" height="100px" />
                                                        </a>
                                                    </div>
                                                    <div class="caption">
                                                        <p>Tertiary Sales</p>
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
                <!-- /page content -->
            </div>
        </div>
    </form>
</body>
</html>
