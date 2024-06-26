﻿using Blish_HUD;
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

        public event EventHandler<ValueEventArgs<BaseMacro>> LinkFileChange;

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
                                           bson => {
                                               var keyBinding = JsonConvert.DeserializeObject<KeyBinding>(bson) ?? new KeyBinding();
                                               keyBinding.Enabled = false;
                                               return keyBinding;
                                           });
        }

        private bool Upsert<T>(T model, string table) {
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return false;
            }

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

        public void LinkFileChanged(ChatMacro macro) {
            Upsert(macro);

            if (!macro.LinkFile.IsNullOrWhiteSpace() && !macro.LinkFile.IsWebLink()) {
                LinkFileChange?.Invoke(this, new ValueEventArgs<BaseMacro>(macro));
            }
        }

        private bool InsertMany<T>(List<T> model, string table) {
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return false;
            }

            try {
                using var db         = new LiteDatabase(_connectionString);
                var       collection = db.GetCollection<T>(table);
                return collection.InsertBulk(model) > 0;
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
                return false;
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }

        public bool Upsert(ChatLine model) {
            return Upsert(model, TBL_CHATLINES);
        }

        public bool Insert(params ChatLine[] chatLines) {
            return InsertMany(chatLines.ToList(), TBL_CHATLINES);
        }

        public List<ChatMacro> GetActiveMacros() {
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return Enumerable.Empty<ChatMacro>().ToList();
            }

            try {
                using var db = new LiteDatabase(_connectionString);
                var collection = db.GetCollection<ChatMacro>(TBL_CHATMACROS);

                var mapMacros = collection.Include(x => x.Lines)
                                           .Find(x => x.Lines != null && x.Lines.Any());

                // Locally because bitwise operators and foreign methods cannot be converted by LiteDB to a valid query.
                var mode = MapUtil.GetCurrentGameMode();
                return mapMacros.Where(x => 
                                           // Macro has no map restrictions and the game mode matches.
                                           (x.MapIds == null || !x.MapIds.Any()) && (x.GameModes == GameMode.None || (x.GameModes & mode) == mode) ||
                                           // Game mode does not match but the map does.
                                           x.MapIds != null && x.MapIds.Any() && x.MapIds.Contains(GameService.Gw2Mumble.CurrentMap.Id)).ToList();
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }

            return Enumerable.Empty<ChatMacro>().ToList();
        }

        public List<ChatMacro> GetAllMacros() {
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return Enumerable.Empty<ChatMacro>().ToList();
            }

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
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return default;
            }

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

        private bool Delete<T>(string table, params BsonValue[] ids) {
            if (!LockUtil.TryAcquire(_rwLock, _lockReleased, ref _lockAcquired)) {
                return false;
            }

            try {
                using var db = new LiteDatabase(_connectionString);
                
                var collection = db.GetCollection<T>(table);
                return collection.DeleteMany(Query.In("_id", ids)) == ids.Length;
            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, e.Message);
                return false;
            } finally {
                LockUtil.Release(_rwLock, _lockReleased, ref _lockAcquired);
            }
        }   

        public bool Delete(ChatMacro macro) {
            ChatMacros.Instance.Macro.Observer.Remove(macro.Id);
            return Delete<ChatMacro>(TBL_CHATMACROS, macro.Id);
        }
        public bool Delete(ChatLine line) {
            return Delete<ChatLine>(TBL_CHATLINES, line.Id);
        }

        public bool DeleteMany(IEnumerable<ChatLine> lines) {
            return Delete<ChatLine>(TBL_CHATLINES, lines.Select(line => new BsonValue(line.Id)).ToArray());
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
