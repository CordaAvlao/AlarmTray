using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace BeepScheduler
{
    public class AboutForm : Form
    {
        public AboutForm(bool isDarkMode)
        {
            InitializeComponent();
            ThemeHelper.ApplyTheme(this, isDarkMode);
        }

        private void InitializeComponent()
        {
            this.Text = "À propos de Beep Scheduler";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Beep Scheduler V2.5",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };

            Label lblAuthor = new Label
            {
                Text = "Made by CordaAvlao",
                Font = new Font("Segoe UI", 10F),
                Location = new Point(20, 60),
                AutoSize = true
            };

            Label lblDate = new Label
            {
                Text = "Date : 18 Décembre 2025",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(20, 85),
                AutoSize = true
            };

            LinkLabel lnkGithub = new LinkLabel
            {
                Text = "github.com/CordaAvlao",
                Location = new Point(20, 120),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F)
            };
            lnkGithub.LinkClicked += (s, e) => {
                try { Process.Start(new ProcessStartInfo("https://github.com/CordaAvlao") { UseShellExecute = true }); }
                catch { MessageBox.Show("Impossible d'ouvrir le lien."); }
            };

            Button btnClose = new Button
            {
                Text = "Fermer",
                Location = new Point(240, 170),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };

            this.Controls.AddRange(new Control[] { lblTitle, lblAuthor, lblDate, lnkGithub, btnClose });
        }
    }
}
