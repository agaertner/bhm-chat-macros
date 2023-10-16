using Blish_HUD;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nekres.ChatMacros.Core {
    internal static class FileUtil {
        public static bool Exists(string filePath, out string path, Logger logger = null, params string[] basePaths) {
            logger ??= Logger.GetLogger<BlishHud>();

            path = string.Empty;

            if (filePath.IsNullOrEmpty()) {
                return false;
            }

            try {
                filePath = filePath.Replace("%20", " ");

                if (filePath.IsPathFullyQualified()) {
                    path = filePath;
                    return File.Exists(filePath);
                }

                filePath = filePath.TrimStart('/');
                filePath = filePath.TrimStart('\\');
                filePath = filePath.Replace("/", "\\");

                foreach (var basePath in basePaths) {
                    var testPath = Path.Combine(basePath, filePath);
                    testPath = Path.GetFullPath(testPath);

                    if (File.Exists(testPath)) {
                        path = testPath;
                        return true;
                    }
                }
            } catch (Exception e) {
                logger.Info(e, e.Message);
            }
            return false;
        }

        public static bool TryReadAllLines(string filePath, out IReadOnlyList<string> lines, Logger logger = null, params string[] basePaths) {
            logger ??= Logger.GetLogger<BlishHud>();

            var result = new List<string>();
            lines = result;

            if (!Exists(filePath, out var path, logger, basePaths)) {
                return false;
            }

            try {
                result.AddRange(ReadAllLines(path));
            } catch (Exception e) {
                logger.Info(e, e.Message);
                return false;
            }
            return true;
        }

        public static IEnumerable<string> ReadAllLines(string path) {
            using var fil = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fil);
            var file = new List<string>();
            while (!sr.EndOfStream) {
                file.Add(sr.ReadLine());
            }
            return file;
        }
    }
}
