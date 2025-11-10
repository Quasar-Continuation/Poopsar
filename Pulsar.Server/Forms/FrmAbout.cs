using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Utilities;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmAbout : Form
    {
        private readonly string _repositoryUrl = @"https://github.com/Quasar-Continuation/Poopsar";
        private readonly string _telegramUrl = @"https://t.me/novashadowisgay";
        private const string ContributorsMessage = """
- [KingKDot](https://github.com/KingKDot) – Lead Developer
- [TheChosenSkywalker](https://github.com/thechosenskywalker) – Lead Developer
- [Twobit](https://github.com/officialtwobit) – Multi-Feature Wizard
- [Lucky](https://t.me/V_Lucky_V) – HVNC Specialist
- [fedx](https://github.com/fedx-988) – README Designer & Discord RPC
- [Ace](https://github.com/Knakiri) – HVNC Features & WinRE Survival
- [Java](https://github.com/JavaRenamed-dev) – Feature Additions
- [Body](https://body.sh) – Obfuscation
- [cpores](https://github.com/vahrervert) – VNC Drawing, Favorites, Overlays
- [Rishie](https://github.com/rishieissocool) – Gatherer Options
- [jungsuxx](https://github.com/jungsuxx) – HVNC Input & Code Simplification
- [MOOM aka my lebron](https://github.com/moom825) – Inspiration & Batch Obfuscation
- [Poli](https://github.com/paulmaster59) – Discord Server & Custom Pulsar Crypter
- [Deadman](https://github.com/DeadmanLabs) – Memory Dumping and Shellcode Builder
- [User76](https://github.com/user76-real) – Networking Optimizations
""";

        public FrmAbout()
        {
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            lblVersion.Text = ServerVersion.Display;
            rtxtContent.Text = Properties.Resources.License;
            cntTxtContent.Text = ContributorsMessage;

            lnkGithubPage.Links.Add(new LinkLabel.Link { LinkData = _repositoryUrl });
            lnkTelegram.Links.Add(new LinkLabel.Link { LinkData = _telegramUrl });
            lnkCredits.Links.Add(new LinkLabel.Link { LinkData = "https://github.com/Quasar-Continuation/Poopsar/tree/main/Licenses" });
        }

        private void lnkGithubPage_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(lnkGithubPage, e);
        }

        private void lnkCredits_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(lnkCredits, e);
        }

        private void lnkTelegram_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenLink(lnkTelegram, e);
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private static void OpenLink(LinkLabel label, LinkLabelLinkClickedEventArgs e)
        {
            if (label == null)
            {
                return;
            }

            label.LinkVisited = true;

            if (e.Link?.LinkData is string target)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to open link.\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private Image SetOpacityGradient(Image image)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            Bitmap src = new Bitmap(image);

            float startOpacity = 0.15f; // 15% at top
            float endOpacity = 0.01f;  // 1% at bottom

            for (int y = 0; y < bmp.Height; y++)
            {
                // Interpolate opacity based on vertical position
                float opacity = startOpacity + (endOpacity - startOpacity) * ((float)y / (bmp.Height - 1));

                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = src.GetPixel(x, y);
                    Color newPixel = Color.FromArgb((int)(opacity * 255), pixel.R, pixel.G, pixel.B);
                    bmp.SetPixel(x, y, newPixel);
                }
            }

            return bmp;
        }

        private void FrmAbout_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = SetOpacityGradient(pictureBox1.Image);
            // Ensure this is done in the designer or in code
            cntTxtContent.DetectUrls = true;  // Auto-detect URLs
            cntTxtContent.ReadOnly = true;    // Optional

            // Handle the LinkClicked event
            cntTxtContent.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to open link.\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string disclaimer = "This software is intended for educational and research purposes ONLY.\n" +
                                "Unauthorized use on computers you do not own or have explicit permission to access is illegal.\n\n" +
                                "By using this software, you agree that you are solely responsible for your actions.";

            MessageBox.Show(disclaimer, "Legal Disclaimer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }
}
