﻿using DevExpress.XtraBars.Docking2010.Views.Widget;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using PersonalPlanner.Data;
using PersonalPlanner.Define;
using PersonalPlanner.GUI.Components;
using PersonalPlanner.GUI.Forms;
using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace PersonalPlanner.GUI.Frame
{
    public partial class MainFrame : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        /*-------------------------------------------
         *
         *      Private members
         *
         -------------------------------------------*/

        private readonly SynchronizationContext SyncContext;
        private NotificationData NotificationData;

        /*-------------------------------------------
         *
         *      Constructor / Destructor
         *
         -------------------------------------------*/

        public MainFrame()
        {
            InitializeComponent();
            SyncContext = SynchronizationContext.Current;

            LoadingForm.OpenDialog();
            LoadingForm.SetVersion(Assembly.GetExecutingAssembly().GetName().Version.ToString());

            LoadingForm.SetProgress("Set UI Elements...");
            SetUIElements();
            LoadingForm.SetProgress("Set UI Elements Done...");

            LoadingForm.SetProgress("Loading Environments...");
            LoadDatas();
            CheckSkinPaletteColor();
            LoadingForm.SetProgress("Loading Environments Done...");

            LoadingForm.SetProgress("Init Scheduler...");
            InitScheduler();
            LoadingForm.SetProgress("Init Scheduler Done...");
            LoadingForm.SetProgress("Init Memo...");
            InitMemos();
            LoadingForm.SetProgress("Init Memo Done...");
            LoadingForm.SetProgress("Init Gantt...");
            InitGantts();
            LoadingForm.SetProgress("Init Gantt Done...");

            LoadingForm.SetProgress("Connecting User Events...");
            ConnectingUserEvents();

            LoadingForm.SetProgress("Set UI Layout...");
            SetUILayout();
            LoadingForm.SetProgress("Set UI Layout Done...");

            LoadingForm.SetProgress("Program Start...");
            LoadingForm.CloseDialog();
        }

        /*-------------------------------------------
         *
         *      Event functions
         *
         -------------------------------------------*/

        private void MainFrame_Resized(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized) GlobalData.Parameters.MainFrameWindowState = this.WindowState;
            if (this.WindowState == FormWindowState.Normal)
            {
                GlobalData.Parameters.MainFrameLocation = this.Location;
                GlobalData.Parameters.MainFrameSize = this.Size;
            }
        }

        private void MainFrame_Move(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                GlobalData.Parameters.MainFrameLocation = this.Location;
                GlobalData.Parameters.MainFrameSize = this.Size;
            }
        }

        private void MainFrame_FormClosing(object sender, FormClosingEventArgs e)
        {
            WaitForm.OpenDialog("Closing...", "Saving datas...");
            try
            {
                SaveMemoWidgetStatus();
                SaveGanttWidgetStatus();
                GlobalData.SaveData();
                MemoData.SaveData();
                CalendarData.WriteCalendar(MainSchedulerDataStorage);
                GanttData.SaveData();
                SaveUILayout();
            }
            finally
            {
                WaitForm.CloseDialog();
            }
        }

        private void Navigation_ElementClicked(AccordionControlElement element, Point point)
        {
            if (GroupMemoLists.Elements.Contains(element)) ShowMemoContextMenu(element, point);
            else if (GroupGanttLists.Elements.Contains(element)) ShowGanttContextMenu(element, point);
        }

        private void View_DocumentClosing(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentCancelEventArgs e)
        {
            if (MemoDocs.Contains((Document)e.Document))
            {
                var widgetClose = new WidgetClose();
                if (XtraDialog.Show(widgetClose, "Close Memo", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    switch (widgetClose.WidgetCloseMode)
                    {
                        case WidgetCloseMode.WidgetOnly:
                            RemoveMemoDoc((Document)e.Document);
                            break;

                        case WidgetCloseMode.WidgetAndData:
                            RemoveMemoDoc((Document)e.Document);
                            RemoveMemoData((Document)e.Document);
                            break;
                    }
                    MemoData.SaveData();
                }
                else e.Cancel = true;
            }
            if (GanttDocs.Contains((Document)e.Document))
            {
                var widgetClose = new WidgetClose();
                if (XtraDialog.Show(widgetClose, "Close Gantt", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    switch (widgetClose.WidgetCloseMode)
                    {
                        case WidgetCloseMode.WidgetOnly:
                            RemoveGanttDoc((Document)e.Document);
                            break;

                        case WidgetCloseMode.WidgetAndData:
                            RemoveGanttDoc((Document)e.Document);
                            RemoveGanttData((Document)e.Document);
                            break;
                    }
                    GanttData.SaveData();
                }
                else e.Cancel = true;
            }
        }

        private void View_DocumentClosed(object sender, DevExpress.XtraBars.Docking2010.Views.DocumentEventArgs e)
        {
            if (e.Document == SchedulerDoc)
            {
                SchedulerDoc = null;
                InitSchedulerOnly();
            }
            else if (e.Document == CalendarDoc) CalendarDoc = null;
        }

        /*-------------------------------------------
         *
         *      Private functions
         *
         -------------------------------------------*/

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            this.Resize += MainFrame_Resized;
            this.Move += MainFrame_Move;
            this.FormClosing += MainFrame_FormClosing;
        }

        private void ConnectingUserEvents()
        {
            Navigation.ElementClicked += Navigation_ElementClicked;
            View.DocumentClosing += View_DocumentClosing;
            View.DocumentClosed += View_DocumentClosed;

            Calendar.Click += CalendarLabel_Click;
            Calendar.ContextButtons[0].Click += CalendarShowButton_Click;
            Scheduler.Click += Scheduler_Click;
            Scheduler.ContextButtons[0].Click += SchedulerShowButton_Click;

            NewMemo.Click += NewMemo_Click;
            NewGantt.Click += NewGantt_Click;

            SkinGalleryEdit.Properties.Gallery.ItemClick += SkinGallery_ItemClick;
            SkinPaletteGalleryEdit.Properties.Gallery.ItemClick += SkinPaletteGallery_ItemClick;

            WorkingTimeStart.EditValueChanged += WorkingTimeStart_EditValueChanged;
            WorkingTimeEnd.EditValueChanged += WorkingTimeEnd_EditValueChanged;

            AppointmentLabel.ContextButtons[0].Click += AppointmentLabelButton_Click;
            AppointmentStatus.ContextButtons[0].Click += AppointmentStatusButton_Click;
            AppointmentResource.ContextButtons[0].Click += AppointmentResourceButton_Click;
        }
    }
}