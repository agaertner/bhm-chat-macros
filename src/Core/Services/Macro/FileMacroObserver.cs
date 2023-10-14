using Blish_HUD;
using LiteDB;
using Nekres.ChatMacros.Core.Services.Data;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Nekres.ChatMacros.Core.Services.Macro {
    internal class FileMacroObserver : IDisposable {

        public event EventHandler<ValueEventArgs<BaseMacro>>      MacroUpdate;
        private ConcurrentDictionary<ObjectId, FileSystemWatcher> _watchers;

        public FileMacroObserver() {
            _watchers = new ConcurrentDictionary<ObjectId, FileSystemWatcher>();

            var macros = ChatMacros.Instance.Data.GetAllMacros();
            foreach (var macro in macros) {
                AddOrRemove(macro.Id, macro.LinkFile);
            }

            ChatMacros.Instance.Data.LinkFileChange += OnLinkFileChanged;
        }

        private void OnLinkFileChanged(object sender, ValueEventArgs<BaseMacro> e) {
            AddOrRemove(e.Value.Id, e.Value.LinkFile);
        }

        public void AddOrRemove(ObjectId id, string path) {
            if (!FileUtil.Exists(path, out var qualifiedPath, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray())) {
                Remove(id);
                return;
            }

            try {
                var dir  = Path.GetDirectoryName(qualifiedPath);
                var file = Path.GetFileName(qualifiedPath);

                _watchers.AddOrUpdate(id, k => {
                    var watcher = new FileSystemWatcher(dir!) {
                        Filter = file
                    };
                    RegisterEvents(watcher);
                    return watcher;
                }, (_, v) => {
                    v?.Dispose();
                    var watcher = new FileSystemWatcher(dir!) {
                        Filter = file
                    };
                    RegisterEvents(watcher);
                    return watcher;
                });
            } catch (Exception e) {
                ChatMacros.Logger.Info(e, e.Message);
            }
        }

        private void RegisterEvents(FileSystemWatcher fw) {
            fw.EnableRaisingEvents =  true;
            fw.Changed             += OnChanged;
            fw.Renamed             += OnChanged;
            fw.Deleted             += OnChanged;
            fw.Created             += OnChanged;
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            var fw   = (FileSystemWatcher)sender;
            var dir  = Path.GetDirectoryName(e.FullPath);
            var file = Path.GetFileName(e.FullPath);
            fw.Path   = dir;
            fw.Filter = file;

            var id = _watchers.FirstOrDefault(x => x.Value == fw).Key;

            if (id == null) {
                return;
            }

            var macro = ChatMacros.Instance.Data.GetChatMacro(id);

            if (!ChatMacros.Instance.Macro.TryImportFromFile(e.FullPath, out var lines)) {
                return;
            }

            var oldLines = macro.Lines.ToList();
            macro.Lines = lines.ToList();

            if (!ChatMacros.Instance.Data.Insert(macro.Lines.ToArray()) || !ChatMacros.Instance.Data.Upsert(macro)) {
                macro.Lines = oldLines;
                ChatMacros.Logger.Warn($"Failed to update macro {macro.Id} ('{macro.Title}') in database.");
                return;
            }

            foreach (var oldLine in oldLines) {
                ChatMacros.Instance.Data.Delete(oldLine);
            }

            MacroUpdate?.Invoke(this, new ValueEventArgs<BaseMacro>(macro));
        }

        public void Remove(ObjectId id) {
            if (_watchers.TryRemove(id, out var watcher)) {
                watcher?.Dispose();
            }
        }

        public void Dispose() {
            ChatMacros.Instance.Data.LinkFileChange -= OnLinkFileChanged;
            foreach (var fsw in _watchers.Values) {
                fsw?.Dispose();
            }
        }
    }
}
