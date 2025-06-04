# pomodoro_widget.py

import sys
import json
import os
from PyQt5.QtCore import Qt, QTimer
from PyQt5.QtWidgets import (
    QWidget,
    QVBoxLayout,
    QLabel,
    QPushButton,
    QHBoxLayout,
    QMessageBox,
    QDialog,
    QFormLayout,
    QSpinBox,
    QDialogButtonBox,
)

# We expect settings.json to live in the same folder as this script
SETTINGS_FILE = os.path.join(os.path.dirname(__file__), "settings.json")


def load_settings():
    """Load settings from JSON or create defaults if missing."""
    defaults = {
        "work_minutes": 25,
        "short_break_minutes": 5,
        "long_break_minutes": 15,
        "cycles_before_long_break": 4,
    }
    if not os.path.exists(SETTINGS_FILE):
        with open(SETTINGS_FILE, "w") as f:
            json.dump(defaults, f, indent=4)
        return defaults

    try:
        with open(SETTINGS_FILE, "r") as f:
            data = json.load(f)
        # If a key is missing, fill in from defaults
        for k, v in defaults.items():
            if k not in data:
                data[k] = v
        return data
    except (json.JSONDecodeError, IOError):
        # If file is corrupted, overwrite with defaults
        with open(SETTINGS_FILE, "w") as f:
            json.dump(defaults, f, indent=4)
        return defaults


def save_settings(settings_dict):
    """Persist current settings to JSON."""
    with open(SETTINGS_FILE, "w") as f:
        json.dump(settings_dict, f, indent=4)


class SettingsDialog(QDialog):
    """A simple dialog to adjust Pomodoro durations."""

    def __init__(self, parent=None, current_settings=None):
        super().__init__(parent)
        self.setWindowTitle("Pomodoro Settings")
        self.resize(300, 200)
        self.settings = current_settings or {}

        layout = QFormLayout(self)

        # Spin boxes for each setting
        self.work_spin = QSpinBox()
        self.work_spin.setRange(1, 180)
        self.work_spin.setValue(self.settings.get("work_minutes", 25))
        layout.addRow("Work (minutes):", self.work_spin)

        self.short_break_spin = QSpinBox()
        self.short_break_spin.setRange(1, 60)
        self.short_break_spin.setValue(self.settings.get("short_break_minutes", 5))
        layout.addRow("Short Break (minutes):", self.short_break_spin)

        self.long_break_spin = QSpinBox()
        self.long_break_spin.setRange(1, 120)
        self.long_break_spin.setValue(self.settings.get("long_break_minutes", 15))
        layout.addRow("Long Break (minutes):", self.long_break_spin)

        self.cycles_spin = QSpinBox()
        self.cycles_spin.setRange(1, 10)
        self.cycles_spin.setValue(self.settings.get("cycles_before_long_break", 4))
        layout.addRow("Cycles before Long Break:", self.cycles_spin)

        # OK / Cancel buttons
        buttons = QDialogButtonBox(QDialogButtonBox.Ok | QDialogButtonBox.Cancel)
        buttons.accepted.connect(self.accept)
        buttons.rejected.connect(self.reject)
        layout.addWidget(buttons)

    def get_values(self):
        """Return a dict of the updated values."""
        return {
            "work_minutes": self.work_spin.value(),
            "short_break_minutes": self.short_break_spin.value(),
            "long_break_minutes": self.long_break_spin.value(),
            "cycles_before_long_break": self.cycles_spin.value(),
        }


