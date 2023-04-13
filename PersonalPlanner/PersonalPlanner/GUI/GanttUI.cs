﻿using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGantt;
using DevExpress.XtraGantt.Options;
using DevExpress.XtraTab;
using DevExpress.XtraTreeList.Columns;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PersonalPlanner.Define;

namespace PersonalPlanner.GUI
{
    public partial class GanttUI : XtraTabPage
    {
        /*-------------------------------------------
         *
         *      Design time properties
         *
         -------------------------------------------*/

        /*-------------------------------------------
         *
         *      Events
         *
         -------------------------------------------*/

        public delegate void GanttDataSaveEventHandler();

        public event GanttDataSaveEventHandler GanttDataSave;

        /*-------------------------------------------
         *
         *      Public members
         *
         -------------------------------------------*/

        public GanttDefine GanttData;

        /*-------------------------------------------
         *
         *      Private members
         *
         -------------------------------------------*/

        private readonly SynchronizationContext SyncContext;

        /*-------------------------------------------
         *
         *      Constructor / Destructor
         *
         -------------------------------------------*/

        public GanttUI()
        {
            InitializeComponent();
            // Do not use this
        }

        public GanttUI(GanttDefine ganttData)
        {
            InitializeComponent();
            SyncContext = SynchronizationContext.Current;
            MainGanttControl.Dock = DockStyle.Fill;
            this.Controls.Add(MainGanttControl);
            GanttData = ganttData;

            this.Text = ganttData.Name;
            SetTabPageColor();

            SetGanttControlSettings();
            BindData();

            MainGanttControl.EditFormHidden += MainGanttControl_EditFormHidden;
            this.VisibleChanged += GanttUI_VisibleChanged;
        }

        /*-------------------------------------------
         *
         *      Event functions
         *
         -------------------------------------------*/

        private void GanttControl_Load(object sender, EventArgs e) => MainGanttControl.ScrollChartToDate(DateTime.Now.Date);

        private void DrawTodayLine(CustomDrawTimescaleColumnEventArgs e)
        {
            e.DrawBackground();
            float x = (float)e.GetPosition(DateTime.Now);
            float width = 4;

            RectangleF rectangle = new RectangleF(x, e.Column.Bounds.Y, width, e.Column.Bounds.Height);

            e.Cache.FillRectangle(System.Drawing.Color.Red, rectangle);
            e.DrawHeader();
        }

        private void MainGanttControl_EditFormHidden(object sender, DevExpress.XtraTreeList.EditFormHiddenEventArgs e)
        {
            if (e.Result == DevExpress.XtraTreeList.EditFormResult.Update) SaveData();
        }

        private void MainGanttControl_TaskProgressModified(object sender, TaskProgressModifiedEventArgs e) => SaveData();

        private void MainGanttControl_TaskFinishDateModified(object sender, TaskFinishModifiedEventArgs e) => SaveData();

        private void MainGanttControl_TaskDependencyModified(object sender, TaskDependencyModificationEventArgs e) => SaveData();

        private void MainGanttControl_TaskMoved(object sender, TaskMovedEventArgs e) => SaveData();

        private void GanttUI_VisibleChanged(object sender, EventArgs e)
        {
            if (this.PageVisible) MainGanttControl.ExpandAll();
        }

        /*-------------------------------------------
         *
         *      Public functions
         *
         -------------------------------------------*/

        public void ChangeTabPageColor(System.Drawing.Color color)
        {
            GanttData.Color.ToInternalColor(color);
            SetTabPageColor();
        }

        public bool AddTask(Task task)
        {
            var taskData = GanttData.Task.Find(item => item.ID.Equals(task.ID));
            if (taskData == default)
            {
                GanttData.Task.Add(task);
                MainGanttControl.RefreshDataSource();
                MainGanttControl.ExpandAll();
                return true;
            }
            else
            {
                XtraMessageBox.Show("Duplicated ID. Retry again", "Could not add new task");
            }
            return false;
        }

