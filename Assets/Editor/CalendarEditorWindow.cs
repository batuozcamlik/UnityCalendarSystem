using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CalendarDataWrapper : ScriptableObject
{
    public CalendarData data;
}

public class CalendarEditorWindow : EditorWindow
{
    private CalendarDataWrapper dataWrapper;
    private CalendarData CalendarData => dataWrapper.data;

    private string jsonPath;
    private Vector2 scrollPosition;

    private ReorderableList reorderableDayParts;
    private ReorderableList reorderableMonths;

    private GUIStyle titleStyle;
    private GUIStyle linkStyle;
    private GUIStyle statsStyle;
    private GUIStyle toastStyle;

    private string lastSavedJson = "";
    private string focusRequest = "";

    private double lastSaveTime = -100;
    private const float TOAST_DURATION = 2.5f;
    private string currentToastMessage = "";
    private double toastStartTime = -100;

    [MenuItem("Tools/Calendar Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<CalendarEditorWindow>("Calendar Manager");
        window.minSize = new Vector2(650, 650);
    }

    private void OnEnable()
    {
        jsonPath = Application.dataPath + "/calendar_config.json";
        this.saveChangesMessage = "Yapılan değişiklikleri kaydetmediniz. Çıkmak istiyor musunuz?";

        if (dataWrapper == null)
        {
            dataWrapper = ScriptableObject.CreateInstance<CalendarDataWrapper>();
            dataWrapper.hideFlags = HideFlags.DontSave;
            LoadData(showNotification: false, forceLoad: true);
        }

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        InitializeLists();
        CheckIfDirty();
        Repaint();
    }

    private void ShowNotification(string message)
    {
        currentToastMessage = message;
        toastStartTime = EditorApplication.timeSinceStartup;
        Repaint();
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

        if (statsStyle == null)
        {
            statsStyle = new GUIStyle(EditorStyles.helpBox);
            statsStyle.fontSize = 11;
            statsStyle.alignment = TextAnchor.MiddleCenter;
            statsStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.9f, 0.7f) : new Color(0.2f, 0.5f, 0.2f);
        }

        if (toastStyle == null)
        {
            toastStyle = new GUIStyle(EditorStyles.label);
            toastStyle.fontSize = 16;
            toastStyle.fontStyle = FontStyle.Bold;
            toastStyle.alignment = TextAnchor.MiddleLeft;
            toastStyle.normal.textColor = new Color(0.1f, 1.0f, 0.1f);
            toastStyle.normal.background = null;
        }
    }

    private void InitializeLists()
    {
        if (CalendarData == null) return;

        reorderableDayParts = new ReorderableList(CalendarData.dayParts, typeof(string), true, true, false, false);

        reorderableDayParts.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Day Parts List", EditorStyles.boldLabel);
        };

