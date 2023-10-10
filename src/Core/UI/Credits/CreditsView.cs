using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Flurl.Http;
using Microsoft.Xna.Framework;
using Nekres.ChatMacros.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Container = Blish_HUD.Controls.Container;

namespace Nekres.ChatMacros.Core.UI.Credits {
    internal class CreditsView : View {

        private const string DONORS_URI = @"https://pastebin.com/raw/1Wd03Bmg";

        private IReadOnlyList<Donor> _donors;

        protected override async Task<bool> Load(IProgress<string> progress) {
            _donors = await HttpUtil.RetryAsync(() => DONORS_URI.GetJsonAsync<List<Donor>>());
            return await Task.FromResult(await base.Load(progress));
        }
        
        protected override void Build(Container buildPanel) {

            var creditsWrap = new FlowPanel {
                Parent = buildPanel,
                Width = buildPanel.ContentRegion.Width,
                Height = buildPanel.ContentRegion.Height,
                Title = Resources.Credits_and_Thanks
            };

            var thanks = new FormattedLabelBuilder().SetHeight(100)
                                                    .SetWidth(creditsWrap.ContentRegion.Width - Panel.RIGHT_PADDING * 2)
                                                    .Wrap()
                                                    .SetHorizontalAlignment(HorizontalAlignment.Center)
                                                    .SetVerticalAlignment(VerticalAlignment.Middle)
                                                    .CreatePart(Resources.Thanks_to_my_awesome_supporters_who_keep_me_motivated_, o => {
                                                         o.SetFontSize(ContentService.FontSize.Size18);
                                                         o.MakeBold();
                                                     })
                                                    .Build();
            thanks.Parent = creditsWrap;

            var donorsList = new FlowPanel {
                Parent              = creditsWrap,
                Width               = creditsWrap.ContentRegion.Width,
                Height              = creditsWrap.ContentRegion.Height - 50 - thanks.Height,
                FlowDirection       = ControlFlowDirection.SingleTopToBottom,
                OuterControlPadding = new Vector2(5, 5),
                ControlPadding      = new Vector2(5, 5),
                CanScroll           = true,
                ShowBorder          = true
            };

            if (_donors != null) {
                foreach (var donor in _donors) {
                    AddDonor(donorsList, donor);
                }
            }

            var donateBttn = new ViewContainer {
                Parent = creditsWrap,
                Width  = donorsList.Width,
                Height = 50
            };
            donateBttn.Show(new KofiButton());

            base.Build(buildPanel);
        }

        private void AddDonor(Container parent, Donor donor) {
            var donorsEntry = new ViewContainer {
                Parent = parent,
                Width  = parent.ContentRegion.Width,
                Height = 50
            };
            donorsEntry.Show(new DonorEntryView(donor));
        }

        private class DonorEntryView : View {

            private Donor _donor;
            public DonorEntryView(Donor donor) {
                _donor = donor;
            }

            protected override void Build(Container buildPanel) {

                var flow = new FlowPanel {
                    Parent = buildPanel,
                    Width = buildPanel.ContentRegion.Width,
                    Height = buildPanel.ContentRegion.Height,
                    FlowDirection = ControlFlowDirection.LeftToRight,
                    OuterControlPadding = new Vector2(5, 5),
                    ControlPadding = new Vector2(5, 5)
                };

                var name = new Label {
                    Parent = flow,
                    Height = flow.ContentRegion.Height,
                    AutoSizeWidth = true,
                    Text   = _donor.Name,
                    Font = ChatMacros.Instance.Resources.LatoRegular24,
                    BasicTooltipText = string.Format(Resources.Supporter_since__0_, _donor.SupporterSince.ToShortDateString())
                };

                if (!string.IsNullOrEmpty(_donor.Socials.GuildWars2)) {
                    var gw2Acc = new Label {
                        Parent        = flow,
                        Height        = flow.ContentRegion.Height,
                        AutoSizeWidth = true,
                        Text          = $"({_donor.Socials.GuildWars2})"
                    };
                }

                AddSocialButton(flow, _donor.Socials.Homepage, _donor.Socials.Homepage, _donor.Socials.Homepage, GameService.Content.DatAssetCache.GetTextureFromAssetId(255369));
                AddSocialButton(flow, _donor.Socials.Twitch, $"twitch.tv/{_donor.Socials.Twitch}", $"https://www.twitch.tv/{_donor.Socials.Twitch}", ChatMacros.Instance.Resources.TwitchLogo);
                AddSocialButton(flow, _donor.Socials.Youtube, $"youtube.com/@{_donor.Socials.Youtube}", $"https://www.youtube.com/@{_donor.Socials.Youtube}", ChatMacros.Instance.Resources.YoutubeLogo);

                base.Build(buildPanel);
            }

            private void AddSocialButton(Container parent, string raw, string name, string url, AsyncTexture2D buttonTexture) {
                if (string.IsNullOrEmpty(raw)) {
                    return;
                }

                var button = new Image {
                    Parent           = parent,
                    Height           = 32,
                    Width            = 32,
                    Texture          = buttonTexture,
                    BasicTooltipText = name
                };
                button.Click += (_, _) => Process.Start(url);
            }
        }
        public class Donor {

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("socials")]
            public SocialUrls Socials { get; set; }

            [JsonProperty("supporter_since")]
            public DateTime SupporterSince { get; set; }

            public class SocialUrls {
                [JsonProperty("guildwars2")]
                public string GuildWars2 { get; set; }
                [JsonProperty("homepage")]
                public string Homepage   { get; set; }
                [JsonProperty("twitch")]
                public string Twitch     { get; set; }
                [JsonProperty("youtube")]
                public string Youtube    { get; set; }
            }
        }
    }
}
