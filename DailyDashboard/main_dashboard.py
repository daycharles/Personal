# main_dashboard.py

import sys
import os
import json
from PyQt5.QtCore import Qt
from PyQt5.QtWidgets import (
    QApplication,
    QMainWindow,
    QAction,
    QWidget,
    QVBoxLayout,
    QTabWidget,
    QListWidget,
    QPushButton,
    QHBoxLayout,
    QLineEdit,
    QListWidgetItem,
    QMessageBox,
    QTextEdit,
    QLabel,
    QComboBox,
    QGridLayout,
    QCalendarWidget,
    QSystemTrayIcon,
    QMenu,
    QStyle,
)
from PyQt5.QtGui import QIcon, QFont

# Import the PomodoroTimer widget from pomodoro_widget.py
from pomodoro_widget import PomodoroTimer

# Paths for persistence files
BASE_DIR = os.path.dirname(__file__)
TASKS_FILE = os.path.join(BASE_DIR, "tasks.json")
NOTES_FILE = os.path.join(BASE_DIR, "notes.txt")
EISENHOWER_FILE = os.path.join(BASE_DIR, "eisenhower.json")


# === Utility functions for persistence ===

def load_tasks():
    """Load the task list from tasks.json. Return a list of dicts."""
    if not os.path.exists(TASKS_FILE):
        with open(TASKS_FILE, "w") as f:
            json.dump([], f, indent=4)
        return []
    try:
        with open(TASKS_FILE, "r") as f:
            data = json.load(f)
            valid = []
            for item in data:
                if isinstance(item, dict) and "text" in item and "done" in item:
                    valid.append(item)
            return valid
    except (json.JSONDecodeError, IOError):
        with open(TASKS_FILE, "w") as f:
            json.dump([], f, indent=4)
        return []


def save_tasks(task_list):
    """Save the in-memory list of tasks (list of dicts) back to tasks.json."""
    with open(TASKS_FILE, "w") as f:
        json.dump(task_list, f, indent=4)


def load_notes():
    """Load notes from notes.txt (plain text). Return a string."""
    if not os.path.exists(NOTES_FILE):
        return ""
    try:
        with open(NOTES_FILE, "r", encoding="utf-8") as f:
            return f.read()
    except IOError:
        return ""


def save_notes(text):
    """Save the given text into notes.txt."""
    try:
        with open(NOTES_FILE, "w", encoding="utf-8") as f:
            f.write(text)
    except IOError:
        QMessageBox.warning(None, "Error", "Could not save notes to file.")


def load_eisenhower():
    """Load Eisenhower tasks from eisenhower.json. Return list of dicts."""
    if not os.path.exists(EISENHOWER_FILE):
        with open(EISENHOWER_FILE, "w") as f:
            json.dump([], f, indent=4)
        return []
    try:
        with open(EISENHOWER_FILE, "r") as f:
            data = json.load(f)
            valid = []
            for item in data:
                if (
                    isinstance(item, dict)
                    and "text" in item
                    and "quadrant" in item
                    and item["quadrant"] in [1, 2, 3, 4]
                ):
                    valid.append(item)
            return valid
    except (json.JSONDecodeError, IOError):
        with open(EISENHOWER_FILE, "w") as f:
            json.dump([], f, indent=4)
        return []


def save_eisenhower(data_list):
    """Save the Eisenhower array of dicts back to eisenhower.json."""
    with open(EISENHOWER_FILE, "w") as f:
        json.dump(data_list, f, indent=4)


# === Task Manager Tab ===

