extends Control

@onready var _play_button: Button = %Play

func _on_play_button_pressed() -> void:
  Store.start_game()

func _ready():
  _play_button.connect("pressed", self._on_play_button_pressed)

  ViewController.register_view(ViewController.CLIENT_VIEWS.MAIN_MENU, self)
