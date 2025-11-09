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
        private readonly string _repositoryUrl = @"https://github.com/Quasar-Continuation/Pulsar";
        private readonly string _telegramUrl = @"https://t.me/novashadowisgay";
        private const string ContributorsMessage = """
Thanks to the contributors below for making this project possible:

- **[KingKDot](https://github.com/KingKDot)** – Lead Developer
- **[TheChosenSkywalker](https://github.com/thechosenskywalker)** – Lead Developer
- **[Twobit](https://github.com/officialtwobit)** – Multi-Feature Wizard
- **[Lucky](https://t.me/V_Lucky_V)** – HVNC Specialist
- **[fedx](https://github.com/fedx-988)** – README Designer & Discord RPC
- **[Ace](https://github.com/Knakiri)** – HVNC Features & WinRE Survival
- **[Java](https://github.com/JavaRenamed-dev)** – Feature Additions
- **[Body](https://body.sh)** – Obfuscation
- **[cpores](https://github.com/vahrervert)** – VNC Drawing, Favorites, Overlays
- **[Rishie](https://github.com/rishieissocool)** – Gatherer Options
- **[jungsuxx](https://github.com/jungsuxx)** – HVNC Input & Code Simplification
- **[MOOM aka my lebron](https://github.com/moom825/)** – Inspiration & Batch Obfuscation
- **[Poli](https://github.com/paulmaster59/)** – Discord Server & Custom Pulsar Crypter
- **[Deadman](https://github.com/DeadmanLabs)** – Memory Dumping and Shellcode Builder
- **[User76](https://github.com/user76-real)** – Networking Optimizations
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
            lnkCredits.Links.Add(new LinkLabel.Link { LinkData = "https://github.com/Pulsar/Pulsar/tree/master/Licenses" });
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
        private Image SetOpacity(Image image, float opacity)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity; // 0.1 for 10%
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                g.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height),
                            0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        private void FrmAbout_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = SetOpacity(pictureBox1.Image, 0.1f);
        }
    }
}
