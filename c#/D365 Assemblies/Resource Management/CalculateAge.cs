using System;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;

namespace Resource_Management
{
    public class CalculateAge : CodeActivity
    {
        [Input("dateOfBirth")]
        public InArgument<DateTime> DateOfBirthInput { get; set; }

        [Output("age")]
        public OutArgument<int> Age { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            DateTime dateOfBirth = DateOfBirthInput.Get(context);
            int age = CalculateAgeInYears(dateOfBirth);
            Age.Set(context, age);
        }

        private int CalculateAgeInYears(DateTime birthDate)
        {
            DateTime today = DateTime.Today;
            if (birthDate > today)
            {
                return 0;
            }
            double totalDays = (today - birthDate).TotalDays;
            double daysInYear = 365.2425;
            double ageInYears = totalDays / daysInYear;
            return (int)Math.Round(ageInYears);
        }
    }
}
