using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CalendarData
{
    #region Time Definitions
    public List<string> dayParts = new List<string>();
    public List<string> weekDays = new List<string>();
    public List<MonthDefinition> months = new List<MonthDefinition>();
    #endregion

    #region Current State
    public int currentYear = 1;
    public int currentMonthIndex = 0;
    public int currentDay = 1;
    public int currentDayPartIndex = 0;
    public int currentWeekDayIndex = 0;
    #endregion
}

#region Helper Classes
[System.Serializable]
public class MonthDefinition
{
    public string monthName;
    public int daysInMonth;

    public MonthDefinition(string name, int days)
    {
        monthName = name;
        daysInMonth = days;
    }
}
#endregion