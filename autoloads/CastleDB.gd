extends Node

var db: Variant

var _json:JSON

func get_entries(sheet_name: String) -> Array:
  for sheet in db.sheets:
    if sheet_name == sheet.name:
      return sheet.lines

  return []

func get_entry(sheet_name: String, type: String) -> Dictionary:
  for sheet in db.sheets:
    if sheet_name == sheet.name:
      for entry in sheet.lines:
        if entry.type == type:
          return entry

  return {}

func _load_db() -> Variant:
  _json = JSON.new()
  
  var file = File.new()
  file.open("res://data/base.json", File.READ)

  var json:String = file.get_as_text()
  file.close()

  _json.parse(json)
  return _json.get_data()

func _init():
  db = _load_db()
