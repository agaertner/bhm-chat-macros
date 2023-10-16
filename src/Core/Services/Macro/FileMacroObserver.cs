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
                if (!AddOrRemove(macro.Id, macro.LinkFile)) {
                    macro.LinkFile = string.Empty;
                    ChatMacros.Instance.Data.Upsert(macro);
                }
            }

            ChatMacros.Instance.Data.LinkFileChange += OnLinkFileChanged;
        }

        private void OnLinkFileChanged(object sender, ValueEventArgs<BaseMacro> e) {
            AddOrRemove(e.Value.Id, e.Value.LinkFile);
        }

        public bool AddOrRemove(ObjectId id, string path) {
            if (!FileUtil.Exists(path, out var qualifiedPath, ChatMacros.Logger, ChatMacros.Instance.BasePaths.ToArray())) {
                Remove(id);
                return false;
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
                return false;
            }
            return true;
        }

        private void RegisterEvents(FileSystemWatcher fw) {
            Import(_watchers.FirstOrDefault(x => x.Value == fw).Key, Path.Combine(fw.Path, fw.Filter));

            fw.EnableRaisingEvents =  true;
            fw.Changed += OnChanged;
            fw.Deleted += OnDeleted;
            fw.Created += OnChanged;
        }

        private void OnDeleted(object sender, FileSystemEventArgs e) {
            var fw = (FileSystemWatcher)sender;
            var id = _watchers.FirstOrDefault(x => x.Value == fw).Key;

            var macro = ChatMacros.Instance.Data.GetChatMacro(id);
            macro.LinkFile = string.Empty;
            ChatMacros.Instance.Data.Upsert(macro);
            
            Remove(id);

            MacroUpdate?.Invoke(this, new ValueEventArgs<BaseMacro>(macro));
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            var fw   = (FileSystemWatcher)sender;
            var dir  = Path.GetDirectoryName(e.FullPath);
            var file = Path.GetFileName(e.FullPath);
            fw.Path   = dir;
            fw.Filter = file;

            Import(_watchers.FirstOrDefault(x => x.Value == fw).Key, e.FullPath);
        }

        private void Import(ObjectId id, string filePath) {
            if (id == null) {
                return;
            }

            var macro = ChatMacros.Instance.Data.GetChatMacro(id);

            if (!ChatMacros.Instance.Macro.TryImportFromFile(filePath, out var lines)) {
                return;
            }

            var oldLines = macro.Lines.ToList();
            macro.Lines = lines.ToList();

            if (!macro.Lines.IsNullOrEmpty() && !ChatMacros.Instance.Data.Insert(macro.Lines.ToArray())) {
                macro.Lines = oldLines;
                ChatMacros.Logger.Warn($"Failed to insert lines from file for macro {macro.Id} ('{macro.Title}')");
                return;
            }
            
            if (!ChatMacros.Instance.Data.Upsert(macro)) {
                macro.Lines = oldLines;
                ChatMacros.Logger.Warn($"Failed to upsert macro {macro.Id} ('{macro.Title}')");
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
