using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Globalization;
namespace PeakMp3Win7
{
 internal sealed class DensityInput : NoTabDialog
 {
  internal int Value;private readonly TextBox input;private readonly Label error;private readonly int language;private readonly StudioSurface surface;
  internal DensityInput(int value,int lang)
  {
   Value=value;language=lang;Text=L10n.Get("strength",lang);ClientSize=new Size(440,245);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;DoubleBuffered=true;BackColor=StudioPalette.Silver;
   surface=new StudioSurface();surface.BorderVisible=true;surface.Dock=DockStyle.Fill;Controls.Add(surface);
   Label title=new Label();title.Text=Text;title.Font=new Font("Tahoma",15,FontStyle.Bold);title.SetBounds(22,16,354,32);title.BackColor=Color.Transparent;surface.Controls.Add(title);
   StudioButton close=new StudioButton();close.Tag="closeWindow";close.SetBounds(391,11,35,32);close.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};surface.Controls.Add(close);
   input=new TextBox();input.Text=value.ToString(CultureInfo.InvariantCulture);input.Font=new Font("Tahoma",22,FontStyle.Bold);input.TextAlign=HorizontalAlignment.Center;input.BackColor=Color.FromArgb(250,241,215);input.ForeColor=Color.FromArgb(43,35,22);input.BorderStyle=BorderStyle.FixedSingle;input.SetBounds(26,61,388,46);input.TabStop=false;surface.Controls.Add(input);
   Label hint=new Label();hint.Text=L10n.Get("densityRangeHint",lang);hint.SetBounds(26,116,388,42);hint.Font=new Font("Tahoma",9);hint.BackColor=Color.Transparent;surface.Controls.Add(hint);
   error=new Label();error.SetBounds(26,157,388,24);error.ForeColor=Color.FromArgb(158,44,16);error.BackColor=Color.Transparent;surface.Controls.Add(error);
   StudioButton cancel=new StudioButton();cancel.Text=L10n.Get("cancel",lang);cancel.SetBounds(26,190,178,36);cancel.Click+=delegate{DialogResult=DialogResult.Cancel;Close();};surface.Controls.Add(cancel);CancelButton=cancel;
   StudioButton ok=new StudioButton();ok.Text="OK";ok.SetBounds(226,190,188,36);ok.Click+=delegate{int parsed;if(!int.TryParse(input.Text.Replace("−","-"),NumberStyles.Integer,CultureInfo.CurrentCulture,out parsed)){error.Text=L10n.Get("integerRequired",language);input.Focus();return;}Value=parsed;DialogResult=DialogResult.OK;Close();};surface.Controls.Add(ok);AcceptButton=ok;
   Shown+=delegate{StudioHardware.RoundWindow(this,12);input.Focus();input.SelectionStart=input.Text.Length;};
  }
 }
}
