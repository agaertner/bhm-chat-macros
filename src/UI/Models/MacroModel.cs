using Blish_HUD;
using Blish_HUD.Input;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Nekres.Chat_Shorts.UI.Models
{
    internal class MacroModel
    {
        public event EventHandler<EventArgs> Changed;

        public Guid Id { get; private init; }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (!string.IsNullOrEmpty(_title) && _title.Equals(value)) return;
                _title = value;
                Changed?.Invoke(this, new ValueEventArgs<string>(value));
            }
        }

        private GameMode _gameMode;
        public GameMode Mode
        {
            get => _gameMode;
            set
            {
                if (_gameMode == value) return;
                _gameMode = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private string _text;
        [Obsolete("The property Text is obsolete.")]
        public string Text { 
            get => _text;
            set
            {
                if (!string.IsNullOrEmpty(_text) && _text.Equals(value)) return;
                _text = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private ObservableCollection<string> _textLines;
        public ObservableCollection<string> TextLines
        {
            get => _textLines;
            set
            {
                if (_textLines != null && _textLines.Equals(value)) return;
                _textLines = value;
                _textLines.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private ObservableCollection<int> _mapIds;
        public ObservableCollection<int> MapIds
        {
            get => _mapIds;
            set
            {
                if (_mapIds != null && _mapIds.Equals(value)) return;
                _mapIds = value;
                _mapIds.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private ObservableCollection<int> _excludedMapIds;
        public ObservableCollection<int> ExcludedMapIds
        {
            get => _excludedMapIds;
            set
            {
                if (_excludedMapIds != null && _excludedMapIds.Equals(value)) return;
                _excludedMapIds = value;
                _excludedMapIds.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _squadBroadcast;
        public bool SquadBroadcast
        {
            get => _squadBroadcast;
            set
            {
                if (value == _squadBroadcast) return;
                _squadBroadcast = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        private KeyBinding _keyBinding;
        public KeyBinding KeyBinding
        {
            get => _keyBinding;
            set
            {
                if (_keyBinding != null)
                {
                    if (_keyBinding.Equals(value)) return;
                    _keyBinding.BindingChanged -= OnKeysChanged;
                }
                _keyBinding = value;
                if (_keyBinding != null)
                {
                    _keyBinding.BindingChanged += OnKeysChanged;
                }
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public MacroModel(Guid id, KeyBinding binding)
        {
            this.Id = id;
            this.MapIds = new ObservableCollection<int>();
            this.ExcludedMapIds = new ObservableCollection<int>();
            this.KeyBinding = binding;
            this.KeyBinding.Enabled = false;
            this.Title = "Empty Macro";
            this.Text = string.Empty;
            this.TextLines = new ObservableCollection<string>();
        }

        public MacroModel() : this(Guid.NewGuid(), new KeyBinding(Keys.None))
        {
        }

        public MacroEntity ToEntity()
        {
            return new MacroEntity(this.Id)
            {
                Title = this.Title,
                GameMode = this.Mode,
                Text = this.Text,
                TextLines = this.TextLines.ToList(),
                MapIds = this.MapIds.ToList(),
                ExcludedMapIds = this.ExcludedMapIds.ToList(),
                SquadBroadcast = this.SquadBroadcast,
                ModifierKey = this.KeyBinding.ModifierKeys,
                PrimaryKey = this.KeyBinding.PrimaryKey
            };
        }

        public static MacroModel FromEntity(MacroEntity entity)
        {
            return new MacroModel(entity.Id, new KeyBinding(entity.ModifierKey, entity.PrimaryKey))
            {
                Title = entity.Title,
                Mode = entity.GameMode,
                SquadBroadcast = entity.SquadBroadcast,
                Text = entity.Text,
                TextLines = new ObservableCollection<string>(entity.TextLines),
                MapIds = new ObservableCollection<int>(entity.MapIds),
                ExcludedMapIds = new ObservableCollection<int>(entity.ExcludedMapIds)
            };
        }

        private void OnKeysChanged(object o, EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
