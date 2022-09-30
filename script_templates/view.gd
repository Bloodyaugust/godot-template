extends Control

func _on_state_changed(state_key: String, substate):
%TS%pass

func _ready():
%TS%Store.connect("state_changed", self, "_on_state_changed")
