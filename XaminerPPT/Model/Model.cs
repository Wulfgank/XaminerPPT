using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XaminerConverter;

namespace XaminerPPT.Model
{
    class Model
    {
        #region Fields
        private string _desc;
        private string _filterKeywords;
        private int _currentIndex;
        private Exam _currentExam;
        private List<ExamSubmission> _filteredSubmissions;
        private ExamSubmission _currentSubmission;
        #endregion

        #region Properties
        public string Desc { get => _desc; set => _desc = value; }
        public string FilterKeywords { get => _filterKeywords; set => _filterKeywords = value; }
        public int CurrentIndex { get => _currentIndex; set => _currentIndex = value; }
        internal Exam CurrentExam { get => _currentExam; set => _currentExam = value; }        
        internal List<ExamSubmission> FilteredSubmissions { get => _filteredSubmissions; set => _filteredSubmissions = value; }
        internal ExamSubmission CurrentSubmission { get => _currentSubmission; set => _currentSubmission = value; }
        #endregion

        #region Constructor
        public Model()
        {
            this.FilteredSubmissions = new List<ExamSubmission>();
        }
        #endregion

        #region Methods
        public void SetExam(string path)
        {
            this.CurrentExam = new Exam(Path.GetFileName(path), path, DateTime.Now);
            this.CurrentExam.LoadSubmissions();
            this.FilteredSubmissions = this.CurrentExam.ExamSubmissions;
        }

        public void ApplyFilter()
        {
            if (this.CurrentExam != null)
            {
                if (this.FilterKeywords != string.Empty)
                {
                    this.FilteredSubmissions = this.CurrentExam.ExamSubmissions.FindAll(x =>
                    {
                        if (x.TxtContent != string.Empty)
                        {
                            string txtContent = x.TxtContent.ToLower();
                            bool contains = txtContent.Contains(this.FilterKeywords.ToLower());
                            return contains;
                        }
                        else
                        {
                            return false;
                        }
                    });
                }
                else
                {
                    this.FilteredSubmissions = this.CurrentExam.ExamSubmissions;
                }
            }
        }
        #endregion
    }
}
