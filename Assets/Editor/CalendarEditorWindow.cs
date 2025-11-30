using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;

public class CalendarEditorWindow : EditorWindow
{
    private CalendarData calendarData;
    private string jsonPath;
    private Vector2 scrollPosition;

    private ReorderableList reorderableDayParts;
    private ReorderableList reorderableMonths;

    private GUIStyle titleStyle;
    private GUIStyle linkStyle;

    private string lastSavedJson = "";

    [MenuItem("Tools/Calendar Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<CalendarEditorWindow>("Calendar Manager");
        window.minSize = new Vector2(550, 600);
    }

    private void OnEnable()
    {
        jsonPath = Application.dataPath + "/calendar_config.json";
        this.saveChangesMessage = "Yapılan değişiklikleri kaydetmediniz. Çıkmak istiyor musunuz?";

        if (calendarData == null)
        {
            LoadData();
        }
    }

    private void InitStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 13;
            titleStyle.margin = new RectOffset(0, 0, 5, 5);
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : Color.black;
        }

        if (linkStyle == null)
        {
            linkStyle = new GUIStyle(EditorStyles.label);
            linkStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            linkStyle.hover.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            linkStyle.active.textColor = Color.white;
        }
    }

    private void InitializeLists()
    {
        if (calendarData == null) return;

        reorderableDayParts = new ReorderableList(calendarData.dayParts, typeof(string), true, true, false, false);

        reorderableDayParts.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Day Parts List", EditorStyles.boldLabel);
        };

        reorderableDayParts.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= calendarData.dayParts.Count) return;
            rect.y += 2;

            float buttonWidth = 50f;
            float spacing = 5f;
            float textWidth = rect.width - buttonWidth - spacing;

            string oldVal = calendarData.dayParts[index];
            string newVal = EditorGUI.TextField(
                new Rect(rect.x, rect.y, textWidth, EditorGUIUtility.singleLineHeight),
                oldVal);

            if (oldVal != newVal)
            {
                calendarData.dayParts[index] = newVal;
            }

            if (GUI.Button(new Rect(rect.x + textWidth + spacing, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight), "Del"))
            {
                bool isDefaultName = calendarData.dayParts[index] == "New Part";

                if (isDefaultName || EditorUtility.DisplayDialog("Silme Onayı",
                    $"'{calendarData.dayParts[index]}' parçasını silmek istediğine emin misin?", "Evet", "İptal"))
                {
                    calendarData.dayParts.RemoveAt(index);
                    CheckIfDirty();
                    GUIUtility.ExitGUI();
                }
            }
        };

        reorderableDayParts.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
            CheckIfDirty();
        };

        reorderableMonths = new ReorderableList(calendarData.months, typeof(MonthDefinition), true, true, false, false);

        reorderableMonths.drawHeaderCallback = (Rect rect) => {
            float buttonWidth = 50f;
            float spacing = 5f;
            float daysWidth = 60f;
            float nameWidth = rect.width - daysWidth - buttonWidth - (spacing * 2);

            EditorGUI.LabelField(new Rect(rect.x, rect.y, nameWidth, rect.height), "Month Name", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, rect.height), "Days", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, buttonWidth, rect.height), "Action", EditorStyles.boldLabel);
        };

        reorderableMonths.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= calendarData.months.Count) return;

            var element = calendarData.months[index];
            rect.y += 2;
            float height = EditorGUIUtility.singleLineHeight;
            float buttonWidth = 50f;
            float spacing = 5f;
            float daysWidth = 60f;
            float nameWidth = rect.width - daysWidth - buttonWidth - (spacing * 2);

            element.monthName = EditorGUI.TextField(
                new Rect(rect.x, rect.y, nameWidth, height),
                element.monthName);

            element.daysInMonth = EditorGUI.IntField(
                new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, height),
                element.daysInMonth);

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, buttonWidth, height), "Del"))
            {
                bool isDefaultName = element.monthName == "New Month";

                if (isDefaultName || EditorUtility.DisplayDialog("Silme Onayı",
                    $"'{element.monthName}' ayını silmek istediğine emin misin?", "Evet", "İptal"))
                {
                    calendarData.months.RemoveAt(index);
                    CheckIfDirty();
                    GUIUtility.ExitGUI();
                }
            }
        };

        reorderableMonths.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
            CheckIfDirty();
        };
    }

    public override void SaveChanges()
    {
        SaveData();
        base.SaveChanges();
    }

    private void OnGUI()
    {
        InitStyles();
        if (reorderableDayParts == null || reorderableMonths == null) InitializeLists();

        EditorGUI.BeginChangeCheck();

        DrawToolbar();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.Space(10);

        DrawSection("Global Day Parts Configuration", () =>
        {
            EditorGUILayout.HelpBox("Sıralamayı soldaki bardan sürükleyerek yapabilirsiniz.", MessageType.Info);
            GUILayout.Space(5);

            if (reorderableDayParts != null) reorderableDayParts.DoLayoutList();

            if (GUILayout.Button("+ Add New Day Part", GUILayout.Height(25)))
            {
                calendarData.dayParts.Add("New Part");
                CheckIfDirty();
            }
        });

        GUILayout.Space(15);

        DrawSection("Months Configuration", () =>
        {
            if (reorderableMonths != null) reorderableMonths.DoLayoutList();

            if (GUILayout.Button("+ Add New Month", GUILayout.Height(25)))
            {
                calendarData.months.Add(new MonthDefinition("New Month", 30));
                CheckIfDirty();
            }
        });

        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();

        DrawFooter();

        if (EditorGUI.EndChangeCheck())
        {
            CheckIfDirty();
        }
    }

    private void CheckIfDirty()
    {
        string currentJson = JsonUtility.ToJson(calendarData, true);
        if (currentJson != lastSavedJson)
        {
            this.hasUnsavedChanges = true;
        }
        else
        {
            this.hasUnsavedChanges = false;
        }
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50))) LoadData();
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50))) SaveData();

        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Reset Data",
                "Tüm veriler standart Miladi Takvim (Gregorian Calendar) yapısına ve 4 parçalı gün sistemine sıfırlanacak.\n\nMevcut ayarlarınız kalıcı olarak silinecek. Emin misiniz?",
                "Yes, Reset", "Cancel"))
            {
                ResetToDefault();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"File Location: Assets/calendar_config.json", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (this.hasUnsavedChanges)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            GUILayout.Box("⚠️ Unsaved Changes", EditorStyles.toolbarButton, GUILayout.Width(130));
            GUI.backgroundColor = oldColor;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSection(string title, System.Action content)
    {
        GUILayout.BeginVertical("helpBox");
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        GUILayout.Label(EditorGUIUtility.IconContent("d_SettingsIcon"), GUILayout.Width(20), GUILayout.Height(20));
        GUILayout.Label(title, titleStyle);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
        content.Invoke();
        GUILayout.Space(5);
        GUILayout.EndVertical();
    }

    private void DrawFooter()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label("Created by Batu Özçamlık", EditorStyles.boldLabel);

        if (GUILayout.Button("| www.batuozcamlik.com", linkStyle))
        {
            Application.OpenURL("https://www.batuozcamlik.com");
        }

        Rect linkRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);

        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.Space(5);
    }

    private void ResetToDefault()
    {
        calendarData = new CalendarData();

        calendarData.dayParts.Add("Morning");
        calendarData.dayParts.Add("Noon");
        calendarData.dayParts.Add("Afternoon");
        calendarData.dayParts.Add("Evening");

        calendarData.months.Add(new MonthDefinition("January", 31));
        calendarData.months.Add(new MonthDefinition("February", 28));
        calendarData.months.Add(new MonthDefinition("March", 31));
        calendarData.months.Add(new MonthDefinition("April", 30));
        calendarData.months.Add(new MonthDefinition("May", 31));
        calendarData.months.Add(new MonthDefinition("June", 30));
        calendarData.months.Add(new MonthDefinition("July", 31));
        calendarData.months.Add(new MonthDefinition("August", 31));
        calendarData.months.Add(new MonthDefinition("September", 30));
        calendarData.months.Add(new MonthDefinition("October", 31));
        calendarData.months.Add(new MonthDefinition("November", 30));
        calendarData.months.Add(new MonthDefinition("December", 31));

        InitializeLists();
        GUI.FocusControl(null);
        CheckIfDirty();
        Debug.Log("Calendar reset to Gregorian Calendar defaults.");
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(calendarData, true);
        File.WriteAllText(jsonPath, json);
        AssetDatabase.Refresh();

        lastSavedJson = json;
        this.hasUnsavedChanges = false;

        Debug.Log($"Calendar Data Saved to: {jsonPath}");
    }

    private void LoadData()
    {
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            calendarData = JsonUtility.FromJson<CalendarData>(json);
            lastSavedJson = json;
        }
        else
        {
            calendarData = new CalendarData();
            lastSavedJson = "";
        }

        InitializeLists();
        this.hasUnsavedChanges = false;
        GUI.FocusControl(null);
    }
}