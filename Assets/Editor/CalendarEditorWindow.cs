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

public enum NotificationType
{
    Success,
    Warning
}

public class CalendarEditorWindow : EditorWindow
{
    private CalendarDataWrapper dataWrapper;
    private CalendarData CalendarData => dataWrapper != null ? dataWrapper.data : null;

    private string jsonPath;
    private Vector2 scrollPosition;

    private ReorderableList reorderableDayParts;
    private ReorderableList reorderableMonths;

    private GUIStyle titleStyle;
    private GUIStyle linkStyle;
    private GUIStyle statsStyle;
    private GUIStyle toastStyle;
    private GUIStyle setupButtonStyle;

    private string lastSavedJson = "";
    private string focusRequest = "";

    private string defaultResetJson;

    private double lastSaveTime = -100;
    private const float TOAST_DURATION = 2.5f;
    private string currentToastMessage = "";
    private double toastStartTime = -100;
    private Color currentToastColor = Color.white;

    private string saveChangesMessage;

    [MenuItem("Tools/Calendar Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<CalendarEditorWindow>("Calendar Manager");
        window.minSize = new Vector2(650, 750);
    }

    private void OnEnable()
    {
        string resourcesDir = Path.Combine(Application.dataPath, "Resources");
        if (!Directory.Exists(resourcesDir))
        {
            Directory.CreateDirectory(resourcesDir);
            AssetDatabase.Refresh();
        }

        jsonPath = Path.Combine(resourcesDir, "calendar_config.json");

        this.saveChangesMessage = "Yapılan değişiklikleri kaydetmediniz. Çıkmak istiyor musunuz?";

        if (dataWrapper == null)
        {
            dataWrapper = ScriptableObject.CreateInstance<CalendarDataWrapper>();
            dataWrapper.hideFlags = HideFlags.DontSave;

            CalculateDefaultJson();
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

    private void CalculateDefaultJson()
    {
        CalendarData defaultData = new CalendarData();

        defaultData.dayParts = new List<string> { "Morning", "Noon", "Afternoon", "Evening" };

        defaultData.months = new List<MonthDefinition>
        {
            new MonthDefinition("January", 31), new MonthDefinition("February", 28),
            new MonthDefinition("March", 31), new MonthDefinition("April", 30),
            new MonthDefinition("May", 31), new MonthDefinition("June", 30),
            new MonthDefinition("July", 31), new MonthDefinition("August", 31),
            new MonthDefinition("September", 30), new MonthDefinition("October", 31),
            new MonthDefinition("November", 30), new MonthDefinition("December", 31)
        };

        defaultData.currentYear = 2025;
        defaultData.currentMonthIndex = 0;
        defaultData.currentDay = 1;
        defaultData.currentDayPartIndex = 0;

        defaultResetJson = JsonUtility.ToJson(defaultData, true);
    }

    private void ShowNotification(string message, NotificationType type)
    {
        currentToastMessage = message;
        toastStartTime = EditorApplication.timeSinceStartup;

        if (type == NotificationType.Success)
            currentToastColor = new Color(0.2f, 1.0f, 0.2f);
        else
            currentToastColor = new Color(1.0f, 0.4f, 0.4f);

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
            toastStyle.normal.background = null;
        }

        if (setupButtonStyle == null)
        {
            setupButtonStyle = new GUIStyle(GUI.skin.button);
            setupButtonStyle.fontSize = 14;
            setupButtonStyle.padding = new RectOffset(10, 10, 10, 10);
            setupButtonStyle.fontStyle = FontStyle.Bold;
        }
    }

    private void InitializeLists()
    {
        if (CalendarData == null) return;

        reorderableDayParts = new ReorderableList(CalendarData.dayParts, typeof(string), true, true, false, false);
        reorderableDayParts.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, new GUIContent("Day Parts List", "Gün içindeki döngü parçalarının sırasını ve adını belirler."), EditorStyles.boldLabel); };
        reorderableDayParts.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= CalendarData.dayParts.Count) return;
            rect.y += 2;
            float btnWidth = 75f; float spacing = 5f; float totalBtnArea = (btnWidth * 2) + spacing; float textWidth = rect.width - totalBtnArea - spacing;

            string controlName = "DayPart_" + index;
            GUI.SetNextControlName(controlName);

