extends Node

signal ui_click

func game_set_start_time(time)->Dictionary:
  return {
    "type": action_types.GAME_SET_START_TIME,
    "time": time
  }

func player_damage(amount)->Dictionary:
  emit_signal("player_damaged")
  return {
    "type": action_types.PLAYER_DAMAGE,
    "damage": amount
  }
  
func player_set_food(food)->Dictionary:
  return {
    "type": action_types.PLAYER_SET_FOOD,
    "food": food
  }
  
func player_set_gold(gold)->Dictionary:
  return {
    "type": action_types.PLAYER_SET_GOLD,
    "gold": gold
  }
  
func player_set_health(health)->Dictionary:
  return {
    "type": action_types.PLAYER_SET_HEALTH,
    "health": health
  }
  
func player_set_state(state)->Dictionary:
  return {
    "type": action_types.PLAYER_SET_STATE,
    "state": state
  }
