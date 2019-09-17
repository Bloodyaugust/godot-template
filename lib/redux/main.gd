extends Node

onready var actions = get_node('/root/actions')
onready var reducers = get_node('/root/reducers')
onready var store = get_node('/root/store')

func _ready():
  store.create([
    {'name': 'game', 'instance': reducers},
    {'name': 'player', 'instance': reducers},
    {'name': 'tiles', 'instance': reducers}
  ], [
    {'name': '_on_store_changed', 'instance': self}
  ])
  store.dispatch(actions.game_set_start_time(OS.get_unix_time()))
  store.dispatch(actions.player_set_food(100))
  store.dispatch(actions.player_set_gold(100))
  store.dispatch(actions.player_set_health(100))

func _on_store_changed(name, state):
  print(name, ": ", state)
