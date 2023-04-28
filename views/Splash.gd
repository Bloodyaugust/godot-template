extends Control

var _skipped: bool = false

func _ready():
  ViewController.register_view(ViewController.CLIENT_VIEWS.SPLASH, self)

  await get_tree().create_timer(2).timeout

  if !_skipped:
    ViewController.set_client_view(ViewController.CLIENT_VIEWS.MAIN_MENU)

func _unhandled_input(event):
  if event is InputEventKey && !event.pressed && event.keycode == KEY_ESCAPE:
    _skipped = true
    ViewController.set_client_view(ViewController.CLIENT_VIEWS.MAIN_MENU)
