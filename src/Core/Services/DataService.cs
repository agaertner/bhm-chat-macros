using Blish_HUD;
using Blish_HUD.Input;
using LiteDB;
using Nekres.ChatMacros.Core.Services.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

            BsonMapper.Global.RegisterType(binding => JsonConvert.SerializeObject(binding, IncludePropertyResolver.Settings(nameof(KeyBinding.PrimaryKey), 
                                                                                                                           nameof(KeyBinding.ModifierKeys))),
                                           bson => JsonConvert.DeserializeObject<KeyBinding>(bson));
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

                var mapMacros = collection.Include(x => x.Lines)
                                           .Find(x => x.Lines != null && x.Lines.Any());

                // Locally because bitwise operators and foreign methods cannot be converted by LiteDB to a valid query.
                var mode = MapUtil.GetCurrentGameMode();
                return mapMacros.Where(x => (x.GameModes == GameMode.None || (x.GameModes & mode) == mode || 
                                            x.MapIds != null && x.MapIds.Any() && x.MapIds.Contains(GameService.Gw2Mumble.CurrentMap.Id)) && 
                                            x.VoiceCommands != null && x.VoiceCommands.Any() || 
                                            x.KeyBinding != null && !x.KeyBinding.GetBindingDisplayText().Equals(string.Empty)).ToList();
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }

            return Enumerable.Empty<ChatMacro>().ToList();
        }

        public List<ChatMacro> GetAllMacros() {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db         = new LiteDatabase(_connectionString);
                var       collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);

                return collection.Include(x => x.Lines).FindAll().ToList();
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }

            return Enumerable.Empty<ChatMacro>().ToList();
        }

        public ChatMacro GetChatMacro(BsonValue id) {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db         = new LiteDatabase(_connectionString);
                var       collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);
                return collection.Include(macro => macro.Lines).FindById(id);
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }

            return default;
        }

        private bool Delete<T>(BsonValue id, string table) {
            LockUtil.Acquire(_rwLock, _lockReleased, ref _lockAcquired);

            try {
                using var db = new LiteDatabase(_connectionString);

                var collection = db.GetCollection<T>(table);
                return collection.Delete(id);
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
                return false;
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }

        public bool Delete(ChatMacro macro) {
            return Delete<ChatMacro>(macro.Id, TBL_CHATMACROS);
        }
        public bool Delete(ChatLine line) {
            return Delete<ChatLine>(line.Id, TBL_CHATLINES);
        }

        public void Dispose() {
            // Wait for the lock to be released
            if (_lockAcquired) {
                _lockReleased.WaitOne(500);
            }

            _lockReleased.Dispose();

            // Dispose the lock
            try {
                _rwLock.Dispose();
            } catch (Exception ex) {
                ChatMacros.Logger.Debug(ex, ex.Message);
            }
        }
    }
}
