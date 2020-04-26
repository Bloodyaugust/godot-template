extends Node

func _ready()->void:
  store.create([
    {"name": "game", "instance": reducers},
    {"name": "player", "instance": reducers}
  ], [
    {"name": "_on_store_changed", "instance": self}
  ])
  store.dispatch(actions.game_set_start_time(OS.get_unix_time()))
  store.dispatch(actions.player_set_food(100))
  store.dispatch(actions.player_set_gold(100))
  store.dispatch(actions.player_set_health(100))

func _on_store_changed(name, state)->void:
  print(name, ": ", state)
