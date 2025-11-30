using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CalendarData // Burasý dosya adýyla BÝREBÝR ayný olmalý
{
    public List<string> dayParts = new List<string>();
    public List<MonthDefinition> months = new List<MonthDefinition>();
}

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