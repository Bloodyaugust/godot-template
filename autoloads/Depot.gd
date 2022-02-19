extends Node

var db:Dictionary
var json:JSON
var sheets:Dictionary
var guid_hash:Dictionary

func get_line(sheet_name:String, id:String) -> Dictionary:
  var _lines = get_lines(sheet_name)

  for _line in _lines:
    if _line.id == id:
      return _line

  return {}

func get_lines(sheet_name:String) -> Array:
  return get_sheet(sheet_name).lines

func get_sheet(name:String) -> Dictionary:
  for _key in sheets.keys():
    if sheets[_key].name == name:
      return sheets[_key]
  
  return {}

func _load_db() -> Dictionary:
  var _file = File.new()
  _file.open("res://data/depot.dpo", File.READ)

  var _json_text = _file.get_as_text()
  _file.close()

  json.parse(_json_text)
  return json.get_data()

func _init():
  json = JSON.new()
  db = _load_db()

  for _sheet in db.sheets:
    sheets[_sheet.guid] = _sheet
    guid_hash[_sheet.guid] = _sheet

    for _line in _sheet.lines:
      guid_hash[_line.guid] = _line

  for _sheet in db.sheets:
    var _line_references:Array = []
    for _column in _sheet.columns:
      if _column.typeStr == "lineReference":
        _line_references.append(_column.name)

    for _line in _sheet.lines:
      for _line_reference in _line_references:
        _line[_line_reference] = guid_hash[_line[_line_reference]]

    # Parse sheets that define a `parentSheetGUID` which are list definitions to replace references with full object
    if _sheet.has("parentSheetGUID"):
      var _entry_replace_key = _sheet.columns[0].name
      var _parent_sheet = sheets[_sheet.parentSheetGUID]
      var _parent_sheet_column_name

      for _parent_sheet_column in _parent_sheet.columns:
        if _parent_sheet_column.guid == _sheet.columnGUID:
          _parent_sheet_column_name = _parent_sheet_column.name
          break

      if _parent_sheet_column_name:
        for _line in _parent_sheet.lines:
          for _entry in _line[_parent_sheet_column_name]:
            _entry[_entry_replace_key] = guid_hash[_entry[_entry_replace_key]]
