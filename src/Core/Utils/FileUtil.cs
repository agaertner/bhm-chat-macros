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
                    path = File.Exists(filePath) ? filePath : path;
                } else {
                    filePath = filePath.TrimStart('/');
                    filePath = filePath.TrimStart('\\');
                    filePath = filePath.Replace("/", "\\");

                    foreach (var basePath in basePaths) {
                        var testPath = Path.Combine(basePath, filePath);
                        testPath = Path.GetFullPath(testPath);

                        if (File.Exists(testPath)) {
                            path = testPath;
                            break;
                        }
                    }
                }
            } catch (Exception e) {
                logger.Info(e, e.Message);
                return false;
            }
            return true;
        }

        public static bool TryReadAllLines(string filePath, out IReadOnlyList<string> lines, Logger logger = null, params string[] basePaths) {
            logger ??= Logger.GetLogger<BlishHud>();

            var result = new List<string>();
            lines = result;

            if (!Exists(filePath, out var path, logger, basePaths)) {
                return false;
            }

            try {
                result.AddRange(File.ReadAllLines(path));
            } catch (Exception e) {
                logger.Info(e, e.Message);
                return false;
            }
            return true;
        }
    }
}