class PomodoroTimer(QWidget):
    """A Pomodoro timer widget that can be embedded inside a larger dashboard."""

    def __init__(self, parent=None):
        super().__init__(parent)

        # Load (or create) settings
        self.settings = load_settings()

        # State variables
        self.current_mode = "Work"  # or "Short Break" or "Long Break"
        self.cycles_completed = 0
        self.remaining_seconds = self.settings["work_minutes"] * 60

        # Timer
        self.timer = QTimer(self)
        self.timer.setInterval(1000)  # 1 second
        self.timer.timeout.connect(self.tick)

        # Build UI
        self.init_ui()

    def init_ui(self):
        main_layout = QVBoxLayout(self)

        # Label for Mode (Work / Break)
        self.mode_label = QLabel(self.current_mode, self)
        self.mode_label.setAlignment(Qt.AlignCenter)
        self.mode_label.setStyleSheet("font-size: 24px; font-weight: bold;")
        main_layout.addWidget(self.mode_label)

        # Label for Countdown
        self.time_label = QLabel(self.format_time(self.remaining_seconds), self)
        self.time_label.setAlignment(Qt.AlignCenter)
        self.time_label.setStyleSheet("font-size: 36px;")
        main_layout.addWidget(self.time_label)

        # Start / Pause / Reset buttons
        button_layout = QHBoxLayout()

        self.start_button = QPushButton("Start", self)
        self.start_button.clicked.connect(self.start_timer)
        button_layout.addWidget(self.start_button)

        self.pause_button = QPushButton("Pause", self)
        self.pause_button.clicked.connect(self.pause_timer)
        self.pause_button.setEnabled(False)
        button_layout.addWidget(self.pause_button)

        self.reset_button = QPushButton("Reset", self)
        self.reset_button.clicked.connect(self.reset_timer)
        self.reset_button.setEnabled(False)
        button_layout.addWidget(self.reset_button)

        main_layout.addLayout(button_layout)

        # Settings button
        self.settings_button = QPushButton("⚙️ Settings", self)
        self.settings_button.clicked.connect(self.open_settings)
        main_layout.addWidget(self.settings_button)

        # Stretch to push everything toward the top
        main_layout.addStretch()

        # Set layout
        self.setLayout(main_layout)

    def format_time(self, total_seconds):
        minutes = total_seconds // 60
        seconds = total_seconds % 60
        return f"{minutes:02d}:{seconds:02d}"

    def start_timer(self):
        self.timer.start()
        self.start_button.setEnabled(False)
        self.pause_button.setEnabled(True)
        self.reset_button.setEnabled(True)
        self.settings_button.setEnabled(False)  # Prevent changing settings mid‐cycle

    def pause_timer(self):
        self.timer.stop()
        self.start_button.setEnabled(True)
        self.pause_button.setEnabled(False)
        self.settings_button.setEnabled(True)

    def reset_timer(self):
        self.timer.stop()
        # Reset remaining_seconds depending on mode
        if self.current_mode == "Work":
            self.remaining_seconds = self.settings["work_minutes"] * 60
        elif self.current_mode == "Short Break":
            self.remaining_seconds = self.settings["short_break_minutes"] * 60
        else:  # Long Break
            self.remaining_seconds = self.settings["long_break_minutes"] * 60

        self.time_label.setText(self.format_time(self.remaining_seconds))
        self.start_button.setEnabled(True)
        self.pause_button.setEnabled(False)
        self.reset_button.setEnabled(False)
        self.settings_button.setEnabled(True)

    def tick(self):
        """Called every second by the QTimer."""
        if self.remaining_seconds > 0:
            self.remaining_seconds -= 1
            self.time_label.setText(self.format_time(self.remaining_seconds))
        else:
            # Time’s up for this period
            self.timer.stop()
            self.alert_cycle_complete()

    def alert_cycle_complete(self):
        """Popup when a session ends and switch modes."""
        # Increment cycle if it was a work period
        if self.current_mode == "Work":
            self.cycles_completed += 1

        # Choose next mode
        if self.current_mode == "Work":
            # If we’ve hit the long‐break threshold
            if self.cycles_completed % self.settings["cycles_before_long_break"] == 0:
                self.current_mode = "Long Break"
                self.remaining_seconds = self.settings["long_break_minutes"] * 60
            else:
                self.current_mode = "Short Break"
                self.remaining_seconds = self.settings["short_break_minutes"] * 60
        else:
            self.current_mode = "Work"
            self.remaining_seconds = self.settings["work_minutes"] * 60

        # Update UI
        self.mode_label.setText(self.current_mode)
        self.time_label.setText(self.format_time(self.remaining_seconds))
        self.start_button.setEnabled(True)
        self.pause_button.setEnabled(False)
        self.reset_button.setEnabled(False)
        self.settings_button.setEnabled(True)

        # Simple popup alert
        QMessageBox.information(
            self,
            "Pomodoro Complete",
            f"{self.current_mode} session is ready to start!",
        )

    def open_settings(self):
        """Open dialog to change durations, then save if accepted."""
        dlg = SettingsDialog(self, current_settings=self.settings)
        if dlg.exec_() == QDialog.Accepted:
            new_vals = dlg.get_values()
            # Update settings
            self.settings.update(new_vals)
            save_settings(self.settings)

            # If timer is idle, reset display to reflect new settings
            if not self.timer.isActive():
                self.current_mode = "Work"
                self.cycles_completed = 0
                self.remaining_seconds = self.settings["work_minutes"] * 60
                self.mode_label.setText(self.current_mode)
                self.time_label.setText(self.format_time(self.remaining_seconds))
