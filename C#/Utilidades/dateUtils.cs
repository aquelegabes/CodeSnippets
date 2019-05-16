// Get weekends days in date
public static int GetWeekendsDaysInDate(this DateTime value)
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

public static bool IsWeekend(this DateTime value)
{
    return (value.DayOfWeek == DayOfWeek.Saturday || value.DayOfWeek ==  DayOfWeek.Sunday)
}

public static DateTime GetLastDayOfMonth(this DateTime value)
{
    return new DateTime(value.Year, value.Month, 1).AddMonths(1).AddDays(-1);
}

public static public int GetAge(this DateTime value) {
     if (DateTime.Today.Month < dateOfBirth.Month ||
     DateTime.Today.Month == dateOfBirth.Month &&
      DateTime.Today.Day < dateOfBirth.Day) {
          return DateTime.Now.Year - dateOfBirth.Year - 1;
    } else
     return DateTime.Now.Year - dateOfBirth.Year;
}
