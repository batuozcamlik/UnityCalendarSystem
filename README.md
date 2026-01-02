# 📅 Unity Advanced Calendar & Time System

A fully customizable, JSON-based, and Editor-supported Calendar and Day Cycle system for Unity.

This system allows you to create your own months, week days, and day cycles (e.g., Morning, Noon, Evening) without writing a single line of code, thanks to its powerful **Custom Editor Window**. It handles time progression, save/load operations, and complex calendar logic (month rollovers, year advancement) automatically.

## ✨ Features

* **🛠️ Custom Editor Window:** Manage your calendar visually via `Tools > Calendar Manager`.
* **🔄 Flexible Time Cycles:** Define custom "Day Parts" (e.g., *Dawn -> Morning -> Afternoon -> Dusk -> Night*). The system calculates day passing based on these cycles.
* **📅 Fully Customizable Structure:**
    * Create custom **Months** with specific day counts.
    * Define custom **Week Days**.
    * Supports both standard (Gregorian) and fantasy calendar systems.
* **💾 Smart JSON Save System:**
    * **Editor:** Configuration is saved to `Resources/calendar_config.json`.
    * **Runtime:** Player progress is automatically saved to `Application.persistentDataPath`.
* **↩️ Undo/Redo Support:** Full support for Unity's Undo system in the Editor Window.
* **⏩ Time Manipulation:** Built-in logic to skip time **forward** or **backward**.
* **🐞 Debug Tools:** Includes a ready-to-use Debug UI script to test time skipping and visualize dates.

---

## 🚀 Installation & Getting Started

### 1. Installation
Simply drag and drop the scripts into your Unity project folder. After compilation, a new **Tools** menu will appear in the Unity toolbar.

### 2. Configuration (Editor)
Before using the system, you need to create the initial configuration:

1.  Navigate to **Tools > Calendar Manager** in the top menu.
2.  Click **"Create Default Calendar System"** to initialize.
3.  Customize your settings:
    * **Global Day Parts:** Add, remove, or reorder parts of the day.
    * **Week Days:** Rename or organize days of the week.
    * **Months:** Add months and define their day counts.
    * **Current Date State:** Set the starting date for your game.
4.  Click **Save** in the toolbar.

### 3. Scene Setup
1.  Create an empty GameObject in your scene (e.g., `CalendarSystem`).
2.  Add the `CalendarManager` component to it.
3.  *(Optional)* For testing, add the `CalendarDebugTool` component and assign a TextMeshPro object to visualize the date.

---

## 💻 Coding API

### Accessing the Date
You can get the current formatted date string via `CalendarManager`.

```csharp
public class MyGameScript : MonoBehaviour
{
    public CalendarManager calendarManager;

    void Start()
    {
        // Example Output: "15 January 2025 Wednesday - Morning"
        Debug.Log(calendarManager.GetFormattedDate());
    }
}
```

### Manipulating Time
Use the `SkipTimePart` method to advance or rewind time. The system automatically handles Day, Month, and Year rollovers.

```csharp
// Advance time by 1 unit (e.g., Morning -> Noon)
calendarManager.SkipTimePart(1);

// Advance time by 4 units (Skips a full day if you have 4 day parts)
calendarManager.SkipTimePart(4);

// Go BACK in time (Rewinds logic is included!)
// Example: Noon -> Morning
calendarManager.SkipTimePart(-1);
```

---

## 📂 Save System Logic

The system operates with two JSON files:

1.  **Config File (`Resources/calendar_config.json`):** Created by the Editor Window. Holds the default game settings.
2.  **Save File (`PersistentDataPath/calendar_save.json`):** Created at runtime. Holds the player's current progress.

**Runtime Logic:**
When the game starts, the system first looks for a valid **Save File**. If none is found, it loads the default values from the **Config File**.

---

## ❤️ Credits
Created by **Batu Özçamlık**.
Visit [www.batuozcamlik.com](https://www.batuozcamlik.com) for more tools and tutorials.

---

## 📄 License
This project is open-source and free to use in your commercial or personal projects.
