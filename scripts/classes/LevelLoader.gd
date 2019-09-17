extends Node
class_name LevelLoader

onready var current_wave_index = 0
onready var parent = $"../"

onready var level_json = _load_level()

# [{
#   actors: [{
#     amount: int,
#     timer: int,
#     type: string
#   }],
# }]
onready var waves = level_json['waves']

var current_wave = {}
var wave_timers = {}

func _ready():
  _initialize_wave()

func _process(delta):
  if current_wave_index < waves.size():
    for i in range(0, current_wave['actors'].size()):
      var current_actor = current_wave['actors'][i]

      if current_actor['amount'] > 0:
        wave_timers[i] -= delta

        if wave_timers[i] <= 0:
          current_actor['amount'] -= 1
          wave_timers[i] = current_actor['timer']

    if _is_actors_exhausted():
      current_wave_index += 1
      _initialize_wave()

func _is_actors_exhausted():
  var exhausted = true

  # Are all the enemies dead?

  # return exhausted
  return false

func _initialize_wave():
  print("Initializing wave: " + str(current_wave_index))
  current_wave = waves[current_wave_index]

  for i in range(0, current_wave['actors'].size()):
    wave_timers[i] = current_wave['actors'][i]['timer']

func _load_level():
  var file = File.new()
  file.open("res://data/" + parent.name + ".json", File.READ)

  var json = file.get_as_text()
  file.close()

  var parsed_json = JSON.parse(json)

  return parsed_json.result
