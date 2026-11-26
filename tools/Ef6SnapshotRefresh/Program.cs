using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using AnmolDristi.ITSM.DAL;

namespace Ef6SnapshotRefresh
{
    /// <summary>
    /// Regenerates 202611261430000_AddSupportGroups.resx Target from the live
    /// Code First model using EdmxWriter + the same gzip/base64 envelope EF6
    /// stores in migration resources. Does not call Update-Database and does
    /// not execute migration Up/Down.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var worktree = args.Length > 0
                ? args[0]
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            var migrationsDir = Path.Combine(worktree, "AnmolDristi", "ITSM", "DAL", "Migrations");
            var targetResx = Path.Combine(migrationsDir, "202611261430000_AddSupportGroups.resx");
            var approvalResx = Path.Combine(migrationsDir, "202608240451107_AddApprovalConfiguration.resx");

            if (!File.Exists(targetResx) || !File.Exists(approvalResx))
            {
                Console.Error.WriteLine("Missing migration resx under " + migrationsDir);
                return 2;
            }

            Database.SetInitializer<ItsmDbContext>(null);

            var beforeTarget = ReadDataValue(targetResx, "Target");
            var approvalTarget = ReadDataValue(approvalResx, "Target");
            Console.WriteLine("Before: AddSupportGroups Target == ApprovalConfiguration Target? "
                + string.Equals(beforeTarget, approvalTarget, StringComparison.Ordinal));
            Console.WriteLine("Before Target length: " + (beforeTarget == null ? 0 : beforeTarget.Length));

            // Prove we can decompress the baseline ApprovalConfiguration Target (model baseline).
            string approvalEdmx;
            try
            {
                approvalEdmx = DecompressTarget(approvalTarget);
                Console.WriteLine("Baseline ApprovalConfiguration EDMX chars: " + approvalEdmx.Length);
                Console.WriteLine("Baseline EDMX contains MST_ApprovalConfiguration? "
                    + approvalEdmx.Contains("MST_ApprovalConfiguration"));
                Console.WriteLine("Baseline EDMX contains MST_SupportGroup? "
                    + approvalEdmx.Contains("MST_SupportGroup"));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BLOCKER: Cannot decode ApprovalConfiguration Target as EF model snapshot.");
                Console.Error.WriteLine(ex.ToString());
                return 3;
            }

            string newTarget;
            byte[] newEdmxUtf8;
            try
            {
                // UTF-8 MemoryStream + BOM so Xml declaration matches bytes (EF ModelCompressor).
                newEdmxUtf8 = WriteCurrentModelEdmxUtf8WithBom();
                newTarget = CompressTargetBytes(newEdmxUtf8);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("BLOCKER: Failed to build current Code First EDMX/Target.");
                Console.Error.WriteLine(ex.ToString());
                return 4;
            }

            var newEdmxText = Encoding.UTF8.GetString(newEdmxUtf8);
            Console.WriteLine("Current model EDMX UTF-8 bytes: " + newEdmxUtf8.Length);
            Console.WriteLine("EDMX head hex: " + BitConverter.ToString(newEdmxUtf8, 0, Math.Min(8, newEdmxUtf8.Length)));
            Console.WriteLine("EDMX has UTF-8 BOM? "
                + (newEdmxUtf8.Length >= 3
                    && newEdmxUtf8[0] == 0xEF
                    && newEdmxUtf8[1] == 0xBB
                    && newEdmxUtf8[2] == 0xBF));
            Console.WriteLine("EDMX declares utf-8? " + newEdmxText.Contains("encoding=\"utf-8\""));
            Console.WriteLine("EDMX declares utf-16? " + newEdmxText.Contains("encoding=\"utf-16\""));
            Console.WriteLine("Current EDMX contains MST_SupportGroup? " + newEdmxText.Contains("MST_SupportGroup"));
            Console.WriteLine("Current EDMX contains MST_SupportGroupMember? " + newEdmxText.Contains("MST_SupportGroupMember"));
            Console.WriteLine("Current EDMX contains MST_SupportGroupMapping? " + newEdmxText.Contains("MST_SupportGroupMapping"));
            Console.WriteLine("Current EDMX contains Tickets.SupportGroupId / SupportGroupId? "
                + (newEdmxText.Contains("SupportGroupId")));
            Console.WriteLine("New Target length: " + newTarget.Length);
            Console.WriteLine("New Target == ApprovalConfiguration Target? "
                + string.Equals(newTarget, approvalTarget, StringComparison.Ordinal));

            if (!newEdmxText.Contains("MST_SupportGroup")
                || !newEdmxText.Contains("MST_SupportGroupMember")
                || !newEdmxText.Contains("MST_SupportGroupMapping"))
            {
                Console.Error.WriteLine("BLOCKER: Current model EDMX is missing R6 support-group entity sets.");
                return 5;
            }

            if (!newEdmxText.Contains("encoding=\"utf-8\"") || newEdmxText.Contains("encoding=\"utf-16\""))
            {
                Console.Error.WriteLine("BLOCKER: Regenerated EDMX encoding declaration is not utf-8-only.");
                return 8;
            }

