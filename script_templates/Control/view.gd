extends Control

func _on_state_changed(_state_key: String, _substate):
  pass

func _ready():
  Store.state_changed.connect(self._on_state_changed)
