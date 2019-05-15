private int GetWeekendsInDate(DateTime startDate)
    {
        int result = 0;
        startDate = new DateTime(startDate.Year, startDate.Month, 1);
        int daysinmonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
        var endDate = new DateTime(startDate.Year, startDate.Month, daysinmonth);
        TimeSpan diff = endDate - startDate;
        int days = diff.Days;
        for (var i = 0; i<=days; i++)
        {
            var auxdate = startDate.AddDays(i);
            switch (auxdate.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    result++;
                    break;
            }
        }
        return result;
    }