            if (newEdmxUtf8.Length < 3
                || newEdmxUtf8[0] != 0xEF
                || newEdmxUtf8[1] != 0xBB
                || newEdmxUtf8[2] != 0xBF)
            {
                Console.Error.WriteLine("BLOCKER: Regenerated EDMX is missing UTF-8 BOM.");
                return 9;
            }

            if (string.Equals(newTarget, approvalTarget, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("BLOCKER: Regenerated Target is still identical to ApprovalConfiguration.");
                return 6;
            }

            WriteDataValue(targetResx, "Target", newTarget);
            var after = ReadDataValue(targetResx, "Target");
            Console.WriteLine("After write equals new Target? "
                + string.Equals(after, newTarget, StringComparison.Ordinal));
            Console.WriteLine("After write equals ApprovalConfiguration? "
                + string.Equals(after, approvalTarget, StringComparison.Ordinal));

            // Round-trip check (string path)
            var roundTrip = DecompressTarget(after);
            Console.WriteLine("Round-trip EDMX contains MST_SupportGroup? " + roundTrip.Contains("MST_SupportGroup"));
            Console.WriteLine("Round-trip declares utf-8? " + roundTrip.Contains("encoding=\"utf-8\""));

            // Discovery of IMigrationMetadata ids
            var migrationIds = typeof(ItsmDbContext).Assembly.GetTypes()
                .Where(t => typeof(System.Data.Entity.Migrations.Infrastructure.IMigrationMetadata).IsAssignableFrom(t)
                    && !t.IsAbstract)
                .Select(t =>
                {
                    var meta = (System.Data.Entity.Migrations.Infrastructure.IMigrationMetadata)Activator.CreateInstance(t);
                    return meta.Id;
                })
                .OrderBy(id => id)
                .ToList();

            Console.WriteLine("Discovered migration ids:");
            foreach (var id in migrationIds)
            {
                Console.WriteLine("  " + id);
            }

            if (!migrationIds.Contains("202611261430000_AddSupportGroups"))
            {
                Console.Error.WriteLine("BLOCKER: AddSupportGroups metadata type not discovered.");
                return 7;
            }

            // Designer Id sanity
            var designerPath = Path.Combine(migrationsDir, "202611261430000_AddSupportGroups.Designer.cs");
            var designerText = File.ReadAllText(designerPath);
            Console.WriteLine("Designer returns AddSupportGroups id? "
                + designerText.Contains("202611261430000_AddSupportGroups"));

            Console.WriteLine("SUCCESS: AddSupportGroups Target regenerated from current ItsmDbContext model.");
            return 0;
        }

        /// <summary>
        /// Write EDMX to a UTF-8 MemoryStream with BOM. XmlWriter on StringBuilder
        /// ignores Encoding and emits encoding="utf-16", which breaks ModelCompressor.
        /// </summary>
        private static byte[] WriteCurrentModelEdmxUtf8WithBom()
        {
            using (var context = new ItsmDbContext())
            using (var stream = new MemoryStream())
            {
                // EdmxWriter builds the Code First model. SetInitializer(null) avoids
                // running migrations; SqlClient provider may still resolve a named DbConn.
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                };

                using (var writer = XmlWriter.Create(stream, settings))
                {
                    EdmxWriter.WriteEdmx(context, writer);
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// EF6 migration Target payload: GZip(UTF-8 EDMX bytes including BOM) then Base64.
        /// Matches System.Data.Entity.Migrations.Edm.ModelCompressor envelope.
        /// </summary>
        private static string CompressTargetBytes(byte[] utf8EdmxBytes)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzip.Write(utf8EdmxBytes, 0, utf8EdmxBytes.Length);
                }

                return Convert.ToBase64String(output.ToArray());
            }
        }

        private static string DecompressTarget(string target)
        {
            var compressed = Convert.FromBase64String(target);
            using (var input = new MemoryStream(compressed))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var reader = new StreamReader(gzip, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static string ReadDataValue(string resxPath, string name)
        {
            var doc = XDocument.Load(resxPath);
            var data = doc.Root.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "data"
                    && (string)e.Attribute("name") == name);
            if (data == null)
            {
                return null;
            }

            var value = data.Elements().FirstOrDefault(e => e.Name.LocalName == "value");
            return value == null ? null : value.Value;
        }

        private static void WriteDataValue(string resxPath, string name, string newValue)
        {
            var doc = XDocument.Load(resxPath);
            var data = doc.Root.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "data"
                    && (string)e.Attribute("name") == name);
            if (data == null)
            {
                throw new InvalidOperationException("resx missing data " + name);
            }

            var value = data.Elements().FirstOrDefault(e => e.Name.LocalName == "value");
            if (value == null)
            {
                throw new InvalidOperationException("resx data missing value " + name);
            }

            value.Value = newValue;
            doc.Save(resxPath);
        }
    }
}
