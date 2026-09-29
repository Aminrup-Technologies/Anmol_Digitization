using System;
using System.Configuration;
using System.Text;
using System.Web.Security;

namespace AnmolDristi
{
    /// <summary>
    /// Issues and verifies HostQuickLogin capability tokens (AR-VMS-13).
    /// Integrity comes from ASP.NET MachineKey.Protect/Unprotect (HMAC + AES via machineKey).
    /// Does not accept legacy Base64(EmployeeCode) tokens.
    /// </summary>
    public static class VmsHostQuickLoginToken
    {
        public const string TtlHoursAppSettingKey = "VmsHostQuickLoginTokenTtlHours";
        public const int DefaultTtlHours = 24;

        private const byte PayloadVersion = 1;
        private const string ProtectPurpose = "VMS.HostQuickLogin.v1";
        private const int HeaderLength = 1 + 4 + 8 + 2;

        public enum Status
        {
            Invalid = 0,
            Expired = 1,
            Valid = 2
        }

        /// <summary>
        /// Builds a URL-safe token bound to logId + hostEmployeeCode with a UTC expiry.
        /// </summary>
        public static string Create(int logId, string hostEmployeeCode)
        {
            if (logId <= 0)
                throw new ArgumentOutOfRangeException("logId", "LogID must be a positive visit identifier.");
            if (string.IsNullOrWhiteSpace(hostEmployeeCode))
                throw new ArgumentException("HostEmployeeCode is required.", "hostEmployeeCode");

            string resolvedHost = hostEmployeeCode.Trim();
            DateTime expiresAtUtc = DateTime.UtcNow.AddHours(ResolveTtlHours());
            byte[] payload = EncodePayload(logId, resolvedHost, expiresAtUtc.Ticks);
            byte[] protectedBytes = MachineKey.Protect(payload, ProtectPurpose);
            return ToBase64Url(protectedBytes);
        }

        /// <summary>
        /// Verifies integrity, expiry, and LogID binding. Never throws.
        /// Invalid signature, malformed payload, and vid mismatch all return Invalid
        /// (no distinction that would help tune a forged token). Expired is returned
        /// only when integrity and LogID binding already succeeded.
        /// </summary>
        public static Status TryValidate(string token, int expectedLogId, out string hostEmployeeCode)
        {
            hostEmployeeCode = string.Empty;

            if (expectedLogId <= 0 || string.IsNullOrWhiteSpace(token))
                return Status.Invalid;

            byte[] protectedBytes;
            if (!TryFromBase64Url(token.Trim(), out protectedBytes))
                return Status.Invalid;

            byte[] payload;
            try
            {
                payload = MachineKey.Unprotect(protectedBytes, ProtectPurpose);
            }
            catch
            {
                return Status.Invalid;
            }

            int tokenLogId;
            string tokenHost;
            long expiryUtcTicks;
            if (!TryDecodePayload(payload, out tokenLogId, out tokenHost, out expiryUtcTicks))
                return Status.Invalid;

            if (tokenLogId != expectedLogId)
                return Status.Invalid;

            if (DateTime.UtcNow.Ticks > expiryUtcTicks)
                return Status.Expired;

            hostEmployeeCode = tokenHost;
            return Status.Valid;
        }

        internal static int ResolveTtlHours()
        {
            string configured = ConfigurationManager.AppSettings[TtlHoursAppSettingKey];
            int hours;
            if (string.IsNullOrWhiteSpace(configured) ||
                !int.TryParse(configured.Trim(), out hours) ||
                hours <= 0)
            {
                return DefaultTtlHours;
            }

            return hours;
        }

        private static byte[] EncodePayload(int logId, string hostEmployeeCode, long expiryUtcTicks)
        {
            byte[] empBytes = Encoding.UTF8.GetBytes(hostEmployeeCode);
            if (empBytes.Length == 0 || empBytes.Length > ushort.MaxValue)
                throw new ArgumentException("HostEmployeeCode length is invalid.", "hostEmployeeCode");

            byte[] buffer = new byte[HeaderLength + empBytes.Length];
            buffer[0] = PayloadVersion;
            WriteInt32BigEndian(buffer, 1, logId);
            WriteInt64BigEndian(buffer, 5, expiryUtcTicks);
            WriteUInt16BigEndian(buffer, 13, (ushort)empBytes.Length);
            Buffer.BlockCopy(empBytes, 0, buffer, HeaderLength, empBytes.Length);
            return buffer;
        }

        private static bool TryDecodePayload(
            byte[] payload,
            out int logId,
            out string hostEmployeeCode,
            out long expiryUtcTicks)
        {
            logId = 0;
            hostEmployeeCode = string.Empty;
            expiryUtcTicks = 0;

            if (payload == null || payload.Length < HeaderLength)
                return false;
            if (payload[0] != PayloadVersion)
                return false;

            logId = ReadInt32BigEndian(payload, 1);
            expiryUtcTicks = ReadInt64BigEndian(payload, 5);
            int empLength = ReadUInt16BigEndian(payload, 13);
            if (empLength <= 0 || HeaderLength + empLength != payload.Length)
                return false;
            if (logId <= 0 || expiryUtcTicks <= 0)
                return false;

            hostEmployeeCode = Encoding.UTF8.GetString(payload, HeaderLength, empLength);
            return !string.IsNullOrWhiteSpace(hostEmployeeCode);
        }

        private static string ToBase64Url(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static bool TryFromBase64Url(string token, out byte[] data)
        {
            data = null;
            if (string.IsNullOrEmpty(token))
                return false;

            string base64 = token.Replace('-', '+').Replace('_', '/');
            int pad = base64.Length % 4;
            if (pad == 1)
                return false;
            if (pad > 0)
                base64 = base64.PadRight(base64.Length + (4 - pad), '=');

            try
            {
                data = Convert.FromBase64String(base64);
                return data != null && data.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static void WriteInt64BigEndian(byte[] buffer, int offset, long value)
        {
            buffer[offset] = (byte)(value >> 56);
            buffer[offset + 1] = (byte)(value >> 48);
            buffer[offset + 2] = (byte)(value >> 40);
            buffer[offset + 3] = (byte)(value >> 32);
            buffer[offset + 4] = (byte)(value >> 24);
            buffer[offset + 5] = (byte)(value >> 16);
            buffer[offset + 6] = (byte)(value >> 8);
            buffer[offset + 7] = (byte)value;
        }

        private static void WriteUInt16BigEndian(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)value;
        }

        private static int ReadInt32BigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24)
                | (buffer[offset + 1] << 16)
                | (buffer[offset + 2] << 8)
                | buffer[offset + 3];
        }

        private static long ReadInt64BigEndian(byte[] buffer, int offset)
        {
            return ((long)buffer[offset] << 56)
                | ((long)buffer[offset + 1] << 48)
                | ((long)buffer[offset + 2] << 40)
                | ((long)buffer[offset + 3] << 32)
                | ((long)buffer[offset + 4] << 24)
                | ((long)buffer[offset + 5] << 16)
                | ((long)buffer[offset + 6] << 8)
                | buffer[offset + 7];
        }

        private static int ReadUInt16BigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset] << 8) | buffer[offset + 1];
        }
    }
}
