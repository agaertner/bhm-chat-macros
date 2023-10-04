using LiteDB;
using Nekres.ChatMacros.Core.Services.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Blish_HUD;

namespace Nekres.ChatMacros.Core.Services {
    internal class DataService : IDisposable {

        private ConnectionString _connectionString;

        private readonly ReaderWriterLockSlim _rwLock        = new();
        private          ManualResetEvent     _lockReleased  = new(false);
        private          bool                 _lockAcquired  = false;

        public const  string TBL_CHATLINES   = "chat_lines";
        private const string TBL_CHATMACROS  = "chat_macros";
        private const string LITEDB_FILENAME = "macros.db";

        public DataService() {
            _connectionString = new ConnectionString {
                Filename   = Path.Combine(ChatMacros.Instance.ModuleDirectory, LITEDB_FILENAME),
                Connection = ConnectionType.Shared
            };
        }

        private bool Upsert<T>(T model, string table) {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db = new LiteDatabase(_connectionString);

                var collection = db.GetCollection<T>(table);
                collection.Upsert(model); // Returns true on insertion and false on update.
                return true;
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
                return false;
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }

        public bool Upsert(ChatMacro model) {
            return Upsert(model, TBL_CHATMACROS);
        }

        public bool Upsert(ChatLine model) {
            return Upsert(model, TBL_CHATLINES);
        }

        public List<ChatMacro> GetActiveMacros() {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db = new LiteDatabase(_connectionString);
                var collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);
                return collection.Include(x => x.Lines).Find(x => (x.GameMode == GameMode.All || 
                                             x.GameMode == MapUtil.GetCurrentGameMode()) && 
                                             (x.MapIds == null || !x.MapIds.Any() || 
                                             x.MapIds.Contains(GameService.Gw2Mumble.CurrentMap.Id))).ToList();
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