class TaskManager(QWidget):
    """A simple task manager: add, check/uncheck, delete tasks with JSON persistence."""

    def __init__(self, parent=None):
        super().__init__(parent)
        self.tasks = load_tasks()  # List of dicts: [{"text": "...", "done": False}, ...]
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)

        # The list widget (each item is a QListWidgetItem with checkboxes)
        self.list_widget = QListWidget(self)
        self.list_widget.setSelectionMode(QListWidget.NoSelection)
        layout.addWidget(self.list_widget)

        # Horizontal row: [ QLineEdit ] [ Add Button ] [ Delete Button ]
        row = QHBoxLayout()

        self.new_task_input = QLineEdit(self)
        self.new_task_input.setPlaceholderText("Enter a new task here...")
        row.addWidget(self.new_task_input)

        self.add_button = QPushButton("Add", self)
        self.add_button.clicked.connect(self.add_task)
        row.addWidget(self.add_button)

        self.delete_button = QPushButton("Delete Selected", self)
        self.delete_button.clicked.connect(self.delete_selected_task)
        row.addWidget(self.delete_button)

        layout.addLayout(row)

        # Populate the list with existing tasks
        self.reload_task_list()

    def reload_task_list(self):
        """Clear the QListWidget and repopulate from self.tasks."""
        try:
            self.list_widget.itemChanged.disconnect(self.handle_item_changed)
        except TypeError:
            pass

        self.list_widget.clear()
        for idx, task in enumerate(self.tasks):
            item = QListWidgetItem(task["text"])
            item.setFlags(item.flags() | Qt.ItemIsUserCheckable | Qt.ItemIsSelectable)
            item.setCheckState(Qt.Checked if task["done"] else Qt.Unchecked)
            item.setData(Qt.UserRole, idx)
            self.list_widget.addItem(item)

        self.list_widget.itemChanged.connect(self.handle_item_changed)

    def handle_item_changed(self, item):
        """Called when user toggles the checkbox. Update self.tasks and save."""
        idx = item.data(Qt.UserRole)
        self.tasks[idx]["done"] = (item.checkState() == Qt.Checked)
        save_tasks(self.tasks)

    def add_task(self):
        """Take text from QLineEdit, add to self.tasks, and refresh."""
        text = self.new_task_input.text().strip()
        if not text:
            return  # ignore blank entries

        # Append to our in-memory list
        self.tasks.append({"text": text, "done": False})
        save_tasks(self.tasks)

        # Refresh the QListWidget
        self.reload_task_list()
        self.new_task_input.clear()

    def delete_selected_task(self):
        """Remove any checked tasks from the list."""
        to_delete = []
        for i in range(self.list_widget.count()):
            item = self.list_widget.item(i)
            if item.checkState() == Qt.Checked:
                to_delete.append(item.data(Qt.UserRole))

        if not to_delete:
            QMessageBox.information(self, "No Selection", "Please check the box of any task you want to delete.")
            return

        for idx in sorted(to_delete, reverse=True):
            del self.tasks[idx]

        save_tasks(self.tasks)
        self.reload_task_list()


# === Calendar Tab ===

class CalendarTab(QWidget):
    """A simple calendar view using QCalendarWidget."""

    def __init__(self, parent=None):
        super().__init__(parent)
        layout = QVBoxLayout(self)

        label = QLabel("Calendar", self)
        label.setStyleSheet("font-size: 18px; font-weight: bold;")
        layout.addWidget(label)

        self.calendar = QCalendarWidget(self)
        self.calendar.setGridVisible(True)
        layout.addWidget(self.calendar)

        layout.addStretch()


# === Notes Tab ===

class NotesTab(QWidget):
    """A basic text editor that loads/saves to notes.txt."""

    def __init__(self, parent=None):
        super().__init__(parent)
        layout = QVBoxLayout(self)

        label = QLabel("Notes", self)
        label.setStyleSheet("font-size: 18px; font-weight: bold;")
        layout.addWidget(label)

        self.text_edit = QTextEdit(self)
        self.text_edit.setPlaceholderText("Type your notes here…")
        layout.addWidget(self.text_edit)

        self.save_button = QPushButton("Save Notes", self)
        self.save_button.clicked.connect(self.save_notes_to_file)
        layout.addWidget(self.save_button)

        layout.addStretch()

        # Load existing notes on startup
        existing = load_notes()
        self.text_edit.setPlainText(existing)

    def save_notes_to_file(self):
        content = self.text_edit.toPlainText()
        save_notes(content)
        QMessageBox.information(self, "Notes Saved", "Your notes have been saved to notes.txt.")


# === Eisenhower Tab ===

