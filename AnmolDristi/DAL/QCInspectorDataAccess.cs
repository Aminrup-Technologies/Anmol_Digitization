using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;

namespace AnmolDristi.DAL
{
    public class QCInspectorDataAccess
    {
        private readonly string connectionString;

        public QCInspectorDataAccess()
        {
            // Get the connection string from the configuration file
            connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }


        public void InsertQCInspectorDataOld(string plantName, string line, string productCategory, string productBrand, string SKUId, int numberOfPieces, decimal gaugeValue, decimal dryWeight, decimal dippedWeight, string varietyOrLotNo, string bakingTime, string bakingTime2, int ColorAppearance, string CommentsForColorAppearance,  int flavourAndTaste, string commentsForFlavourAndTaste, int DesignImplementation, string CommentsForDesignImplementation,int TextureBite,string CommentsForTextureBite, string shapeOrSize, string commentsForShapeOrSize, decimal moisture, decimal aWmax, decimal pHvalue, decimal gaugeLength, string CommentsGaugeLength, decimal weightWithoutOil, decimal weightWithOil, decimal oilPercentage, decimal packetWeight, string designAndImplementation, string colourAndAppearance, int submittedById, DateTime submittedDate, TimeSpan submittedTime, string Shift, string SubmittedByPNo)
        {
            

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertQCInspectorData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PlantName", plantName);
                    command.Parameters.AddWithValue("@Line", line);
                    command.Parameters.AddWithValue("@ProductCategory", productCategory);
                    command.Parameters.AddWithValue("@ProductBrand", productBrand);
                    command.Parameters.AddWithValue("@SKUId", SKUId);
                    command.Parameters.AddWithValue("@NumberOfPieces", numberOfPieces);
                    command.Parameters.AddWithValue("@GaugeValue", gaugeValue); // Lenght
                    command.Parameters.AddWithValue("@DryWeight", dryWeight);  // Breadth
                    command.Parameters.AddWithValue("@DippedWeight", dippedWeight); //Height
                    command.Parameters.AddWithValue("@VarietyOrLotNo", varietyOrLotNo);
                    command.Parameters.AddWithValue("@BakingTime", bakingTime);

                    //----- Added on 12-Aug-2024 ------START----//
                    command.Parameters.AddWithValue("@BakingTime2", bakingTime);
                    //----- Added on 12-Aug-2024 -------END---//

                    command.Parameters.AddWithValue("@ColorAppearance", ColorAppearance);
                    command.Parameters.AddWithValue("@CommentsForColorAppearance", CommentsForColorAppearance);
                    command.Parameters.AddWithValue("@FlavourAndTaste", flavourAndTaste);
                    command.Parameters.AddWithValue("@CommentsForFlavourAndTaste", commentsForFlavourAndTaste);
                    command.Parameters.AddWithValue("@DesignImplementation", DesignImplementation);
                    command.Parameters.AddWithValue("@CommentsForDesignImplementation", CommentsForDesignImplementation);
                    command.Parameters.AddWithValue("@TextureBite", TextureBite);
                    command.Parameters.AddWithValue("@CommentsForTextureBite", CommentsForTextureBite);
                    command.Parameters.AddWithValue("@ShapeOrSize", shapeOrSize);
                    command.Parameters.AddWithValue("@CommentsForShapeOrSize", commentsForShapeOrSize);
                    command.Parameters.AddWithValue("@Moisture", moisture);

                    //----- Added on 12-Aug-2024 ----START------//

                    command.Parameters.AddWithValue("@aW_max", aWmax);
                    command.Parameters.AddWithValue("@pH_value", pHvalue);

                    //----- Added on 12-Aug-2024 -----END-----//

                    command.Parameters.AddWithValue("@GaugeLength", gaugeLength);
                    command.Parameters.AddWithValue("@CommentsGaugeLength", CommentsGaugeLength);
                    command.Parameters.AddWithValue("@WeightWithoutOil", weightWithoutOil);
                    command.Parameters.AddWithValue("@WeightWithOil", weightWithOil);
                    command.Parameters.AddWithValue("@OilPercentage", oilPercentage);
                    command.Parameters.AddWithValue("@PacketWeight", packetWeight);
                    command.Parameters.AddWithValue("@DesignAndImplementation", designAndImplementation);
                    command.Parameters.AddWithValue("@ColourAndAppearance", colourAndAppearance);
                    command.Parameters.AddWithValue("@SubmittedById", submittedById);
                    command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                    command.Parameters.AddWithValue("@SubmittedTime", submittedTime);

                    ShiftManager shiftManager = new ShiftManager();
                    string currentShift = shiftManager.GetCurrentShiftType();

                    command.Parameters.AddWithValue("@Shift", currentShift);
                    command.Parameters.AddWithValue("@SubmittedByPNo", SubmittedByPNo);

                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }



