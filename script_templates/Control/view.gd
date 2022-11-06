extends Control

func _on_state_changed(state_key: String, substate):
_TS_pass

func _ready():
_TS_Store.state_changed.connect(self._on_state_changed)
