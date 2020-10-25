﻿using Class_Management.Models;
using Newtonsoft.Json;
using System;
using System.Data.SQLite;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Class_Management.Views
{
    /// <summary>
    /// Interaction logic for Attendance.xaml
    /// </summary>
    public partial class Attendance : UserControl
    {
        SQLiteConnection conn = new SQLiteConnection(@"Data Source=D:\Software Testing and QA\Database\MainDatabase.db;Version=3;");

        public Attendance()
        {
            InitializeComponent();
        }

        public Attendance(object context)
        {
            InitializeComponent();
        }

        int activeMonth = int.Parse(DateTime.Now.ToString("MM"));
        string activeBatch = null;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard mystory;
            mystory = (Storyboard)App.Current.Resources["sb"];
            mystory.Begin(this);
            conn.Open();
            GetEverythingReady();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            conn.Close();
        }

        private void closeUC_Click(object sender, RoutedEventArgs e)
        {
            (this.Parent as Grid).Children.Remove(this);
        }

        private void GetEverythingReady()
        {
            FillBatches();
            CreateMonthColumns();
        }

        private void FillBatches()
        {
            try
            {
                string sql = "SELECT batch_name FROM batch;";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                command.ExecuteNonQuery();
                SQLiteDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    Button btn = new Button();
                    btn.Style = Resources["BatchButton"] as Style;
                    btn.Content = dr.GetString(0);
                    batch_list.Children.Add(btn);
                }
                dr.Close();
                command.Dispose();
            }
            catch (Exception ex)
            {
                string msg = ex.GetType().Name + " : " + ex.Message;
                MessageBox.Show(msg);
            }
        }

        private void Button_Batch_Select_Click(object sender, RoutedEventArgs e)
        {
            activeBatch = (sender as Button).Content.ToString();
            AttendanceBoxStackPanel.Visibility = Visibility.Visible;
            BatchNameLabel.Content = "Batch: " + activeBatch;
            FillAttendanceDataGrid();
        }

        private void Button_Batch_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as Button).Background = Brushes.MediumSeaGreen;
        }

        private void Button_Batch_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as Button).Background = Brushes.Teal;
        }

        private void CreateMonthColumns()
        {
            int year = DateTime.Now.Year;
            int days = DateTime.DaysInMonth(year, activeMonth);

            AttendanceDataGrid.Columns.Clear();

            DataGridTextColumn RegNoColumn = new DataGridTextColumn()
            {
                Header = "Reg No",
                Binding = new Binding("RegNo")
            };
            AttendanceDataGrid.Columns.Add(RegNoColumn);
            DataGridTextColumn NameColumn = new DataGridTextColumn()
            {
                Header = "Name",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            };
            AttendanceDataGrid.Columns.Add(NameColumn);

            for (int i = 1; i <= days; i++)
            {
                DataGridTemplateColumn dgtc = new DataGridTemplateColumn();
                dgtc.Header = i;
                FrameworkElementFactory factory1 = new FrameworkElementFactory(typeof(CheckBox));
                Binding b1 = new Binding("IsSelected" + i);
                b1.Mode = BindingMode.TwoWay;
                factory1.SetValue(CheckBox.IsCheckedProperty, b1);
                factory1.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler(ChkSelect_Checked));
                factory1.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler(ChkSelect_Checked));
                DataTemplate cellTemplate1 = new DataTemplate();
                cellTemplate1.VisualTree = factory1;
                dgtc.CellTemplate = cellTemplate1;
                AttendanceDataGrid.Columns.Add(dgtc);
            }
        }

        private void FillAttendanceDataGrid()
        {
            try
            {
                AttendanceDataGrid.Items.Clear();
                string month = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(activeMonth);
                if (SortOptions.SelectedItem == null)
                {
                    SortOptions.SelectedIndex = 0;
                    return;
                }
                string sortOption = (SortOptions.SelectedItem as ComboBoxItem).Name;
                //string sql = "SELECT " + month.ToLower() + " FROM attendance WHERE reg_no IN (SELECT reg_no FROM student WHERE batch='" + activeBatch + "') ORDER BY reg_no ASC;";
                string sql = "SELECT " + month.ToLower() + " FROM attendance INNER JOIN student ON attendance.reg_no = student.reg_no WHERE attendance.reg_no IN (SELECT reg_no FROM student WHERE batch='" + activeBatch + "') ORDER BY student." + sortOption + " ASC;";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                SQLiteDataReader dr = command.ExecuteReader();
                while (dr.Read())
                {
                    string json = dr.GetString(0);
                    Student student = JsonConvert.DeserializeObject<Student>(json);
                    DataGridRow dgr = new DataGridRow()
                    {
                        Item = student
                    };
                    AttendanceDataGrid.Items.Add(dgr);
                }
                dr.Close();
                command.Dispose();
            }
            catch (Exception ex)
            {
                string msg = ex.GetType().Name + " : " + ex.Message;
                MessageBox.Show(msg);
            }
        }

        private void ChkSelect_Checked(object sender, RoutedEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            Tuple<DataGridCell, DataGridRow> tuple = GetDataGridRowAndCell(dep);
            DataGridCell cell = tuple.Item1 as DataGridCell;
            DataGridRow dgr = tuple.Item2 as DataGridRow;
            Student student = dgr.Item as Student;
            string row = student.RegNo;
            string col = cell.Column.Header.ToString();
            try
            {
                string month = (CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(activeMonth)).ToLower();
                string json = JsonConvert.SerializeObject(student);
                string sql = "UPDATE attendance SET " + month + "='" + json + "' WHERE reg_no='" + row + "';";
                SQLiteCommand command = new SQLiteCommand(sql, conn);
                command.ExecuteNonQuery();
                command.Dispose();
            }
            catch (Exception ex)
            {
                string msg = ex.GetType().Name + " : " + ex.Message;
                MessageBox.Show(msg);
            }
        }

        public static Tuple<DataGridCell, DataGridRow> GetDataGridRowAndCell(DependencyObject dep)
        {
            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return null;

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;

                // navigate further up the tree
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }

                DataGridRow row = dep as DataGridRow;
                return new Tuple<DataGridCell, DataGridRow>(cell, row);
            }
            return null;
        }

        private void Month_Select_Click(object sender, RoutedEventArgs e)
        {
            activeMonth = DateTime.ParseExact((sender as MenuItem).Header.ToString(), "MMMM", CultureInfo.InvariantCulture).Month;
            CreateMonthColumns();
            (sender as MenuItem).Focus();
            FillAttendanceDataGrid();
        }

        private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FillAttendanceDataGrid();
        }
    }
}