        public void InsertQCInspectorData(
            string plantName, string line, string productCategory, string productBrand, string SKUId,
            int numberOfPieces, decimal gaugeValue, decimal dryWeight, decimal dippedWeight,
            string varietyOrLotNo, string bakingTime, string bakingTime2,
            int ColorAppearance, string CommentsForColorAppearance,
            int flavourAndTaste, string commentsForFlavourAndTaste,
            int DesignImplementation, string CommentsForDesignImplementation,
            int TextureBite, string CommentsForTextureBite,
            string shapeOrSize, string commentsForShapeOrSize,
            decimal moisture, decimal aWmax, decimal pHvalue, decimal gaugeLength,
            string CommentsGaugeLength, decimal weightWithoutOil, decimal weightWithOil,
            decimal oilPercentage, decimal packetWeight, string designAndImplementation,
            string colourAndAppearance, string Remarks, int submittedById, DateTime submittedDate,
            TimeSpan submittedTime, string Shift, string SubmittedByPNo, int FormID, string Approver1EmployeeCode, string Approver2EmployeeCode, string DottedLineApproverEmployeeCode)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertQCInspectorData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PlantName", plantName);
                    command.Parameters.AddWithValue("@Line", line);
                    command.Parameters.AddWithValue("@ProductCategory", productCategory);
                    command.Parameters.AddWithValue("@ProductBrand", productBrand);
                    command.Parameters.AddWithValue("@SKUId", SKUId);
                    command.Parameters.AddWithValue("@NumberOfPieces", numberOfPieces);
                    command.Parameters.AddWithValue("@GaugeValue", gaugeValue);
                    command.Parameters.AddWithValue("@DryWeight", dryWeight);
                    command.Parameters.AddWithValue("@DippedWeight", dippedWeight);
                    command.Parameters.AddWithValue("@VarietyOrLotNo", varietyOrLotNo);
                    command.Parameters.AddWithValue("@BakingTime", bakingTime);
                    command.Parameters.AddWithValue("@BakingTime2", bakingTime2);
                    command.Parameters.AddWithValue("@ColorAppearance", ColorAppearance);
                    command.Parameters.AddWithValue("@CommentsForColorAppearance", CommentsForColorAppearance);
                    command.Parameters.AddWithValue("@FlavourAndTaste", flavourAndTaste);
                    command.Parameters.AddWithValue("@CommentsForFlavourAndTaste", commentsForFlavourAndTaste);
                    command.Parameters.AddWithValue("@DesignImplementation", DesignImplementation);
                    command.Parameters.AddWithValue("@CommentsForDesignImplementation", CommentsForDesignImplementation);
                    command.Parameters.AddWithValue("@TextureBite", TextureBite);
                    command.Parameters.AddWithValue("@CommentsForTextureBite", CommentsForTextureBite);
                    command.Parameters.AddWithValue("@ShapeOrSize", shapeOrSize);
                    command.Parameters.AddWithValue("@CommentsForShapeOrSize", commentsForShapeOrSize);
                    command.Parameters.AddWithValue("@Moisture", moisture);
                    command.Parameters.AddWithValue("@aW_max", aWmax);
                    command.Parameters.AddWithValue("@pH_value", pHvalue);
                    command.Parameters.AddWithValue("@GaugeLength", gaugeLength);
                    command.Parameters.AddWithValue("@CommentsGaugeLength", CommentsGaugeLength);
                    command.Parameters.AddWithValue("@WeightWithoutOil", weightWithoutOil);
                    command.Parameters.AddWithValue("@WeightWithOil", weightWithOil);
                    command.Parameters.AddWithValue("@OilPercentage", oilPercentage);
                    command.Parameters.AddWithValue("@PacketWeight", packetWeight);
                    command.Parameters.AddWithValue("@DesignAndImplementation", designAndImplementation);
                    command.Parameters.AddWithValue("@ColourAndAppearance", colourAndAppearance);

                    //Newly added on 28-Nov-2024------------------------
                    command.Parameters.AddWithValue("@Remarks", Remarks);


                    command.Parameters.AddWithValue("@SubmittedById", submittedById);
                    command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                    command.Parameters.AddWithValue("@SubmittedTime", submittedTime);
                    command.Parameters.AddWithValue("@Shift", Shift);
                    command.Parameters.AddWithValue("@SubmittedByPNo", SubmittedByPNo);

                    command.Parameters.AddWithValue("@FormID", FormID);
                    command.Parameters.AddWithValue("@SubmittedByEmployeeCode", SubmittedByPNo);
                    command.Parameters.AddWithValue("@Approver1EmployeeCode", Approver1EmployeeCode);
                    command.Parameters.AddWithValue("@Approver2EmployeeCode", Approver2EmployeeCode);
                    command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", DottedLineApproverEmployeeCode);
                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }






        //private void LogToTextFile(params object[] data)
        //{
        //    string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
        //    string directoryPath = "QCInspectorLogs";
        //    string filePath = Path.Combine(directoryPath, $"QCInspectorLog_{currentDate}.txt");

        //    // Check if the directory exists, if not, create it
        //    if (!Directory.Exists(directoryPath))
        //    {
        //        Directory.CreateDirectory(directoryPath);
        //    }

        //    // Check if the file exists, if not, create it
        //    if (!File.Exists(filePath))
        //    {
        //        string headers = string.Join(", ", data.Select((param, index) => $"@param{index + 1}"));
        //        using (StreamWriter writer = File.CreateText(filePath))
        //        {
        //            writer.WriteLine(headers);
        //        }
        //    }

        //    // Write data to text file
        //    using (StreamWriter writer = File.AppendText(filePath))
        //    {
        //        writer.WriteLine(string.Join(", ", data));
        //    }
        //}
    }
}