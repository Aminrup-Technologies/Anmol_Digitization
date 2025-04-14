using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Web;

namespace AnmolDristi.DAL
{
    public class QAProcessCheckingDataAcess
    {
        private readonly string connectionString;

        public QAProcessCheckingDataAcess()
        {
            // Get the connection string from the configuration file
            connectionString = ConfigurationManager.ConnectionStrings["DbConn"].ConnectionString;
        }

        public void InsertBasicData(string pcrNo, string plantName, string line, string productCategory, string productBrand, string skuId, DateTime date, string shift,
                                    TimeSpan time, decimal processWaterTemp, string commentForProcessWater, decimal waterPh, string commentForWaterPh,
                                    decimal waterHardness, string commentForWaterHardness, string waterTest, decimal tds, string commentForTds, string maidaBrand,
                                    string maidaBatchNo, DateTime maidaMfg, int maidaAppColor, string commentForMaidaAppColor, int maidaFlavorTaste,
                                    string commentForMaidaFlavorTaste, int maidaGrittiness, string commentsForMaidaGrittiness, int bbAppColor, string commentForBBColor,
                                    int bbMouthFeel, string commentForBBMouthFeel, int bbFlavorTaste, string commentForBBFlavorTaste, int hvoSmell,
                                    string commentForHvoSmell, int hvoTaste, string commentForHvoTaste, decimal hvoTemp, string commentForHvoTemp, int smpSmell,
                                    string commentForSmpSmell, int smpTaste, string commentForSmpTaste, int smpColor, string commentForSmpColor, decimal syrupTemp,
                                    string commentForSyrupTemp, int syrupColor, string commentForSyrupColor, decimal syrupPh, string commentForSyrupPh,
                                    int invertSyrup, string commentForInvertSyrup, int sugarSol, string commentForSugarSol, int creamerBucket, string commentForCreamerBucket,
                                    int sugarGrinder, string commentForSugarGrinder, int oilSystem, string commentForOilSystem, int oilSpray,
                                    string commentForOilSpray, int milkSpray, string commentForMilkSpray, decimal coldRoomTemp, string commentForColdRoom,
                                    decimal DeepFreezeTemp, string commentForDeepFreeze, string ImgLink1, string ImgLink2)

        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("sp_InsertProcessCheckingBasicData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PcrNo", pcrNo);
                    command.Parameters.AddWithValue("@PlantName", plantName);
                    command.Parameters.AddWithValue("@Line", line);
                    command.Parameters.AddWithValue("@ProductCategory", productCategory);
                    command.Parameters.AddWithValue("@ProductBrand", productBrand);
                    command.Parameters.AddWithValue("@SkuId", skuId);
                    //command.Parameters.AddWithValue("@Date", date);
                    command.Parameters.AddWithValue("@Shift", shift);
                    //command.Parameters.AddWithValue("@Time", time);
                    command.Parameters.AddWithValue("@ProcessWaterTemp", processWaterTemp);
                    command.Parameters.AddWithValue("@ProcessWaterCmnt", commentForProcessWater);
                    command.Parameters.AddWithValue("@WaterPH", waterPh);
                    command.Parameters.AddWithValue("@WaterPhCmnt", commentForWaterPh);
                    command.Parameters.AddWithValue("@WaterHardness", waterHardness);
                    command.Parameters.AddWithValue("@HardnessCmnt", commentForWaterHardness);
                    command.Parameters.AddWithValue("@WaterTest", waterTest);
                    command.Parameters.AddWithValue("@TDS", tds);
                    command.Parameters.AddWithValue("@TdsCmnt", commentForTds);
                    command.Parameters.AddWithValue("@MaidaBrand", maidaBrand);
                    command.Parameters.AddWithValue("@MaidaBatchNo", maidaBatchNo);
                    command.Parameters.AddWithValue("@MaidaMfgDate", maidaMfg);
                    command.Parameters.AddWithValue("@MaidaAppearanceColor", maidaAppColor);
                    command.Parameters.AddWithValue("@CommentForMaidaColor", commentForMaidaAppColor);
                    command.Parameters.AddWithValue("@MaidaFlavorAndTaste", maidaFlavorTaste);
                    command.Parameters.AddWithValue("@CommentsForMaidaFlavourAndTaste", commentForMaidaFlavorTaste);
                    command.Parameters.AddWithValue("@MaidaGrittiness", maidaGrittiness);
                    command.Parameters.AddWithValue("@CommentForGrittiness", commentsForMaidaGrittiness);
                    command.Parameters.AddWithValue("@BBAppearanceColor", bbAppColor);
                    command.Parameters.AddWithValue("@CommentForBBColor", commentForBBColor);
                    command.Parameters.AddWithValue("@BBMouthFeel", bbMouthFeel);
                    command.Parameters.AddWithValue("@CommentForBBMouthFeel", commentForBBMouthFeel);
                    command.Parameters.AddWithValue("@BBFlavorAndTaste", bbFlavorTaste);
                    command.Parameters.AddWithValue("@CommentForBBFlavorAndTaste", commentForBBFlavorTaste);
                    command.Parameters.AddWithValue("@HvoSmell", hvoSmell);
                    command.Parameters.AddWithValue("@CommentForHvoSmell", commentForHvoSmell);
                    command.Parameters.AddWithValue("@HvoTaste", hvoTaste);
                    command.Parameters.AddWithValue("@CommentForHvoTaste", commentForHvoTaste);
                    command.Parameters.AddWithValue("@HvoTemp", hvoTemp);
                    command.Parameters.AddWithValue("@HvoCmnt", commentForHvoTemp);
                    command.Parameters.AddWithValue("@SmpSmell", smpSmell);
                    command.Parameters.AddWithValue("@CommentForSmpSmell", commentForSmpSmell);
                    command.Parameters.AddWithValue("@SmpTaste", smpTaste);
                    command.Parameters.AddWithValue("@CommentForSmpTaste", commentForSmpTaste);
                    command.Parameters.AddWithValue("@SmpColor", smpColor);
                    command.Parameters.AddWithValue("@CommentForSmpColor", commentForSmpColor);
                    command.Parameters.AddWithValue("@SyrupTemp", syrupTemp);
                    command.Parameters.AddWithValue("@SyrupCmnt", commentForSyrupTemp);
                    command.Parameters.AddWithValue("@SyrupColor", syrupColor);
                    command.Parameters.AddWithValue("@CommentForSyrupColor", commentForSyrupColor);
                    command.Parameters.AddWithValue("@SyrupPH", syrupPh);
                    command.Parameters.AddWithValue("@SyrupPhCmnt", commentForSyrupPh);
                    command.Parameters.AddWithValue("@InvertSyrpBucketFilter", invertSyrup);
                    command.Parameters.AddWithValue("@CommentForISBF", commentForInvertSyrup);
                    command.Parameters.AddWithValue("@SugarSolBucketFilter", sugarSol);
                    command.Parameters.AddWithValue("@CommentForSSBF", commentForSugarSol);
                    command.Parameters.AddWithValue("@CreamerBucketFilter", creamerBucket);
                    command.Parameters.AddWithValue("@CommentForCBF", commentForCreamerBucket);
                    command.Parameters.AddWithValue("@SugarGrindedSheet", sugarGrinder);
                    command.Parameters.AddWithValue("@CommentForSGS", commentForSugarGrinder);
                    command.Parameters.AddWithValue("@OilSystemBucketFilter", oilSystem);
                    command.Parameters.AddWithValue("@CommentForOSBF", commentForOilSystem);
                    command.Parameters.AddWithValue("@OilSpray", oilSpray);
                    command.Parameters.AddWithValue("@CommentForOilSpray", commentForOilSpray);
                    command.Parameters.AddWithValue("@MilkSpray", milkSpray);
                    command.Parameters.AddWithValue("@CommentForMilkSpray", commentForMilkSpray);
                    command.Parameters.AddWithValue("@ColdRoomTemp", coldRoomTemp);
                    command.Parameters.AddWithValue("@ColdRoomCmnt", commentForColdRoom);
                    command.Parameters.AddWithValue("@DeepFreezeTemp", DeepFreezeTemp);
                    command.Parameters.AddWithValue("@DeepFreezeCmnt", commentForDeepFreeze);
                    command.Parameters.AddWithValue("@MaidaImageUrl", ImgLink1);
                    command.Parameters.AddWithValue("@BBImageUrl", ImgLink2);
                    command.Parameters.AddWithValue("@BasicData_Status", 1);
                    command.Parameters.AddWithValue("@RM_Status", 0);
                    command.Parameters.AddWithValue("@OvenData_Status", 0);
                    command.Parameters.AddWithValue("@FinalSubmission", 0);


                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }


            }
        }


        //public void InsertRawMaterialData(string PcrNo, string maidaBrandName, decimal maidaWgt, decimal sugarWgt, decimal butter, decimal smp,
        //                    decimal processWater, decimal lecithin, decimal gms, decimal ssl, decimal glucose, decimal hvo, decimal syrup,
        //                    decimal malt, decimal bb, decimal abc, decimal sbc, decimal smbs, decimal wheyPowder, decimal condenceMilk, decimal salt,
        //                    decimal yeast, decimal e1, decimal caramel)
        //{
        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand command = new SqlCommand("InsertRawMaterialData", connection))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;

        //            // Add parameters
        //            command.Parameters.AddWithValue("@PcrNo", PcrNo);
        //            command.Parameters.AddWithValue("@MaidaBrandName", maidaBrandName);
        //            command.Parameters.AddWithValue("@MaidaActWgt", maidaWgt);
        //            command.Parameters.AddWithValue("@SugarActWgt", sugarWgt);
        //            command.Parameters.AddWithValue("@ButterActWgt", butter);
        //            command.Parameters.AddWithValue("@SmpActWgt", smp);
        //            command.Parameters.AddWithValue("@ProcessWaterActualWgt", processWater);
        //            command.Parameters.AddWithValue("@LecithinActWgt", lecithin);
        //            command.Parameters.AddWithValue("@GmsActWgt", gms);
        //            command.Parameters.AddWithValue("@PasteOrPowderActWgt", ssl);
        //            command.Parameters.AddWithValue("@GlucoseActWgt", glucose);
        //            command.Parameters.AddWithValue("@HvoActWgt", hvo);
        //            command.Parameters.AddWithValue("@SyrupActWgt", syrup);
        //            command.Parameters.AddWithValue("@MaltActWgt", malt);
        //            command.Parameters.AddWithValue("@BBActWgt", bb);
        //            command.Parameters.AddWithValue("@AbcActWgt", abc);
        //            command.Parameters.AddWithValue("@SbcActWgt", sbc);
        //            command.Parameters.AddWithValue("@SmbsActWgt", smbs);
        //            command.Parameters.AddWithValue("@WheyPowderActWgt", wheyPowder);
        //            command.Parameters.AddWithValue("@MilkActWgt", condenceMilk);
        //            command.Parameters.AddWithValue("@SaltActWgt", salt);
        //            command.Parameters.AddWithValue("@YeastActWgt", yeast);
        //            command.Parameters.AddWithValue("@E1ActWgt", e1);
        //            command.Parameters.AddWithValue("@CaramelActWgt", caramel);



        //            // Open the connection and execute the command
        //            connection.Open();
        //            command.ExecuteNonQuery();
        //        }

        //    }
        //}


        public void InsertSpongeData(string PcrNo, decimal? roomTemp, string commentForRoomTemp, int? drumCovered, string commentForDrumCovered,
                                        int? quality, string commentForQuality, TimeSpan? standingTime, string commentForStandingTime, decimal? temp,
                                        string commentForTemp, string YesNo)

        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertSpongeData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PcrNo", PcrNo);
                    command.Parameters.AddWithValue("@RoomTemp", roomTemp);
                    command.Parameters.AddWithValue("@RoomTempCmnt", commentForRoomTemp);
                    command.Parameters.AddWithValue("@DrumCovered", drumCovered);
                    command.Parameters.AddWithValue("@DrumCmnt", commentForDrumCovered);
                    command.Parameters.AddWithValue("@Quality", quality);
                    command.Parameters.AddWithValue("@QualityCmnt", commentForQuality);
                    command.Parameters.AddWithValue("@StandingTime", standingTime);
                    command.Parameters.AddWithValue("@StandingTimeCmnt", commentForStandingTime);
                    command.Parameters.AddWithValue("@Temp", temp);
                    command.Parameters.AddWithValue("@TempCmnt", commentForTemp);
                    command.Parameters.AddWithValue("@NotApplicable", YesNo);

                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }


        public void InsertDoughData(string PcrNo, decimal doughTemp, string commentForDoughTemp, TimeSpan restTime, string commentForRestTime,
                                       int metalDetector, string commentForMetalDetector, int processSequence, string commentForProcessSequence,
                                       TimeSpan creamingTime, string commentForCreamingTime, TimeSpan mixingTime, string commentForMixingTime,
                                       TimeSpan bakingTime, string commentForBakingTime, int diceRmp, string commentForDice, int doughCondition, string commentForDoughCondition)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertDoughData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PcrNo", PcrNo);
                    command.Parameters.AddWithValue("@DoughTemp", doughTemp);
                    command.Parameters.AddWithValue("@DoughTempCmnt", commentForDoughTemp);
                    command.Parameters.AddWithValue("@DoughRestTime", restTime);
                    command.Parameters.AddWithValue("@DoughRestTimeCmnt", commentForRestTime);
                    command.Parameters.AddWithValue("@MetalDetector", metalDetector);
                    command.Parameters.AddWithValue("@DetectorCmnt", commentForMetalDetector);
                    command.Parameters.AddWithValue("@ProcessSequence", processSequence);
                    command.Parameters.AddWithValue("@ProcessCmnt", commentForProcessSequence);
                    command.Parameters.AddWithValue("@CreamingTime", creamingTime);
                    command.Parameters.AddWithValue("@CreamingTimeCmnt", commentForCreamingTime);
                    command.Parameters.AddWithValue("@MixingTime", mixingTime);
                    command.Parameters.AddWithValue("@MixingTimeCmnt", commentForMixingTime);
                    command.Parameters.AddWithValue("@BakingTime", bakingTime);
                    command.Parameters.AddWithValue("@BakingTimeCmnt", commentForBakingTime);
                    command.Parameters.AddWithValue("@DiceRpm", diceRmp);
                    command.Parameters.AddWithValue("@DiceRpmCmnt", commentForDice);
                    command.Parameters.AddWithValue("@DoughConditon", doughCondition);
                    command.Parameters.AddWithValue("@DoughCmnt", commentForDoughCondition);


                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }

            }
        }

        //public void InsertOvenData(string PcrNo, string zone1Top, string zone1Bottom, string zone2Top, string zone2Bottom, string zone3Top, string zone3Bottom,
        //                                string zone4Top, string zone4Bottom, string zone5Top, string zone5Bottom, string zone6Top, string zone6Bottom,
        //                        string DPzone1Top, string DPzone1Bottom, string DPzone2Top, string DPzone2Bottom, string DPzone3Top, string DPzone3Bottom,
        //                        string DPzone4Top, string DPzone4Bottom, string DPzone5Top, string DPzone5Bottom, string DPzone6Top, string DPzone6Bottom)
        //{
        //    using (SqlConnection connection = new SqlConnection(connectionString))
        //    {
        //        using (SqlCommand command = new SqlCommand("InsertOvenData", connection))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;

        //            // Add parameters
        //            command.Parameters.AddWithValue("PcrNo", PcrNo);
        //            command.Parameters.AddWithValue("@OvenZone1Top", zone1Top);
        //            command.Parameters.AddWithValue("@OvenZone1Bottom", zone1Bottom);
        //            command.Parameters.AddWithValue("@OvenZone2Top", zone2Top);
        //            command.Parameters.AddWithValue("@OvenZone2Bottom", zone2Bottom);
        //            command.Parameters.AddWithValue("@OvenZone3Top", zone3Top);
        //            command.Parameters.AddWithValue("@OvenZone3Bottom", zone3Bottom);
        //            command.Parameters.AddWithValue("@OvenZone4Top", zone4Top);
        //            command.Parameters.AddWithValue("@OvenZone4Bottom", zone4Bottom);
        //            command.Parameters.AddWithValue("@OvenZone5Top", zone5Top);
        //            command.Parameters.AddWithValue("@OvenZone5Bottom", zone5Bottom);
        //            command.Parameters.AddWithValue("@OvenZone6Top", zone6Top);
        //            command.Parameters.AddWithValue("@OvenZone6Bottom", zone6Bottom);

        //            command.Parameters.AddWithValue("@DamperZone1Top", DPzone1Top);
        //            command.Parameters.AddWithValue("@DamperZone1Bottom", DPzone1Bottom);
        //            command.Parameters.AddWithValue("@DamperZone2Top", DPzone2Top);
        //            command.Parameters.AddWithValue("@DamperZone2Bottom", DPzone2Bottom);
        //            command.Parameters.AddWithValue("@DamperZone3Top", DPzone3Top);
        //            command.Parameters.AddWithValue("@DamperZone3Bottom", DPzone3Bottom);
        //            command.Parameters.AddWithValue("@DamperZone4Top", DPzone4Top);
        //            command.Parameters.AddWithValue("@DamperZone4Bottom", DPzone4Bottom);
        //            command.Parameters.AddWithValue("@DamperZone5Top", DPzone5Top);
        //            command.Parameters.AddWithValue("@DamperZone5Bottom", DPzone5Bottom);
        //            command.Parameters.AddWithValue("@DamperZone6Top", DPzone6Top);
        //            command.Parameters.AddWithValue("@DamperZone6Bottom", DPzone6Bottom);


        //            // Open the connection and execute the command
        //            connection.Open();
        //            command.ExecuteNonQuery();
        //        }

        //    }
        //}


        public void InsertVerifiedData(string PcrNo, int balanceCond, string commentForBalanceCond, string rawBiscuitWgt,
                                            int submittedById, DateTime submittedDate, TimeSpan submittedTime, string submittedByPno,
                                            string submittedByEmployeeCode, string approver1EmployeeCode, int approver1_Status, TimeSpan? approver1_TimeStamp,
                                            string approver2EmployeeCode, int approver2_Status, TimeSpan? approver2_TimeStamp,
                                            string dottedLineApproverEmployeeCode, int dottedApprover_Status, TimeSpan? dottedApprover_TimeStamp)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertVerifiedData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@PcrNo", PcrNo);
                    command.Parameters.AddWithValue("@WghBalanceCond", balanceCond);
                    command.Parameters.AddWithValue("@WghtBalanceCmnt", commentForBalanceCond);
                    command.Parameters.AddWithValue("@RawBiscuitWgt", rawBiscuitWgt);

                    command.Parameters.AddWithValue("@SubmittedById", submittedById);
                    command.Parameters.AddWithValue("@SubmittedDate", submittedDate);
                    command.Parameters.AddWithValue("@SubmittedTime", submittedTime);

                    command.Parameters.AddWithValue("@SubmittedByPNo", submittedByPno);
                    command.Parameters.AddWithValue("@SubmittedByEmployeeCode", submittedByEmployeeCode);

                    command.Parameters.AddWithValue("@Approver1EmployeeCode", approver1EmployeeCode);
                    command.Parameters.AddWithValue("@Approver1_Status", approver1_Status);
                    command.Parameters.AddWithValue("@Approver1_TimeStamp", approver1_TimeStamp);

                    command.Parameters.AddWithValue("@Approver2EmployeeCode", approver2EmployeeCode);
                    command.Parameters.AddWithValue("@Approver2_Status", approver2_Status);
                    command.Parameters.AddWithValue("@Approver2_TimeStamp", approver2_TimeStamp);

                    command.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", dottedLineApproverEmployeeCode);
                    command.Parameters.AddWithValue("@DottedApprover_Status", dottedApprover_Status);
                    command.Parameters.AddWithValue("@DottedApprover_TimeStamp", dottedApprover_TimeStamp);


                    // Open the connection and execute the command
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public class ProcessCheckingBasicData
        {
            public int FormID { get; set; }
            public string PcrNo { get; set; }
            public DateTime SubmittedDate { get; set; }
            public TimeSpan SubmittedTime { get; set; }
            public string Shift { get; set; }
            public int SubmittedById { get; set; }
            public string SubmittedByEmployeeCode { get; set; }
            public string PlantName { get; set; }
            public string Line { get; set; }
            public string ProductCategory { get; set; }
            public string ProductBrand { get; set; }
            public string SKUId { get; set; }
            public decimal? ProcessWaterTemp { get; set; }
            public string ProcessWaterCmnt { get; set; }
            public decimal? WaterPH { get; set; }
            public string WaterPhCmnt { get; set; }
            public decimal? WaterHardness { get; set; }
            public string HardnessCmnt { get; set; }
            public string WaterTest { get; set; }
            public decimal? TDS { get; set; }
            public string TdsCmnt { get; set; }
            public string MaidaBrand { get; set; }
            public string MaidaBatchNo { get; set; }
            public DateTime? MaidaMfgDate { get; set; }
            public int? MaidaAppearanceColor { get; set; }
            public string CommentForMaidaColor { get; set; }
            public int? MaidaFlavorAndTaste { get; set; }
            public string CommentsForMaidaFlavourAndTaste { get; set; }
            public int? MaidaGrittiness { get; set; }
            public string CommentForGrittiness { get; set; }
            public int? BBAppearanceColor { get; set; }
            public string CommentForBBColor { get; set; }
            public int? BBMouthFeel { get; set; }
            public string CommentForBBMouthFeel { get; set; }
            public int? BBFlavorAndTaste { get; set; }
            public string CommentForBBFlavorAndTaste { get; set; }
            public int? HvoSmell { get; set; }
            public string CommentForHvoSmell { get; set; }
            public int? HvoTaste { get; set; }
            public string CommentForHvoTaste { get; set; }
            public decimal? HvoTemp { get; set; }
            public string HvoCmnt { get; set; }
            public int? SmpSmell { get; set; }
            public string CommentForSmpSmell { get; set; }
            public int? SmpTaste { get; set; }
            public string CommentForSmpTaste { get; set; }
            public int? SmpColor { get; set; }
            public string CommentForSmpColor { get; set; }
            public decimal? SyrupTemp { get; set; }
            public string SyrupCmnt { get; set; }
            public int? SyrupColor { get; set; }
            public string CommentForSyrupColor { get; set; }
            public decimal? SyrupPH { get; set; }
            public string SyrupPhCmnt { get; set; }
            public int? InvertSyrpBucketFilter { get; set; }
            public string CommentForISBF { get; set; }
            public int? SugarSolBucketFilter { get; set; }
            public string CommentForSSBF { get; set; }
            public int? CreamerBucketFilter { get; set; }
            public string CommentForCBF { get; set; }
            public int? SugarGrindedSheet { get; set; }
            public string CommentForSGS { get; set; }
            public int? OilSystemBucketFilter { get; set; }
            public string CommentForOSBF { get; set; }
            public int? OilSpray { get; set; }
            public string CommentForOilSpray { get; set; }
            public int? MilkSpray { get; set; }
            public string CommentForMilkSpray { get; set; }
            public decimal? ColdRoomTemp { get; set; }
            public string ColdRoomCmnt { get; set; }
            public decimal? DeepFreezeTemp { get; set; }
            public string DeepFreezeCmnt { get; set; }
            public string MaidaImageUrl { get; set; }
            public string BBImageUrl { get; set; }
            //public int? WghBalanceCond { get; set; }
            //public string WghtBalanceCmnt { get; set; }
            //public string RawBiscuitWgt { get; set; }
            public string Approver1EmployeeCode { get; set; }
            //public TimeSpan? Approver1_TimeStamp { get; set; }
            public string Approver2EmployeeCode { get; set; }
            //public TimeSpan? Approver2_TimeStamp { get; set; }
            public string DottedLineApproverEmployeeCode { get; set; }
            //public TimeSpan? DottedApprover_TimeStamp { get; set; }
        }


        public void InsertProcessCheckingBasicData(ProcessCheckingBasicData data)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertProcessCheckingBasicData", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    cmd.Parameters.AddWithValue("@PcrNo", data.PcrNo);
                    cmd.Parameters.AddWithValue("@FormID", data.FormID);
                    cmd.Parameters.AddWithValue("@SubmittedDate", data.SubmittedDate);
                    cmd.Parameters.AddWithValue("@SubmittedTime", data.SubmittedTime);
                    cmd.Parameters.AddWithValue("@Shift", data.Shift);
                    cmd.Parameters.AddWithValue("@SubmittedById", data.SubmittedById);
                    cmd.Parameters.AddWithValue("@SubmittedByEmployeeCode", (object)data.SubmittedByEmployeeCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PlantName", data.PlantName);
                    cmd.Parameters.AddWithValue("@Line", data.Line);
                    cmd.Parameters.AddWithValue("@ProductCategory", data.ProductCategory);
                    cmd.Parameters.AddWithValue("@ProductBrand", data.ProductBrand);
                    cmd.Parameters.AddWithValue("@SKUId", data.SKUId);
                    cmd.Parameters.AddWithValue("@ProcessWaterTemp", (object)data.ProcessWaterTemp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProcessWaterCmnt", (object)data.ProcessWaterCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WaterPH", (object)data.WaterPH ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WaterPhCmnt", (object)data.WaterPhCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WaterHardness", (object)data.WaterHardness ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HardnessCmnt", (object)data.HardnessCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WaterTest", (object)data.WaterTest ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TDS", (object)data.TDS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TdsCmnt", (object)data.TdsCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaBrand", (object)data.MaidaBrand ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaBatchNo", (object)data.MaidaBatchNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaMfgDate", (object)data.MaidaMfgDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaAppearanceColor", (object)data.MaidaAppearanceColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForMaidaColor", (object)data.CommentForMaidaColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaFlavorAndTaste", (object)data.MaidaFlavorAndTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentsForMaidaFlavourAndTaste", (object)data.CommentsForMaidaFlavourAndTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaGrittiness", (object)data.MaidaGrittiness ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForGrittiness", (object)data.CommentForGrittiness ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BBAppearanceColor", (object)data.BBAppearanceColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForBBColor", (object)data.CommentForBBColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BBMouthFeel", (object)data.BBMouthFeel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForBBMouthFeel", (object)data.CommentForBBMouthFeel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BBFlavorAndTaste", (object)data.BBFlavorAndTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForBBFlavorAndTaste", (object)data.CommentForBBFlavorAndTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HvoSmell", (object)data.HvoSmell ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForHvoSmell", (object)data.CommentForHvoSmell ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HvoTaste", (object)data.HvoTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForHvoTaste", (object)data.CommentForHvoTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HvoTemp", (object)data.HvoTemp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HvoCmnt", (object)data.HvoCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmpSmell", (object)data.SmpSmell ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSmpSmell", (object)data.CommentForSmpSmell ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmpTaste", (object)data.SmpTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSmpTaste", (object)data.CommentForSmpTaste ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SmpColor", (object)data.SmpColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSmpColor", (object)data.CommentForSmpColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyrupTemp", (object)data.SyrupTemp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyrupCmnt", (object)data.SyrupCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyrupColor", (object)data.SyrupColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSyrupColor", (object)data.CommentForSyrupColor ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyrupPH", (object)data.SyrupPH ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SyrupPhCmnt", (object)data.SyrupPhCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@InvertSyrpBucketFilter", (object)data.InvertSyrpBucketFilter ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForISBF", (object)data.CommentForISBF ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SugarSolBucketFilter", (object)data.SugarSolBucketFilter ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSSBF", (object)data.CommentForSSBF ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreamerBucketFilter", (object)data.CreamerBucketFilter ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForCBF", (object)data.CommentForCBF ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SugarGrindedSheet", (object)data.SugarGrindedSheet ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForSGS", (object)data.CommentForSGS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OilSystemBucketFilter", (object)data.OilSystemBucketFilter ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForOSBF", (object)data.CommentForOSBF ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OilSpray", (object)data.OilSpray ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForOilSpray", (object)data.CommentForOilSpray ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MilkSpray", (object)data.MilkSpray ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CommentForMilkSpray", (object)data.CommentForMilkSpray ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ColdRoomTemp", (object)data.ColdRoomTemp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ColdRoomCmnt", (object)data.ColdRoomCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeepFreezeTemp", (object)data.DeepFreezeTemp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeepFreezeCmnt", (object)data.DeepFreezeCmnt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaidaImageUrl", (object)data.MaidaImageUrl ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BBImageUrl", (object)data.BBImageUrl ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@WghBalanceCond", (object)data.WghBalanceCond ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@WghtBalanceCmnt", (object)data.WghtBalanceCmnt ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@RawBiscuitWgt", (object)data.RawBiscuitWgt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Approver1EmployeeCode", (object)data.Approver1EmployeeCode ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@Approver1_TimeStamp", (object)data.Approver1_TimeStamp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Approver2EmployeeCode", (object)data.Approver2EmployeeCode ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@Approver2_TimeStamp", (object)data.Approver2_TimeStamp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DottedLineApproverEmployeeCode", (object)data.DottedLineApproverEmployeeCode ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@DottedApprover_TimeStamp", (object)data.DottedApprover_TimeStamp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BasicData_Status", 1);
                    // Open connection and execute the command
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}