class EisenhowerTab(QWidget):
    """
    A 2×2 Eisenhower Matrix:
      Q1 (Urgent+Important), Q2 (NotUrgent+Important),
      Q3 (Urgent+NotImportant), Q4 (NotUrgent+NotImportant).
    """

    def __init__(self, parent=None):
        super().__init__(parent)
        self.e_data = load_eisenhower()  # List of dicts: {"text": "...", "quadrant": 1..4}
        self.init_ui()

    def init_ui(self):
        layout = QVBoxLayout(self)

        title = QLabel("Eisenhower Matrix", self)
        title.setStyleSheet("font-size: 18px; font-weight: bold;")
        layout.addWidget(title)

        # Top: Add a new task with a quadrant selector
        add_row = QHBoxLayout()
        self.new_task_input = QLineEdit(self)
        self.new_task_input.setPlaceholderText("New task…")
        add_row.addWidget(self.new_task_input)

        self.quadrant_combo = QComboBox(self)
        self.quadrant_combo.addItem("Q1: Urgent + Important", 1)
        self.quadrant_combo.addItem("Q2: NotUrgent + Important", 2)
        self.quadrant_combo.addItem("Q3: Urgent + NotImportant", 3)
        self.quadrant_combo.addItem("Q4: NotUrgent + NotImportant", 4)
        add_row.addWidget(self.quadrant_combo)

        self.add_e_button = QPushButton("Add Task", self)
        self.add_e_button.clicked.connect(self.add_eisenhower_task)
        add_row.addWidget(self.add_e_button)

        layout.addLayout(add_row)

        # Middle: 2x2 grid of QListWidgets, one per quadrant
        grid = QGridLayout()
        self.lists = {}
        config = {
            1: (0, 0, "Q1 (Urgent+Important)"),
            2: (0, 1, "Q2 (NotUrgent+Important)"),
            3: (1, 0, "Q3 (Urgent+NotImportant)"),
            4: (1, 1, "Q4 (NotUrgent+NotImportant)"),
        }
        for quadrant, (r, c, label_text) in config.items():
            sub_layout = QVBoxLayout()
            lbl = QLabel(label_text, self)
            lbl.setStyleSheet("font-weight: bold;")
            sub_layout.addWidget(lbl)

            lw = QListWidget(self)
            lw.setSelectionMode(QListWidget.ExtendedSelection)
            sub_layout.addWidget(lw)
            self.lists[quadrant] = lw

            grid.addLayout(sub_layout, r, c)

        layout.addLayout(grid)

        # Bottom: Delete button to remove selected items from whichever list(s)
        del_row = QHBoxLayout()
        self.del_e_button = QPushButton("Delete Selected", self)
        self.del_e_button.clicked.connect(self.delete_eisenhower_task)
        del_row.addWidget(self.del_e_button)
        layout.addLayout(del_row)

        layout.addStretch()
        self.reload_eisenhower_lists()

    def reload_eisenhower_lists(self):
        """Clear all four QListWidgets and repopulate from self.e_data."""
        for q in [1, 2, 3, 4]:
            self.lists[q].clear()

        for idx, item in enumerate(self.e_data):
            q = item["quadrant"]
            text = item["text"]
            lw_item = QListWidgetItem(text)
            lw_item.setData(Qt.UserRole, idx)
            self.lists[q].addItem(lw_item)

    def add_eisenhower_task(self):
        """Add a new task into the selected quadrant."""
        text = self.new_task_input.text().strip()
        if not text:
            return

        quadrant = self.quadrant_combo.currentData()
        self.e_data.append({"text": text, "quadrant": quadrant})
        save_eisenhower(self.e_data)
        self.reload_eisenhower_lists()
        self.new_task_input.clear()

    def delete_eisenhower_task(self):
        """
        Delete all selected tasks from whichever quadrant(s) are selected.
        """
        to_delete = set()
        for q in [1, 2, 3, 4]:
            lw = self.lists[q]
            for i in range(lw.count()):
                item = lw.item(i)
                if item.isSelected():
                    to_delete.add(item.data(Qt.UserRole))

        if not to_delete:
            QMessageBox.information(self, "No Selection", "Select at least one task to delete.")
            return

        for idx in sorted(to_delete, reverse=True):
            del self.e_data[idx]

        save_eisenhower(self.e_data)
        self.reload_eisenhower_lists()


# === Overlay Timer (Transparent, Always-on-Top, Far Right) ===

class OverlayTimer(QWidget):
    """
    A small frameless widget that displays the Pomodoro countdown in large text,
    fully transparent background except the text (colored red or green),
    always-on-top, positioned far right just above the taskbar.
    """

    def __init__(self, pomodoro_widget, parent=None):
        super().__init__(parent)
        self.pomodoro = pomodoro_widget

        # Frameless & always-on-top
        self.setWindowFlags(
            Qt.FramelessWindowHint
            | Qt.WindowStaysOnTopHint
            | Qt.Tool
        )
        # Allow true transparency
        self.setAttribute(Qt.WA_TranslucentBackground)
        # Make overlay ignore mouse clicks (click-through)
        self.setAttribute(Qt.WA_TransparentForMouseEvents)

        # Create a QLabel that will show "MM:SS"
        self.label = QLabel(self)
        self.label.setAlignment(Qt.AlignCenter)

        # Make the font big and bold
        font = QFont()
        font.setPointSize(16)
        font.setBold(True)
        self.label.setFont(font)

        # Tweak a size (width x height)
        self.resize(100, 40)

        # Initial update and connect to PomodoroTimer's QTimer
        self.update_display()
        self.pomodoro.timer.timeout.connect(self.update_display)

        self.show()

    def position_widget(self):
        """
        Position this widget at the far right of the available screen area
        and at the very bottom of that area (i.e., just above the taskbar).
        """
        screen = QApplication.primaryScreen()
        available_geom = screen.availableGeometry()
        # x = right edge of available area minus our width minus margin
        margin = 10
        x = available_geom.x() + available_geom.width() - self.width() - margin
        # y = bottom of available area minus our height
        y = available_geom.y() + available_geom.height() - self.height()
        self.move(x, y)

    def update_display(self):
        """
        Called every second:  
        1) Format remaining_seconds as MM:SS  
        2) Pick red (Work) or green (Break)  
        3) Update the label text + stylesheet  
        4) Reposition far right above the taskbar  
        """
        total = self.pomodoro.remaining_seconds
        mins = total // 60
        secs = total % 60
        time_str = f"{mins:02d}:{secs:02d}"

        mode = self.pomodoro.current_mode
        if mode == "Work":
            color = "red"
        else:
            color = "green"

        # Transparent background; only text is colored
        self.label.setText(time_str)
        self.label.setStyleSheet(
            f"""
            background-color: transparent;
            color: {color};
            """
        )
        self.label.resize(self.size())

        self.position_widget()


