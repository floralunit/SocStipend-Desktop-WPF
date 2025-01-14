﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using SocStipendDesktop.Models;
using System.Windows;
using SocStipendDesktop.Views;
using SocStipendDesktop.Services;
using System.Text.RegularExpressions;

namespace SocStipendDesktop.ViewModels
{
    public class StipendCollectionViewModel : INotifyPropertyChanged
    {
        public StipendCollectionViewModel()
        {
            StudentCheck = true;
            DtAssignCheck = true;
            ActualStipendCheck = true;
            UpdateStipendCollection();
        }
        public void UpdateStipendCollection()
        {
            var stipends = App.Context.Stipends.ToList();
            var students = App.Context.Students.ToList();
            foreach (Stipend item in stipends)
            {
                if (item.StudentId != null) {
                    item.StudentName = students.FirstOrDefault(x => x.Id == item.StudentId).StudentName;
                    item.StudentGroup = students.FirstOrDefault(x => x.Id == item.StudentId).StudentGroup;
                    item.Status = students.FirstOrDefault(x => x.Id == item.StudentId).Status;
                }
            }
            if (StoppedStipendCheck == true)
            {
                stipends = stipends.Where(p => p.DtStop != null).ToList();
            }
            if (ActualStipendCheck == true)
            {
                stipends = stipends.Where(p => (p.DtEnd == null || p.DtEnd >= DateTime.Now) && p.DtStop == null).ToList();
            }
            if (CardStipendCheck == true)
            {
                stipends = stipends.Where(p => p.HasTravelCard == true).ToList();
            }
            if (DateTo != null)
            {
                if (DtAssignCheck == true) stipends = stipends.Where(p => p.DtAssign != null && p.DtAssign <= DateTo).ToList();
                if (DtEndCheck == true) stipends = stipends.Where(p => p.DtEnd != null && p.DtEnd <= DateTo).ToList();
            }
            if (DateFrom != null)
            {
                if (DtAssignCheck == true) stipends = stipends.Where(p => p.DtAssign != null && p.DtAssign >= DateFrom).ToList();
                if (DtEndCheck == true) stipends = stipends.Where(p => p.DtEnd != null && p.DtEnd >= DateFrom).ToList();
            }
            if (SearchBox != null)
            {
                if (StudentCheck == true) stipends = stipends.Where(p => p.StudentName != null && p.StudentName.ToLower().Contains(SearchBox.ToLower()) || p.Status != null && p.Status.ToLower().Contains(SearchBox.ToLower())).ToList();
                if (GroupCheck == true) stipends = stipends.Where(p => p.StudentGroup != null && p.StudentGroup.ToLower().Contains(SearchBox.ToLower())).ToList();
            }
            StipendCollection = new ObservableCollection<Stipend>(stipends.OrderBy(p => p.StudentName));
            int i = 1;
            foreach (var stipend in StipendCollection)
            {
                stipend.OrderNum = i;
                i++;
            }
        }

