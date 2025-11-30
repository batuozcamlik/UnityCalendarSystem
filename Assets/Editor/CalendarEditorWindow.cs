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

        defaultData.dayParts.Add("Morning");
        defaultData.dayParts.Add("Noon");
        defaultData.dayParts.Add("Afternoon");
        defaultData.dayParts.Add("Evening");

        defaultData.months.Add(new MonthDefinition("January", 31));
        defaultData.months.Add(new MonthDefinition("February", 28));
        defaultData.months.Add(new MonthDefinition("March", 31));
        defaultData.months.Add(new MonthDefinition("April", 30));
        defaultData.months.Add(new MonthDefinition("May", 31));
        defaultData.months.Add(new MonthDefinition("June", 30));
        defaultData.months.Add(new MonthDefinition("July", 31));
        defaultData.months.Add(new MonthDefinition("August", 31));
        defaultData.months.Add(new MonthDefinition("September", 30));
        defaultData.months.Add(new MonthDefinition("October", 31));
        defaultData.months.Add(new MonthDefinition("November", 30));
        defaultData.months.Add(new MonthDefinition("December", 31));

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
    }

    private void InitializeLists()
    {
        if (CalendarData == null) return;

        reorderableDayParts = new ReorderableList(CalendarData.dayParts, typeof(string), true, true, false, false);

        reorderableDayParts.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, new GUIContent("Day Parts List", "Gün içindeki döngü parçalarının sırasını ve adını belirler (Sabah, Öğle, Akşam)."), EditorStyles.boldLabel);
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
                new GUIContent("", "Parçanın adını girin. Boş bırakılamaz."),
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
                ShowNotification(" Day Part " + display + " Duplicated", NotificationType.Success);
            }

            if (GUI.Button(new Rect(rect.x + textWidth + spacing + btnWidth + spacing, rect.y, btnWidth, EditorGUIUtility.singleLineHeight), new GUIContent("Delete", "Bu gün parçasını siler. (Yeni/Boş olanlar onay istemez.)")))
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
                    ShowNotification(" Day Part " + display + " Deleted", NotificationType.Success);
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

            EditorGUI.LabelField(new Rect(rect.x, rect.y, nameWidth, rect.height), new GUIContent("Month Name", "Ayın adını girin."), EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + spacing, rect.y, daysWidth, rect.height), new GUIContent("Days", "Ayın gün sayısını girin."), EditorStyles.boldLabel);
            EditorGUI.LabelField(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, actionWidth, rect.height), new GUIContent("Actions", "İşlemler"), EditorStyles.boldLabel);
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
                new GUIContent("", "Ayın adını girin. Boş bırakılamaz."),
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
                new GUIContent("", "Ayın gün sayısı (Minimum 1 olmalıdır)."),
                element.daysInMonth);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataWrapper, "Change Month Days");
                element.daysInMonth = Mathf.Max(1, newDays);
                CheckIfDirty();
            }

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2), rect.y, btnWidth, height), new GUIContent("Duplicate", "Bu ayı kopyalar.")))
            {
                Undo.RecordObject(dataWrapper, "Duplicate Month");
                string originalName = element.monthName;
                var newMonth = new MonthDefinition(originalName + " (Copy)", element.daysInMonth);
                CalendarData.months.Insert(index + 1, newMonth);

                CheckIfDirty();

                string display = string.IsNullOrWhiteSpace(originalName) ? "Null" : $"'{originalName}'";
                ShowNotification(" Month " + display + " Duplicated", NotificationType.Success);
            }

            if (GUI.Button(new Rect(rect.x + nameWidth + daysWidth + (spacing * 2) + btnWidth + spacing, rect.y, btnWidth, height), new GUIContent("Delete", "Bu ayı siler. (Yeni/Boş ayarlar onay istemez.)")))
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
                    ShowNotification(" Month " + display + " Deleted", NotificationType.Success);
                }
            }
        };

        reorderableMonths.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
            Undo.RecordObject(dataWrapper, "Reorder Months");
            CheckIfDirty();
        };
    }

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.S && (e.control || e.command))
        {
            if (this.hasUnsavedChanges) SaveData(false);
            else ShowNotification("Already Saved", NotificationType.Warning);
            e.Use();
        }

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

        DrawSection(new GUIContent("Global Day Parts Configuration", "Gün parçalarının global ayarlarıdır. Tüm yıl boyunca bu döngü geçerlidir."), () =>
        {
            EditorGUILayout.HelpBox("Sıralamayı soldaki bardan sürükleyerek yapabilirsiniz.", MessageType.Info);
            GUILayout.Space(5);
            if (reorderableDayParts != null) reorderableDayParts.DoLayoutList();

            if (GUILayout.Button(new GUIContent("+ Add New Day Part", "Yeni bir gün parçası ekler."), GUILayout.Height(25)))
            {
                Undo.RecordObject(dataWrapper, "Add Day Part");
                CalendarData.dayParts.Add("New Part");
                focusRequest = "DayPart_" + (CalendarData.dayParts.Count - 1);
                CheckIfDirty();
            }
        });

        GUILayout.Space(15);

        DrawSection(new GUIContent("Months Configuration", "Takvimdeki ayların adlarını ve kaç gün çekeceğini belirlersiniz."), () =>
        {
            if (reorderableMonths != null) reorderableMonths.DoLayoutList();

            if (GUILayout.Button(new GUIContent("+ Add New Month", "Yeni bir ay ekler."), GUILayout.Height(25)))
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
            Color toastColor = currentToastColor;
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

        string statsText = "Total Months: " + totalMonths + "  |  Year Length: " + totalDays + " Days  |  Day Cycles: " + dayPartsCount;
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

        if (GUILayout.Button(new GUIContent("Load", "Son kaydedilen veriyi diskten geri yükler."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (this.hasUnsavedChanges)
            {
                if (EditorUtility.DisplayDialog("Load Data",
                    "Current settings will be reverted to the last saved configuration from the disk.\n\nAny unsaved changes will be lost. Are you sure?",
                    "Yes, Load", "Cancel"))
                {
                    LoadData(showNotification: true, forceLoad: true);
                }
            }
            else
            {
                ShowNotification("No unsaved changes to revert", NotificationType.Warning);
            }
        }

        if (GUILayout.Button(new GUIContent("Save", "Mevcut ayarları JSON dosyasına kaydeder (Ctrl+S)."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            if (this.hasUnsavedChanges)
            {
                SaveData(forceSave: false);
            }
            else
            {
                ShowNotification("Already Saved", NotificationType.Warning);
            }
        }

        if (GUILayout.Button(new GUIContent("Reset", "Takvimi Miladi Standartlara sıfırlar."), EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            string currentJson = JsonUtility.ToJson(CalendarData, true);

            if (currentJson == defaultResetJson)
            {
                ShowNotification("Already set to default!", NotificationType.Warning);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Reset Data",
                    "Tüm veriler standart Miladi Takvim (Gregorian Calendar) yapısına ve 4 parçalı gün sistemine sıfırlanacak.\n\nMevcut ayarlarınız kalıcı olarak silinecek. Emin misiniz?",
                    "Yes, Reset", "Cancel"))
                {
                    ResetToDefault();
                }
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"File Location: Assets/calendar_config.json", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (this.hasUnsavedChanges)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            GUILayout.Box(new GUIContent(" Unsaved Changes", "Kaydedilmemiş değişiklikler var!"), EditorStyles.toolbarButton, GUILayout.Width(130));
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

        InitializeLists();
        GUI.FocusControl(null);
        CheckIfDirty();

        ShowNotification(" Calendar Reset to Defaults!", NotificationType.Success);
        Debug.Log("Calendar reset to Gregorian Calendar defaults.");
    }

    private bool ValidateData()
    {
        if (CalendarData == null) return false;

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

        ShowNotification(" Saved Successfully!", NotificationType.Success);
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

                if (showNotification) ShowNotification(" Data Loaded Successfully!", NotificationType.Success);
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