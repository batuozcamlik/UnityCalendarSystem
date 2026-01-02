using UnityEngine;
using System.Text;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CalendarDebugTool : MonoBehaviour
{
    #region References
    [Header("References")]
    public CalendarManager calendarManager;

    [Header("UI Test (Optional)")]
    public TextMeshProUGUI dateTestText;
    #endregion

    #region Settings
    [Header("Test Settings")]
    [Min(0)]
    public int skipAmount = 1;

    public bool showDebugLogs = true;
    #endregion

    private void Start()
    {
        UpdateTestUI();
    }

    #region Control Logic
    public void RunSkipForward()
    {
        if (CheckPlayMode()) RunTest(skipAmount, true);
    }

    public void RunSkipBackward()
    {
        if (CheckPlayMode()) RunTest(-skipAmount, false);
    }

    private bool CheckPlayMode()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Must be in Play Mode to run tests!");
            return false;
        }
        return true;
    }

    private void RunTest(int amount, bool isForward)
    {
        if (calendarManager == null || calendarManager.calendarData == null)
        {
            Debug.LogError("Calendar Manager or Data is missing!");
            return;
        }

        string beforeDate = "";
        if (showDebugLogs) beforeDate = calendarManager.GetFormattedDate();

        calendarManager.SkipTimePart(amount);

        UpdateTestUI();

        if (showDebugLogs)
        {
            string afterDate = calendarManager.GetFormattedDate();
            PrintLog(Mathf.Abs(amount), isForward, beforeDate, afterDate);
        }
    }
    #endregion

    #region Visualization & Logging
    private void UpdateTestUI()
    {
        if (dateTestText == null || calendarManager == null || calendarManager.calendarData == null) return;

        var data = calendarManager.calendarData;

        if (data.dayParts.Count == 0) return;

        int displayMonth = data.currentMonthIndex + 1;

        string weekDayStr = "";
        if (data.weekDays != null && data.weekDays.Count > 0)
        {
            int wIndex = Mathf.Clamp(data.currentWeekDayIndex, 0, data.weekDays.Count - 1);
            weekDayStr = " " + data.weekDays[wIndex];
        }

        dateTestText.text = $"{data.currentDay:00}/{displayMonth:00}/{data.currentYear}{weekDayStr}\n{data.dayParts[data.currentDayPartIndex]}";
    }

    private void PrintLog(int absAmount, bool isForward, string before, string after)
    {
        StringBuilder sb = new StringBuilder();

        string mainColor = isForward ? "#44FF44" : "#FF6666";
        string arrowColor = "#FFFF00";
        string dateColor = "#88CCFF";
        string directionText = isForward ? "Forward" : "Backward";

        sb.Append($"<color={mainColor}><b>[{absAmount} {directionText}]</b></color> ");
        sb.Append(before);

        sb.Append($" <color={arrowColor}><b>➜</b></color> ");
        sb.Append($"<color={dateColor}>{after}</color>");

        Debug.Log(sb.ToString());
    }
    #endregion

    private void OnValidate()
    {
        if (skipAmount < 0) skipAmount = 0;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CalendarDebugTool))]
public class CalendarDebugToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        CalendarDebugTool script = (CalendarDebugTool)target;
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use test buttons.", MessageType.Info);
            GUI.enabled = false;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($" FORWARD ({script.skipAmount})", GUILayout.Height(40))) script.RunSkipForward();
        if (GUILayout.Button($" BACKWARD ({script.skipAmount})", GUILayout.Height(40))) script.RunSkipBackward();
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }
}
#endif