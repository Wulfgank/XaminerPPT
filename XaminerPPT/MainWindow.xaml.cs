using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using XaminerConverter;
using XaminerPPT.Model;

namespace XaminerPrintPreview
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region InterOp
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private const int GWL_STYLE = -16;

        private const uint WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            // to hide the standard application icon on the 
            // top left of the window, but hides the 3 buttons as well...
            /*IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE,
                GetWindowLong(hwnd, GWL_STYLE) & (0xFFFFFFFF ^ WS_SYSMENU));

            base.OnSourceInitialized(e);*/
        }
        #endregion

        #region Fields
        // Contains 1 exam, the current filter text, a "filter result (list<Submission>)" and a current submission
        private Model _model;

        // for progressBar
        private BackgroundWorker _worker;
        #endregion

        #region Properties
        private Model Model { get => _model; set => _model = value; }
        public BackgroundWorker Worker { get => _worker; set => _worker = value; }
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            // all stuff should be based on _model.filteredSubmissions
            this.Model = new Model();
            this.DataContext = Model;
            this.mainWindow.Loaded += MainWindow_Loaded;
        }
        #endregion

        #region Methods
        private void LoadCurrentPdfPreview()
        {
            if (this.Model.FilteredSubmissions.Count > 0)
            {
                this.pdfPanel.Visibility = Visibility.Visible;
                if (this.Model.CurrentIndex >= this.Model.FilteredSubmissions.Count)
                {
                    this.Model.CurrentIndex = 0;
                }
                
                this.Model.CurrentSubmission = this.Model.FilteredSubmissions[this.Model.CurrentIndex];
                if (File.Exists(this.Model.CurrentSubmission.MappedFileName))
                {
                    this.pdfPanel.OpenFile(this.Model.CurrentSubmission.MappedFileName);
                }
                else
                {
                    this.pdfPanel.Visibility = Visibility.Collapsed;
                }
                this.submissionLabel.Content = "File " + (this.Model.CurrentIndex + 1).ToString() + " / " + this.Model.FilteredSubmissions.Count + ": " + this.Model.CurrentSubmission.Student.ToString();
            }
            else
            {
                this.pdfPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void NextSubmission()
        {
            if (this.Model.CurrentIndex + 1 < this.Model.FilteredSubmissions.Count)
            {
                this.Model.CurrentIndex++;
            }
            else
            {
                this.Model.CurrentIndex = 0;
            }

            this.LoadCurrentPdfPreview();
        }

        private void PreviousSubmission()
        {
            if (this.Model.CurrentIndex - 1 > -1)
            {
                this.Model.CurrentIndex--;
            }
            else
            {
                this.Model.CurrentIndex = this.Model.FilteredSubmissions.Count - 1;
            }

            this.LoadCurrentPdfPreview();
        }

        public bool PrintPDF(string filename, PrinterSettings settings)
        {
            try
            {
                // Create our page settings for the paper size selected
                var pageSettings = new PageSettings(settings)
                {
                    Margins = new Margins(0, 0, 0, 0),
                };
                foreach (PaperSize paperSize in settings.PaperSizes)
                {
                    if (paperSize.Kind == PaperKind.A4)
                    {
                        pageSettings.PaperSize = paperSize;
                        break;
                    }
                }

                // Now print the PDF document
                using (var document = PdfiumViewer.PdfDocument.Load(filename))
                {
                    using (var printDocument = document.CreatePrintDocument())
                    {
                        printDocument.PrinterSettings = settings;
                        printDocument.DefaultPageSettings = pageSettings;
                        printDocument.PrintController = new StandardPrintController();
                        printDocument.Print();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Events
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.exportAllButton.IsEnabled = false;
            this.printAllButton.IsEnabled = false;
            this.exportButton.IsEnabled = false;
            this.printButton.IsEnabled = false;
            this.nextButton.IsEnabled = false;
            this.prevButton.IsEnabled = false;

            Worker = new BackgroundWorker();
            Worker.WorkerReportsProgress = true;
            Worker.DoWork += Worker_DoWork;
            Worker.ProgressChanged += Worker_ProgressChanged;
            Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            this.pdfPanel.PdfLoaded += PdfPanel_PdfLoaded;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }
        private void PdfPanel_PdfLoaded(object sender, EventArgs e)
        {
            this.pdfPanel.ZoomToHeight();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    try
                    {
                        this.Model.SetExam(fbd.SelectedPath);

                        this.examLabel.Content = "Exam: " + this.Model.CurrentExam.Desc;

                        MessageBoxResult messageBoxResult = MessageBox.Show(this, "This will convert all TextFiles from the Exam to PDF Files, if not already present. Start converting?", "Confirm Convertation", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            this.progressBar.Visibility = Visibility.Visible;
                            this.progressBar.Minimum = 0;
                            this.progressBar.Value = 0;
                            this.progressBar.Maximum = this.Model.CurrentExam.ExamSubmissions.Count;
                            this.Worker.RunWorkerAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "There was an error during processing the .txt-files! Please check if the content of all the .txt-files match the specifications!\nError Message: " + ex.Message, "Parsing error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            this.PreviousSubmission();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.NextSubmission();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                if (!this.filterTextBox.IsFocused)
                {
                    this.PreviousSubmission();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Right)
            {
                if (!this.filterTextBox.IsFocused)
                {
                    this.NextSubmission();
                    e.Handled = true;
                }
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.progressBar.Visibility = Visibility.Collapsed;

            if (this.Model.FilteredSubmissions.Count > 0)
            {
                this.Model.CurrentIndex = 0;
                this.LoadCurrentPdfPreview();
            }

            bool buttonState = this.Model.CurrentExam != null;
            this.exportAllButton.IsEnabled = buttonState;
            this.printAllButton.IsEnabled = buttonState;
            this.exportButton.IsEnabled = buttonState;
            this.printButton.IsEnabled = buttonState;
            this.nextButton.IsEnabled = buttonState;
            this.prevButton.IsEnabled = buttonState;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.progressBar.Value = e.ProgressPercentage;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (this.Model.CurrentExam != null)
            {
                this.Model.CurrentExam.AllSubmissionsToPdf(this.Worker);

            }
            else
            {
                //MessageBox.Show(this, "No Submissions loaded!", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            this.Model.ApplyFilter();
            this.LoadCurrentPdfPreview();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // copy currently selected pdf to some selected location
            if (File.Exists(this.Model.CurrentSubmission.MappedFileName))
            {
                using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        MessageBoxResult messageBoxResult = MessageBox.Show(this, $"This will copy the current PDF file to the selected location: {fbd.SelectedPath}. Start exporting?", "Confirm Exporting", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (messageBoxResult == MessageBoxResult.Yes)
                        {
                            string fileName = Path.GetFileName(this.Model.CurrentSubmission.MappedFileName);
                            string destinationFile = fbd.SelectedPath + "\\" + fileName;
                            File.Copy(this.Model.CurrentSubmission.MappedFileName, destinationFile, true);
                        }
                    }
                }
            }
        }

        private void ExportAllButton_Click(object sender, RoutedEventArgs e)
        {
            // copy all (available, can be changed by filter) pdf to some selected location
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath)) 
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show(this, $"This will copy all (filtered) PDF files to the selected location: {fbd.SelectedPath}. Start exporting?", "Confirm Exporting", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        foreach (ExamSubmission es in this.Model.FilteredSubmissions)
                        {
                            if (File.Exists(es.MappedFileName))
                            {
                                string fileName = Path.GetFileName(es.MappedFileName);
                                string destinationFile = fbd.SelectedPath + "\\" + fileName;
                                File.Copy(es.MappedFileName, destinationFile, true);

                            }
                        }
                    }
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // confirmation dialog for printing current selected submissions
            if (File.Exists(this.Model.CurrentSubmission.MappedFileName))
            {
                System.Windows.Forms.PrintDialog dialog = new System.Windows.Forms.PrintDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show(this, $"This will print the current PDF file: {this.Model.CurrentSubmission.MappedFileName}. Start printing?", "Confirm Printing", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        PrinterSettings settings = dialog.PrinterSettings;
                        this.PrintPDF(this.Model.CurrentSubmission.MappedFileName, settings);
                    }
                }
            }
        }

        private void PrintAllButton_Click(object sender, RoutedEventArgs e)
        {
            // confirmation dialog for printing all loaded submissions
            System.Windows.Forms.PrintDialog dialog = new System.Windows.Forms.PrintDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PrinterSettings settings = dialog.PrinterSettings;
                MessageBoxResult messageBoxResult = MessageBox.Show(this, "This will print all (filtered) PDF files with the previously defined printer settings. Start printing?", "Confirm Printing", System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    foreach (ExamSubmission es in this.Model.FilteredSubmissions)
                    {
                        if (File.Exists(es.MappedFileName))
                        {
                            this.PrintPDF(this.Model.CurrentSubmission.MappedFileName, settings);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