        //открытие окна для работы с отдельным студентом
        private RelayCommand selectedStipendClickCommand;
        public RelayCommand SelectedStipendClickCommand => selectedStipendClickCommand ??
                  (selectedStipendClickCommand = new RelayCommand(obj =>
                  {
                      if (SelectedStipend == null)
                      {
                          MessageBox.Show("Выберите справку!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                          return;
                      }
                      else
                      {
                          var studentView = new StudentView();
                          var studentModel = studentView.DataContext as StudentViewModel;
                          studentModel.CurrentStudent = App.Context.Students.FirstOrDefault(s => s.Id == SelectedStipend.StudentId);
                          studentModel.StipendsEnabled = true;
                          studentView.Show();
                      }
                  }));


        //добавление нового студента
        private RelayCommand createNewStudentClickCommand;
        public RelayCommand CreateNewStudentClickCommand => createNewStudentClickCommand ??
                  (createNewStudentClickCommand = new RelayCommand(obj =>
                  {
                      var studentView = new StudentView();
                      var studentModel = studentView.DataContext as StudentViewModel;
                      studentModel.CurrentStudent = new Student ();
                      studentModel.CurrentStudent.IsExpelled = false;
                      studentModel.StipendsEnabled = false;
                      studentView.Show();
                  }));


        //обновление групп в связи с новым учебным годом
        private RelayCommand updateGloupNameCommand;
        public RelayCommand UpdateGloupNameCommand => updateGloupNameCommand ??
                  (updateGloupNameCommand = new RelayCommand(obj =>
                  {
                      var month = DateTime.Now.Month;
                      if (month >= 8 && month <= 12)
                      {
                          var result = MessageBox.Show("Обновить группу для всех студентов в связи с новым уч. годом? \nЭто действие нельзя отменить.", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                          if (result == MessageBoxResult.Yes)
                          {
                              foreach (var student in App.Context.Students)
                              {
                                  if (student.StudentGroup != null)
                                  {
                                      var group = student.StudentGroup;
                                      var groupNew = UpdateGroup(group);
                                      student.StudentGroup = groupNew;
                                  }
                              }
                              App.Context.SaveChanges();
                              UpdateStipendCollection();
                          }
                          else
                              return;
                      }
                      else
                      {
                          MessageBox.Show("Обновление группы возможно только в период с 1 августа по 31 декабря", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                          return;
                      }
                  }));
        public string UpdateGroup (string groupOld)
        {
            string groupNew = groupOld;
            var year = DateTime.Now.Year;
            year = int.Parse(year.ToString().Substring(year.ToString().Length - 2));
            try
            {
                var groupNumOld = Regex.Match(groupOld, @"\d+").Value;
                var groupYear = int.Parse(groupNumOld.Substring(groupNumOld.Length - 2));
                int kurs;
                switch (groupYear, year)
                {
                    case ( > 0, > 0) when groupYear == year:
                        kurs = 1;
                        break;

                    case ( > 0, > 0) when groupYear == (year - 1):
                        kurs = 2;
                        break;

                    case ( > 0, > 0) when groupYear == (year - 2):
                        kurs = 3;
                        break;
                    case ( > 0, > 0) when groupYear == (year - 3):
                        kurs = 4;
                        break;

                    case ( > 0, > 0) when groupYear == (year - 4):
                        kurs = 5;
                        break;
                    default:
                        groupNew = "Выпускник";
                        return groupNew;
                }
                var groupNumNew = string.Format(kurs.ToString() + groupYear.ToString());
                int pos = groupOld.IndexOf(groupNumOld);
                if (pos < 0)
                {
                    return groupNew;
                }
                groupNew = groupOld.Substring(0, pos) + groupNumNew + groupOld.Substring(pos + groupNumOld.Length);
            }
            catch (Exception ex)
            {
                return groupNew;
            }
            return groupNew;
        }

        //удаление записи
        private RelayCommand stipendDeleteClickCommand;
        public RelayCommand StipendDeleteClickCommand => stipendDeleteClickCommand ??
                  (stipendDeleteClickCommand = new RelayCommand(obj =>
                  {
                      if (SelectedStipend == null)
                      {
                          MessageBox.Show("Выберите справку!", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning);
                          return;
                      }
                      else
                      {
                          var result = MessageBox.Show("Удалить выбранную справку?", $"{SelectedStipend.StudentName} от {SelectedStipend.DtAssign}", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                          if (result == MessageBoxResult.Yes)
                          {
                              var stipend = App.Context.Stipends.FirstOrDefault(s => s.Id == SelectedStipend.Id);
                              App.Context.Stipends.Remove(stipend);
                              App.Context.SaveChanges();
                              UpdateStipendCollection();
                          }
                          else
                              return;
                      }
                  }));

        //выход из приложения
        private RelayCommand exitClickCommand;
        public RelayCommand ExitClickCommand => exitClickCommand ??
                  (exitClickCommand = new RelayCommand(obj =>
                  {
                      Application.Current.MainWindow.Close();
                  }));

        //обновить таблицу
        private RelayCommand refreshClickCommand;
        public RelayCommand RefreshClickCommand => refreshClickCommand ??
                  (refreshClickCommand = new RelayCommand(obj =>
                  {
                      DateFrom = null;
                      DateTo = null;
                      SearchBox = "";
                      ActualStipendCheck = true;
                      StoppedStipendCheck = false;
                      CardStipendCheck = false;
                      UpdateStipendCollection();
                  }));

        // поиск
        private RelayCommand searchTextChangedCommand;
        public RelayCommand SearchTextChangedCommand => searchTextChangedCommand ??
                  (searchTextChangedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
        // поиск по студенту
        private RelayCommand studentCheckedCommand;
        public RelayCommand StudentCheckedCommand
        {
            get
            {
                return studentCheckedCommand ??
                  (studentCheckedCommand = new RelayCommand(obj =>
                  {
                      StudentCheck = true;
                      GroupCheck = false;
                      UpdateStipendCollection();
                  }));
            }
        }
        // поиск по группе
        private RelayCommand groupCheckedCommand;
        public RelayCommand GroupCheckedCommand
        {
            get
            {
                return groupCheckedCommand ??
                  (groupCheckedCommand = new RelayCommand(obj =>
                  {
                      StudentCheck = false;
                      GroupCheck = true;
                      UpdateStipendCollection();
                  }));
            }
        }
        // изменение даты От
        private RelayCommand dtFromChangedCommand;
        public RelayCommand DtFromChangedCommand
        {
            get
            {
                return dtFromChangedCommand ??
                  (dtFromChangedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }
        // изменение даты До
        private RelayCommand dtToChangedCommand;
        public RelayCommand DtToChangedCommand
        {
            get
            {
                return dtToChangedCommand ??
                  (dtToChangedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }
        // поиск по дате назначения
        private RelayCommand dtAssignCheckedCommand;
        public RelayCommand DtAssignCheckedCommand
        {
            get
            {
                return dtAssignCheckedCommand ??
                  (dtAssignCheckedCommand = new RelayCommand(obj =>
                  {
                      DtAssignCheck = true;
                      DtEndCheck = false;
                      UpdateStipendCollection();
                  }));
            }
        }
        // поиск по дате окончания
        private RelayCommand dtEndCheckedCommand;
        public RelayCommand DtEndCheckedCommand
        {
            get
            {
                return dtEndCheckedCommand ??
                  (dtEndCheckedCommand = new RelayCommand(obj =>
                  {
                      DtAssignCheck = false;
                      DtEndCheck = true;
                      UpdateStipendCollection();
                  }));
            }
        }
        // актуальная стипендия
        private RelayCommand actualStipendCheckedCommand;
        public RelayCommand ActualStipendCheckedCommand
        {
            get
            {
                return actualStipendCheckedCommand ??
                  (actualStipendCheckedCommand = new RelayCommand(obj =>
                  {
                      StoppedStipendCheck = false;
                      UpdateStipendCollection();
                  }));
            }
        }
        private RelayCommand actualStipendUncheckedCommand;
        public RelayCommand ActualStipendUncheckedCommand
        {
            get
            {
                return actualStipendUncheckedCommand ??
                  (actualStipendUncheckedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }

        // приостановленная стипендия
        private RelayCommand stoppedStipendCheckedCommand;
        public RelayCommand StoppedStipendCheckedCommand
        {
            get
            {
                return stoppedStipendCheckedCommand ??
                  (stoppedStipendCheckedCommand = new RelayCommand(obj =>
                  {
                      ActualStipendCheck = false;
                      UpdateStipendCollection();
                  }));
            }
        }
        private RelayCommand stoppedStipendUncheckedCommand;
        public RelayCommand StoppedStipendUncheckedCommand
        {
            get
            {
                return stoppedStipendUncheckedCommand ??
                  (stoppedStipendUncheckedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }

        // стипендия с проездным
        private RelayCommand cardStipendCheckedCommand;
        public RelayCommand CardStipendCheckedCommand
        {
            get
            {
                return cardStipendCheckedCommand ??
                  (cardStipendCheckedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }
        private RelayCommand cardStipendUncheckedCommand;
        public RelayCommand CardStipendUncheckedCommand
        {
            get
            {
                return cardStipendUncheckedCommand ??
                  (cardStipendUncheckedCommand = new RelayCommand(obj =>
                  {
                      UpdateStipendCollection();
                  }));
            }
        }

        public ObservableCollection<Stipend> stipendcol;
        public ObservableCollection<Stipend> StipendCollection
        {
            get { return stipendcol; }
            set
            {
                stipendcol = value;
                OnPropertyChanged("StipendCollection");
            }
        }
        public Stipend selectedstipend;
        public Stipend SelectedStipend
        {
            get { return selectedstipend; }
            set
            {
                selectedstipend = value;
                OnPropertyChanged("SelectedStipend");
            }
        }
        public bool _ActualStipendCheck;
        public bool ActualStipendCheck
        {
            get { return _ActualStipendCheck; }
            set
            {
                _ActualStipendCheck = value;
                OnPropertyChanged("ActualStipendCheck");
            }
        }
        public bool _StoppedStipendCheck;
        public bool StoppedStipendCheck
        {
            get { return _StoppedStipendCheck; }
            set
            {
                _StoppedStipendCheck = value;
                OnPropertyChanged("StoppedStipendCheck");
            }
        }
        public bool _CardStipendCheck;
        public bool CardStipendCheck
        {
            get { return _CardStipendCheck; }
            set
            {
                _CardStipendCheck = value;
                OnPropertyChanged("CardStipendCheck");
            }
        }
        public bool dtAssignCheck;
        public bool DtAssignCheck
        {
            get { return dtAssignCheck; }
            set
            {
                dtAssignCheck = value;
                OnPropertyChanged("DtAssignCheck");
            }
        }
        public bool dtEndCheck;
        public bool DtEndCheck
        {
            get { return dtEndCheck; }
            set
            {
                dtEndCheck = value;
                OnPropertyChanged("DtEndCheck");
            }
        }
        public DateTime? _DateTo;
        public DateTime? DateTo
        {
            get { return _DateTo; }
            set
            {
                _DateTo = value;
                OnPropertyChanged("DateTo");
            }
        }
        public DateTime? _DateFrom;
        public DateTime? DateFrom
        {
            get { return _DateFrom; }
            set
            {
                _DateFrom = value;
                OnPropertyChanged("DateFrom");
            }
        }
        public bool groupcheck;
        public bool GroupCheck
        {
            get { return groupcheck; }
            set
            {
                groupcheck = value;
                OnPropertyChanged("GroupCheck");
            }
        }
        public bool studentcheck;
        public bool StudentCheck
        {
            get { return studentcheck; }
            set
            {
                studentcheck = value;
                OnPropertyChanged("StudentCheck");
            }
        }
        public string searchbox;
        public string SearchBox
        {
            get { return searchbox; }
            set
            {
                searchbox = value;
                OnPropertyChanged("SearchBox");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
