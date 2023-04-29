extends Control

@onready var _about_button: Button = %About
@onready var _play_button: Button = %Play

func _on_about_button_pressed() -> void:
  ViewController.set_client_view(ViewController.CLIENT_VIEWS.ABOUT)

func _on_play_button_pressed() -> void:
  Store.start_game()

func _ready():
  _about_button.connect("pressed", self._on_about_button_pressed)
  _play_button.connect("pressed", self._on_play_button_pressed)

  ViewController.register_view(ViewController.CLIENT_VIEWS.MAIN_MENU, self)
