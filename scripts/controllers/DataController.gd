extends Node

var data := {}

func _load_actor_data(id: String, type: String):
  var file = File.new()
  file.open("res://data/{id}.{type}.json".format({"id": id, "type": type}), File.READ)

  var json = file.get_as_text()
  file.close()

  var parsed_json = JSON.parse(json)

  return parsed_json.result

func _ready():
  var _data_directory:Directory = Directory.new()
  var _actor_type_regex:RegEx = RegEx.new()

  _actor_type_regex.compile("\\.(.*)\\.")

  if _data_directory.open("res://data") == OK:
    _data_directory.list_dir_begin(true)

    var _file_name: String = _data_directory.get_next()
    while _file_name != "":
      var _actor_type: String = _actor_type_regex.search(_file_name).get_string(1)
      var _actor_id: String = _file_name.substr(0, _file_name.find(".{actor_type}.json".format({"actor_type": _actor_type})))

      if !data.has(_actor_type):
        data[_actor_type] = {}

      data[_actor_type][_actor_id] = _load_actor_data(_actor_id, _actor_type)

      _file_name = _data_directory.get_next()