        reorderableDayParts.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= CalendarData.dayParts.Count) return;
            rect.y += 2;

            float btnWidth = 75f;
            float spacing = 5f;
            float totalBtnArea = (btnWidth * 2) + spacing;
            float textWidth = rect.width - totalBtnArea - spacing;

            string controlName = "DayPart_" + index;
            GUI.SetNextControlName(controlName);

            Color originalColor = GUI.backgroundColor;
            if (string.IsNullOrWhiteSpace(CalendarData.dayParts[index])) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            EditorGUI.BeginChangeCheck();
            string newVal = EditorGUI.TextField(
                new Rect(rect.x, rect.y, textWidth, EditorGUIUtility.singleLineHeight),
                CalendarData.dayParts[index]);
            GUI.backgroundColor = originalColor;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Day Part Name");
                CalendarData.dayParts[index] = newVal;
                CheckIfDirty();
            }

            if (GUI.Button(new Rect(rect.x + textWidth + spacing, rect.y, btnWidth, EditorGUIUtility.singleLineHeight), "Duplicate"))
            {
                Undo.RecordObject(dataWrapper, "Duplicate Day Part");
                string originalName = CalendarData.dayParts[index];
                string copyName = originalName + " (Copy)";
                CalendarData.dayParts.Insert(index + 1, copyName);

                CheckIfDirty();

                string display = string.IsNullOrWhiteSpace(originalName) ? "Null" : $"'{originalName}'";
                ShowNotification($"Day Part {display} Duplicated");
            }

            if (GUI.Button(new Rect(rect.x + textWidth + spacing + btnWidth + spacing, rect.y, btnWidth, EditorGUIUtility.singleLineHeight), "Delete"))
            {
                string currentName = CalendarData.dayParts[index];
                bool canDeleteDirectly = currentName == "New Part" || string.IsNullOrWhiteSpace(currentName);

                if (canDeleteDirectly || EditorUtility.DisplayDialog("Silme Onayı",
                    $"'{currentName}' parçasını silmek istediğine emin misin?", "Evet", "İptal"))
                {
                    Undo.RecordObject(dataWrapper, "Remove Day Part");
                    CalendarData.dayParts.RemoveAt(index);
                    CheckIfDirty();

                    string display = string.IsNullOrWhiteSpace(currentName) ? "Null" : $"'{currentName}'";
                    ShowNotification($"Day Part {display} Deleted");

                    GUIUtility.ExitGUI();
                }
            }
        };

        reorderableDayParts.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
            Undo.RecordObject(dataWrapper, "Reorder Day Parts");
            CheckIfDirty();
        };


        reorderableMonths = new ReorderableList(CalendarData.months, typeof(MonthDefinition), true, true, false, false);

        reorderableMonths.drawHeaderCallback = (Rect rect) => {
            float btnWidth = 75f;
            float spacing = 5f;
            float daysWidth = 50f;
            float actionWidth = (btnWidth * 2) + spacing;
            float nameWidth = rect.width - daysWidth - actionWidth - (spacing * 2);

            EditorGUI.LabelField(new Rect(rect.x, rect.y, nameWidth, rect.height), "Month Name", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, rect.height), "Days", EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, actionWidth, rect.height), "Actions", EditorStyles.boldLabel);
        };

        reorderableMonths.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            if (index >= CalendarData.months.Count) return;

            var element = CalendarData.months[index];
            rect.y += 2;
            float height = EditorGUIUtility.singleLineHeight;

            float btnWidth = 75f;
            float spacing = 5f;
            float daysWidth = 50f;
            float actionWidth = (btnWidth * 2) + spacing;
            float nameWidth = rect.width - daysWidth - actionWidth - (spacing * 2);

            string controlName = "MonthName_" + index;
            GUI.SetNextControlName(controlName);

            Color originalColor = GUI.backgroundColor;
            if (string.IsNullOrWhiteSpace(element.monthName)) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(
                new Rect(rect.x, rect.y, nameWidth, height),
                element.monthName);
            GUI.backgroundColor = originalColor;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Month Name");
                element.monthName = newName;
                CheckIfDirty();
            }

            EditorGUI.BeginChangeCheck();
            int newDays = EditorGUI.IntField(
                new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, height),
                element.daysInMonth);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Month Days");
                element.daysInMonth = Mathf.Max(1, newDays);
                CheckIfDirty();
            }

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, btnWidth, height), "Duplicate"))
            {
                Undo.RecordObject(dataWrapper, "Duplicate Month");
                string originalName = element.monthName;
                var newMonth = new MonthDefinition(originalName + " (Copy)", element.daysInMonth);
                CalendarData.months.Insert(index + 1, newMonth);
                CheckIfDirty();
                string display = string.IsNullOrWhiteSpace(originalName) ? "Null" : $"'{originalName}'";
                ShowNotification($"Month {display} Duplicated");
            }

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2) + btnWidth + spacing, rect.y, btnWidth, height), "Delete"))
            {
                string currentName = element.monthName;
                bool canDeleteDirectly = currentName == "New Month" || string.IsNullOrWhiteSpace(currentName);

                if (canDeleteDirectly || EditorUtility.DisplayDialog("Silme Onayı",
                    $"'{currentName}' ayını silmek istediğine emin misin?", "Evet", "İptal"))
                {
                    Undo.RecordObject(dataWrapper, "Remove Month");
                    CalendarData.months.RemoveAt(index);
                    CheckIfDirty();
                    string display = string.IsNullOrWhiteSpace(currentName) ? "Null" : $"'{currentName}'";
                    ShowNotification($"Month {display} Deleted");
                    GUIUtility.ExitGUI();
                }
            }
        };

        reorderableMonths.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
            Undo.RecordObject(dataWrapper, "Reorder Months");
            CheckIfDirty();
        };
    }

    public override void SaveChanges()
    {
        if (SaveData(true))
        {
            base.SaveChanges();
        }
        else
        {
            GetWindow<CalendarEditorWindow>("Calendar Manager").Show();
        }
    }

    private void OnGUI()
    {
        InitStyles();
        if (reorderableDayParts == null || reorderableMonths == null) InitializeLists();

        if (!string.IsNullOrEmpty(focusRequest) && Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl(focusRequest);
            focusRequest = "";
        }

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
                Undo.RecordObject(dataWrapper, "Add Day Part");
                CalendarData.dayParts.Add("New Part");
                focusRequest = "DayPart_" + (CalendarData.dayParts.Count - 1);
                CheckIfDirty();
            }
        });

        GUILayout.Space(15);

        DrawSection("Months Configuration", () =>
        {
            if (reorderableMonths != null) reorderableMonths.DoLayoutList();

            if (GUILayout.Button("+ Add New Month", GUILayout.Height(25)))
            {
                Undo.RecordObject(dataWrapper, "Add Month");
                CalendarData.months.Add(new MonthDefinition("New Month", 30));
                focusRequest = "MonthName_" + (CalendarData.months.Count - 1);
                CheckIfDirty();
            }
        });

        GUILayout.Space(20);
        EditorGUILayout.EndScrollView();

        DrawStatistics();
        DrawFooter();
        DrawToast();

        if (EditorGUI.EndChangeCheck())
        {
            CheckIfDirty();
        }
    }

    private void DrawToast()
    {
        double timeSinceStart = EditorApplication.timeSinceStartup - toastStartTime;

        if (timeSinceStart < TOAST_DURATION)
        {
            float alpha = 1.0f;
            if (timeSinceStart > 1.0f)
            {
                alpha = 1.0f - (float)((timeSinceStart - 1.0f) / 1.5f);
            }

            Color oldColor = GUI.color;
            Color toastColor = toastStyle.normal.textColor;
            toastColor.a = alpha;
            toastStyle.normal.textColor = toastColor;

            float toastWidth = 400f;
            float toastHeight = 30f;

            Rect toastRect = new Rect(
                5f,
                position.height - 35f,
                toastWidth,
                toastHeight
            );

            GUI.Label(toastRect, currentToastMessage, toastStyle);

            toastStyle.normal.textColor = new Color(0.1f, 1.0f, 0.1f);
            GUI.color = oldColor;

            Repaint();
        }
    }

    private void DrawStatistics()
    {
        if (CalendarData == null) return;

        int totalMonths = CalendarData.months.Count;
        int totalDays = CalendarData.months.Sum(m => m.daysInMonth);
        int dayPartsCount = CalendarData.dayParts.Count;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        string statsText = $"📊 Total Months: {totalMonths}  |  📅 Year Length: {totalDays} Days  |  🔄 Day Cycles: {dayPartsCount}";
        GUILayout.Label(statsText, statsStyle, GUILayout.Height(24), GUILayout.Width(450));

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void CheckIfDirty()
    {
        string currentJson = JsonUtility.ToJson(CalendarData, true);
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

        EditorGUI.BeginDisabledGroup(!this.hasUnsavedChanges);
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (EditorUtility.DisplayDialog("Load Data",
                "Current settings will be reverted to the last saved configuration from the disk.\n\nAny unsaved changes will be lost. Are you sure?",
                "Yes, Load", "Cancel"))
            {
                LoadData(showNotification: true, forceLoad: true);
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(!this.hasUnsavedChanges);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            SaveData(forceSave: false);
        }
        EditorGUI.EndDisabledGroup();

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
        Undo.RecordObject(dataWrapper, "Reset Calendar");

        CalendarData.dayParts.Clear();
        CalendarData.dayParts.Add("Morning");
        CalendarData.dayParts.Add("Noon");
        CalendarData.dayParts.Add("Afternoon");
        CalendarData.dayParts.Add("Evening");

        CalendarData.months.Clear();
        CalendarData.months.Add(new MonthDefinition("January", 31));
        CalendarData.months.Add(new MonthDefinition("February", 28));
        CalendarData.months.Add(new MonthDefinition("March", 31));
        CalendarData.months.Add(new MonthDefinition("April", 30));
        CalendarData.months.Add(new MonthDefinition("May", 31));
        CalendarData.months.Add(new MonthDefinition("June", 30));
        CalendarData.months.Add(new MonthDefinition("July", 31));
        CalendarData.months.Add(new MonthDefinition("August", 31));
        CalendarData.months.Add(new MonthDefinition("September", 30));
        CalendarData.months.Add(new MonthDefinition("October", 31));
        CalendarData.months.Add(new MonthDefinition("November", 30));
        CalendarData.months.Add(new MonthDefinition("December", 31));

        InitializeLists();
        GUI.FocusControl(null);
        CheckIfDirty();

        ShowNotification("Calendar Reset to Defaults");
        Debug.Log("Calendar reset to Gregorian Calendar defaults.");
    }

    private bool ValidateData()
    {
        for (int i = 0; i < CalendarData.dayParts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(CalendarData.dayParts[i]))
            {
                EditorUtility.DisplayDialog("Kaydedilemedi / Save Failed",
                    $"Day Part at Index {i} is empty!\nPlease fill in the red fields.", "OK");
                return false;
            }
        }

        for (int i = 0; i < CalendarData.months.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(CalendarData.months[i].monthName))
            {
                EditorUtility.DisplayDialog("Kaydedilemedi / Save Failed",
                    $"Month Name at Index {i} is empty!\nPlease fill in the red fields.", "OK");
                return false;
            }
        }

        return true;
    }

    private bool SaveData(bool forceSave = false)
    {
        if (!forceSave && !this.hasUnsavedChanges) return true;

        if (!ValidateData()) return false;

        string json = JsonUtility.ToJson(CalendarData, true);
        File.WriteAllText(jsonPath, json);
        AssetDatabase.Refresh();

        lastSavedJson = json;
        this.hasUnsavedChanges = false;

        ShowNotification("Saved Successfully");
        Debug.Log($"Calendar Data Saved to: {jsonPath}");
        return true;
    }

    private void LoadData(bool showNotification = false, bool forceLoad = false)
    {
        if (File.Exists(jsonPath))
        {
            string jsonFromFile = File.ReadAllText(jsonPath);
            string currentJsonInMemory = JsonUtility.ToJson(dataWrapper.data, true);

            bool shouldLoad = forceLoad || (jsonFromFile != currentJsonInMemory);

            if (shouldLoad)
            {
                dataWrapper.data = JsonUtility.FromJson<CalendarData>(jsonFromFile);
                lastSavedJson = jsonFromFile;

                InitializeLists();
                this.hasUnsavedChanges = false;
                GUI.FocusControl(null);

                if (showNotification) ShowNotification("Data Loaded Successfully");
            }
            else
            {
                if (showNotification) Debug.Log("No changes found on disk. Data is up to date.");
            }
        }
        else
        {
            dataWrapper.data = new CalendarData();
            lastSavedJson = "";
            InitializeLists();
            this.hasUnsavedChanges = false;
        }
    }
}