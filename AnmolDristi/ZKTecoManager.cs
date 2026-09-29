using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using zkemkeeper;

namespace AnmolDristi
{
    public class ZKTecoManager
    {
        // COM Object Instantiation
        private CZKEM device = new CZKEMClass();
        private bool isConnected = false;
        private string currentIP = string.Empty;
        private int currentPort = 0;

        // ==========================================
        // 1. CONNECTION LOGIC
        // ==========================================
        public bool Connect(string ipAddress, int port, out int errorCode)
        {
            errorCode = 0;
            if (isConnected) return true;

            // device.SetCommPassword(123456); // Uncomment and set if your device has a Comm Key

            isConnected = device.Connect_Net(ipAddress, port);

            if (isConnected)
            {
                currentIP = ipAddress;
                currentPort = port;
            }
            else
            {
                // Capture the exact reason for failure
                device.GetLastError(ref errorCode);
            }

            return isConnected;
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                device.Disconnect();
                isConnected = false;
            }
        }

        // ==========================================
        // 2. DATA PULL (Extracting Canteen Logs)
        // ==========================================
        public bool PullLogsSTA(string ipAddress, int port, out int errorCode, out List<BiometricLog> extractedLogs)
        {
            bool isConnected = false;
            int errCode = 0;
            List<BiometricLog> tempLogs = new List<BiometricLog>();

            Thread thread = new Thread(() =>
            {
                try
                {
                    CZKEM device = new CZKEMClass();
                    isConnected = device.Connect_Net(ipAddress, port);

                    if (isConnected)
                    {
                        int machineNumber = 1;
                        device.EnableDevice(machineNumber, false); // Lock device to read safely

                        // Load logs into memory
                        if (device.ReadGeneralLogData(machineNumber))
                        {
                            string enrollNo = "";
                            int verifyMode = 0, inOutMode = 0;
                            int year = 0, month = 0, day = 0, hour = 0, min = 0, sec = 0, workCode = 0;

                            // Extract logs one by one
                            while (device.SSR_GetGeneralLogData(machineNumber, out enrollNo, out verifyMode,
                                   out inOutMode, out year, out month, out day, out hour, out min, out sec, ref workCode))
                            {
                                tempLogs.Add(new BiometricLog
                                {
                                    EnrollNumber = enrollNo,
                                    VerifyMode = verifyMode,
                                    InOutMode = inOutMode, // The F-Key (Meal Type)
                                    PunchTime = new DateTime(year, month, day, hour, min, sec)
                                });
                            }
                        }
                        else
                        {
                            device.GetLastError(ref errCode);
                        }

                        device.EnableDevice(machineNumber, true); // Unlock
                        device.Disconnect();
                    }
                    else
                    {
                        device.GetLastError(ref errCode);
                    }
                }
                catch (Exception ex)
                {
                    errCode = -999;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            errorCode = errCode;
            extractedLogs = tempLogs;
            return isConnected;
        }

        // ==========================================
        // 3. DATA PUSH (Syncing Users / VMS PINs)
        // ==========================================

        /// <summary>
        /// Pushes a user/visitor to the device.
        /// </summary>
        /// <param name="enrollNo">EmpID or VMS RequestID (e.g., "V1023")</param>
        /// <param name="name">User's Name</param>
        /// <param name="password">PIN code for Visitors (Leave blank for employees using face)</param>
        /// <param name="privilege">0 = Normal User, 14 = Super Admin</param>
        public bool PushUser(string enrollNo, string name, string password, int privilege = 0)
        {
            if (!isConnected) return false;

            int machineNumber = 1;
            bool success = false;

            device.EnableDevice(machineNumber, false);

            // SSR_SetUserInfo handles adding/updating a user
            success = device.SSR_SetUserInfo(machineNumber, enrollNo, name, password, privilege, true);

            device.EnableDevice(machineNumber, true);
            device.RefreshData(machineNumber); // Important: Forces device to load new data into active memory

            return success;
        }

        /// <summary>
        /// Removes a user (Crucial for deleting Visitor PINs after they leave)
        /// </summary>
        public bool DeleteUser(string enrollNo)
        {
            if (!isConnected) return false;

            bool success = device.SSR_DeleteEnrollData(1, enrollNo, 12); // 12 means delete User and all templates
            device.RefreshData(1);
            return success;
        }

        public class DeviceUser
        {
            public string EnrollNumber { get; set; }
            public string Name { get; set; }
            public string Password { get; set; }
            public int Privilege { get; set; }
            public bool Enabled { get; set; }
            // New fields for Biometric Data
            public string FaceTemplate { get; set; }
            public int FaceLength { get; set; }
            public string CardNumber { get; set; }
        }

        // Helper DTO
        public class BiometricLog
        {
            public string EnrollNumber { get; set; }
            public int VerifyMode { get; set; }
            public int InOutMode { get; set; }
            public DateTime PunchTime { get; set; }
        }

        // ----------------------------------------------------
        // TEST CONNECTION (Using Isolated STA Thread)
        // ----------------------------------------------------
        public bool TestConnection(string ipAddress, int port, out int errorCode)
        {
            bool isConnected = false;
            int errCode = 0;

            // 1. Create the thread just as you suggested
            Thread thread = new Thread(() =>
            {
                try
                {
                    CZKEM device = new CZKEMClass();

                    // 2. Attempt connection
                    isConnected = device.Connect_Net(ipAddress, port);

                    if (isConnected)
                    {
                        // 3. Immediately disconnect if this is just a ping test
                        device.Disconnect();
                    }
                    else
                    {
                        // 4. Capture the exact failure reason
                        device.GetLastError(ref errCode);
                    }
                }
                catch (Exception ex)
                {
                    // Catch unregistered class or missing DLL exceptions
                    errCode = -999;
                }
            });

            // Set to STA and block the main thread until it finishes
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            // Pass the error code back out
            errorCode = errCode;
            return isConnected;
        }

        public bool PullUsersSTA(string ipAddress, int port, out int errorCode, out List<DeviceUser> extractedUsers)
        {
            bool isConnected = false;
            int errCode = 0;
            List<DeviceUser> tempUsers = new List<DeviceUser>();

            Thread thread = new Thread(() =>
            {
                try
                {
                    CZKEM device = new CZKEMClass();
                    isConnected = device.Connect_Net(ipAddress, port);

                    if (isConnected)
                    {
                        int machineNumber = 1;
                        device.EnableDevice(machineNumber, false);

                        // ReadAllUserID loads all users from device memory into the SDK buffer
                        if (device.ReadAllUserID(machineNumber))
                        {
                            string enrollNo = "";
                            string name = "";
                            string password = "";
                            int privilege = 0;
                            bool enabled = false;

                            // Inside PullUsersSTA...
                            while (device.SSR_GetAllUserInfo(machineNumber, out enrollNo, out name, out password, out privilege, out enabled))
                            {
                                string faceTemplate = "";
                                int faceLength = 0;
                                string cardNumber = "";

                                // 1. Get Smart Card Number (Pulls from the buffer of the current user)
                                device.GetStrCardNumber(out cardNumber);

                                // 2. Get Face Template (Index 50)
                                bool hasFace = device.GetUserFaceStr(machineNumber, enrollNo, 50, ref faceTemplate, ref faceLength);

                                tempUsers.Add(new DeviceUser
                                {
                                    EnrollNumber = enrollNo,
                                    Name = name,
                                    Password = password,
                                    Privilege = privilege,
                                    Enabled = enabled,
                                    CardNumber = cardNumber, // Add Card
                                    FaceTemplate = hasFace ? faceTemplate : "",
                                    FaceLength = hasFace ? faceLength : 0
                                });
                            }
                        }
                        else
                        {
                            device.GetLastError(ref errCode);
                        }

                        device.EnableDevice(machineNumber, true);
                        device.Disconnect();
                    }
                    else
                    {
                        device.GetLastError(ref errCode);
                    }
                }
                catch (Exception ex)
                {
                    errCode = -999; // Capture DLL or interop failures
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            errorCode = errCode;
            extractedUsers = tempUsers;
            return isConnected;
        }

        public bool PushUserWithFaceAndCardSTA(string targetIp, int targetPort, DeviceUser userToTransfer, out int errorCode)
        {
            bool isConnected = false;
            int errCode = 0;

            Thread thread = new Thread(() =>
            {
                try
                {
                    CZKEM targetDevice = new CZKEMClass();
                    isConnected = targetDevice.Connect_Net(targetIp, targetPort);

                    if (isConnected)
                    {
                        int machineNumber = 1;
                        targetDevice.EnableDevice(machineNumber, false);

                        // 1. Stage the Smart Card in device memory BEFORE setting the user
                        if (!string.IsNullOrEmpty(userToTransfer.CardNumber))
                        {
                            targetDevice.SetStrCardNumber(userToTransfer.CardNumber);
                        }
                        else
                        {
                            targetDevice.SetStrCardNumber(""); // Clear buffer if no card
                        }

                        // 2. Push Basic Info (This action binds the staged CardNumber to the EnrollNo)
                        bool infoSuccess = targetDevice.SSR_SetUserInfo(machineNumber, userToTransfer.EnrollNumber,
                                                                        userToTransfer.Name, userToTransfer.Password,
                                                                        userToTransfer.Privilege, userToTransfer.Enabled);

                        // 3. Push Face Template
                        if (infoSuccess && !string.IsNullOrEmpty(userToTransfer.FaceTemplate))
                        {
                            targetDevice.SetUserFaceStr(machineNumber, userToTransfer.EnrollNumber, 50,
                                                        userToTransfer.FaceTemplate, userToTransfer.FaceLength);
                        }

                        targetDevice.RefreshData(machineNumber); // Tell device to compile the new data
                        targetDevice.EnableDevice(machineNumber, true);
                        targetDevice.Disconnect();
                    }
                    else
                    {
                        targetDevice.GetLastError(ref errCode);
                    }
                }
                catch (Exception ex)
                {
                    errCode = -999;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            errorCode = errCode;
            return isConnected;
        }

        public bool TransferSpecificUsersSTA(string sourceIp, int sourcePort, string targetIp, int targetPort, List<string> enrollNumbersToTransfer, out int successCount, out string errorMessage)
        {
            bool isComplete = false;
            int success = 0;
            string errMsgs = "";

            Thread thread = new Thread(() =>
            {
                try
                {
                    CZKEM sourceDevice = new CZKEMClass();
                    CZKEM targetDevice = new CZKEMClass();

                    // Connect to both devices
                    bool srcConnected = sourceDevice.Connect_Net(sourceIp, sourcePort);
                    bool tgtConnected = targetDevice.Connect_Net(targetIp, targetPort);

                    if (srcConnected && tgtConnected)
                    {
                        sourceDevice.EnableDevice(1, false);
                        targetDevice.EnableDevice(1, false);

                        foreach (string enrollNo in enrollNumbersToTransfer)
                        {
                            string name = "", password = "", cardNumber = "", faceStr = "";
                            int privilege = 0, faceLength = 0;
                            bool enabled = false;

                            // 1. PULL FROM SOURCE
                            if (sourceDevice.SSR_GetUserInfo(1, enrollNo, out name, out password, out privilege, out enabled))
                            {
                                // Get Card (SDK stores last queried user's card in buffer)
                                sourceDevice.GetStrCardNumber(out cardNumber);

                                // Get Face
                                sourceDevice.GetUserFaceStr(1, enrollNo, 50, ref faceStr, ref faceLength);

                                // 2. PUSH TO TARGET
                                // Set Card buffer first
                                if (!string.IsNullOrEmpty(cardNumber)) targetDevice.SetStrCardNumber(cardNumber);
                                else targetDevice.SetStrCardNumber("");

                                // Set User Info
                                if (targetDevice.SSR_SetUserInfo(1, enrollNo, name, password, privilege, enabled))
                                {
                                    // Set Face
                                    if (!string.IsNullOrEmpty(faceStr))
                                    {
                                        targetDevice.SetUserFaceStr(1, enrollNo, 50, faceStr, faceLength);
                                    }
                                    success++;
                                }
                            }
                        }

                        targetDevice.RefreshData(1); // Compile memory

                        sourceDevice.EnableDevice(1, true);
                        targetDevice.EnableDevice(1, true);
                        sourceDevice.Disconnect();
                        targetDevice.Disconnect();

                        isComplete = true;
                    }
                    else
                    {
                        errMsgs = $"Source Connected: {srcConnected}, Target Connected: {tgtConnected}";
                    }
                }
                catch (Exception ex)
                {
                    errMsgs = "Exception: " + ex.Message;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            successCount = success;
            errorMessage = errMsgs;
            return isComplete;
        }
    }
}