        public bool RemoveTask()
        {
            var currentTask = MainGanttControl.GetFocusedRow() as Task;
            if (currentTask != null)
            {
                GanttData.Task.Remove(currentTask);
                GanttData.Dependency.RemoveAll(item => item.SuccessorID.Equals(currentTask.ID) || item.PredecessorID.Equals(currentTask.ID));
                MainGanttControl.RefreshDataSource();
                MainGanttControl.ExpandAll();
                return true;
            }
            else
            {
                XtraMessageBox.Show("Could not find task.\nPlease select one");
            }
            return false;
        }

        /*-------------------------------------------
         *
         *      Private functions
         *
         -------------------------------------------*/

        private void SetTabPageColor()
        {
            this.BackColor = GanttData.Color.ToDrawingColor();
            this.Appearance.Header.BackColor = GanttData.Color.ToDrawingColor();
            this.Appearance.HeaderActive.BackColor = GanttData.Color.ToDrawingColor();
        }

        private void SetGanttControlSettings()
        {
            MainGanttControl.OptionsCustomization.AllowModifyTasks = DefaultBoolean.True;
            MainGanttControl.OptionsCustomization.AllowModifyProgress = DefaultBoolean.True;
            MainGanttControl.OptionsCustomization.AllowModifyDependencies = DefaultBoolean.True;
            MainGanttControl.TaskDependencyModified += MainGanttControl_TaskDependencyModified;
            MainGanttControl.TaskFinishDateModified += MainGanttControl_TaskFinishDateModified;
            MainGanttControl.TaskProgressModified += MainGanttControl_TaskProgressModified;
            MainGanttControl.TaskMoved += MainGanttControl_TaskMoved;
            MainGanttControl.OptionsBehavior.ScheduleMode = ScheduleMode.Manual;
            MainGanttControl.OptionsSplitter.SplitterThickness = 3;
            MainGanttControl.OptionsSplitter.OverlayResizeZoneThickness = 5;

            MainGanttControl.OptionsBehavior.EditingMode = DevExpress.XtraTreeList.TreeListEditingMode.EditForm;

            MainGanttControl.CustomDrawTimescaleColumn += (object sender, CustomDrawTimescaleColumnEventArgs e) => DrawTodayLine(e);
            MainGanttControl.Load += GanttControl_Load;
        }

        private void BindData()
        {
            AddGanttColumn(nameof(Task.ID));
            AddGanttColumn(nameof(Task.ParentID));
            AddGanttColumn(nameof(Task.Name));
            AddGanttColumn(nameof(Task.StartDate));
            AddGanttColumn(nameof(Task.FinishDate));
            AddGanttColumn(nameof(Task.Progress));
            AddGanttColumn(nameof(Task.Responsibility));

            MainGanttControl.TreeListMappings.KeyFieldName = nameof(Task.ID);
            MainGanttControl.TreeListMappings.ParentFieldName = nameof(Task.ParentID);

            MainGanttControl.ChartMappings.TextFieldName = nameof(Task.Name);
            MainGanttControl.ChartMappings.StartDateFieldName = nameof(Task.StartDate);
            MainGanttControl.ChartMappings.FinishDateFieldName = nameof(Task.FinishDate);
            MainGanttControl.ChartMappings.ProgressFieldName = nameof(Task.Progress);

            MainGanttControl.DependencyMappings.PredecessorFieldName = nameof(Dependency.PredecessorID);
            MainGanttControl.DependencyMappings.SuccessorFieldName = nameof(Dependency.SuccessorID);

            MainGanttControl.DataSource = GanttData.Task;
            MainGanttControl.DependencySource = GanttData.Dependency;

            MainGanttControl.ExpandAll();
        }

        /*-------------------------------------------
         *
         *      Helper functions
         *
         -------------------------------------------*/

        private void AddGanttColumn(string name)
        {
            TreeListColumn column = new TreeListColumn();

            column.Name = name;
            column.FieldName = name;
            column.Caption = name;
            column.Width = 100;
            column.Visible = true;

            MainGanttControl.Columns.Add(column);
        }

        private void SaveData()
        {
            GanttDataSave?.Invoke();
            MainGanttControl.RefreshDataSource();
            MainGanttControl.ExpandAll();
        }
    }
}