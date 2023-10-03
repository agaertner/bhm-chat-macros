using LiteDB;
using Nekres.ChatMacros.Core.Services.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Nekres.ChatMacros.Core.Services {
    internal class DataService : IDisposable {

        private ConnectionString _connectionString;

        private readonly ReaderWriterLockSlim _rwLock       = new();
        private          ManualResetEvent     _lockReleased = new(false);
        private          bool                 _lockAcquired = false;

        private const string TBL_CHATMACROS = "chat_macros";

        private const string LITEDB_FILENAME  = "macros.db";

        public DataService() {
            _connectionString = new ConnectionString {
                Filename   = Path.Combine(ChatMacros.Instance.ModuleDirectory, LITEDB_FILENAME),
                Connection = ConnectionType.Shared
            };
        }

        public bool Upsert(ChatMacro model) {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db = new LiteDatabase(_connectionString);

                var collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);
                collection.Upsert(model); // Returns true on insertion and false on update.
                return true;
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
                return false;
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }

        public List<ChatMacro> GetAllChatMacros() {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db = new LiteDatabase(_connectionString);
                var collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);
                return collection.FindAll().ToList();
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }

            return Enumerable.Empty<ChatMacro>().ToList();
        }

        public void Dispose() {

        }
    }
}
