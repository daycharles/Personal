import sys
import types
import importlib
import json
import pytest


def import_dashboard(monkeypatch):
    """Import main_dashboard with PyQt5 and pomodoro_widget stubbed."""
    qt = types.ModuleType("PyQt5")
    core = types.ModuleType("PyQt5.QtCore")
    widgets = types.ModuleType("PyQt5.QtWidgets")
    gui = types.ModuleType("PyQt5.QtGui")

    class DummyQt:
        def __getattr__(self, name):
            return 0

    core.Qt = DummyQt()

    widget_names = [
        "QApplication",
        "QMainWindow",
        "QAction",
        "QWidget",
        "QVBoxLayout",
        "QTabWidget",
        "QListWidget",
        "QPushButton",
        "QHBoxLayout",
        "QLineEdit",
        "QListWidgetItem",
        "QMessageBox",
        "QTextEdit",
        "QLabel",
        "QComboBox",
        "QGridLayout",
        "QCalendarWidget",
        "QSystemTrayIcon",
        "QMenu",
        "QStyle",
    ]
    for name in widget_names:
        setattr(widgets, name, type(name, (), {}))

    for name in ["QIcon", "QFont"]:
        setattr(gui, name, type(name, (), {}))

    monkeypatch.setitem(sys.modules, "PyQt5", qt)
    monkeypatch.setitem(sys.modules, "PyQt5.QtCore", core)
    monkeypatch.setitem(sys.modules, "PyQt5.QtWidgets", widgets)
    monkeypatch.setitem(sys.modules, "PyQt5.QtGui", gui)

    # Stub pomodoro_widget
    pomo = types.ModuleType("pomodoro_widget")
    pomo.PomodoroTimer = type("PomodoroTimer", (), {})
    monkeypatch.setitem(sys.modules, "pomodoro_widget", pomo)

    # Load the module directly from the source file so we don't need a package
    from pathlib import Path
    path = Path(__file__).resolve().parents[1] / "DailyDashboard" / "main_dashboard.py"
    spec = importlib.util.spec_from_file_location("main_dashboard", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


@pytest.fixture
def dashboard(tmp_path, monkeypatch):
    module = import_dashboard(monkeypatch)
    monkeypatch.setattr(module, "TASKS_FILE", str(tmp_path / "tasks.json"), raising=False)
    monkeypatch.setattr(module, "NOTES_FILE", str(tmp_path / "notes.txt"), raising=False)
    monkeypatch.setattr(module, "EISENHOWER_FILE", str(tmp_path / "eisen.json"), raising=False)
    return module


def test_tasks_roundtrip_and_creation(dashboard, tmp_path):
    tasks_file = tmp_path / "tasks.json"
    assert not tasks_file.exists()
    assert dashboard.load_tasks() == []
    assert tasks_file.exists()
    assert json.load(open(tasks_file)) == []

    data = [{"text": "a", "done": False}]
    dashboard.save_tasks(data)
    assert json.load(open(tasks_file)) == data
    assert dashboard.load_tasks() == data


def test_load_tasks_filters_invalid(dashboard, tmp_path):
    tasks_file = tmp_path / "tasks.json"
    with open(tasks_file, "w") as f:
        json.dump([1, {"text": "x"}, {"text": "y", "done": True}], f)
    assert dashboard.load_tasks() == [{"text": "y", "done": True}]


def test_notes_roundtrip(dashboard, tmp_path):
    notes_file = tmp_path / "notes.txt"
    assert not notes_file.exists()
    assert dashboard.load_notes() == ""
    dashboard.save_notes("hello")
    assert notes_file.read_text() == "hello"
    assert dashboard.load_notes() == "hello"


def test_eisenhower_roundtrip_and_creation(dashboard, tmp_path):
    e_file = tmp_path / "eisen.json"
    assert dashboard.load_eisenhower() == []
    assert e_file.exists()
    assert json.load(open(e_file)) == []

    data = [{"text": "task", "quadrant": 2}]
    dashboard.save_eisenhower(data)
    assert json.load(open(e_file)) == data
    assert dashboard.load_eisenhower() == data


def test_load_eisenhower_filters_invalid(dashboard, tmp_path):
    e_file = tmp_path / "eisen.json"
    with open(e_file, "w") as f:
        json.dump([
            {"text": "a"},
            {"text": "b", "quadrant": 5},
            {"text": "c", "quadrant": 1},
        ], f)
    assert dashboard.load_eisenhower() == [{"text": "c", "quadrant": 1}]
