using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeepScheduler
{
    public static class ThemeHelper
    {
        public static Color DarkBg = Color.FromArgb(32, 32, 32);
        public static Color DarkControlBg = Color.FromArgb(45, 45, 48);
        public static Color DarkText = Color.White;
        public static Color DarkGridLines = Color.FromArgb(63, 63, 70);

        public static Color LightBg = Color.WhiteSmoke;
        public static Color LightControlBg = Color.White;
        public static Color LightText = Color.Black;
        public static Color LightGridLines = Color.FromArgb(220, 220, 220);

        public static void ApplyTheme(Form form, bool isDarkMode)
        {
            Color bg = isDarkMode ? DarkBg : LightBg;
            Color fg = isDarkMode ? DarkText : LightText;

            form.BackColor = bg;
            form.ForeColor = fg;

            foreach (Control ctrl in form.Controls)
            {
                ApplyToControl(ctrl, isDarkMode);
            }
        }

        private static void ApplyToControl(Control ctrl, bool isDarkMode)
        {
            Color ctrlBg = isDarkMode ? DarkControlBg : (ctrl is Button ? SystemColors.Control : Color.White);
            Color fg = isDarkMode ? DarkText : LightText;

            if (ctrl is Panel || ctrl is TableLayoutPanel || ctrl is GroupBox)
            {
                ctrl.BackColor = isDarkMode ? DarkBg : (ctrl is TableLayoutPanel ? LightBg : Color.Transparent);
                ctrl.ForeColor = fg;
                foreach (Control child in ctrl.Controls)
                {
                    ApplyToControl(child, isDarkMode);
                }
            }
            else if (ctrl is DataGridView dgv)
            {
                dgv.BackgroundColor = isDarkMode ? DarkControlBg : Color.White;
                dgv.GridColor = isDarkMode ? DarkGridLines : LightGridLines;
                dgv.DefaultCellStyle.BackColor = isDarkMode ? DarkControlBg : Color.White;
                dgv.DefaultCellStyle.ForeColor = fg;
                dgv.DefaultCellStyle.SelectionBackColor = isDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(230, 240, 255);
                dgv.DefaultCellStyle.SelectionForeColor = fg;

                dgv.ColumnHeadersDefaultCellStyle.BackColor = isDarkMode ? Color.FromArgb(50, 50, 50) : Color.FromArgb(245, 245, 245);
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = fg;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
                
                dgv.EnableHeadersVisualStyles = false;
            }
            else if (ctrl is Button btn)
            {
                // Preserve specific button colors if they are not default
                if (btn.BackColor == Color.DodgerBlue || btn.BackColor == Color.MediumSeaGreen || btn.BackColor == Color.Red)
                {
                    // Keep them
                }
                else
                {
                    btn.BackColor = isDarkMode ? DarkControlBg : SystemColors.Control;
                    btn.ForeColor = fg;
                }
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = isDarkMode ? DarkGridLines : Color.Silver;
            }
            else if (ctrl is TextBox txt)
            {
                txt.BackColor = isDarkMode ? DarkControlBg : Color.White;
                txt.ForeColor = fg;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (ctrl is CheckBox chk)
            {
                chk.ForeColor = fg;
            }
            else if (ctrl is Label lbl)
            {
                lbl.ForeColor = fg;
            }
            else if (ctrl is ComboBox cmb)
            {
                cmb.BackColor = isDarkMode ? DarkControlBg : Color.White;
                cmb.ForeColor = fg;
                cmb.FlatStyle = FlatStyle.Flat;
            }
            else if (ctrl is TrackBar trk)
            {
                // TrackBar is hard to theme perfectly in WinForms, but we can try
                trk.BackColor = isDarkMode ? DarkBg : LightBg;
            }
        }
    }
}
