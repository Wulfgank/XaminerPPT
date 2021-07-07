using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XaminerConverter
{
    class Exam
    {
        #region Fields
        private string _desc;
        private string _directoryPath;
        private DateTime _date;
        private List<ExamSubmission> _examSubmissions;
        #endregion

        #region Properties
        public string Desc { get => _desc; set => _desc = value; }
        public string DirectoryPath { get => _directoryPath; set => _directoryPath = value; }
        public DateTime Date { get => _date; set => _date = value; }
        internal List<ExamSubmission> ExamSubmissions { get => _examSubmissions; set => _examSubmissions = value; }
        #endregion

        #region Constructor
        public Exam(string desc, string directoryPath, DateTime date)
        {
            this.Desc = desc;
            this.DirectoryPath = directoryPath;
            this.Date = date;
            this.ExamSubmissions = new List<ExamSubmission>();
        }
        #endregion

        #region Methods
        public string[] ReadTxtExams()
        {
            return Directory.GetFiles(this.DirectoryPath, "*.txt", SearchOption.AllDirectories);
        }

        public void LoadSubmissions()
        {
            string[] files = ReadTxtExams();

            foreach (string file in files)
            {
                ExamSubmission es = new ExamSubmission(this.DirectoryPath, file);
                this.ExamSubmissions.Add(es);
                es.LoadAnswers();
            }

            // sort submissions by LastName, then FirstName
            this.ExamSubmissions.Sort((x, y) =>
            {
                int comparison = x.Student.LastName.CompareTo(y.Student.LastName);
                if (comparison == 0)
                {
                    comparison = x.Student.FirstName.CompareTo(y.Student.FirstName);
                }
                return comparison;
            });
        }

        public void AllSubmissionsToPdf(BackgroundWorker worker)
        {
            int progress = 0;
            foreach (ExamSubmission es in this.ExamSubmissions)
            {
                if (!File.Exists(es.MappedFileName))
                {
                    string html = es.GenerateHtml(this.Desc, this.DirectoryPath);
                    es.ToPdf(html);
                }
                progress++;
                worker.ReportProgress(progress);
            }
        }

        public override string ToString()
        {
            string toString = this.Desc;
            
            foreach(ExamSubmission es in this.ExamSubmissions)
            {
                toString += Environment.NewLine + es.ToString();
            }

            return toString;
        }
        #endregion
    }
}
