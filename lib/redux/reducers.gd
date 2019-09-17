extends Node

onready var types = get_node('/root/action_types')
onready var store = get_node('/root/store')

func game(state, action):
  if action['type'] == action_types.GAME_SET_START_TIME:
    var next_state = store.shallow_copy(state)
    next_state['start_time'] = action['time']
    return next_state
  return state

func player(state, action):
  if action['type'] == action_types.PLAYER_DAMAGE:
    var next_state = store.shallow_copy(state)
    next_state['health'] = next_state['health'] - action['damage']
    return next_state
  if action['type'] == action_types.PLAYER_SET_FOOD:
    var next_state = store.shallow_copy(state)
    next_state['food'] = action['food']
    return next_state
  if action['type'] == action_types.PLAYER_SET_GOLD:
    var next_state = store.shallow_copy(state)
    next_state['gold'] = action['gold']
    return next_state
  if action['type'] == action_types.PLAYER_SET_HEALTH:
    var next_state = store.shallow_copy(state)
    next_state['health'] = action['health']
    return next_state
  if action['type'] == action_types.PLAYER_SET_TILE:
    var next_state = store.shallow_copy(state)
    next_state['tile'] = action['tile']
    return next_state
  if action['type'] == action_types.PLAYER_SET_STATE:
    var next_state = store.shallow_copy(state)
    next_state['state'] = action['state']
    return next_state
  return state

func tiles(state, action):
  if action['type'] == action_types.TILES_SET_CHILD:
    var next_state = store.shallow_copy(state)
    next_state[action['tile']] = action['child']
    return next_state
  return state