using System;
using System.Threading;

namespace Nekres.ChatMacros.Core {
    internal static class LockUtil {
        public static bool TryAcquire(ReaderWriterLockSlim rwLock, ManualResetEvent lockReleased, ref bool lockAcquired, int msTimeout = 200) {
            try {
                if (!rwLock.TryEnterWriteLock(msTimeout)) {
                    return false;
                }
                lockReleased.Reset();
                lockAcquired = true;
                return true;
            } catch (Exception ex) {
                ChatMacros.Logger.Debug(ex, ex.Message);
                return false;
            }
        }

        public static void Release(ReaderWriterLockSlim rwLock, ManualResetEvent lockReleased, ref bool lockAcquired) {
            try {
                if (lockAcquired) {
                    rwLock.ExitWriteLock();
                    lockAcquired = false;
                }
            } catch (Exception ex) {
                ChatMacros.Logger.Debug(ex, ex.Message);
            } finally {
                lockReleased.Set();
            }
        }

        public static void WaitOne(ManualResetEvent lockReleased, ref bool lockAcquired, int millisecondsTimeout = 500) {
            // Wait for the lock to be released
            if (lockAcquired) {
                lockReleased.WaitOne(millisecondsTimeout);
            }
        }
    }
}
