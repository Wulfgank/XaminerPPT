using Codaxy.WkHtmlToPdf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XaminerConverter
{
    class ExamSubmission
    {
        #region Fields
        private const string ANSWER_SEPARATOR = "===============";

        private string _filePath;
        private string _directoryPath;
        private string _mappedFileName;
        private string _txtContent;
        private Student _student;
        private List<ExamAnswer> _answers;
        #endregion

        #region Properties
        public string FilePath { get => _filePath; set => _filePath = value; }
        public string DirectoryPath { get => _directoryPath; set => _directoryPath = value; }
        public string MappedFileName { get => _mappedFileName; set => _mappedFileName = value; }
        public string TxtContent { get => _txtContent; set => _txtContent = value; }
        internal Student Student { get => _student; set => _student = value; }
        internal List<ExamAnswer> Answers { get => _answers; set => _answers = value; }
        #endregion

        #region Constructor
        public ExamSubmission(string directoryPath, string filePath)
        {
            this.DirectoryPath = directoryPath;
            this.FilePath = filePath;
            this.Answers = new List<ExamAnswer>();
        }
        #endregion

        #region Methods
        public void LoadAnswers()
        {
            // open from file
            this.TxtContent = File.ReadAllText(this.FilePath);
            string[] lines = this.TxtContent.Replace("\r", "").Split('\n');

            // first, read student from file
            this.ParseStudent(lines);

            string fileName = this.Student.LastName + "_" + this.Student.FirstName + "_" + this.Student.MatNr;
            this.MappedFileName = this.DirectoryPath + "\\" + fileName + ".pdf";

            if (!File.Exists(this.MappedFileName))
            {
                // read contents from file (answers with their type) until eof
                this.ParseAnswers(lines);
            }
        }

        private void ParseStudent(string[] lines)
        {
            string lastName = lines[4].Substring(11);
            string firstName = lines[5].Substring(12);
            this.Student = new Student(firstName, lastName, lines[2].Substring(7), Convert.ToInt32(lines[3].Substring(5)));
        }

        private void ParseAnswers(string[] lines)
        {
            int currentLine = 0;
            while (currentLine < lines.Length)
            {
                currentLine = ParseNextAnswer(lines, currentLine);
            }
        }

        private int ParseNextAnswer(string[] lines, int currentLine)
        {
            StringBuilder question = new StringBuilder();
            AnswerType answerType = AnswerType.TEXT;
            string answer = string.Empty;
            bool readAnswerType = false;
            bool readAnswerContent = false;

            while (currentLine < lines.Length)
            {
                string line = lines[currentLine];

                if (line.Equals(ANSWER_SEPARATOR))
                {
                    if (!readAnswerType && !readAnswerContent)
                    {
                        readAnswerType = true;
                    }
                    else if (readAnswerType && !readAnswerContent)
                    {
                        readAnswerContent = true;
                    }
                    else
                    {
                        ExamAnswer ea = new ExamAnswer(answerType, answer, question.ToString());
                        this.Answers.Add(ea);
                        break;
                    }
                }
                else
                {
                    if (readAnswerType && !readAnswerContent) // question-section
                    {
                        if (answerType == AnswerType.TEXT)
                        {
                            string type = line.Split(' ').Last();
                            Enum.TryParse(type, out answerType);
                        }
                        else
                        {
                            question.AppendLine(line);
                        }
                    }
                    else if (readAnswerType && readAnswerContent)
                    {
                        answer += line + Environment.NewLine;

                        if (currentLine == lines.Length - 1)
                        {
                            // add last line, if right before EOF
                            ExamAnswer ea = new ExamAnswer(answerType, answer, question.ToString());
                            this.Answers.Add(ea);
                        }
                    }
                }
                currentLine++;
            }
            return currentLine;
        }

        public string GenerateHtml(string description, string directory)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<html>");
            sb.AppendLine("  <head>");
            sb.AppendLine("    <meta http-equiv=\"Content-type\" content=\"text/html; charset=UTF-8\">");
            sb.AppendLine("  </head>");
            sb.AppendLine("  <body>");
            CreateDocumentTitle(description, sb);
            CreateAnswerSections(sb);
            sb.AppendLine("  </body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private void CreateDocumentTitle(string description, StringBuilder sb)
        {
            sb.AppendLine("    <div>");
            sb.AppendLine($"      <h1> {description} - {this.Student} </h1>");

            sb.AppendLine("      <p>");
            sb.AppendLine($"        MatNr: {this.Student.MatNr} <br />");
            sb.AppendLine($"        SKZ: {this.Student.Skz} <br />");
            sb.AppendLine($"        Name: {this.Student} <br />");
            sb.AppendLine("      </p>");

            sb.AppendLine("      <hr />");
            sb.AppendLine("    </div>");
        }

        private void CreateAnswerSections(StringBuilder sb)
        {
            int exerciseNumber = 1;

            foreach (ExamAnswer ea in this.Answers)
            {
                if (ea.Type != AnswerType.TEXT)
                {
                    sb.AppendLine();
                    sb.AppendLine("    <div style=\"page-break-inside: avoid\">");
                    sb.AppendLine("      <h1>Exercise " + exerciseNumber + "</h1>");
                    sb.AppendLine("      <p style=\"font-size: 22px; white-space: pre-wrap;\">" + ea.Question + "</p>");
                }

                switch(ea.Type)
                {
                    case AnswerType.TEXT: // skip
                        break;
                    case AnswerType.TEXT_QUESTION:
                    case AnswerType.CODE_QUESTION:
                        sb.AppendLine("      <pre style=\"font-size: 22px; white-space: pre-wrap;\">");
                        sb.AppendLine(ea.Answer.Replace("<", "&lt;").TrimEnd('\n', '\r'));
                        sb.AppendLine("      </pre>");
                        exerciseNumber++;
                        break;
                    case AnswerType.SINGLE_CHOICE_QUESTION:
                    case AnswerType.MULTIPLE_CHOICE_QUESTION:
                        int choiceNumber = 1;
                        string[] choices = ea.Answer.Split('\n');

                        sb.AppendLine("        <form>");
                        sb.AppendLine("          <fieldset>");

                        foreach (string c in choices)
                        {
                            string choice = c;
                            choice = choice.Replace("\r", string.Empty);
                            if (choice != string.Empty)
                            {
                                bool rightAnswer = choice.EndsWith("(true)", true, CultureInfo.InvariantCulture);
                                choice = rightAnswer ? choice.Substring(0, choice.Length - 7) : choice.Substring(0, choice.Length - 8);
                                choice = choice.Trim();
                                bool choiceAnswer = choice.EndsWith("true", true, CultureInfo.InvariantCulture);
                                string answerText = choiceAnswer ? choice.Substring(0, choice.Length - 6) : choice.Substring(0, choice.Length - 7);
                                answerText = answerText.Trim();
                                string id = "\"choice " + exerciseNumber + "_" + choiceNumber + "\"";
                                
                                if (ea.Type == AnswerType.SINGLE_CHOICE_QUESTION)
                                {
                                    sb.AppendLine("            <input type=\"radio\" onclick=\"return false;\" name=" + id + (choiceAnswer ? " checked>" : ">"));
                                }
                                else
                                {
                                    sb.AppendLine("            <input type=\"checkbox\" onclick=\"return false;\" name=" + id + (choiceAnswer ? " checked>" : ">"));
                                }

                                sb.Append("            <label style=\"font-size: 22px; white-space: pre-wrap;\" for=" + id + ">" + answerText);
                                if (choiceAnswer)
                                {
                                    if (rightAnswer)
                                    {
                                        sb.Append(" ✓");
                                    }
                                    else
                                    {
                                        sb.Append(" ✖");
                                    }
                                }
                                else
                                {
                                    if (!rightAnswer)
                                    {
                                        sb.Append(" ✓");
                                    }
                                    else
                                    {
                                        sb.Append(" ✖");
                                    }
                                }
                                sb.AppendLine("</label><br>");
                                choiceNumber++;
                            }
                        }

                        sb.AppendLine("          </fieldset>");
                        sb.AppendLine("        </form>");
                        exerciseNumber++;
                        break;
                    case AnswerType.IMAGE_QUESTION:
                        sb.AppendLine(ea.Answer.TrimEnd('\n', '\r'));
                        exerciseNumber++;
                        break;
                    default:
                        break;
                }

                if (ea.Type != AnswerType.TEXT)
                {
                    sb.AppendLine("      <hr />");
                    sb.AppendLine("    </div>");
                }
            }
        }
        
        public void ToPdf(string html)
        {
            PdfDocument pdfd = new PdfDocument();
            pdfd.Html = html;
            PdfOutput pdfo = new PdfOutput();
            pdfo.OutputFilePath = this.MappedFileName;

            PdfConvert.ConvertHtmlToPdf(pdfd, pdfo);
        }

        public override string ToString()
        {
            string toString = string.Empty;

            foreach (ExamAnswer ea in this.Answers)
            {
                toString += Environment.NewLine + ea.ToString();
            }

            return toString;
        }
        #endregion
    }
}
