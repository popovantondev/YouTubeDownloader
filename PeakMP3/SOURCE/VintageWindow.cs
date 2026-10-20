using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
namespace PeakMp3Win7
{
    internal sealed partial class ApprovedWindow
    {
        private VintageSurface vintage;
        private int previewVolume=100;
        private void SetupVintage()
        {
            header.Visible=false;viewport.Visible=false;
            wave.EnsurePreviewHandle();
            vintage=new VintageSurface(Path.Combine(Application.StartupPath,"ASSETS\\Vintage\\Runtime"));
            vintage.Dock=DockStyle.Fill;Controls.Add(vintage);vintage.BringToFront();
            vintage.DragEnter+=DragEnterFiles;vintage.DragDrop+=DropFiles;
            vintage.Command+=VintageCommand;
            previewVolume=prefs.PreviewVolume;
            double dpi;using(Graphics g=Graphics.FromHwnd(IntPtr.Zero))dpi=g.DpiX/96.0;
            Rectangle area=Screen.PrimaryScreen.WorkingArea;
            double scale=Math.Min(435*dpi/973,Math.Min((area.Width-40)/973.0,(area.Height-40)/1616.0));
            MinimumSize=new Size(Math.Min(380,area.Width-20),Math.Min(632,area.Height-20));
            ClientSize=new Size((int)Math.Round(973*scale),(int)Math.Round(1616*scale));
            LayoutAll();RefreshVintage();
        }
        private void RoundVintageWindow()
        {
            if(vintage==null)return;
            // Smooth opacity is supplied by LayeredSkin, never by a binary clipping Region.
            if(Region!=null){Region old=Region;Region=null;old.Dispose();}
        }

        private void RefreshVintage()
        {
            if(vintage==null||vintage.IsDisposed)return;
            VintageState state=new VintageState();state.Language=prefs.Language;state.Density=CurrentDensity;state.Volume=previewVolume;
            state.HasFile=activeFile!=null;state.FileName=activeFile==null?"":IncomingTitle(activeFile,Path.GetFileNameWithoutExtension(activeFile));
            state.Metadata=fileMeta.Text;state.Status=LocalizeHeadline(status.Text);state.Headline=LocalizeHeadline(headline);state.AutoInfo=AutoCaption();state.TrackKey=activeFile??"";state.Requested=pendingPlay&&!autoBusy||player!=null&&player.IsPlaying&&!player.IsPaused;state.Transport=transportState;state.Vu=meter.DisplayDb;
            state.Position=position;state.Duration=duration;state.Wave=wave.PreviewPeaks;
            state.Playing=player!=null&&player.IsPlaying;state.Paused=player!=null&&player.IsPaused;
            state.Processed=comparison.Processed;state.Busy=worker.IsBusy;
            state.Progress=progressKnown?itemProgress.Value:-1;state.JobIndex=jobIndex;state.JobCount=jobFiles.Count;state.QueueCount=files.Count;state.QueueIndex=files.IndexOf(activeFile);state.Label=recordName;
            vintage.Bind(state);
            if(IsHandleCreated)skin.Present(this,vintage);
        }
        private void VintageCommand(string name,double value)
        {
            if(name=="ru"||name=="de"||name=="en")ChangeLanguage(name=="ru"?0:name=="de"?1:2);
            else if(name=="minimize")WindowState=FormWindowState.Minimized;
            else if(name=="close")Close();
            else if(name=="drag")DragWindow(vintage,new MouseEventArgs(MouseButtons.Left,1,0,0,0));
            else if(name=="calibrate")OpenSettings();
            else if(name=="density"){int next=(int)Math.Max(int.MinValue,Math.Min(int.MaxValue,Math.Round(value)));if(next!=CurrentDensity)UiSounds.Tick();SetManualDensity(next);}
            else if(name=="densityInput")EditDensity();
            else if(name=="soft")SetManualDensity(2);else if(name=="balance")SetManualDensity(5);else if(name=="loud")SetManualDensity(8);else if(name=="max")SetManualDensity(10);
            else if(name=="add"||name=="file"){if(!worker.IsBusy)AddDialog();}
            else if(name=="menu")VintageMenu();
            else if(name=="play"){if(activeFile!=null)RequestPlayback();}
            else if(name=="stop"){SmartStop();}
            else if(name=="seek"){if(activeFile!=null&&!worker.IsBusy)Seek(value*duration);}
            else if(name=="recordSeek"){if(activeFile!=null&&!worker.IsBusy){Seek(Math.Min(.995,value)*duration);if(player!=null&&player.IsPlaying)player.Pause();pendingPlay=pendingResume=player!=null&&player.IsPlaying;pendingPlay=true;transportState=1;vintage.Mechanics.BeginSeek();}}
            else if(name=="compare"){if(activeFile!=null)comparison.Processed=value<0?!comparison.Processed:value>.5;}
            else if(name=="previous"||name=="next")
            {
                if(!worker.IsBusy&&files.Count>0)
                {
                    int index=files.IndexOf(activeFile),next=(index+(name=="previous"?-1:1)+files.Count)%files.Count;
                    SetCurrent(files[next]);pendingPlay=true;pendingResume=false;transportState=1;
                }
            }
            else if(name=="export"){if(worker.IsBusy)CancelJob();else if(files.Count>0)StartJob();}
            else if(name=="volume")
            {
                int next=Math.Max(0,Math.Min(100,(int)Math.Round(value)));
                if(next!=previewVolume){previewVolume=next;prefs.PreviewVolume=previewVolume;Save();}
                if(player!=null)player.Volume=previewVolume/100f;
            }

            RefreshVintage();
        }
        private void VintageMenu()
        {
            ContextMenuStrip menu=new ContextMenuStrip();menu.RenderMode=ToolStripRenderMode.System;
            menu.Items.Add(T("add"),null,delegate{if(!worker.IsBusy)AddDialog();});
            menu.Items.Add(T("autoSelecting"),null,delegate{if(activeFile!=null&&!worker.IsBusy){manualSelections.Remove(FileKey(activeFile));PrepareSelectedFile();}});
            ToolStripItem remove=menu.Items.Add(T("remove"),null,delegate{RemoveCurrent();});remove.Enabled=activeFile!=null&&!worker.IsBusy;
            ToolStripItem list=menu.Items.Add(T("queue"),null,delegate{ShowVintageQueue();});list.Enabled=files.Count>0;
            menu.Closed+=delegate{menu.Dispose();};menu.Show(vintage,vintage.PointToClient(System.Windows.Forms.Cursor.Position));
        }
        private void ShowVintageQueue()
        {
            using(Form dialog=new NoTabDialog())
            {
                dialog.Text=T("queue");dialog.ClientSize=new Size(540,280);dialog.StartPosition=FormStartPosition.CenterParent;
                dialog.BackColor=Color.FromArgb(227,221,206);
                ListBox list=new NoFocusListBox();list.Dock=DockStyle.Fill;list.BackColor=Color.FromArgb(247,239,218);list.BorderStyle=BorderStyle.None;list.Font=new Font("Tahoma",11);
                foreach(string file in files)list.Items.Add(Path.GetFileName(file));list.SelectedIndex=files.IndexOf(activeFile);list.Enabled=!worker.IsBusy;
                list.DoubleClick+=delegate{if(list.SelectedIndex>=0){SetCurrent(files[list.SelectedIndex]);dialog.Close();}};
                dialog.Controls.Add(list);dialog.ShowDialog(this);
            }
        }
    }
}
