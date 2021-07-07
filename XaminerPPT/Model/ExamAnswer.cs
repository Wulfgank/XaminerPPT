using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XaminerConverter
{
    class ExamAnswer
    {
        #region Fields
        private AnswerType _type;
        private string _question;
        private string _answer;
        private int _width;
        private int _height;
        #endregion

        #region Properties
        public string Question { get => _question; set => _question = value; }
        public string Answer { get => _answer; set => _answer = value; }
        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        internal AnswerType Type { get => _type; set => _type = value; }
        #endregion

        #region Constructor
        public ExamAnswer(AnswerType type, string answer, string question = "")
        {
            this.Type = type;
            this.Answer = answer;
            this.Question = question;
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return this.Answer;
        }
        #endregion
    }
}
