extends Control

@onready var _main_menu_button: Button = %MainMenu

func _on_main_menu_button_pressed() -> void:
  ViewController.set_client_view(ViewController.CLIENT_VIEWS.MAIN_MENU)

func _ready():
  _main_menu_button.connect("pressed", self._on_main_menu_button_pressed)

  ViewController.register_view(ViewController.CLIENT_VIEWS.ABOUT, self)
