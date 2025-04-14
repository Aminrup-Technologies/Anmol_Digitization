using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace AnmolDristi
{
    /// <summary>
    /// Summary description for UploadImageHandler
    /// </summary>
    public class UploadImageHandler : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            try
            {
                // Retrieve the file and prefix from the request
                HttpPostedFile file = context.Request.Files["image"];
                string prefix = context.Request.Form["prefix"];

                if (file != null && file.ContentLength > 0 && !string.IsNullOrEmpty(prefix))
                {
                    // Define folder paths based on prefix
                    string targetFolderPath;
                    switch (prefix)
                    {
                        case "QCIR/ClrApp":
                            targetFolderPath = context.Server.MapPath("~/UploadedFiles/QCIR/ClrApp/");
                            break;
                        case "QCIR/DesignImp":
                            targetFolderPath = context.Server.MapPath("~/UploadedFiles/QCIR/DesignImp/");
                            break;
                        case "QAPC/MaidaImage":
                            targetFolderPath = context.Server.MapPath("~/UploadedFiles/QAPC/MaidaImage/");
                            break;
                        case "QAPC/BBImage":
                            targetFolderPath = context.Server.MapPath("~/UploadedFiles/QAPC/BBImage/");
                            break;
                        // Add more cases as needed for different prefixes
                        default:
                            targetFolderPath = context.Server.MapPath("~/UploadedFiles/DefaultFolder/");
                            break;
                    }

                    // Ensure the directory exists
                    if (!Directory.Exists(targetFolderPath))
                    {
                        Directory.CreateDirectory(targetFolderPath);
                    }

                    // Create the file name with timestamp
                    //string fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
                    string fileName = Path.GetFileNameWithoutExtension(file.FileName) + ".jpg";
                    string filePath = Path.Combine(targetFolderPath, fileName);

                    // Save the file to the specified directory
                    file.SaveAs(filePath);

                    // Construct the image URL for the client
                    string imageUrl = "/UploadedFiles/" + prefix + "/" + fileName;  // Corrected URL construction
                    context.Response.Write("{ \"imageUrl\": \"" + imageUrl + "\" }");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Write("{ \"error\": \"Invalid file or prefix\" }");
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                context.Response.Write("{ \"error\": \"" + ex.Message + "\" }");
            }
        }

        public bool IsReusable => false;
    }
}