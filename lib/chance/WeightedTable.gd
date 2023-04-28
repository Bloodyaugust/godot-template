extends Object

var table: Array
var total_weight: float

func initialize_table(objects: Array) -> void:
  table = []
  total_weight = 0

  for object in objects:
    table.append({
      "type": object.type,
      "end_weight": total_weight + object.weight,
      "weight": object.weight
    })

    total_weight += object.weight

  for item in table:
    item.chance = item.weight / total_weight
    print(item.chance)

func pick() -> Dictionary:
  var _pick_weight: float = randf_range(0, total_weight)
  var _picked_item: Dictionary = {
    "type": null
  }

  for item in table:
    if _pick_weight <= item.end_weight:
      _picked_item = item
      break

  assert(_picked_item.type != null)
  return _picked_item