# === Dashboard Main Window ===

class DashboardWindow(QMainWindow):
    """Main window with five tabs + tray icon + overlay timer."""

    def __init__(self):
        super().__init__()

        #  ── ADD QT.TOOL FLAG TO REMOVE TASKBAR ICON ──
        # By OR’ing Qt.Tool into the window flags, this window will no longer
        # show up in the Windows taskbar—only the tray icon remains.
        self.setWindowFlags(self.windowFlags() | Qt.Tool)

        self.setWindowTitle("🗂️ Daily Productivity Dashboard")
        self.resize(800, 600)

        # 1) Build the QTabWidget and each tab
        tabs = QTabWidget(self)
        self.setCentralWidget(tabs)

        # — Pomodoro Tab
        self.pomodoro_tab = PomodoroTimer(self)
        tabs.addTab(self.pomodoro_tab, "Pomodoro")

        # — Tasks Tab
        self.task_tab = TaskManager(self)
        tabs.addTab(self.task_tab, "Tasks")

        # — Calendar Tab
        self.calendar_tab = CalendarTab(self)
        tabs.addTab(self.calendar_tab, "Calendar")

        # — Notes Tab
        self.notes_tab = NotesTab(self)
        tabs.addTab(self.notes_tab, "Notes")

        # — Eisenhower Tab
        self.eisenhower_tab = EisenhowerTab(self)
        tabs.addTab(self.eisenhower_tab, "Eisenhower")

        # 2) Create the OverlayTimer and pass it the PomodoroTimer instance
        self.overlay = OverlayTimer(self.pomodoro_tab)

        # 3) Create the system tray icon + menu
        self._is_hidden_to_tray = False
        self.init_tray_icon()

    def init_tray_icon(self):
        """Set up QSystemTrayIcon with a context menu (Show Dashboard / Quit)."""
        icon_path = os.path.join(BASE_DIR, "icon.png")
        if os.path.exists(icon_path):
            tray_ic = QIcon(icon_path)
        else:
            tray_ic = self.style().standardIcon(QStyle.SP_ComputerIcon)

        self.tray = QSystemTrayIcon(self)
        self.tray.setIcon(tray_ic)

        menu = QMenu()
        show_action = QAction("Show Dashboard", self)
        show_action.triggered.connect(self.show_dashboard)
        menu.addAction(show_action)

        menu.addSeparator()

        quit_action = QAction("Quit", self)
        quit_action.triggered.connect(QApplication.instance().quit)
        menu.addAction(quit_action)

        self.tray.setContextMenu(menu)
        self.tray.show()

    def closeEvent(self, event):
        """
        Override closeEvent so that clicking [X] hides the window and overlay to tray.
        """
        event.ignore()
        self.hide()
        self.overlay.show()
        self._is_hidden_to_tray = True
        self.tray.showMessage(
            "Dashboard Minimized",
            "The app is still running in the tray. Double-click the tray icon or use 'Show Dashboard' to restore.",
            QSystemTrayIcon.Information,
            2000,
        )

    def show_dashboard(self):
        """Restore the main window (if it was hidden) and re-show the overlay."""
        if self._is_hidden_to_tray:
            self.show()
            self.raise_()
            self.activateWindow()
            self.overlay.true()
            self._is_hidden_to_tray = False


# === Application entry point ===

def main():
    app = QApplication(sys.argv)

    # Optional: global stylesheet
    app.setStyleSheet("""
        QMainWindow { background: #f9f9f9; }
        QLabel { font-family: Arial; }
        QListWidget { font-size: 14px; }
        QPushButton { padding: 6px; }
        QTextEdit { font-size: 13px; }
    """)

    window = DashboardWindow()
    window.show()
    sys.exit(app.exec_())


if __name__ == "__main__":
    main()
