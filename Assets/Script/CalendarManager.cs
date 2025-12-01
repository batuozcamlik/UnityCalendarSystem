using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CalendarManager : MonoBehaviour
{
    public CalendarData calendarData;

    private string saveFileName = "calendar_save.json";
    private string resourceFileName = "calendar_config";

    private void Awake()
    {
        LoadCalendarData();
        Debug.Log($"Sistem Baslatildi. Tarih: {GetFormattedDate()}");
    }

    void LoadCalendarData()
    {
#if UNITY_EDITOR
        string fullPath = Path.Combine(Application.dataPath, "Resources", resourceFileName + ".json");

        if (File.Exists(fullPath))
        {
            string json = File.ReadAllText(fullPath);
            calendarData = JsonUtility.FromJson<CalendarData>(json);
            Debug.Log("EDITOR MODU: Veriler Ana Config dosyasindan yüklendi.");
        }
        else
        {
            Debug.LogError("Config dosyasi bulunamadi! Lutfen Calendar Window ile olusturun.");
            calendarData = new CalendarData();
        }

#else
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            calendarData = JsonUtility.FromJson<CalendarData>(json);
            Debug.Log("OYUN MODU: Oyuncu kayit dosyasindan yüklendi.");
        }
        else
        {
            TextAsset configFile = Resources.Load<TextAsset>(resourceFileName);
            
            if (configFile != null)
            {
                calendarData = JsonUtility.FromJson<CalendarData>(configFile.text);
                Debug.Log("OYUN MODU: Kayit yok. Varsayilan Config yüklendi.");
            }
            else
            {
                calendarData = new CalendarData();
            }
        }
#endif
    }

    public void SaveCalendarData()
    {
        if (calendarData == null) return;

        string json = JsonUtility.ToJson(calendarData, true);

#if UNITY_EDITOR
        string resourcePath = Path.Combine(Application.dataPath, "Resources", resourceFileName + ".json");
        File.WriteAllText(resourcePath, json);

        AssetDatabase.Refresh();

#else
        string savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        File.WriteAllText(savePath, json);
#endif
    }

    public void SkipTimePart(int amount)
    {
        if (calendarData == null) return;

        calendarData.currentDayPartIndex += amount;
        int totalPartsInADay = calendarData.dayParts.Count;

        if (amount > 0)
        {
            while (calendarData.currentDayPartIndex >= totalPartsInADay)
            {
                calendarData.currentDayPartIndex -= totalPartsInADay;
                AdvanceDay();
            }
        }
        else if (amount < 0)
        {
            while (calendarData.currentDayPartIndex < 0)
            {
                calendarData.currentDayPartIndex += totalPartsInADay;
                RegressDay();
            }
        }

        SaveCalendarData();
    }

    private void AdvanceDay()
    {
        if (calendarData.months.Count == 0) return;

      
        calendarData.currentDay++;

      
        if (calendarData.weekDays.Count > 0)
        {
            calendarData.currentWeekDayIndex++;
         
            if (calendarData.currentWeekDayIndex >= calendarData.weekDays.Count)
            {
                calendarData.currentWeekDayIndex = 0;
            }
        }

   
        int daysInCurrentMonth = calendarData.months[calendarData.currentMonthIndex].daysInMonth;
        if (calendarData.currentDay > daysInCurrentMonth)
        {
            calendarData.currentDay = 1;
            AdvanceMonth();
        }
    }

    private void AdvanceMonth()
    {
        calendarData.currentMonthIndex++;
        if (calendarData.currentMonthIndex >= calendarData.months.Count)
        {
            calendarData.currentMonthIndex = 0;
            calendarData.currentYear++;
        }
    }

    private void RegressDay()
    {
        if (calendarData.months.Count == 0) return;

    
        calendarData.currentDay--;

       
        if (calendarData.weekDays.Count > 0)
        {
            calendarData.currentWeekDayIndex--;
      
            if (calendarData.currentWeekDayIndex < 0)
            {
                calendarData.currentWeekDayIndex = calendarData.weekDays.Count - 1;
            }
        }

       
        if (calendarData.currentDay < 1)
        {
            RegressMonth();
     
            int daysInPrevMonth = calendarData.months[calendarData.currentMonthIndex].daysInMonth;
            calendarData.currentDay = daysInPrevMonth;
        }
    }

    private void RegressMonth()
    {
        calendarData.currentMonthIndex--;
        if (calendarData.currentMonthIndex < 0)
        {
            calendarData.currentMonthIndex = calendarData.months.Count - 1;
            calendarData.currentYear--;
            if (calendarData.currentYear < 1) calendarData.currentYear = 1;
        }
    }

    public string GetFormattedDate()
    {
        if (calendarData == null || calendarData.dayParts.Count == 0 || calendarData.months.Count == 0) return "Veri Yok";

        int mIndex = Mathf.Clamp(calendarData.currentMonthIndex, 0, calendarData.months.Count - 1);
        int pIndex = Mathf.Clamp(calendarData.currentDayPartIndex, 0, calendarData.dayParts.Count - 1);

     
        string weekDayName = "";
        if (calendarData.weekDays.Count > 0)
        {
            int wIndex = Mathf.Clamp(calendarData.currentWeekDayIndex, 0, calendarData.weekDays.Count - 1);
            weekDayName = calendarData.weekDays[wIndex];
        }

       
        return $"{calendarData.currentDay} {calendarData.months[mIndex].monthName} {calendarData.currentYear} {weekDayName} - {calendarData.dayParts[pIndex]}";
    }
}