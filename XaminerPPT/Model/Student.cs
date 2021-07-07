using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XaminerConverter
{
    class Student
    {
        #region Fields
        private string _firstName;
        private string _lastName;
        private string _matNr;
        private int _skz;
        #endregion

        #region Properties
        public string FirstName { get => _firstName; set => _firstName = value; }
        public string LastName { get => _lastName; set => _lastName = value; }
        public string MatNr { get => _matNr; set => _matNr = value; }
        public int Skz { get => _skz; set => _skz = value; }
        #endregion

        #region Constructor
        public Student(string firstName, string lastName, string matNr, int skz)
        {
            this.FirstName = firstName;
            this.LastName = lastName;
            this.MatNr = matNr;
            this.Skz = skz;
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return this.LastName + " " + this.FirstName + " [" + this.MatNr + "]";
        }
        #endregion
    }
}
