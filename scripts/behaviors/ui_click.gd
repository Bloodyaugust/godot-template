extends Button

func _on_button_pressed()->void:
  actions.emit_signal("ui_click")

func _ready()->void:
  connect("pressed", self, "_on_button_pressed")
