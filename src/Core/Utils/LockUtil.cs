using System;
using System.Threading;

namespace Nekres.ChatMacros.Core {
    internal static class LockUtil {
        public static void Acquire(ReaderWriterLockSlim rwLock, ManualResetEvent lockReleased, ref bool lockAcquired) {
            try {
                rwLock.EnterWriteLock();
                lockReleased.Reset();
                lockAcquired = true;
            } catch (Exception ex) {
                ChatMacros.Logger.Debug(ex, ex.Message);
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
    }
}