            Color originalColor = GUI.backgroundColor;
            if (string.IsNullOrWhiteSpace(CalendarData.dayParts[index])) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            EditorGUI.BeginChangeCheck();
            string newVal = EditorGUI.TextField(new Rect(rect.x, rect.y, textWidth, EditorGUIUtility.singleLineHeight), new GUIContent("", "Parça adı."), CalendarData.dayParts[index]);
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
                CalendarData.dayParts.Insert(index + 1, CalendarData.dayParts[index] + " (Copy)");
                CheckIfDirty();
                ShowNotification("Day Part Duplicated", NotificationType.Success);
            }
            if (GUI.Button(new Rect(rect.x + textWidth + spacing + btnWidth + spacing, rect.y, btnWidth, EditorGUIUtility.singleLineHeight), "Delete"))
            {
                bool canDelete = CalendarData.dayParts[index] == "New Part" || string.IsNullOrWhiteSpace(CalendarData.dayParts[index]);
                if (canDelete || EditorUtility.DisplayDialog("Silme Onayı", $"'{CalendarData.dayParts[index]}' silinsin mi?", "Evet", "İptal"))
                {
                    Undo.RecordObject(dataWrapper, "Remove Day Part");
                    CalendarData.dayParts.RemoveAt(index);
                    CheckIfDirty();
                    ShowNotification("Day Part Deleted", NotificationType.Success);
                    GUIUtility.ExitGUI();
                }
            }
        };
        reorderableDayParts.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => { Undo.RecordObject(dataWrapper, "Reorder Day Parts"); CheckIfDirty(); };

        reorderableMonths = new ReorderableList(CalendarData.months, typeof(MonthDefinition), true, true, false, false);
        reorderableMonths.drawHeaderCallback = (Rect rect) =>
        {
            float btnWidth = 75f; float spacing = 5f; float daysWidth = 50f; float actionWidth = (btnWidth * 2) + spacing; float nameWidth = rect.width - daysWidth - actionWidth - (spacing * 2);
            EditorGUI.LabelField(new Rect(rect.x, rect.y, nameWidth, rect.height), new GUIContent("Month Name"), EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, rect.height), new GUIContent("Days"), EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, actionWidth, rect.height), new GUIContent("Actions"), EditorStyles.boldLabel);
        };
        reorderableMonths.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            if (index >= CalendarData.months.Count) return;
            var element = CalendarData.months[index];
            rect.y += 2; float height = EditorGUIUtility.singleLineHeight;
            float btnWidth = 75f; float spacing = 5f; float daysWidth = 50f; float actionWidth = (btnWidth * 2) + spacing; float nameWidth = rect.width - daysWidth - actionWidth - (spacing * 2);

            string controlName = "MonthName_" + index;
            GUI.SetNextControlName(controlName);

            Color originalColor = GUI.backgroundColor;
            if (string.IsNullOrWhiteSpace(element.monthName)) GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(new Rect(rect.x, rect.y, nameWidth, height), new GUIContent("", "Ay adı."), element.monthName);
            GUI.backgroundColor = originalColor;
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataWrapper, "Change Month Name"); element.monthName = newName; CheckIfDirty(); }

            EditorGUI.BeginChangeCheck();
            int newDays = EditorGUI.IntField(new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, height), new GUIContent("", "Gün sayısı."), element.daysInMonth);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(dataWrapper, "Change Month Days"); element.daysInMonth = Mathf.Max(1, newDays); CheckIfDirty(); }

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, btnWidth, height), "Duplicate"))
            {
                Undo.RecordObject(dataWrapper, "Duplicate Month");
                CalendarData.months.Insert(index + 1, new MonthDefinition(element.monthName + " (Copy)", element.daysInMonth));
                CheckIfDirty();
                ShowNotification("Month Duplicated", NotificationType.Success);
            }
            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2) + btnWidth + spacing, rect.y, btnWidth, height), "Delete"))
            {
                bool canDelete = element.monthName == "New Month" || string.IsNullOrWhiteSpace(element.monthName);
                if (canDelete || EditorUtility.DisplayDialog("Silme Onayı", $"'{element.monthName}' silinsin mi?", "Evet", "İptal"))
                {
                    Undo.RecordObject(dataWrapper, "Remove Month");
                    CalendarData.months.RemoveAt(index);
                    CheckIfDirty();
                    ShowNotification("Month Deleted", NotificationType.Success);
                    GUIUtility.ExitGUI();
                }
            }
        };
        reorderableMonths.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => { Undo.RecordObject(dataWrapper, "Reorder Months"); CheckIfDirty(); };
    }

    private void OnGUI()
    {
        InitStyles();

        bool fileExists = File.Exists(jsonPath);
        bool dataIsEmpty = (CalendarData == null || (CalendarData.months.Count == 0 && CalendarData.dayParts.Count == 0));

        if (!fileExists || dataIsEmpty)
        {
            DrawSetupScreen();
            DrawToast();
            return;
        }

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S && (e.control || e.command))
        {
            if (this.hasUnsavedChanges) SaveData(false);
            else ShowNotification("Already Saved", NotificationType.Warning);
            e.Use();
        }

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

        DrawSection(new GUIContent("Current Date State", "Oyunun başlayacağı veya şu anki kayıtlı tarihi ayarlayın."), () =>
        {
            EditorGUI.BeginChangeCheck();
            int newYear = EditorGUILayout.IntField(new GUIContent("Current Year", "Oyunun şu anki yılı."), CalendarData.currentYear);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Year");
                CalendarData.currentYear = Mathf.Max(1, newYear);
            }

            EditorGUI.BeginChangeCheck();
            string[] monthOptions = CalendarData.months.Select(m => m.monthName).ToArray();

            if (monthOptions.Length > 0)
            {
                int newMonthIndex = EditorGUILayout.Popup(new GUIContent("Current Month", "Şu anki ay."), CalendarData.currentMonthIndex, monthOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(dataWrapper, "Change Month");
                    CalendarData.currentMonthIndex = newMonthIndex;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Lütfen önce ay ekleyin.", MessageType.Warning);
            }

            EditorGUI.BeginChangeCheck();
            int maxDays = (CalendarData.months.Count > 0 && CalendarData.currentMonthIndex < CalendarData.months.Count)
                          ? CalendarData.months[CalendarData.currentMonthIndex].daysInMonth : 30;

            int newDay = EditorGUILayout.IntSlider(new GUIContent("Current Day", "Ayın kaçıncı günü?"), CalendarData.currentDay, 1, maxDays);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Day");
                CalendarData.currentDay = newDay;
            }

            EditorGUI.BeginChangeCheck();
            string[] dayPartOptions = CalendarData.dayParts.ToArray();
            if (dayPartOptions.Length > 0)
            {
                int newPartIndex = EditorGUILayout.Popup(new GUIContent("Current Day Part", "Günün vakti."), CalendarData.currentDayPartIndex, dayPartOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(dataWrapper, "Change Day Part");
                    CalendarData.currentDayPartIndex = newPartIndex;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Lütfen önce gün parçası ekleyin.", MessageType.Warning);
            }
        });

        GUILayout.Space(15);

        DrawSection(new GUIContent("Global Day Parts Configuration", "Gün döngüsü ayarları."), () =>
        {
            if (reorderableDayParts != null) reorderableDayParts.DoLayoutList();
            if (GUILayout.Button(new GUIContent("+ Add New Day Part"), GUILayout.Height(25)))
            {
                Undo.RecordObject(dataWrapper, "Add Day Part");
                CalendarData.dayParts.Add("New Part");
                focusRequest = "DayPart_" + (CalendarData.dayParts.Count - 1);
                CheckIfDirty();
            }
        });

        GUILayout.Space(15);

        DrawSection(new GUIContent("Months Configuration", "Ay ve gün sayısı ayarları."), () =>
        {
            if (reorderableMonths != null) reorderableMonths.DoLayoutList();
            if (GUILayout.Button(new GUIContent("+ Add New Month"), GUILayout.Height(25)))
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

    private void DrawSetupScreen()
    {
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(400));

        GUILayout.Space(25);

        var icon = EditorGUIUtility.IconContent("d_CreateAddNew");
        GUILayout.Label(icon, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter }, GUILayout.Height(40));

        GUILayout.Space(10);

        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            wordWrap = true
        };
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.9f, 0.9f) : Color.black;

        GUILayout.Label("Configuration Missing", headerStyle);

        GUILayout.Space(10);

        var descStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = 12
        };
        descStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : Color.gray;

        GUILayout.Label("No calendar configuration file was found in Resources.\nA new default system needs to be created to proceed.", descStyle);

        GUILayout.Space(25);

        GUILayout.BeginHorizontal();
        GUILayout.Space(40);

        if (GUILayout.Button("Create Default Calendar System", setupButtonStyle, GUILayout.Height(45)))
        {
            ResetToDefault();
            SaveData(forceSave: true);
            InitializeLists();
            Repaint();
        }

        GUILayout.Space(40);
        GUILayout.EndHorizontal();

        GUILayout.Space(25);

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
    }

    private void DrawToast()
    {
        double timeSinceStart = EditorApplication.timeSinceStartup - toastStartTime;
        if (timeSinceStart < TOAST_DURATION)
        {
            float alpha = 1.0f;
            if (timeSinceStart > 1.0f) alpha = 1.0f - (float)((timeSinceStart - 1.0f) / 1.5f);

            Color oldColor = GUI.color;
            Color toastColor = currentToastColor;
            toastColor.a = alpha;
            toastStyle.normal.textColor = toastColor;

            Rect toastRect = new Rect(5f, position.height - 35f, 400f, 30f);
            GUI.Label(toastRect, currentToastMessage, toastStyle);

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
        string statsText = $"Total Months: {totalMonths}  |  Year Length: {totalDays} Days  |  Day Cycles: {dayPartsCount}";
        GUILayout.Label(statsText, statsStyle, GUILayout.Height(24), GUILayout.Width(450));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
    }

    private void CheckIfDirty()
    {
        if (CalendarData == null) return;
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

        if (GUILayout.Button(new GUIContent("Load", "Diskten yükle."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (this.hasUnsavedChanges)
            {
                if (EditorUtility.DisplayDialog("Load Data", "Kaydedilmemiş değişiklikler var. Yüklerseniz kaybolacak.", "Yes, Load", "Cancel"))
                    LoadData(showNotification: true, forceLoad: true);
            }
            else
            {
                ShowNotification("No unsaved changes to revert", NotificationType.Warning);
            }
        }

        if (GUILayout.Button(new GUIContent("Save", "Diske kaydet (Ctrl+S)."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (this.hasUnsavedChanges) SaveData(forceSave: false);
            else ShowNotification("Already Saved", NotificationType.Warning);
        }

        if (GUILayout.Button(new GUIContent("Reset", "Sıfırla."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            string currentJson = JsonUtility.ToJson(CalendarData, true);
            if (string.IsNullOrEmpty(defaultResetJson)) CalculateDefaultJson();

            if (currentJson == defaultResetJson)
            {
                ShowNotification("Already set to default!", NotificationType.Warning);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Reset Data", "Tüm veriler varsayılana dönecek. Emin misiniz?", "Yes, Reset", "Cancel"))
                    ResetToDefault();
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"File Location: Assets/Resources/calendar_config.json", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();

        if (this.hasUnsavedChanges)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            GUILayout.Box(new GUIContent("Unsaved Changes"), EditorStyles.toolbarButton, GUILayout.Width(130));
            GUI.backgroundColor = oldColor;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawSection(GUIContent title, System.Action content)
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
        if (GUILayout.Button(new GUIContent("| www.batuozcamlik.com", "Web sayfasına git."), linkStyle))
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
        if (dataWrapper == null) return;
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

        CalendarData.currentYear = 2025;
        CalendarData.currentMonthIndex = 0;
        CalendarData.currentDay = 1;
        CalendarData.currentDayPartIndex = 0;

        InitializeLists();
        GUI.FocusControl(null);
        CheckIfDirty();

        ShowNotification("Calendar Reset to Defaults!", NotificationType.Success);
        Debug.Log("Calendar reset to Gregorian Calendar defaults.");
    }

    private bool ValidateData()
    {
        if (CalendarData == null) return false;
        for (int i = 0; i < CalendarData.dayParts.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(CalendarData.dayParts[i]))
            {
                EditorUtility.DisplayDialog("Hata", $"Day Part index {i} boş olamaz.", "OK");
                return false;
            }
        }
        for (int i = 0; i < CalendarData.months.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(CalendarData.months[i].monthName))
            {
                EditorUtility.DisplayDialog("Hata", $"Month Name index {i} boş olamaz.", "OK");
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

        ShowNotification("Saved Successfully!", NotificationType.Success);
        Debug.Log($"Calendar Data Saved to: {jsonPath}");
        return true;
    }

    private void LoadData(bool showNotification = false, bool forceLoad = false)
    {
        if (dataWrapper == null)
        {
            dataWrapper = ScriptableObject.CreateInstance<CalendarDataWrapper>();
            dataWrapper.hideFlags = HideFlags.DontSave;
        }

        if (File.Exists(jsonPath))
        {
            string jsonFromFile = File.ReadAllText(jsonPath);
            string currentJsonInMemory = dataWrapper.data != null ? JsonUtility.ToJson(dataWrapper.data, true) : "";

            bool shouldLoad = forceLoad || (jsonFromFile != currentJsonInMemory);

            if (shouldLoad)
            {
                dataWrapper.data = JsonUtility.FromJson<CalendarData>(jsonFromFile);
                lastSavedJson = jsonFromFile;
                InitializeLists();
                this.hasUnsavedChanges = false;
                GUI.FocusControl(null);
                if (showNotification) ShowNotification("Data Loaded Successfully!", NotificationType.Success);
            }
            else
            {
                if (showNotification) Debug.Log("No changes found.");
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